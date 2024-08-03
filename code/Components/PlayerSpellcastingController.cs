public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerController PlayerController { get; set; }

	// NOTE: setting this internally MUST make sure it's valid.
	private ISpell.SpellType _activeSpell = ISpell.SpellType.Fireball;

	// TODO: is there a better data type for this?
	private List<ISpell> castSpells = new List<ISpell>();
	// Defer removal so we can avoid locks (blegh)
	private List<ISpell> deferredRemovals = new List<ISpell>();

	private ISpell _castingSpell;
	private float _castingSpellFinishTime;

	private float[] _spellNextCastTime;

	protected override void OnStart()
	{
		// This should be zeroed by definition. It should be noted that this
		// allocates 2 more floats than necessary, but it's probably fine.
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

	private bool CanCastSpell(ISpell.SpellType spellType)
	{
		// TODO: we should also consider mana cost here
		return spellType > ISpell.SpellType.SpellTimeMin &&
			   spellType < ISpell.SpellType.SpellTypeMax &&
			   _spellNextCastTime[(int)spellType] <= Time.Now;
	}

	protected override void OnFixedUpdate()
	{
		if (_castingSpell != null)
		{
			_castingSpell.OnFixedUpdate();

			if (_castingSpellFinishTime <= Time.Now)
			{
				_castingSpell.FinishCasting(PlayerController);
				_castingSpell.OnDestroy += OnSpellDestroyed;
				castSpells.Add(_castingSpell);
				// TODO: interesting gameplay question here of:
				// "does cancelling a cast result in no cooldown?"
				_spellNextCastTime[(int)_activeSpell] =
					Time.Now + _castingSpell.Cooldown;
				_castingSpell = null;
			}
		}
		else if (Input.Pressed("attack1"))
		{
			if (CanCastSpell(_activeSpell))
			{
				_castingSpell = CreateSpell(_activeSpell);
				_castingSpellFinishTime = Time.Now + _castingSpell.CastTime;
				_castingSpell.StartCasting(PlayerController);
			}
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
