public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerMovementController PlayerMovementControllerRef { get; set; }

	// NOTE: The default value for this must be set in SaveDataFormat, as we
	// will end up overwriting this value if we have it as a property!
	public float MaxMana
	{
		get { return SaveData.Instance.Data.MaxMana; }
		set {
			SaveData.Instance.Data.MaxMana = value;
			SaveData.Save();
		}
	}

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

	private UInt64 _unlockedSpellsMask {
		get { return SaveData.Instance.Data.UnlockedSpells; }
		set {
			SaveData.Instance.Data.UnlockedSpells = value;
			SaveData.Save();
		}
	}

	private List<BaseSpell.SpellType> _availableSpells;
	private int _selectedSpellIdx;

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

		// _unlockedSpellsMask = 0xfffffffffffffffful;
		UpdateUnlockedSpells();
		_selectedSpellIdx = 0;

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
		// TODO: this really belongs in BaseSpell or something like that but
		// I have unilaterally decided that it is too much effort to move it :)
		return spellType switch
		{
			BaseSpell.SpellType.Polymorph => new PolymorphSpell(caster),
			BaseSpell.SpellType.MagicMissile => new MagicMissileSpell(caster),
			BaseSpell.SpellType.Fireball => new FireballSpell(caster),
			BaseSpell.SpellType.WaterBeam => new WaterBeamSpell(caster),
			BaseSpell.SpellType.RendingGale => new RendingGaleSpell(caster),
			BaseSpell.SpellType.MagicBarrier => new MagicBarrierSpell(caster),
			BaseSpell.SpellType.RockArmour => new RockArmourSpell(caster),
			BaseSpell.SpellType.LightningStrike => new LightningStrikeSpell(caster),
			_ => null,
		};
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		_deferredRemovals.Add((BaseSpell)spell);
	}

	public bool IsSpellBlocked(BaseSpell.SpellType spellType)
	{
		// Player is polymorphed implies only able to cast polymorph
		return (PlayerMovementControllerRef.IsPolymorphed &&
				spellType != BaseSpell.SpellType.Polymorph);
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
			   // Sufficient mana to cast
			   Mana >= _spellBuffer[(int)spellType].ManaCost &&
			   !IsSpellBlocked(spellType);
	}

	public void SetSpellUnlocked(BaseSpell.SpellType spellType, bool unlocked)
	{
		if (unlocked)
			_unlockedSpellsMask |= (1ul << (int)spellType);
		else
			_unlockedSpellsMask &= ~(1ul << (int)spellType);

		SaveData.Instance.Data.UnlockedSpells = _unlockedSpellsMask;

		UpdateUnlockedSpells();
	}

	public bool IsSpellUnlocked(BaseSpell.SpellType spellType)
	{
		return (_unlockedSpellsMask & (1ul << (int)spellType)) != 0;
	}

	private void UpdateUnlockedSpells()
	{
		_availableSpells = new List<BaseSpell.SpellType>();
		for (int i = (int)BaseSpell.SpellType.SpellTypeMin + 1;
			 i < (int)BaseSpell.SpellType.SpellTypeMax; i++)
		{
			if (IsSpellUnlocked((BaseSpell.SpellType)i))
			{
				_availableSpells.Add((BaseSpell.SpellType)i);
			}
		}
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

	public float GetSpellCooldownMax(BaseSpell.SpellType spellType)
	{
		return _spellBuffer[(int)spellType].Cooldown;
	}

	public float GetSpellCooldownPercent(BaseSpell.SpellType spellType)
	{
		return GetSpellCooldown(spellType) / GetSpellCooldownMax(spellType);
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

	private void SelectSpellSlot(int slot)
	{
		_selectedSpellIdx = slot;
		ActiveSpell = _availableSpells[_selectedSpellIdx];
	}

	private void SelectNextUnlockedSpell()
	{
		int slot = (_selectedSpellIdx + 1 < _availableSpells.Count)
				 ? _selectedSpellIdx + 1 : 0;
		SelectSpellSlot(slot);
	}

	private void SelectPrevUnlockedSpell()
	{
		int slot = (_selectedSpellIdx - 1 >= 0)
				 ? _selectedSpellIdx - 1 : _availableSpells.Count - 1;
		SelectSpellSlot(slot);
	}

	private void StartCasting()
	{
		_castingSpell = _spellBuffer[(int)ActiveSpell];
		// Stateful spells are singletons, so only recreate spells that
		// aren't stateful!
		if (!_spellBuffer[(int)ActiveSpell].IsStateful)
			_spellBuffer[(int)ActiveSpell] =
				CreateSpell(GameObject, ActiveSpell);

		if (_castingSpell.TakeManaTime == BaseSpell.ManaTakeTime.OnStartCasting)
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
		if (!_castingSpell.FinishCasting())
		{
			_castingSpell = null;
			return;
		}

		if (_castingSpell.TakeManaTime == BaseSpell.ManaTakeTime.OnFinishCasting)
			Mana -= _castingSpell.ManaCost;

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
		_spellNextCastTime[(int)_castingSpell.GetSpellType()] =
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

	private void HandleSpellSelection()
	{
		var mouseScroll = Input.MouseWheel;
		if (Input.Pressed("SlotNext") || mouseScroll.y < 0)
			SelectNextUnlockedSpell();
		else if (Input.Pressed("SlotPrev") || mouseScroll.y > 0)
			SelectPrevUnlockedSpell();

		for (int slot = 0; slot < _availableSpells.Count; slot++)
		{
			if (Input.Pressed($"slot{slot + 1}"))
			{
				SelectSpellSlot(slot);
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		if (!PlayerMovementControllerRef.LevelStarted)
			return;

		HandleSpellSelection();

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
