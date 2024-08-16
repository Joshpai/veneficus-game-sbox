using Sandbox.Citizen;

public sealed class PlayerMovementController : Component
{
	[Property]
	public GameObject Body { get; set; }

	[Property]
	public GameObject Camera { get; set; }

	[Property]
	public CharacterController Controller { get; set; }

	[Property]
	public Vector3 HumanEyePosition { get; set; }

	[Property]
	public Vector3 PolymorphedEyePosition { get; set; }

	[Property]
	public float CameraHeightInterpolationRate { get; set; } = 8.0f;

	public Vector3 EyePosition { get; set; }

	// NOTE: Not necessarily normalised!
	[Property]
	public Vector3 CameraFollowDirection { get; set; }

	[Property]
	public float CameraFollowDistance { get; set; }

	[Property]
	public float CameraPitchClamp { get; set; } = 85.0f;

	[Property]
	public float WalkSpeed { get; set; } = 250.0f;

	[Property]
	public float AirSpeed { get; set; } = 30.0f;

	// In a full jump, how long should the player be in the air for? In reality
	// this will actually be a little shorter than set here as we make some
	// inaccurate assumptions in calculations - but it's approximately right.
	[Property]
	public float JumpDuration { get; set; } = 0.8f;

	// How long do we hold jump to get a max jump?
	[Property]
	public float MaxJumpHoldLength { get; set; } = 0.3f;

	// What percentage of the total jump force is exerted instantaneously?
	[Property]
	public float InitialJumpAmount { get; set; } = 0.5f;

	// How long after falling off something can the player still jump?
	[Property]
	public float JumpCoyoteTime { get; set; } = 0.15f;

	[Property]
	public float HumanMass { get; set; } = 80.0f;

	[Property]
	public float PolymorphedMass { get; set; } = 10.0f;

	[Property]
	public float HumanHeight { get; set; } = 64.0f;

	[Property]
	public float PolymorphedHeight { get; set; } = 22.0f;

	[Property]
	public float HumanRadius { get; set; } = 16.0f;

	[Property]
	public float PolymorphedRadius { get; set; } = 12.0f;

	[Property]
	public float HumanStepHeight { get; set; } = 18.0f;

	[Property]
	public float PolymorphedStepHeight { get; set; } = 5.0f;

	// Too small a thing to fit elsewhere. Maximum range of an item the player
	// can interact with.
	[Property]
	public float InteractRange { get; set; } = 64.0f;

	public float Mass;

	private int _airJumpRemainingTicks;
	private Vector3 _airJumpForce;
	private bool _canAirJump;
	private bool _didJump;
	private float _airStartTime;

	private Vector3 _cameraFollowDirectionNormalised;

	public Vector3 CameraFollowPosition => _cameraFollowDirectionNormalised *
										   CameraFollowDistance;

	public Angles EyeAngles = new Angles();

	private Transform _cameraReference;
	private Transform _cameraReferenceInterpolated;
	private Transform _cameraReferenceHuman;
	private Transform _cameraReferencePolymorphed;

	private CitizenAnimationHelper _animationHelper;

	public bool IsPolymorphed;

	public bool LevelStarted { get; private set; }= true;
	private ModelRenderer _modelRenderer;
	private Model _oldModel;

	protected override void OnStart()
	{
		base.OnStart();

		if (Body == null || Camera == null || Controller == null)
			throw new ArgumentException("PlayerMovementController must have " +
										"all of Body, Camera, Controller " +
										"set to some value!");

		_animationHelper = Body.Components.Get<CitizenAnimationHelper>();
		if (_animationHelper == null)
			throw new ArgumentException("Body must have a CitizenAnimationHelper");

		// NOTE: we must set this before using CameraFollowPosition! A side
		// effect of caching this is that we can't edit this value live. Maybe
		// worth only including this "optimisation" iff we're in Release?
		_cameraFollowDirectionNormalised = CameraFollowDirection.Normal;
		_cameraReferenceHuman = new Transform(
				HumanEyePosition + CameraFollowPosition,
				Transform.Rotation
		);
		_cameraReferencePolymorphed = new Transform(
				PolymorphedEyePosition + CameraFollowPosition,
				Transform.Rotation
		);

		IsPolymorphed = false;
		EyePosition = HumanEyePosition;
		_cameraReference = _cameraReferenceHuman;
		_cameraReferenceInterpolated = _cameraReference;
		Mass = HumanMass;
		Controller.Height = HumanHeight;
		Controller.Radius = HumanRadius;
		Controller.StepHeight = HumanStepHeight;
	}

	public void SetPlayerNotStarted()
	{
		_modelRenderer = Components.GetInDescendantsOrSelf<ModelRenderer>();
		if (_modelRenderer != null)
		{
			_oldModel = _modelRenderer.Model;
			// _modelRenderer.Model = Model.Builder.Create();
			// TODO: spawn a prefab portal here instead. This offset business
			// is temporary as the sphere's origin is inside of it.
			_modelRenderer.Model = Model.Sphere;
			_modelRenderer.Transform.Position += Vector3.Up * 32.0f;
		}
		Transform.Position += HumanEyePosition;
		EyePosition = Vector3.Zero;
		LevelStarted = false;
	}

	private void StartLevel()
	{
		if (_modelRenderer != null)
		{
			_modelRenderer.Model = _oldModel;
			_modelRenderer.Transform.Position -= Vector3.Up * 32.0f;
		}
		Transform.Position -= HumanEyePosition;
		EyePosition = HumanEyePosition;
		LevelStarted = true;
	}

	public void TogglePolymorph()
	{
		// We aren't in charge of changing the model here, just the behaviour
		// of the player movement itself.
		IsPolymorphed = !IsPolymorphed;

		EyePosition = IsPolymorphed ? PolymorphedEyePosition
									 : HumanEyePosition;
		_cameraReference = IsPolymorphed ? _cameraReferencePolymorphed
										  : _cameraReferenceHuman;

		Mass = IsPolymorphed ? PolymorphedMass : HumanMass;

		Controller.Height = IsPolymorphed ? PolymorphedHeight : HumanHeight;
		Controller.Radius = IsPolymorphed ? PolymorphedRadius : HumanRadius;
		Controller.StepHeight = IsPolymorphed ? PolymorphedStepHeight
											   : HumanStepHeight;
	}

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (!Gizmo.IsSelected)
			return;

		Gizmo.Draw.LineSphere(HumanEyePosition, 10.0f);
		Gizmo.Draw.LineSphere(PolymorphedEyePosition, 10.0f);
		// NOTE: this function is called outside of a context with `OnStart`,
		// so we need to do this calculation ourselves, which is fine because
		// we don't care about being super fast here.
		Gizmo.Draw.LineSphere(
			HumanEyePosition + CameraFollowDirection.Normal *
							   CameraFollowDistance,
			10.0f
		);

		Gizmo.Draw.LineThickness = 16.0f;
		var startPos = HumanEyePosition;
		var endPos = startPos - CameraFollowDirection.Normal * InteractRange;
		Gizmo.Draw.Line(startPos, endPos);
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// Update our eye angles with respect to camera movement
		EyeAngles += Input.AnalogLook;
		var clampedPitch = MathX.Clamp(EyeAngles.pitch,
									   -CameraPitchClamp,
									    CameraPitchClamp);
		EyeAngles = EyeAngles.WithPitch(clampedPitch);

		// Rotate the player body to align with the camera direction
		Body.Transform.Rotation = Rotation.FromYaw(EyeAngles.yaw);

		// If we have just polymorphed, then our camera height probably changed
		// so we want to move towards the new height.
		var currentPos = _cameraReferenceInterpolated.Position;
		var targetPos = _cameraReference.Position;
		if (currentPos.AlmostEqual(targetPos))
		{
			_cameraReferenceInterpolated = _cameraReference;
		}
		else
		{
			_cameraReferenceInterpolated.Position =
				Vector3.Lerp(currentPos, targetPos,
							 CameraHeightInterpolationRate  * Time.Delta);
		}

		// Update the transform of the camera to orbit around the EyePosition
		var cameraTransform =
			_cameraReferenceInterpolated.RotateAround(EyePosition, EyeAngles);
		Camera.Transform.Position =
			Transform.Local.PointToWorld(cameraTransform.Position);
		Camera.Transform.Rotation = cameraTransform.Rotation;
	}

	// Short dump on design justification for this feature:
	// I want to allow players to jump as a human, polymorph, and jump
	// higher as a result. This makes sense if the exact instant they
	// exert force to jump, they become a frog (F_net = F_jump - mg).
	// To make this easier to achieve, we steal 2D platformer tech and
	// exert the jump force over a few ticks, and only if the jump
	// button is held. We get the second part for free honestly, and it
	// might make platforming for speed faster and more skillful.
	private void GroundJump()
	{
		// This is derived from suvat: s = ut - 0.5at^2 => u = 0.5at (s=0)
		var vel = 0.5f * -Scene.PhysicsWorld.Gravity * JumpDuration;
		// F = ma = mv / t
		var jumpForce = (vel / Time.Delta) * Mass;
		var groundJumpForce = InitialJumpAmount * jumpForce;

		_airJumpRemainingTicks = (int)(MaxJumpHoldLength / Time.Delta);
		_airJumpForce = (1.0f - InitialJumpAmount) * jumpForce;
		_airJumpForce /= _airJumpRemainingTicks;

		if (!_didJump)
		{
			_didJump = true;
			// If we're coyote jumping, then be a bit nice and cancel out the
			// already applied gravity to give a bigger jump. I'm sure there
			// will be some exploit with this to increase max jump but it's ok.
			Controller.Velocity = Controller.Velocity.WithZ(0.0f);
		}

		_canAirJump = true;

		Controller.Punch(groundJumpForce * Time.Delta / Mass);
	}

	private void AirJump()
	{
		Controller.Velocity += _airJumpForce * Time.Delta / Mass;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (!LevelStarted)
		{
			if (Input.Pressed("use"))
			{
				StartLevel();
			}
			else
			{
				return;
			}
		}

		Vector3 direction = Input.AnalogMove.Normal * Body.Transform.Rotation;
		if (Controller.IsOnGround)
		{
			Controller.Accelerate(direction * WalkSpeed);

			Controller.ApplyFriction(5.0f, 20.0f);

			_airStartTime = Time.Now;
			_didJump = Input.Pressed("Jump");
			if (_didJump)
				GroundJump();
		}
		else
		{
			// Let the player have a tiny bit of air movement, otherwise trying
			// to jump up small ledges is awful.
			Controller.Accelerate(direction * AirSpeed);

			if (!_didJump && Input.Pressed("Jump") &&
				Time.Now - _airStartTime <= JumpCoyoteTime)
				GroundJump();

			_canAirJump &= Input.Down("Jump");
			if (_canAirJump && (_airJumpRemainingTicks--) > 0)
				AirJump();

			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		Controller.Move();

		_animationHelper.IsGrounded = Controller.IsOnGround;
		_animationHelper.WithVelocity(Controller.Velocity);

		// DO THIS AT THE END! We might load a new level.
		if (Input.Pressed("use"))
		{
			var startPos = Transform.Position + EyePosition;
			var endPos = startPos + EyeAngles.Forward * InteractRange;
			var trace = Scene.Trace.Ray(startPos, endPos)
								   .WithTag("interactable")
								   .Run();
			if (trace.Hit)
			{
				var interactable =
					trace.GameObject.Components
									.GetInChildrenOrSelf<InteractableComponent>();
				if (interactable != null)
				{
					interactable.Interact(GameObject);
				}
			}
		}
	}
}
