public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerMovementController PlayerMovementControllerRef { get; set; }

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

	private bool _attackHeldAfterSwitch = false;

	protected override void OnStart()
	{
		// This should be zeroed by definition. It should be noted that this
		// allocates 2 more floats than necessary, but it's probably fine.
		_spellNextCastTime = new float[(int)BaseSpell.SpellType.SpellTypeMax];
		// Default to having all spells unlocked
		// TODO: This will need to be serialised in some player data thing
		_unlockedSpellsMask = 0xfffffffffffffffful;
		// _unlockedSpellsMask = 0x6ul;

		ActiveSpell = BaseSpell.SpellType.SpellTypeMin + 1;
		_spellBuffer = new BaseSpell[(int)BaseSpell.SpellType.SpellTypeMax];
		for (int i = 0; i < _spellBuffer.Length; i++)
			_spellBuffer[i] = CreateSpell(GameObject, (BaseSpell.SpellType)i);

		Mana = MaxMana;
		_manaRegenStartTime = 0.0f;
	}

	public static BaseSpell CreateSpell(GameObject caster,
										BaseSpell.SpellType spellType)
	{
		return spellType switch
		{
			BaseSpell.SpellType.Polymorph => new PolymorphSpell(caster),
			BaseSpell.SpellType.MagicMissile => new MagicMissileSpell(caster),
			BaseSpell.SpellType.Fireball => new FireballSpell(caster),
			BaseSpell.SpellType.WaterBeam => new WaterBeamSpell(caster),
			_ => null,
		};
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		_deferredRemovals.Add((BaseSpell)spell);
	}

	private bool CanCastSpell(BaseSpell.SpellType spellType)
	{
		// TODO: don't let player cast spells if polymorphed!

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

	public bool IsSpellUnlocked(BaseSpell.SpellType spellType)
	{
		return (_unlockedSpellsMask & (1ul << (int)spellType)) != 0;
	}

	public void SetActiveSpell(BaseSpell.SpellType spellType)
	{
		if (spellType > BaseSpell.SpellType.SpellTypeMin &&
			spellType < BaseSpell.SpellType.SpellTypeMax &&
			IsSpellUnlocked(spellType))
			ActiveSpell = spellType;
	}

	public float GetSpellCost(BaseSpell.SpellType spellType)
	{
		return _spellBuffer[(int)spellType].ManaCost;
	}

	public float GetSpellCooldown(BaseSpell.SpellType spellType)
	{
		return Math.Max(_spellNextCastTime[(int)spellType] - Time.Now, 0.0f);
	}

	public float GetSpellCooldownPercent(BaseSpell.SpellType spellType)
	{
		float cooldownMax = _spellBuffer[(int)spellType].Cooldown;
		return GetSpellCooldown(spellType) / cooldownMax;
	}

	protected override void OnUpdate()
	{
		if (_castingSpell != null)
		{
			_castingSpell.CastDirection =
				PlayerMovementControllerRef.EyeAngles.Forward;
			_castingSpell.OnUpdate();
		}

		foreach (BaseSpell spell in _castSpells)
		{
			spell.OnUpdate();
		}
	}

	private void SelectNextUnlockedSpell()
	{
		int i;

		// I really would prefer some bit magic here, but it doesn't look like
		// C# doesn't come with some of the intrinsics I'm used to, e.g., ffs.

		for (i = (int)ActiveSpell + 1;
			 i < (int)BaseSpell.SpellType.SpellTypeMax; i++)
		{
			if (IsSpellUnlocked((BaseSpell.SpellType)i))
			{
				ActiveSpell = (BaseSpell.SpellType)i;
				return;
			}
		}

		for (i = (int)BaseSpell.SpellType.SpellTypeMin + 1;
			 i < (int)ActiveSpell; i++)
		{
			if (IsSpellUnlocked((BaseSpell.SpellType)i))
			{
				ActiveSpell = (BaseSpell.SpellType)i;
				return;
			}
		}
	}

	private void SelectPrevUnlockedSpell()
	{
		int i;

		for (i = (int)ActiveSpell - 1;
			 i > (int)BaseSpell.SpellType.SpellTypeMin; i--)
		{
			if (IsSpellUnlocked((BaseSpell.SpellType)i))
			{
				ActiveSpell = (BaseSpell.SpellType)i;
				return;
			}
		}

		for (i = (int)BaseSpell.SpellType.SpellTypeMax - 1;
			 i > (int)ActiveSpell; i--)
		{
			if (IsSpellUnlocked((BaseSpell.SpellType)i))
			{
				ActiveSpell = (BaseSpell.SpellType)i;
				return;
			}
		}
	}

	private void StartCasting()
	{
		_castingSpell = _spellBuffer[(int)ActiveSpell];
		// Stateful spells are singletons, so only recreate spells that
		// aren't stateful!
		if (!_spellBuffer[(int)ActiveSpell].IsStateful)
			_spellBuffer[(int)ActiveSpell] =
				CreateSpell(GameObject, ActiveSpell);

		// TODO: it would be cool if this is progressively taken during
		// the casting process. But that's not needed for now.
		Mana -= _castingSpell.ManaCost;

		_castingSpell.CasterEyeOrigin =
			PlayerMovementControllerRef.EyePosition;
		_castingSpell.CastDirection =
			PlayerMovementControllerRef.EyeAngles.Forward;
		_castingSpell.StartCasting();
		_castingSpellIsHeld = true;
	}

	private void CancelCasting()
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

	private void FinishCasting()
	{
		// TODO: fully charged spells should cost more mana (maybe?)
		_castingSpell.FinishCasting();

		if (_castingSpell.SpellMass != 0.0f)
		{
			var pushback =
				_castingSpell.SpellMass * _castingSpell.SpellSpeed /
				PlayerMovementControllerRef.Mass *
				(1.0f + _castingSpell.GetChargeAmount()) *
				-PlayerMovementControllerRef.EyeAngles.Forward;
			PlayerMovementControllerRef.Controller.Punch(pushback);
		}

		_castingSpell.OnDestroy += OnSpellDestroyed;
		_castSpells.Add(_castingSpell);
		_manaRegenStartTime = Time.Now + ManaRegenDelay;
		// TODO: interesting gameplay question here of:
		// "does cancelling a cast result in no cooldown?"
		_spellNextCastTime[(int)ActiveSpell] =
			Time.Now + _castingSpell.Cooldown;
		_castingSpell = null;
	}

	private bool CanHoldCastSpell()
	{
		// If a spell is "instant", then we can hold to cast it. This is mainly
		// for water beam which acts as a rapid fire spell.
		return !_attackHeldAfterSwitch &&
			   _spellBuffer[(int)ActiveSpell].MaxChargeTime == 0.0f &&
			   _spellBuffer[(int)ActiveSpell].CastTime == 0.0f;
	}

	protected override void OnFixedUpdate()
	{
		if (Input.Pressed("SlotNext"))
			SelectNextUnlockedSpell();
		else if (Input.Pressed("SlotPrev"))
			SelectPrevUnlockedSpell();

		if (_castingSpell != null)
		{
			_castingSpell.OnFixedUpdate();

			// Cancel the spell
			if (Input.Pressed("attack2"))
			{
				CancelCasting();
			}

			_castingSpellIsHeld &= Input.Down("attack1");
			if (!_castingSpellIsHeld && _castingSpell.CanFinishCasting())
			{
				FinishCasting();
			}
		}
		else if (Input.Pressed("attack1") ||
				 (Input.Down("attack1") && CanHoldCastSpell()))
		{
			if (CanCastSpell(ActiveSpell))
			{
				StartCasting();

				if (_castingSpell.CastTime == 0.0f &&
					_castingSpell.MaxChargeTime == 0.0f)
				{
					FinishCasting();
				}
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
