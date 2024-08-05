public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerController PlayerControllerRef { get; set; }

	private BaseSpell.SpellType _activeSpell = BaseSpell.SpellType.Fireball;

	// TODO: is there a better data type for this?
	private List<BaseSpell> _castSpells = new List<BaseSpell>();
	// Defer removal so we can avoid locks (blegh)
	private List<BaseSpell> _deferredRemovals = new List<BaseSpell>();

	private BaseSpell _castingSpell;
	private bool _castingSpellIsHeld;

	private float[] _spellNextCastTime;

	private UInt64 _unlockedSpellsMask;

	protected override void OnStart()
	{
		// This should be zeroed by definition. It should be noted that this
		// allocates 2 more floats than necessary, but it's probably fine.
		_spellNextCastTime = new float[(int)BaseSpell.SpellType.SpellTypeMax];
		// Default to having all spells unlocked
		// TODO: This will need to be serialised in some player data thing
		_unlockedSpellsMask = 0xfffffffffffffffful;
	}

	private BaseSpell CreateSpell(BaseSpell.SpellType spellType)
	{
		GameObject caster = this.GameObject;
		return spellType switch
		{
			BaseSpell.SpellType.Fireball => new FireballSpell(caster),
			_ => null,
		};
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		_deferredRemovals.Add((BaseSpell)spell);
	}

	private bool CanCastSpell(BaseSpell.SpellType spellType)
	{
		// TODO: we should also consider mana cost here

		// Spell type is valid
		return spellType > BaseSpell.SpellType.SpellTypeMin &&
			   spellType < BaseSpell.SpellType.SpellTypeMax &&
			   // Spell isn't on cooldown
			   _spellNextCastTime[(int)spellType] <= Time.Now &&
			   // Spell is unlocked
			   (_unlockedSpellsMask & (1ul << (int)spellType)) != 0ul;
	}

	public void SetSpellUnlocked(BaseSpell.SpellType spellType, bool unlocked)
	{
		if (unlocked)
			_unlockedSpellsMask |= (1ul << (int)spellType);
		else
			_unlockedSpellsMask &= ~(1ul << (int)spellType);
	}

	public void SetActiveSpell(BaseSpell.SpellType spellType)
	{
		if (spellType > BaseSpell.SpellType.SpellTypeMin &&
			spellType < BaseSpell.SpellType.SpellTypeMax)
			_activeSpell = spellType;
	}

	protected override void OnUpdate()
	{
		if (_castingSpell != null)
		{
			_castingSpell.CastDirection = PlayerControllerRef.EyeAngles.Forward;
			_castingSpell.OnUpdate();
		}

		foreach (BaseSpell spell in _castSpells)
		{
			spell.OnUpdate();
		}
	}

	protected override void OnFixedUpdate()
	{
		if (_castingSpell != null)
		{
			_castingSpell.OnFixedUpdate();

			// Cancel the spell
			if (Input.Pressed("attack2"))
			{
				// Even though the spell is cancelled, we still want to service
				// it as if it wasn't cancelled. We don't really care about the
				// difference and it just gives a nice way to allow cancel
				// animations (for example).
				_castingSpell.OnDestroy += OnSpellDestroyed;
				_castSpells.Add(_castingSpell);
				_castingSpell.CancelCasting();
				_castingSpell = null;
				// TODO: partial mana refund?
			}

			_castingSpellIsHeld &= Input.Down("attack1");
			if (!_castingSpellIsHeld && _castingSpell.CanFinishCasting())
			{
				_castingSpell.FinishCasting();
				// TODO: replace magic 100 with "PlayerWeight" or something
				var pushback =
					_castingSpell.SpellMass * _castingSpell.SpellSpeed /
					100.0f * (1.0f + _castingSpell.GetChargeAmount()) *
					-PlayerControllerRef.EyeAngles.Forward;
				PlayerControllerRef.Controller.Punch(pushback);
				_castingSpell.OnDestroy += OnSpellDestroyed;
				_castSpells.Add(_castingSpell);
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
				_castingSpell.CasterEyeOrigin = PlayerControllerRef.EyePosition;
				_castingSpell.CastDirection =
					PlayerControllerRef.EyeAngles.Forward;
				_castingSpell.StartCasting();
				_castingSpellIsHeld = true;
			}
		}

		foreach (BaseSpell spell in _castSpells)
		{
			spell.OnFixedUpdate();
		}

		// spell.OnFixedUpdate() can result in a spell deleting itself, thus
		// invalidating the iterator so we defer real removal until afterwards
		foreach (BaseSpell spell in _deferredRemovals)
		{
			_castSpells.Remove(spell);
		}
	}
}
