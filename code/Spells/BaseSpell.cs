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

	// TODO: maybe take a player controller reference in the constructor?
	public abstract void StartCasting(PlayerController playerController);
	public abstract void FinishCasting(PlayerController playerController,
									   float chargeAmount);
	public abstract void OnFixedUpdate();
	// This should just help avoid forgetting to create a SpellType.
	public abstract SpellType GetSpellType();
}
