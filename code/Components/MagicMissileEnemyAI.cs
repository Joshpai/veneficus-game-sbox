public sealed class MagicMissileEnemyAI : BaseEnemyAI
{
	[Property, Group("Combat")]
	public int BurstCount { get; set; } = 3;

	[Property, Group("Combat")]
	public float TimeBetweenBurstShots { get; set; } = 0.15f;

	private MagicMissileSpell _missile = null;

	private float _nextBurstShotTime = 0.0f;
	private int _remainingBurstShots = 0;

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

			// TODO: consider what we should do for determining how long this
			// enemy type should charge the fireball for.
			bool shouldFinishCasting = true;
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
				_nextBurstShotTime = Time.Now + TimeBetweenBurstShots;
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
