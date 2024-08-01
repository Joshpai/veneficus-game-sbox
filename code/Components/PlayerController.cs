using System;
using Sandbox;
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

	private Vector3 _cameraFollowDirectionNormalised;

	public Vector3 CameraFollowPosition => _cameraFollowDirectionNormalised *
										   CameraFollowDistance;

	private Angles _eyeAngles = new Angles();

	private Transform _cameraReference;

	private CitizenAnimationHelper _animationHelper;

	protected override void DrawGizmos()
	{
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
		// Update our eye angles with respect to camera movement
		_eyeAngles += Input.AnalogLook;
		var clampedPitch = MathX.Clamp(_eyeAngles.pitch,
									   -CameraPitchClamp,
									    CameraPitchClamp);
		_eyeAngles = _eyeAngles.WithPitch(clampedPitch);

		// Rotate the player body to align with the camera direction
		Body.Transform.Rotation = Rotation.FromYaw(_eyeAngles.yaw);

		// Update the transform of the camera to orbit around the EyePosition
		var cameraTransform = _cameraReference.RotateAround(EyePosition,
															_eyeAngles);
		var worldPos = Transform.Local.PointToWorld(cameraTransform.Position);
		Camera.Transform.Position = worldPos;
		Camera.Transform.LocalRotation = cameraTransform.Rotation;
	}

	protected override void OnFixedUpdate()
	{

	}

	protected override void OnStart()
	{
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
