public abstract class BaseSpell
{
	public enum SpellType
	{
		SpellTypeMin,
		// The above must be at the front, so add new types below here!
		Fireball,
		// The below must be at the end, so add new types above here!
		SpellTypeMax
	}

	public abstract float ManaCost { get; }
	public abstract float Cooldown { get; }
	public abstract float CastTime { get; }
	// This is the "additional" charge time (after the cast is ready), so total
	// time to charge fully is CastTime + MaxChargeTime.
	public abstract float MaxChargeTime { get; }

	public abstract event EventHandler OnDestroy;

	protected GameObject _caster;
	public Vector3 CasterEyeOrigin { get; set; }
	public Vector3 CastDirection { get; set; }

	private float _castTime;

	public bool HasFinishedCasting { get; private set; }

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
		OnStartCasting();
	}

	public void FinishCasting()
	{
		HasFinishedCasting = true;
		OnFinishCasting();
	}

	public float GetChargeAmount()
	{
		float chargeAmount = (Time.Now - _castTime - CastTime) / MaxChargeTime;
		chargeAmount = Math.Min(chargeAmount, 1.0f);
		return chargeAmount;
	}

	public abstract void OnStartCasting();
	public abstract void OnFinishCasting();

	public abstract void OnFixedUpdate();
	public abstract void OnUpdate();
	// This should just help avoid forgetting to create a SpellType.
	public abstract SpellType GetSpellType();
}
