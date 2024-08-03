public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerController PlayerController { get; set; }

	private TimeSince _timeSinceLastSpell = 0.0f;

	// NOTE: setting this internally MUST make sure it's valid.
	private ISpell.SpellType _activeSpell = ISpell.SpellType.Fireball;

	// TODO: is there a better data type for this?
	private List<ISpell> castSpells = new List<ISpell>();
	// Defer removal so we can avoid locks (blegh)
	private List<ISpell> deferredRemovals = new List<ISpell>();

	private float[] _spellNextCastTime;

	protected override void OnStart()
	{
		// This should be zeroed by definition.
		_spellNextCastTime = new float[(int)ISpell.SpellType.SpellTypeMax];
	}

	private ISpell CreateSpell(ISpell.SpellType spellType)
	{
		switch (spellType)
		{
		case ISpell.SpellType.Fireball: return new FireballSpell();
		}
		return null;
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		deferredRemovals.Add((ISpell)spell);
	}

	protected override void OnFixedUpdate()
	{
		// TODO: we should also consider mana cost here
		if (Input.Pressed("attack1") &&
			_spellNextCastTime[(int)_activeSpell] <= Time.Now)
		{
			ISpell spell = CreateSpell(_activeSpell);
			spell.Cast(PlayerController);
			spell.OnDestroy += OnSpellDestroyed;
			castSpells.Add(spell);
			_spellNextCastTime[(int)_activeSpell] = Time.Now + spell.Cooldown;
		}

		foreach (ISpell spell in castSpells)
		{
			spell.OnFixedUpdate();
		}

		// spell.OnFixedUpdate() can result in a spell deleting itself, thus
		// invalidating the iterator so we defer real removal until afterwards
		foreach (ISpell spell in deferredRemovals)
		{
			castSpells.Remove(spell);
		}
	}
}
