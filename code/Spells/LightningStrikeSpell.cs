public class LightningStrikeSpell : WorldPlacementSpell
{
	// BaseSpell
	public override float ManaCost => 20.0f;
	public override float Cooldown => 2.0f;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.1f; // Non-zero, doesn't matter
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 0.0f;
	// WorldPlacementSpell
	public override float MinRange => 0.0f;
	public override float MaxRange => 250.0f;
	public override int MaxPlacedObjects => 1;

	private const string LIGHTNING_PREFAB = "prefabs/lightning_strike.prefab";
	// TODO: could get this from the particle effect, but for now this works
	private const float LIFETIME = 0.7f;
	// Radius of capsule representing damage area
	private const float DAMAGE_RADIUS = 100.0f;
	// Height of capsule representing damage area (from cylinder bottom->top)
	private const float DAMAGE_HEIGHT = 100.0f;
	private const float DAMAGE_AMOUNT = 75.0f;

	private float _despawnTime;
	private GameObject _placedObject;

	public LightningStrikeSpell(GameObject caster)
		: base(caster)
	{

	}

	public override GameObject OnPlaced(GameTransform transform)
	{
		// This handles its own lifetime
		_placedObject =
			new GameObject(false, GetSpellType().ToString() + "Spawned");
		_placedObject.Transform.Position = transform.Position;
		_placedObject.Transform.Rotation = transform.Rotation;
		_placedObject.SetPrefabSource(LIGHTNING_PREFAB);
		_placedObject.UpdateFromPrefab();
		_placedObject.Enabled = true;
		_despawnTime = Time.Now + LIFETIME;

		// I really would've loved a cylinder here.
		var capBottom = transform.Position;
		var capTop = transform.Position + Vector3.Up * DAMAGE_HEIGHT;
		var cap = new Capsule(capBottom, capTop, DAMAGE_RADIUS);
		var trace = _placedObject.Scene.Trace
									   .Capsule(cap)
									   .HitTriggers()
									   .RunAll();
		foreach (var hit in trace)
		{
			if (!hit.Hit)
				continue;

			var hp = hit.GameObject.Components
								   .GetInDescendantsOrSelf<HealthComponent>();
			if (hp != null)
				hp.Damage(DAMAGE_AMOUNT);
		}

		return _placedObject;
	}

	public override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (_despawnTime < Time.Now)
		{
			DestroyPlacedObject(_placedObject);
		}
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.LightningStrike;
	}
}
