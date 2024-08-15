public class MagicMissileSpell : ProjectileSpell
{
	// BaseSpell
	public override float ManaCost => 5.0f;
	public override float Cooldown => 0.1f;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.1f;
	public override float SpellMass => 5.0f;
	public override float SpellSpeed => 500.0f;
	// ProjectileSpell
	public override String ProjectilePrefabPath => "prefabs/magic_missile.prefab";
	public override float ProjectileScale => 0.1f;
	public override float StartOffset => 75.0f;
	public override float Duration => 20.0f;

	public MagicMissileSpell(GameObject caster)
		: base(caster)
	{

	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.MagicMissile;
	}
}
