public class FireballSpell : ProjectileSpell
{
	// BaseSpell
	public override float ManaCost => 20.0f;
	public override float Cooldown => 1.0f;
	public override float CastTime => 0.3f;
	public override float MaxChargeTime => 0.5f;
	public override float SpellMass => 35.0f;
	public override float SpellSpeed => 1000.0f;
	public override String IconPath =>
		"materials/PlayerMaterials/Spells/fireball.png";
	public override String SpellSound => "sounds/Spells/fireball";
	// ProjectileSpell
	public override String ProjectilePrefabPath =>
		"prefabs/spells/fireball.prefab";
	public override float ProjectileScale => 0.1f;
	public override float StartOffset => 150.0f;
	public override float Duration => 20.0f;

	public FireballSpell(GameObject caster)
		: base(caster)
	{

	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.Fireball;
	}
}
