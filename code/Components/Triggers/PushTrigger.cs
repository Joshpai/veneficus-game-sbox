public sealed class PushTrigger : Component, Component.ITriggerListener
{
	[Property]
	public Vector3 PushVector { get; set; }

	[Property]
	public List<String> PushableTags { get; set; } =
		new List<String>() { "player", "enemy" };

	private HashSet<String> _pushableTags;

	// TODO: is a hash set actually what we want here? I wanted a tree, really,
	// but I think HashSets are typically implemented as trees.
	private HashSet<Rigidbody> _insideObjects;
	private PlayerMovementController _insidePlayer;

	protected override void DrawGizmos()
	{
		base.DrawGizmos();

		if (!Gizmo.IsSelected)
			return;

		Gizmo.Draw.LineThickness = 8.0f;
		var end = Transform.Position + PushVector;
		Gizmo.Draw.Line(Transform.Position, end);
	}

	protected override void OnStart()
	{
		_insideObjects = new HashSet<Rigidbody>();
		_pushableTags = new HashSet<String>(PushableTags);
	}

	public void OnTriggerEnter(Collider other)
	{
		// Any of these things might happen before OnStart!
		if (other.GameObject == null || other.GameObject.Tags == null ||
			_pushableTags == null)
			return;

		if (!other.GameObject.Tags.HasAny(_pushableTags))
			return;

		var rigidBody =
			other.GameObject.Components.GetInDescendantsOrSelf<Rigidbody>();
		if (rigidBody != null)
			_insideObjects.Add(rigidBody);

		_insidePlayer =
			other.GameObject.Components.GetInDescendantsOrSelf<PlayerMovementController>();
	}

	public void OnTriggerExit(Collider other)
	{
		var rigidBody =
			other.GameObject.Components.GetInDescendantsOrSelf<Rigidbody>();
		if (rigidBody != null)
			_insideObjects.Remove(rigidBody);

		var player =
			other.GameObject.Components.GetInDescendantsOrSelf<PlayerMovementController>();
		if (player != null)
			_insidePlayer = null;
	}

	protected override void OnFixedUpdate()
	{
		foreach (var obj in _insideObjects)
		{
			obj.Transform.Position += PushVector * Time.Delta;
		}

		if (_insidePlayer != null)
			_insidePlayer.Controller.Velocity += PushVector * Time.Delta;
	}
}
