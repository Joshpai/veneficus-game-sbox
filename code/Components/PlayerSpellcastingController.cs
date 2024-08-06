public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerController PlayerControllerRef { get; set; }

	[Property]
	public float MaxMana { get; set; } = 100.0f;

	[Property]
	public float ManaRefundAmount { get; set; } = 0.5f;

	// Mana per second
	[Property]
	public float ManaRegenRate { get; set; } = 5.0f;

	// Time after casting when mana regens
	[Property]
	public float ManaRegenDelay { get; set; } = 1.0f;

	public float Mana { get; set; }

	public BaseSpell.SpellType ActiveSpell { get; private set; }

	// TODO: is there a better data type for this?
	private List<BaseSpell> _castSpells = new List<BaseSpell>();
	// Defer removal so we can avoid locks (blegh)
	private List<BaseSpell> _deferredRemovals = new List<BaseSpell>();

	private BaseSpell _castingSpell;
	private bool _castingSpellIsHeld;

	private float[] _spellNextCastTime;
	private UInt64 _unlockedSpellsMask;

	private float _manaRegenStartTime;

	// This makes a strange architectural format for the spell system. I really
	// want to know things like `ManaCost` without needing an object but C#
	// doesn't allow static abstract members. To get around this, and also
	// avoid many random allocations, we just create a big buffer of all the
	// spells and just grab the one we need (and replace it).
	private BaseSpell[] _spellBuffer;

	protected override void OnStart()
	{
		// This should be zeroed by definition. It should be noted that this
		// allocates 2 more floats than necessary, but it's probably fine.
		_spellNextCastTime = new float[(int)BaseSpell.SpellType.SpellTypeMax];
		// Default to having all spells unlocked
		// TODO: This will need to be serialised in some player data thing
		_unlockedSpellsMask = 0xfffffffffffffffful;

		ActiveSpell = BaseSpell.SpellType.SpellTypeMin + 1;
		_spellBuffer = new BaseSpell[(int)BaseSpell.SpellType.SpellTypeMax];
		for (int i = 0; i < _spellBuffer.Length; i++)
			_spellBuffer[i] = CreateSpell((BaseSpell.SpellType)i);

		Mana = MaxMana;
		_manaRegenStartTime = 0.0f;
	}

	private BaseSpell CreateSpell(BaseSpell.SpellType spellType)
	{
		GameObject caster = this.GameObject;
		return spellType switch
		{
			BaseSpell.SpellType.Fireball => new FireballSpell(caster),
			BaseSpell.SpellType.Polymorph => new PolymorphSpell(caster),
			_ => null,
		};
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		_deferredRemovals.Add((BaseSpell)spell);
	}

	private bool CanCastSpell(BaseSpell.SpellType spellType)
	{
		// Spell type is valid
		return spellType > BaseSpell.SpellType.SpellTypeMin &&
			   spellType < BaseSpell.SpellType.SpellTypeMax &&
			   // Spell isn't on cooldown
			   _spellNextCastTime[(int)spellType] <= Time.Now &&
			   // Spell is unlocked
			   (_unlockedSpellsMask & (1ul << (int)spellType)) != 0ul &&
			   Mana >= _spellBuffer[(int)spellType].ManaCost;
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
			ActiveSpell = spellType;
	}

	public float GetActiveSpellCost()
	{
		return _spellBuffer[(int)ActiveSpell].ManaCost;
	}

	public float GetActiveSpellCooldown()
	{
		return Math.Max(_spellNextCastTime[(int)ActiveSpell] - Time.Now, 0.0f);
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
		// This is very ugly because C# forces explicit casting :(
		if (Input.Pressed("SlotNext"))
			ActiveSpell =
				((int)ActiveSpell + 1 < (int)BaseSpell.SpellType.SpellTypeMax)
				? (BaseSpell.SpellType)((int)ActiveSpell + 1)
				: BaseSpell.SpellType.SpellTypeMin + 1;
		else if (Input.Pressed("SlotPrev"))
			ActiveSpell =
				((int)ActiveSpell - 1 > (int)BaseSpell.SpellType.SpellTypeMin)
				? (BaseSpell.SpellType)((int)ActiveSpell - 1)
				: BaseSpell.SpellType.SpellTypeMax - 1;

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
				Mana += _castingSpell.ManaCost * ManaRefundAmount;
				// Cancel => start regen immediately?
				_manaRegenStartTime = Time.Now;
				_castingSpell = null;
			}

			_castingSpellIsHeld &= Input.Down("attack1");
			if (!_castingSpellIsHeld && _castingSpell.CanFinishCasting())
			{
				// TODO: fully charged spells should cost more mana (maybe?)
				_castingSpell.FinishCasting();
				// TODO: replace magic 100 with "PlayerWeight" or something
				var pushback =
					_castingSpell.SpellMass * _castingSpell.SpellSpeed /
					100.0f * (1.0f + _castingSpell.GetChargeAmount()) *
					-PlayerControllerRef.EyeAngles.Forward;
				PlayerControllerRef.Controller.Punch(pushback);
				_castingSpell.OnDestroy += OnSpellDestroyed;
				_castSpells.Add(_castingSpell);
				_manaRegenStartTime = Time.Now + ManaRegenDelay;
				// TODO: interesting gameplay question here of:
				// "does cancelling a cast result in no cooldown?"
				_spellNextCastTime[(int)ActiveSpell] =
					Time.Now + _castingSpell.Cooldown;
				_castingSpell = null;
			}
		}
		else if (Input.Pressed("attack1"))
		{
			if (CanCastSpell(ActiveSpell))
			{
				_castingSpell = _spellBuffer[(int)ActiveSpell];
				_spellBuffer[(int)ActiveSpell] = CreateSpell(ActiveSpell);

				// TODO: it would be cool if this is progressively taken during
				// the casting process. But that's not needed for now.
				Mana -= _castingSpell.ManaCost;

				_castingSpell.CasterEyeOrigin = PlayerControllerRef.EyePosition;
				_castingSpell.CastDirection =
					PlayerControllerRef.EyeAngles.Forward;
				_castingSpell.StartCasting();
				_castingSpellIsHeld = true;
			}
		}
		// Implicit preconditions include that we aren't casting a spell now
		else if (_manaRegenStartTime <= Time.Now && Mana < MaxMana)
		{
			Mana = Math.Min(Mana + ManaRegenRate * Time.Delta, MaxMana);
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
