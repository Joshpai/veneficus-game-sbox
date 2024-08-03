public interface ISpell
{
	enum SpellType
	{
		Fireball,
		// The below must be at the end, so add new types above here!
		SpellTypeMax
	}

	public float ManaCost { get; }
	public float Cooldown { get; }

	public event EventHandler OnDestroy;

	public void Cast(PlayerController playerController);
	public void OnFixedUpdate();
	// This should just help avoid forgetting to create a SpellType.
	public SpellType GetSpellType();
}

