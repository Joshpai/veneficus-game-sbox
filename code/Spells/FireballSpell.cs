public class FireballSpell : ProjectileSpell
{
	// BaseSpell
	public override float ManaCost => 50.0f;
	public override float Cooldown => 2.0f;
	public override float CastTime => 0.3f;
	public override float MaxChargeTime => 0.5f;
	public override float SpellMass => 100.0f;
	public override float SpellSpeed => 300.0f;
	// ProjectileSpell
	public override String ProjectilePrefabPath =>
		"prefabs/spells/fireball.prefab";
	public override float ProjectileScale => 0.1f;
	public override float StartOffset => 150.0f;
	public override float Duration => 5.0f;

	public FireballSpell(GameObject caster)
		: base(caster)
	{

	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.Fireball;
	}
}
