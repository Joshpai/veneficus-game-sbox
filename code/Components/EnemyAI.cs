public sealed class EnemyAI : Component
{
	[Property, Group("References")]
	public NavMeshAgent Agent { get; set; }

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

	private PlayerMovementController _player;

	private bool _passive;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (!Gizmo.IsSelected)
			return;

		var lookDirection = new Vector3(1, 0, 0);
		Gizmo.Draw.Color = new Color(1.0f, 1.0f, 1.0f, 0.2f);
		Gizmo.Draw.SolidCone(EyePosition + lookDirection * VisionRange,
							 -lookDirection * VisionRange,
							 VisionRange * MathF.Tan(VisionAngle));

		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.LineSphere(EyePosition, HearingRadius);
	}

	protected override void OnStart()
	{
		var players = Scene.GetAllComponents<PlayerMovementController>();
		foreach (var player in players)
		{
			if (!player.IsProxy)
			{
				_player = player;
				break;
			}
		}

		_passive = !AlwaysActive;
	}

	private void MoveTo(Vector3 destination)
	{
		Agent.MoveTo(destination);
	}

	private void OnFixedUpdatePassive()
	{
		Vector3 playerOffset = Transform.Position - _player.Transform.Position;
		float angleToPlayer =
			MathF.Acos(-playerOffset.Normal.Dot(Transform.Rotation.Forward));

		if (playerOffset.Length < VisionRange && angleToPlayer < VisionAngle)
		{
			_passive = false;
			// get an early headstart?
			OnFixedUpdateActive();
			return;
		}
	}

	private void OnFixedUpdateActive()
	{
		List<Vector3> x =
			Scene.NavMesh.GetSimplePath(
				Transform.Position,
				_player.Transform.Position
			);

		if (x.Count == 0)
			return;

		Vector3 terminal = x[x.Count - 1];
		// 10 is arbitrary
		bool reachable = terminal.DistanceSquared(_player.Transform.Position) < 10.0f;
		Log.Info(reachable);

		MoveTo(_player.Transform.Position);
		// TODO: this works (ish, angles are correct), but slows the agent to a
		// crawl. Probably need to roll our own movement in that case or maybe
		// change the rotation of a parent/child or something instead.
		// Transform.Rotation = (_player.Transform.Position - Transform.Position).EulerAngles;
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
	}
}
