public abstract class BaseSpell
{
	public enum SpellType
	{
		SpellTypeMin,
		// The above must be at the front, so add new types below here!
		Polymorph,
		MagicMissile,
		RockArmour,
		MagicBarrier,
		Fireball,
		RendingGale,
		WaterBeam,
		LightningStrike,
		// The below must be at the end, so add new types above here!
		SpellTypeMax
	}

	public enum ManaTakeTime
	{
		OnStartCasting,
		OnFinishCasting,
		OnTick
	}

	public abstract float ManaCost { get; }
	public abstract float Cooldown { get; }
	public abstract float CastTime { get; }
	// This is the "additional" charge time (after the cast is ready), so total
	// time to charge fully is CastTime + MaxChargeTime.
	public abstract float MaxChargeTime { get; }
	// A heavier spell will push the player back more on cast.
	public abstract float SpellMass { get; }
	public abstract float SpellSpeed { get; }
	// Stateful spells must be kept in memory and reused (i.e., are singletons)
	public abstract bool IsStateful { get; }
	public abstract ManaTakeTime TakeManaTime { get; }
	public abstract String IconPath { get; }

	public abstract event EventHandler OnDestroy;

	protected GameObject _caster;
	public Vector3 CasterEyeOrigin { get; set; }
	public Vector3 CastDirection { get; set; }

	private float _castTime;

	public bool HasFinishedCasting { get; private set; }
	public bool WasCancelled { get; private set; }

	public BaseSpell(GameObject caster)
	{
		_caster = caster;
	}

	public bool CanFinishCasting()
	{
		return (Time.Now - _castTime) >= CastTime;
	}

	public bool IsFullyCharged()
	{
		return (Time.Now - _castTime) >= CastTime + MaxChargeTime;
	}

	public void StartCasting()
	{
		_castTime = Time.Now;
		HasFinishedCasting = false;
		WasCancelled = false;
		OnStartCasting();
	}

	public bool FinishCasting()
	{
		HasFinishedCasting = true;
		return OnFinishCasting();
	}

	public void CancelCasting()
	{
		WasCancelled = true;
		OnCancelCasting();
	}

	public float GetChargeAmount()
	{
		float chargeAmount = (Time.Now - _castTime - CastTime) / MaxChargeTime;
		chargeAmount = Math.Min(chargeAmount, 1.0f);
		return chargeAmount;
	}

	public abstract void OnStartCasting();
	public abstract bool OnFinishCasting();
	public abstract void OnCancelCasting();

	public abstract bool OnFixedUpdate();
	public abstract void OnUpdate();

	public abstract SpellType GetSpellType();
}
