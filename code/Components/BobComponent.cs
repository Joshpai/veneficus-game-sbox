public sealed class BobComponent : Component
{
	[Property]
	public float BobPeriod { get; set; } = 3.0f;

	[Property]
	public float BobAmplitude { get; set; } = 10.0f;

	private Vector3 _startPosition = Vector3.Zero;
	private float _bobTime = 0.0f;

	protected override void OnStart()
	{
		_startPosition = Transform.Position;
	}

	protected override void OnUpdate()
	{
		_bobTime += Time.Delta;

		Transform.Position =
			_startPosition + Vector3.Up * BobAmplitude *
			MathF.Sin(_bobTime / BobPeriod * MathF.PI);
	}
}
