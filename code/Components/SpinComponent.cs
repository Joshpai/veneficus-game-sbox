public sealed class SpinComponent : Component
{
	[Property]
	public float SpinPeriod { get; set; } = 1.0f;

	protected override void OnUpdate()
	{
		Transform.Rotation = Transform.Rotation.RotateAroundAxis(
			Vector3.Forward,
			360.0f * Time.Delta / SpinPeriod
		);
	}
}
