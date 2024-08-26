public sealed class SpinComponent : Component
{
	[Property]
	public Vector3 SpinAxis { get; set; } = Vector3.Forward;

	[Property]
	public float SpinPeriod { get; set; } = 1.0f;

	[Property]
	public float StopCameraFaceProbability { get; set; } = 0.5f;

	[Property]
	public GameObject Camera { get; set; }

	private bool _shouldRotate = true;
	private float _nextStopTime = 0.0f;

	protected override void OnUpdate()
	{
		if (!_shouldRotate)
			return;

		Transform.Rotation = Transform.Rotation.RotateAroundAxis(
			SpinAxis,
			360.0f * Time.Delta / SpinPeriod
		);

		if (Camera == null || !Camera.IsValid)
			return;

		if (_nextStopTime > Time.Now)
			return;

		float angleToCamera =
			MathF.Acos(
				-Camera.Transform.Rotation.Forward.Normal
					.Dot(Transform.Rotation.Forward)
			);
		if (angleToCamera < MathF.PI / SpinPeriod)
		{
			_nextStopTime = Time.Now + SpinPeriod / 2.0f;

			float rand = MathF.Abs(Vector3.Random.Normal.x);
			if (rand > StopCameraFaceProbability)
				return;

			_shouldRotate = false;
			Transform.Rotation =
				Camera.Transform.Rotation.RotateAroundAxis(Vector3.Up, 180.0f);
		}
	}
}
