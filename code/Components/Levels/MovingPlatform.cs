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

	[Property]
	public bool RepeatPath { get; set; } = false;

	[Property]
	public bool ShouldResetTouchedAtEnd { get; set; } = false;

	[Property]
	public bool ShouldResetTouchedAtStart { get; set; } = false;

	[Property]
	public float StartMoveDelay { get; set; } = 0.0f;

	[Property]
	public float ReturnMoveDelay { get; set; } = 1.0f;

	public bool PlayerTouching { get; set; } = false;

	public Vector3 Velocity { get; private set; } = Vector3.Zero;

	private bool _playerTouched = false;

	private Vector3 _startingPosition;

	private List<Vector3> _movePathWorld = new List<Vector3>();
	private int _moveNextIndex = 0;
	private int _pathDirection = 1;

	private float _startMoveTime = 0.0f;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (!Gizmo.IsSelected)
			return;

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
		return _moveNextIndex >= 0 && _moveNextIndex < _movePathWorld.Count &&
			   _startMoveTime <= Time.Now;
	}

	private bool IsMoving()
	{
		return (MoveType == MoveCharacteristics.Always) ||
			   (MoveType == MoveCharacteristics.AfterPlayerTouch &&
				_playerTouched) ||
			   (MoveType == MoveCharacteristics.WhilePlayerTouching &&
				PlayerTouching);
	}

	private float GetStartDelayTime()
	{
		return (_pathDirection == 1) ? StartMoveDelay : ReturnMoveDelay;
	}

	protected override void OnFixedUpdate()
	{
		if (!_playerTouched && PlayerTouching)
		{
			_playerTouched = true;
			_startMoveTime = Time.Now + StartMoveDelay;
		}

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
				_moveNextIndex += _pathDirection;
			}
			else
			{
				Velocity = MoveSpeed * direction;
				Transform.Position += moveAmount;
			}

			// we have reached the end
			if (!CanMove() && RepeatPath)
			{
				_pathDirection = -_pathDirection;
				_moveNextIndex += 2 * _pathDirection;
				if (ShouldResetTouchedAtStart && _pathDirection == 1)
					_playerTouched = false;
				else if (ShouldResetTouchedAtEnd && _pathDirection == -1)
					_playerTouched = false;
				else
					_startMoveTime = Time.Now + GetStartDelayTime();
			}
		}
	}
}
