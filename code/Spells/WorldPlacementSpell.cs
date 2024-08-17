public abstract class WorldPlacementSpell : BaseSpell
{
	// allow some number to be placed in the world?
	public override bool IsStateful => true;
	public override ManaTakeTime TakeManaTime => ManaTakeTime.OnFinishCasting;
	public override event EventHandler OnDestroy;

	public string PlacementIndicatorPrefab { get; } =
		"prefabs/spell_placement_indicator.prefab";
	public abstract float MaxRange { get; }
	public abstract int MaxPlacedObjects { get; }

	protected PlayerMovementController _playerMovementController;
	protected GameObject _placementIndicator;
	protected ModelRenderer _placementIndicatorRenderer;
	private bool _isPlaceable;

	private LinkedList<GameObject> _placedObjects;

	public WorldPlacementSpell(GameObject caster)
		: base(caster)
	{
		_placementIndicator =
			new GameObject(false, GetSpellType().ToString() + "PlacementIndicator");
		_placementIndicator.SetPrefabSource(PlacementIndicatorPrefab);
		_placementIndicator.UpdateFromPrefab();
		_isPlaceable = false;

		// Silly dance to stop the compiler screaming and crying
		OnDestroy = null;
		var copy = OnDestroy;
		OnDestroy = copy;

		_playerMovementController =
			_caster.Components
				   .GetInDescendantsOrSelf<PlayerMovementController>();

		_placedObjects = new LinkedList<GameObject>();
	}

	public void UpdatePlacementIndicator()
	{
		Vector3 startPos =
			_caster.Transform.Position + _playerMovementController.EyePosition;
		Vector3 endPos =
			startPos + _playerMovementController.EyeAngles.Forward * MaxRange;
		Vector3? placePos = null;

		// TODO: give traces an AABB or other?
		var tr =  _caster.Scene.Trace.Ray(startPos, endPos)
									 .Run();
		if (tr.Hit && tr.Normal.Dot(Vector3.Up) > 0.0f)
		{
			placePos = tr.HitPosition;
		}
		else
		{
			var floorTraceStart = (tr.Hit) ? tr.HitPosition : endPos;
			var floorTraceEnd = floorTraceStart + Vector3.Down * MaxRange * 2.0f;
			var downTr =
				_caster.Scene.Trace.Ray(floorTraceStart, floorTraceEnd)
								   .Run();
			if (downTr.Hit)
			{
				placePos = downTr.HitPosition;
			}
		}

		// Try to get any kind of model renderer.
		_placementIndicatorRenderer =
			_placementIndicator.Components
							   .GetInDescendantsOrSelf<ModelRenderer>();
		if (_placementIndicatorRenderer == null)
			_placementIndicator.Components
							   .GetInDescendantsOrSelf<SkinnedModelRenderer>();
		
		_isPlaceable = (placePos != null);
		if (placePos == null)
			placePos = endPos;

		_placementIndicator.Transform.Position = placePos.Value;

		Vector3 playerToPlace = (placePos.Value - _caster.Transform.Position);
		_placementIndicator.Transform.Rotation =
			playerToPlace.EulerAngles.WithPitch(0.0f);
	}

	protected void DestroyPlacedObject(GameObject obj)
	{
		LinkedListNode<GameObject> iter = _placedObjects.First;

		if (obj == null)
			return;

		while (iter != null)
		{
			if (iter.Value == obj)
			{
				// TODO: also create some kind of destruction gib?
				_placedObjects.Remove(iter);
				obj.Destroy();
				break;
			}

			iter = iter.Next;
		}
	}

	public override void OnStartCasting()
	{
		UpdatePlacementIndicator();
		_placementIndicator.Enabled = true;
	}

	public override bool OnFinishCasting()
	{
		_placementIndicator.Enabled = false;

		if (!_isPlaceable)
			return false;
			
		if (_placedObjects.Count >= MaxPlacedObjects)
		{
			DestroyPlacedObject(_placedObjects.First.Value);
		}

		// TODO: give these a maximum lifetime? Do some incremental DOT
		var placedObject = OnPlaced(_placementIndicator.Transform);
		if (placedObject != null)
			_placedObjects.AddLast(placedObject);

		return true;
	}

	public override void OnCancelCasting()
	{
		_placementIndicator.Enabled = false;
	}

	public override void OnUpdate()
	{
		UpdatePlacementIndicator();
		if (_placementIndicatorRenderer != null)
			_placementIndicatorRenderer.Tint =
				(_isPlaceable) ? Color.Green : Color.Red;
	}

	public override void OnFixedUpdate()
	{
	}

	public abstract GameObject OnPlaced(GameTransform transform);
}
