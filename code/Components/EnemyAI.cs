public sealed class EnemyAI : Component
{
	[Property, Group("Basic Definitions")]
	public BaseSpell.SpellType EnemySpell { get; set; }

	[Property, Group("Basic Definitions")]
	public int EnemyLevel { get; set; } = 1;

	[Property, Group("Movement")]
	public float MaxSpeed { get; set; } = 250.0f;

	[Property, Group("Movement")]
	public float JumpPower { get; set; } = 273.0f;

	// This may be set for things like ranged enemies
	[Property, Group("Movement")]
	public bool WantHighground { get; set; } = false;

	[Property, Group("Movement")]
	public bool CanSpellJump { get; set; } = false;

	[Property, Group("Movement")]
	public bool AvoidSpells { get; set; } = true;

	public enum PassiveBehaviour
	{
		// Stand still in current location
		Stationary,
		// Move aimlessly in a small area
		Wander,
		// Move along a pre-defined route
		// TODO: how to define and set? Probably create an empty child to which
		// we can attach a "PatrolPathComponent" and use that to define it.
		Patrol
	}

	[Property, Group("Passive")]
	public PassiveBehaviour PassiveMode { get; set; }

	[Property, Group("Passive")]
	public List<Vector3> PatrolPath { get; set; }

	private List<Vector3> _patrolPathWorld;

	[Property, Group("Passive")]
	public bool AlwaysActive { get; set; } = false;

	[Property, Group("Passive")]
	public float HearingRadius { get; set; } = 125.0f;

	[Property, Group("Passive")]
	public float VisionRange { get; set; } = 300.0f;

	// If the route to the player is longer than this, give up, or try to pass
	// a message to other enemies with the last player location instead.
	[Property, Group("Passive")]
	public float MaxRouteLength { get; set; } = 3000.0f;

	[Property, Group("Passive")]
	public bool CanPassMessage { get; set; } = true;

	// After this, enemies won't continue the chain of message passing.
	[Property, Group("Passive")]
	public int MaxMessageChainLength { get; set; } = 5;

	// Angle, in radians, of vision cone
	[Property, Range(0.01f, MathF.PI, MathF.PI / 25.0f), Group("Passive")]
	public float VisionAngle { get; set; } = MathF.PI / 4.0f;

	[Property, Group("Basic Definitions")]
	public Vector3 EyePosition { get; set; } = new Vector3(0.0f, 0.0f, 64.0f);

	[Property, Group("Combat")]
	public float AttackRate { get; set; } = 1.0f;

	[Property, Group("Combat")]
	public float BurstCount { get; set; } = 1.0f;

	[Property, Group("Combat")]
	public float BurstRate { get; set; } = 0.15f;

	// If we haven't made much progress at getting to the player, e.g., they
	// are unreachable, they're too good at dodging, we lost track of them, etc
	// then give up.
	[Property, Group("Passive")]
	public float GiveUpAfter { get; set; } = 35.0f;

	public enum GiveUpBehaviour
	{
		ReturnToOrigin,
		WanderEndLocation,
		// Start going back to our origin, and if we find any other enemies to
		// help us then enlist their support and go back. Otherwise, cheat and
		// search for all enemies on the map and path to one.
		FindHelp
	}

	[Property, Group("Passive")]
	public GiveUpBehaviour GiveUpMode { get; set; }

	private NavMeshAgent _agent { get; set; }

	private PlayerMovementController _player;

	private bool _passive;

	private Vector3 _startingPosition;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (!Gizmo.IsSelected)
			return;

		var lookDirection = Transform.Rotation.Forward;
		Gizmo.Transform = global::Transform.Zero;
		Gizmo.Draw.Color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
		Gizmo.Draw.SolidCone(Transform.Position + EyePosition +
							 lookDirection * VisionRange,
							 -lookDirection * VisionRange,
							 VisionRange * MathF.Tan(VisionAngle));

		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.LineSphere(Transform.Position + EyePosition, HearingRadius);

		Gizmo.Draw.LineThickness = 16.0f;
		var offset = (_startingPosition == Vector3.Zero) ? Transform.Position
														 : _startingPosition;
		for (int i = 0; i < PatrolPath.Count - 1; i++)
		{
			if (i == 0)
				Gizmo.Draw.Color = Color.Green;
			else if (i == PatrolPath.Count - 2)
				Gizmo.Draw.Color = Color.Red;
			else
				Gizmo.Draw.Color = ((i & 0x1) == 0) ? Color.Orange : Color.Cyan;

			Gizmo.Draw.Line(offset + PatrolPath[i],
							offset + PatrolPath[i+1]);
		}
	}

	private void HandleEnemyDeath()
	{
		// TODO: if restarting a level, we should make sure that enemies aren't
		// counted twice! Perhaps this value gets reset to the last "SavePoint"
		LevelManagerStaticStore.Stats.EnemiesKilled++;
	}

	protected override void OnStart()
	{
		_agent = Components.GetInChildrenOrSelf<NavMeshAgent>();
		_startingPosition = Transform.Position;

		var players = Scene.GetAllComponents<PlayerMovementController>();
		foreach (var player in players)
		{
			if (!player.IsProxy)
			{
				_player = player;
				break;
			}
		}

		var health = Components.GetInDescendantsOrSelf<HealthComponent>();
		if (health != null)
			health.OnDeath += HandleEnemyDeath;

		_passive = !AlwaysActive;
		if (_passive)
			_agent.UpdateRotation = true;

		_patrolPathWorld = new List<Vector3>();
		foreach (var pos in PatrolPath)
			_patrolPathWorld.Add(pos + _startingPosition);
	}

	private void MoveTo(Vector3 destination)
	{
		Log.Info($"Moving to {destination}");
		_agent.MoveTo(destination);
	}

	private int _patrolNextIndex = -1;
	private int _patrolDirection = 1;
	private float _passiveNextWanderTime = 0.0f;

	private void OnFixedUpdatePassive()
	{
		if (PassiveMode == PassiveBehaviour.Patrol &&
			_patrolPathWorld.Count > 0)
		{
			// Magic uninited number
			if (_patrolNextIndex == -1)
			{
				_patrolNextIndex = 1;
				_patrolDirection = 1;
				MoveTo(_patrolPathWorld[_patrolNextIndex]);
			}

			if (Transform.Position.Distance(_patrolPathWorld[_patrolNextIndex]) < 10.0f)
			{
				_patrolNextIndex += _patrolDirection;

				if (_patrolNextIndex < 0 ||
					_patrolNextIndex >= _patrolPathWorld.Count)
				{
					_patrolDirection *= -1;
					_patrolNextIndex += 2 * _patrolDirection;
				}

				MoveTo(_patrolPathWorld[_patrolNextIndex]);
			}
		}
		else if (PassiveMode == PassiveBehaviour.Wander)
		{
			if (_passiveNextWanderTime < Time.Now)
			{
				var pos = Scene.NavMesh.GetRandomPoint(Transform.Position, 100.0f);
				if (pos != null)
					MoveTo(pos.Value);
				_passiveNextWanderTime = Time.Now + 5.0f;
			}

			if (_agent.TargetPosition != null &&
				Transform.Position.Distance(_agent.TargetPosition.Value) < 10.0f)
			{
				_agent.Stop();
			}
		}

		Vector3 playerOffset = Transform.Position - _player.Transform.Position;
		float angleToPlayer =
			MathF.Acos(-playerOffset.Normal.Dot(Transform.Rotation.Forward));

		if (playerOffset.Length < VisionRange && angleToPlayer < VisionAngle)
		{
			_passive = false;
			return;
		}
	}

	private bool PointIsReachableByPath(List<Vector3> path, Vector3 dest)
	{
		// TODO: this function doesn't work that well where the destination is
		// floating in the air. For example, if the player is jumping.

		if (path.Count == 0)
			return Transform.Position.DistanceSquared(dest) < 10.0f;

		Vector3 ultimateDest = path[path.Count - 1];

		// 2*Agent.Radius?
		if (ultimateDest.Distance(dest) > 50.0f)
			return false;

		Vector3 penultimateDest = path.Count > 2 ? path[path.Count - 2]
												 : Transform.Position;
		Vector3 finalStep = ultimateDest - penultimateDest;
		var slopeAngle = MathF.Atan(finalStep.z / finalStep.WithZ(0).Length);
		if (slopeAngle > Scene.NavMesh.AgentMaxSlope)
			return false;

		return true;
	}

	private TimeSince lastAttack = 0.0f;
	private List<BaseSpell> _castSpells = new List<BaseSpell>();
	private List<BaseSpell> _deferredSpellRemovals = new List<BaseSpell>();

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		_deferredSpellRemovals.Add((BaseSpell)spell);
	}

	private void OnUpdateSpells()
	{
		foreach (BaseSpell spell in _castSpells)
			spell.OnUpdate();
	}


	private void OnFixedUpdateSpells()
	{
		foreach (BaseSpell spell in _castSpells)
			spell.OnFixedUpdate();

		foreach (BaseSpell spell in _deferredSpellRemovals)
			_castSpells.Remove(spell);
	}

	private void AttackPlayer()
	{
		// TODO: bursts
		if (lastAttack < 3.0f)
			return;

		var spell =
			PlayerSpellcastingController.CreateSpell(GameObject, EnemySpell);
		spell.CasterEyeOrigin = EyePosition;
		// TODO: prediction?
		// TODO: this also shoots in a weird direction... tricky to debug
		spell.CastDirection =
			(_player.Transform.Position - Transform.Position).Normal;
		spell.StartCasting();
		spell.FinishCasting();
		_castSpells.Add(spell);
		lastAttack = 0.0f;
	}

	private void OnFixedUpdateActive()
	{
		List<Vector3> pathToPlayer =
			Scene.NavMesh.GetSimplePath(
				Transform.Position,
				_player.Transform.Position
			);

		bool pathEndsCloserToPlayer = true;
		bool pathTooLong = false;
		if (pathEndsCloserToPlayer && !pathTooLong)
		{
			MoveTo(_player.Transform.Position);
		}

		bool playerInAttackRange = true;
		bool canAttack = true;
		if (playerInAttackRange && canAttack)
		{
			AttackPlayer();
		}

		// bool reachable = PointIsReachableByPath(pathToPlayer, _player.Transform.Position);
		// if (!reachable)
		// 	return;
	}

	protected override void OnUpdate()
	{
		if (!_agent.UpdateRotation)
		{
			// TODO: look direction if target not dest
			// var lookAhead = _agent.GetLookAhead(30.0f);
			// Vector3 vector = lookAhead - Transform.Position;
			// vector.z = 0f;
			// if (vector.Length > 0.1f)
			// {
			// 	Rotation wantRotation = Rotation.LookAt(_player.Transform.Position);
			// 	_agent.SyncAgentPosition = false;
			// 	Transform.Rotation = wantRotation;
			// 	// Transform.Rotation = Rotation.Slerp(Transform.Rotation,
			// 	// 									wantRotation,
			// 	// 									Time.Delta * 3f);
			// 	_agent.SyncAgentPosition = true;
			// }
		}

		OnUpdateSpells();
	}

	protected override void OnFixedUpdate()
	{
		if (_passive)
		{
			OnFixedUpdatePassive();
		}
		else
		{
			OnFixedUpdateActive();
		}

		OnFixedUpdateSpells();
	}
}
