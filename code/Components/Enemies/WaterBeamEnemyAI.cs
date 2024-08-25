public sealed class WaterBeamEnemyAI : BaseEnemyAI
{
	[Property]
	public HealthComponent Health { get; set; }

	// After we're in range, how long do we wait before our attack actually
	// starts?
	[Property]
	public float AttackChargeDuration { get; set; } = 0.75f;

	// How long can we continuously shoot a beam for?
	[Property]
	public float BeamDuration { get; set; } = 3.0f;

	[Property]
	public float AttackEndDuration { get; set; } = 0.79f;

	private WaterBeamSpell _beam = null;
	private float _beamChargeFinishTime = 0.0f;
	private float _beamEndFinishTime = 0.0f;
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

		var b_start_attack = _beamChargeFinishTime != 0.0f &&
							 _beamChargeFinishTime > Time.Now;
		var b_stop_attack = _beamEndFinishTime > Time.Now;
		var b_attacking = b_start_attack || b_stop_attack || _beam != null;
		// The order here matters:
		// - b_start_attack = true
		// - b_attacking = true
		// - b_start_attack = false
		// - b_stop_attack = true
		// - b_attacking = false
		// - b_stop_attack = false
		_modelRenderer.Set("b_start_attack", b_start_attack);
		if (_beamEndFinishTime != 0.0f)
		{
			_modelRenderer.Set("b_attacking", b_attacking);
			_modelRenderer.Set("b_stop_attack", b_stop_attack);
			if (!b_stop_attack)
				_beamEndFinishTime = 0.0f;
		}
		else
		{
			_modelRenderer.Set("b_stop_attack", b_stop_attack);
			_modelRenderer.Set("b_attacking", b_attacking);
		}
		_modelRenderer.Set("b_walking",
						   !_passive && !PlayerInRange(AttackRangeIdeal));

		if (_beam != null)
		{
			UpdateSpellCastDirection();
			_beam.OnUpdate();
		}

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool WantsToAttack()
	{
		return !_passive &&
				PlayerInRange(AttackRangeMax) &&
				!PlayerObscured();
	}

	private bool ShouldCastSpell()
	{
		// We must not be passive to attack, and we shouldn't cast if we
		// haven't finished casting the currently pending spell.
		return WantsToAttack() && _beam == null &&
				CanAttack() &&
				_beamChargeFinishTime <= Time.Now;
	}

	private void HandleMovement()
	{
		if (_passive || _beam != null || _beamChargeFinishTime != 0.0f)
			return;

		if (!PlayerInRange(AttackRangeIdeal) || !PlayerInVisionCone())
		{
			MoveToPlayer();
		}
	}

	private void HandleAttacks()
	{
		// If we aren't casting a spell already, and we're ready to cast it
		if (_beam == null && WantsToAttack() && CanAttack() &&
			_beamChargeFinishTime == 0.0f)
			_beamChargeFinishTime = Time.Now + AttackChargeDuration;

		if (_beam != null)
		{
			_beam.OnFixedUpdate();

			if (_beamEndTime <= Time.Now)
			{
				_beam.FinishCasting();

				SetAttackCooldown();

				_beamEndFinishTime = Time.Now + AttackEndDuration;

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
			_beamChargeFinishTime = 0.0f;
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
