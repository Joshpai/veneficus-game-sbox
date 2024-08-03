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

	public event EventHandler OnDestroy;

	// TODO: maybe take a player controller reference in the constructor?
	public void StartCasting(PlayerController playerController);
	public void FinishCasting(PlayerController playerController);
	public void OnFixedUpdate();
	// This should just help avoid forgetting to create a SpellType.
	public SpellType GetSpellType();
}

