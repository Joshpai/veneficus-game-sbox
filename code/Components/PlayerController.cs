using Sandbox.Citizen;

public sealed class PlayerController : Component
{
	[Property]
	public GameObject Body { get; set; }

	[Property]
	public GameObject Camera { get; set; }

	[Property]
	public CharacterController Controller { get; set; }

	[Property]
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
	public float JumpStrength { get; set; } = 273.0f;

	private Vector3 _cameraFollowDirectionNormalised;

	public Vector3 CameraFollowPosition => _cameraFollowDirectionNormalised *
										   CameraFollowDistance;

	public Angles EyeAngles = new Angles();

	private Transform _cameraReference;

	private CitizenAnimationHelper _animationHelper;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (!Gizmo.IsSelected)
			return;

		Gizmo.Draw.LineSphere(EyePosition, 10.0f);
		// NOTE: this function is called outside of a context with `OnStart`,
		// so we need to do this calculation ourselves, which is fine because
		// we don't care about being super fast here.
		Gizmo.Draw.LineSphere(
			EyePosition + CameraFollowDirection.Normal * CameraFollowDistance,
			10.0f
		);
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

		// Update the transform of the camera to orbit around the EyePosition
		var cameraTransform = _cameraReference.RotateAround(EyePosition,
															EyeAngles);
		var worldPos = Transform.Local.PointToWorld(cameraTransform.Position);
		Camera.Transform.Position = worldPos;
		Camera.Transform.LocalRotation = cameraTransform.Rotation;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (Controller.IsOnGround)
		{
			var speed = WalkSpeed;
			var velocity = Input.AnalogMove.Normal * speed * Body.Transform.Rotation;
			Controller.Accelerate(velocity);

			Controller.ApplyFriction(5.0f, 20.0f);

			if (Input.Pressed("Jump"))
			{
				Controller.Punch(Vector3.Up * JumpStrength);
			}
		}
		else
		{
			Controller.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;
		}

		Controller.Move();

		_animationHelper.IsGrounded = Controller.IsOnGround;
		_animationHelper.WithVelocity(Controller.Velocity);
	}

	protected override void OnStart()
	{
		base.OnStart();

		if (Body == null || Camera == null || Controller == null)
			throw new ArgumentException("PlayerController must have all of " +
										"Body, Camera, Controller set to " +
										"some value!");

		_animationHelper = Body.Components.Get<CitizenAnimationHelper>();
		if (_animationHelper == null)
			throw new ArgumentException("Body must have a CitizenAnimationHelper");

		// NOTE: we must set this before using CameraFollowPosition! A side
		// effect of caching this is that we can't edit this value live. Maybe
		// worth only including this "optimisation" iff we're in Release?
		_cameraFollowDirectionNormalised = CameraFollowDirection.Normal;
		_cameraReference = new Transform(EyePosition + CameraFollowPosition,
										 Transform.Rotation);
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
	}
}
