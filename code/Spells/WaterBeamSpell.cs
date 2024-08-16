public class WaterBeamSpell : ProjectileSpell
{
	// BaseSpell
	public override float ManaCost => 0.5f;
	public override float Cooldown => 0.05f;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.0f;
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 700.0f;
	// ProjectileSpell
	public override String ProjectilePrefabPath => "prefabs/water_beam.prefab";
	public override float ProjectileScale => 0.2f;
	public override float StartOffset => 75.0f;
	public override float Duration => 20.0f;

	public WaterBeamSpell(GameObject caster)
		: base(caster)
	{

	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.WaterBeam;
	}
}
