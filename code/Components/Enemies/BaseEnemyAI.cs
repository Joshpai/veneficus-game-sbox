public class BaseEnemyAI : Component
{
	[Property, Group("Basic Definitions")]
	public int EnemyLevel { get; set; } = 1;

	[Property, Group("Movement")]
	public float MaxSpeed { get; set; } = 250.0f;

	[Property, Group("Movement")]
	public bool AvoidSpells { get; set; } = true;

	[Property, Group("Movement")]
	public float RotationSpeed { get; set; } = 3.0f;

	public enum PassiveBehaviour
	{
		// Stand still in current location
		Stationary,
		// Move aimlessly in a small area
		Wander,
		// Move along a pre-defined route
		Patrol
	}

	[Property, Group("Passive")]
	public PassiveBehaviour PassiveMode { get; set; }

	[Property, Group("Passive")]
	public List<Vector3> PatrolPath { get; set; } = new List<Vector3>();

	private List<Vector3> _patrolPathWorld;

	[Property, Group("Passive")]
	public bool AlwaysActive { get; set; } = false;

	[Property, Group("Passive")]
	public float HearingRadius { get; set; } = 125.0f;

	[Property, Group("Passive")]
	public float VisionRange { get; set; } = 300.0f;

	// Angle, in radians, of vision cone
	[Property, Range(0.01f, MathF.PI, MathF.PI / 25.0f), Group("Passive")]
	public float VisionAngle { get; set; } = MathF.PI / 4.0f;

	[Property, Group("Basic Definitions")]
	public Vector3 EyePosition { get; set; } = new Vector3(0.0f, 0.0f, 64.0f);

	// Try to be in this range while attacking
	[Property, Group("Combat")]
	public float AttackRangeIdeal { get; set; } = 100.0f;

	// Start attacking in this range
	[Property, Group("Combat")]
	public float AttackRangeMax { get; set; } = 500.0f;

	[Property, Group("Combat")]
	public float AttackCooldown { get; set; } = 1.0f;

	public NavMeshAgent Agent { get; set; }

	protected PlayerMovementController _player;

	protected bool _passive;

	protected Vector3 _startingPosition;

	protected EnemyManager _enemyManager { get; set; } = null;

	private int _patrolNextIndex = -1;
	private int _patrolDirection = 1;
	private float _passiveNextWanderTime = 0.0f;

	private float _nextAttackTime = 0.0f;

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
		LevelManagerStaticStore.Stats.EnemiesKilled++;
		LevelManagerStaticStore.UsedObjects.Add(GameObject.Id);
	}

	private void HandleEnemyHealthChanged(float amount)
	{
		// TODO: only go into aggro mode if the player hurt us?
		if (amount < 0)
			_passive = false;
	}

	protected override void OnStart()
	{
		Agent = Components.GetInChildrenOrSelf<NavMeshAgent>();
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
		if (_player == null)
			Log.Error("Unable to find PlayerMovementController!");

		// _enemyManager
		var enemyManagers = Scene.GetAllComponents<EnemyManager>();
		foreach (var enemyManager in enemyManagers)
		{
			_enemyManager = enemyManager;
			break;
		}
		if (_enemyManager == null)
			Log.Error("Unable to find EnemyManager!");

		var health = Components.GetInDescendantsOrSelf<HealthComponent>();
		if (health != null)
		{
			health.OnDeath += HandleEnemyDeath;
			health.OnHealthChanged += HandleEnemyHealthChanged;
		}

		_passive = !AlwaysActive;
		if (_passive)
			Agent.UpdateRotation = true;

		_patrolPathWorld = new List<Vector3>();
		foreach (var pos in PatrolPath)
			_patrolPathWorld.Add(pos + _startingPosition);
	}

	private void MoveTo(Vector3 destination)
	{
		// Log.Info($"Moving to {destination}");
		Agent.MoveTo(destination);
	}

	private void OnFixedUpdatePassive()
	{
		if (MaxSpeed != 0.0f)
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

				if (Agent.TargetPosition != null &&
						Transform.Position.Distance(Agent.TargetPosition.Value) < 10.0f)
				{
					Agent.Stop();
				}
			}
		}

		if (PlayerInVisionCone())
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

	protected void MoveToPlayer()
	{
		if (_player != null)
			MoveTo(_player.Transform.Position);
	}

	protected Vector3 GetDirectionToPlayerEyes()
	{
		var playerEyePos = _player.Transform.Position + _player.EyePosition;
		var enemyEyePos = Transform.Position + EyePosition;
		return playerEyePos - enemyEyePos;
	}

	protected bool PlayerInRange(float range)
	{
		if (_player == null)
			return false;

		Vector3 playerOffset = Transform.Position - _player.Transform.Position;
		return playerOffset.Length <= range;
	}

	protected bool PlayerObscured()
	{
		if (_player == null)
			return false;

		// TODO: this could be improved to check a few set positions over the
		// player, but this should be fine-ish.
		var playerEyePos = _player.Transform.Position + _player.EyePosition;
		var enemyEyePos = Transform.Position + EyePosition;
		var tr = Scene.Trace.Ray(enemyEyePos, playerEyePos)
							.WithoutTags("player")
							.Run();

		return tr.Hit;
	}

	protected bool PlayerInVisionCone()
	{
		if (_player == null)
			return false;

		Vector3 playerOffset = Transform.Position - _player.Transform.Position;
		float angleToPlayer =
			MathF.Acos(-playerOffset.Normal.Dot(Transform.Rotation.Forward));

		return playerOffset.Length < VisionRange &&
			   angleToPlayer < VisionAngle;
	}

	protected void LookInDirection(Vector3 direction)
	{
		if (direction.IsNearlyZero())
			return;

		// TODO: can we angle the head instead with the Z component?
		direction.z = 0.0f;
		Rotation wantRotation = Rotation.LookAt(direction);
		Agent.SyncAgentPosition = false;
		Transform.Rotation = Rotation.Slerp(Transform.Rotation,
											wantRotation,
											Time.Delta * RotationSpeed);
		Agent.SyncAgentPosition = true;
	}

	protected void TurnToFacePlayer()
	{
		if (_player == null)
			return;

		var playerEyePos = _player.Transform.Position + _player.EyePosition;
		var enemyEyePos = Transform.Position + EyePosition;

		LookInDirection(playerEyePos - enemyEyePos);
	}

	protected bool CanAttack()
	{
		return _nextAttackTime <= Time.Now;
	}

	protected void SetAttackCooldown()
	{
		_nextAttackTime = Time.Now + AttackCooldown;
	}

	protected override void OnUpdate()
	{
		Agent.UpdateRotation = _passive;
	}

	protected override void OnFixedUpdate()
	{
		if (_passive)
		{
			OnFixedUpdatePassive();
		}
		// Active behaviour should be handled by subclasses!
	}
}
