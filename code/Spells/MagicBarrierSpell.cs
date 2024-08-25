public class MagicBarrierSpell : BaseSpell
{
	public override float ManaCost => 25.0f;
	public override float Cooldown => 2.0f;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.0f;
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 0.0f;
	public override bool IsStateful => true;
	public override String IconPath =>
		"materials/PlayerMaterials/Spells/manabarrier.png";
	public override String SpellSound => "sounds/Spells/magicbarrier";
	// TODO: "custom" or "ontick"?
	public override ManaTakeTime TakeManaTime => ManaTakeTime.OnStartCasting;

	private const String BARRIER_PREFAB = "prefabs/spells/magic_barrier.prefab";
	// This is the damage multiplier, so a value of 0.75f would mean we reduce
	// damage taken by 25%.
	private const float BARRIER_DAMAGE_REDUCTION = 0.5f;

	public override event EventHandler OnDestroy;

	private PlayerMovementController _playerMovementController;
	private HealthComponent _health;
	private GameObject _barrierObj;
	private bool _enabled;

	public MagicBarrierSpell(GameObject caster)
		: base(caster)
	{
		if (caster == null)
			return;

		_enabled = false;

		_playerMovementController =
			_caster.Components
				   .GetInDescendantsOrSelf<PlayerMovementController>();
		_health =
			_caster.Components
				   .GetInDescendantsOrSelf<HealthComponent>();


		_barrierObj = new GameObject(false, "MagicBarrier");
		_barrierObj.SetPrefabSource(BARRIER_PREFAB);
		_barrierObj.UpdateFromPrefab();
		_barrierObj.SetParent(_caster);
		_barrierObj.Transform.LocalPosition = Vector3.Zero;
	}

	public override void OnStartCasting()
	{
	}

	public override bool OnFinishCasting()
	{
		_enabled = !_enabled;

		_barrierObj.Enabled = _enabled;

		// TODO: this assumes nothing else will modify the damage multiplier
		// and that the default value is 1.0, which may not necessarily be
		// true. If it isn't in the future, then make this smarter.
		if (_health != null)
			_health.DamageMultiplier = (_enabled)
									 ? BARRIER_DAMAGE_REDUCTION : 1.0f;

		return true;
	}

	public override void OnCancelCasting()
	{
	}

	public override void OnUpdate()
	{
	}

	public override bool OnFixedUpdate()
	{
		// TODO: we actually want to constantly draw mana when enabled
		OnDestroy?.Invoke(this, EventArgs.Empty);

		// TODO: this should be true when above is fixed.
		return false;
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.MagicBarrier;
	}
}
