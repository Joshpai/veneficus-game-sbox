public sealed class DoorComponent : Component
{
	[Property]
	public List<GameObject> Keys { get; set; } = new List<GameObject>();

	public enum DoorOpenType
	{
		MoveUp,
		MoveDown,
		MoveLeft,
		MoveRight,
		Delete
	}

	[Property]
	public DoorOpenType OpenType { get; set; } = DoorOpenType.Delete;

	[Property]
	public float OpenDistance { get; set; } = 100.0f;

	[Property]
	public float OpenTime { get; set; } = 1.0f;

	private Vector3 _startPos;
	private float _startOpenTime = 0.0f;

	protected override void OnStart()
	{
		_startPos = Transform.Position;
	}

	private bool ShouldOpen()
	{
		foreach (var keyObj in Keys)
		{
			if (keyObj.IsValid)
			{
				return false;
			}
		}

		return true;
	}

	private Vector3 GetOpenDirection()
	{
		return OpenType switch
		{
			DoorOpenType.MoveUp => Vector3.Up,
			DoorOpenType.MoveDown => Vector3.Down,
			DoorOpenType.MoveRight => Vector3.Right,
			DoorOpenType.MoveLeft => Vector3.Left,
			_ => Vector3.Zero
		};
	}

	protected override void OnUpdate()
	{
		if (_startOpenTime != 0.0f && Time.Now - _startOpenTime < OpenTime)
		{
			var closedAmount = (Time.Now - _startOpenTime) / OpenTime;
			Transform.Position =
				Vector3.Lerp(
					_startPos,
					_startPos + GetOpenDirection() * OpenDistance,
					closedAmount
				);
		}
	}

	protected override void OnFixedUpdate()
	{
		if (_startOpenTime == 0.0f && ShouldOpen())
		{
			if (OpenType == DoorOpenType.Delete)
				GameObject.Destroy();
			else
				_startOpenTime = Time.Now;
		}
	}
}
