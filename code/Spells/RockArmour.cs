public class RockArmourSpell : WorldPlacementSpell
{
	// BaseSpell
	public override float ManaCost => 20.0f;
	public override float Cooldown => 2.0f;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.1f; // Non-zero, doesn't matter
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 0.0f;
	// WorldPlacementSpell
	public override float MaxRange => 250.0f;
	public override int MaxPlacedObjects => 3;

	private const string WALL_PREFAB = "prefabs/rock_wall.prefab";

	public RockArmourSpell(GameObject caster)
		: base(caster)
	{

	}

	private Action HandleDestroyed(GameObject placedObject)
	{
		return () => DestroyPlacedObject(placedObject);
	}

	public override GameObject OnPlaced(GameTransform transform)
	{
		var placedObject =
			new GameObject(false, GetSpellType().ToString() + "Spawned");
		placedObject.Transform.Position = transform.Position;
		placedObject.Transform.Rotation = transform.Rotation;
		placedObject.SetPrefabSource(WALL_PREFAB);
		placedObject.UpdateFromPrefab();
		placedObject.Enabled = true;

		var health = placedObject.Components
								 .GetInDescendantsOrSelf<HealthComponent>();
		if (health != null)
		{
			// Keep it alive and we will clean up (should be set in prefab)
			//health.DestroyOnDeath = false;
			health.OnDeath += HandleDestroyed(placedObject);
		}

		return placedObject;
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.RockArmour;
	}
}
