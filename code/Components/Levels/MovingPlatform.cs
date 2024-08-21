public sealed class MovingPlatform : Component
{
	public enum MoveCharacteristics
	{
		Always,
		AfterPlayerTouch,
		WhilePlayerTouching
	}

	[Property]
	public MoveCharacteristics MoveType { get; set; } =
		MoveCharacteristics.AfterPlayerTouch;

	[Property]
	public List<Vector3> MovePathRelative { get; set; } = new List<Vector3>();

	[Property]
	public float MoveSpeed { get; set; } = 200.0f;

	public bool PlayerTouching { get; set; } = false;

	public Vector3 Velocity { get; private set; } = Vector3.Zero;

	private bool _playerTouched = false;

	private Vector3 _startingPosition;

	private List<Vector3> _movePathWorld = new List<Vector3>();
	private int _moveNextIndex = 0;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		Gizmo.Draw.LineThickness = 16.0f;
		for (int i = 0; i < MovePathRelative.Count - 1; i++)
		{
			if (i == 0)
				Gizmo.Draw.Color = Color.Green;
			else if (i == MovePathRelative.Count - 2)
				Gizmo.Draw.Color = Color.Red;
			else
				Gizmo.Draw.Color = ((i & 0x1) == 0) ? Color.Orange : Color.Cyan;

			Gizmo.Draw.Line(MovePathRelative[i],
							MovePathRelative[i+1]);
		}
	}

	protected override void OnStart()
	{
		_startingPosition = Transform.Position;
		_movePathWorld = new List<Vector3>();
		foreach (var pos in MovePathRelative)
			_movePathWorld.Add(pos + _startingPosition);
	}

	private bool CanMove()
	{
		return _moveNextIndex < _movePathWorld.Count;
	}

	private bool IsMoving()
	{
		return (MoveType == MoveCharacteristics.Always) ||
			   (MoveType == MoveCharacteristics.AfterPlayerTouch &&
				_playerTouched) ||
			   (MoveType == MoveCharacteristics.WhilePlayerTouching &&
				PlayerTouching);
	}

	protected override void OnFixedUpdate()
	{
		if (!_playerTouched && PlayerTouching)
			_playerTouched = true;

		if (CanMove() && IsMoving())
		{
			Vector3 destination = _movePathWorld[_moveNextIndex];
			Vector3 remainingOffset = destination - Transform.Position;
			Vector3 direction = remainingOffset.Normal;
			Vector3 moveAmount = MoveSpeed * direction * Time.Delta;


			if (remainingOffset.Length <= moveAmount.Length)
			{
				Velocity = remainingOffset;
				Transform.Position = destination;
				_moveNextIndex++;
			}
			else
			{
				Velocity = MoveSpeed * direction;
				Transform.Position += moveAmount;
			}
		}
	}
}
