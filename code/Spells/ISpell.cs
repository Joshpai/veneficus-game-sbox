public interface ISpell
{
	enum SpellType
	{
		SpellTypeMin,
		// The above must be at the front, so add new types below here!
		Fireball,
		// The below must be at the end, so add new types above here!
		SpellTypeMax
	}

	public float ManaCost { get; }
	public float Cooldown { get; }
	public float CastTime { get; }
	// This is the "additional" charge time (after the cast is ready), so total
	// time to charge fully is CastTime + MaxChargeTime.
	public float MaxChargeTime { get; }

	public event EventHandler OnDestroy;

	// TODO: maybe take a player controller reference in the constructor?
	public void StartCasting(PlayerController playerController);
	public void FinishCasting(PlayerController playerController,
							  float chargeAmount);
	public void OnFixedUpdate();
	// This should just help avoid forgetting to create a SpellType.
	public SpellType GetSpellType();
}

