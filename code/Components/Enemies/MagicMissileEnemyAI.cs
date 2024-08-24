public sealed class MagicMissileEnemyAI : BaseEnemyAI
{
	[Property]
	public float AttackAnimationLength { get; set; } = 4.5f;

	[Property, Group("Combat")]
	public int BurstCount { get; set; } = 3;

	[Property, Group("Combat")]
	public float TimeBetweenBurstShots { get; set; } = 0.15f;

	private MagicMissileSpell _missile = null;

	private float _nextBurstShotTime = 0.0f;
	private int _remainingBurstShots = 0;

	private float _finishSpellTime = 0.0f;

	protected override void OnStart()
	{
		base.OnStart();
	}

	private void UpdateSpellCastDirection()
	{
		if (_missile != null)
			_missile.CastDirection = GetDirectionToPlayerEyes().Normal;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		_modelRenderer.Set("b_attacking", _missile != null);
		// TODO: maybe a derived variable would be better here
		_modelRenderer.Set("b_walking",
						   !_passive && !PlayerInRange(AttackRangeIdeal));

		if (_missile != null)
		{
			UpdateSpellCastDirection();
			_missile.OnUpdate();
		}

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool ShouldCastSpell()
	{
		// We must not be passive to attack, and we shouldn't cast if we
		// haven't finished casting the currently pending spell.
		return !_passive && _missile == null &&
			   (
					// Either we have already started a burst of shots (in
					// which case we want to finish it regardless).
					(
						_remainingBurstShots > 0 &&
						_nextBurstShotTime <= Time.Now
					)
					||
					// Or we aren't in the middle of a burst, but we do
					// actually want to start a new burst.
					(
						_remainingBurstShots == 0 &&
						PlayerInRange(AttackRangeMax) &&
						!PlayerObscured() &&
						CanAttack()
					)
			   );
	}

	private void HandleMovement()
	{
		if (_passive)
			return;

		// HACK: temporary measure: I haven't worked out how to use bone masks
		// so please I beg do not attack and move!!!!
		if (_missile != null)
		{
			Agent.Velocity = Vector3.Zero;
			TurnToFacePlayer();
		}
		if (!PlayerInRange(AttackRangeIdeal))
			MoveToPlayer();
		else
			TurnToFacePlayer();
	}

	private void HandleAttacks()
	{
		if (_missile != null)
		{
			_missile.OnFixedUpdate();

			bool shouldFinishCasting = _finishSpellTime <= Time.Now;
			if (shouldFinishCasting)
			{
				_missile.FinishCasting();

				if (_enemyManager != null)
					_enemyManager.AddCastSpell(_missile);

				_missile = null;
			}
		}
		else if (ShouldCastSpell())
		{
			_missile =
				(MagicMissileSpell)PlayerSpellcastingController.CreateSpell(
					GameObject, BaseSpell.SpellType.MagicMissile
				);
			_missile.CasterEyeOrigin = EyePosition;
			UpdateSpellCastDirection();
			_missile.StartCasting();

			// We are starting a new burst
			if (_remainingBurstShots == 0)
			{
				// We have already shot one!
				_remainingBurstShots = BurstCount - 1;
				_finishSpellTime = Time.Now + AttackAnimationLength;
				_nextBurstShotTime = Time.Now + AttackAnimationLength
											  + TimeBetweenBurstShots;
			}
			else
			{
				_remainingBurstShots--;

				if (_remainingBurstShots == 0)
					SetAttackCooldown();
				else
					_nextBurstShotTime = Time.Now + TimeBetweenBurstShots;
			}
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		HandleMovement();
		HandleAttacks();
	}
}
