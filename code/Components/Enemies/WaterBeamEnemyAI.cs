public sealed class WaterBeamEnemyAI : BaseEnemyAI
{
	[Property]
	public HealthComponent Health { get; set; }

	// How long can we continuously shoot a beam for?
	[Property, Group("Combat")]
	public float BeamDuration { get; set; } = 3.0f;

	// After we're in range, how long do we wait before our attack actually
	// starts?
	[Property, Group("Combat")]
	public float AttackChargeDuration { get; set; } = 0.5f;

	private WaterBeamSpell _beam = null;
	private float _beamChargeFinishTime = 0.0f;
	private float _beamEndTime = 0.0f;

	private void CleanupSpell()
	{
		if (_beam != null)
			_beam.FinishCasting();
	}

	protected override void OnStart()
	{
		base.OnStart();

		Health.OnDeath += CleanupSpell;
	}

	private void UpdateSpellCastDirection()
	{
		if (_beam != null)
		{
			// NOTE: this vector probably isn't normalised, but it's close
			// enough.
			Vector3 direction = Transform.Rotation.Forward;
			direction.z = GetDirectionToPlayerEyes().Normal.z;
			_beam.CastDirection = direction;
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (_beam != null)
		{
			UpdateSpellCastDirection();
			_beam.OnUpdate();
		}

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool ShouldCastSpell()
	{
		// We must not be passive to attack, and we shouldn't cast if we
		// haven't finished casting the currently pending spell.
		return !_passive && _beam == null &&
				PlayerInRange(AttackRangeMax) &&
				!PlayerObscured() &&
				CanAttack() &&
				_beamChargeFinishTime <= Time.Now;
	}

	private void HandleMovement()
	{
		if (_passive)
			return;

		if (!PlayerInRange(AttackRangeIdeal))
		{
			MoveToPlayer();
		}
		else
		{
			// If we aren't casting a spell already, and we're ready to cast it
			if (_beam == null && _beamChargeFinishTime >= Time.Now)
				// TODO: some kind of charge animation?
				_beamChargeFinishTime = Time.Now + AttackChargeDuration;

			TurnToFacePlayer();
		}
	}

	private void HandleAttacks()
	{
		if (_beam != null)
		{
			_beam.OnFixedUpdate();

			if (_beamEndTime <= Time.Now)
			{
				_beam.FinishCasting();

				SetAttackCooldown();

				if (_enemyManager != null)
					_enemyManager.AddCastSpell(_beam);

				_beam = null;
			}
		}
		else if (ShouldCastSpell())
		{
			_beam =
				(WaterBeamSpell)PlayerSpellcastingController.CreateSpell(
					GameObject, BaseSpell.SpellType.WaterBeam
				);
			_beam.CasterEyeOrigin = EyePosition;
			UpdateSpellCastDirection();
			_beam.StartCasting();
			_beamEndTime = Time.Now + BeamDuration;
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		HandleMovement();
		HandleAttacks();
	}
}
