public sealed class FireEnemyAI : BaseEnemyAI
{
	private FireballSpell _fireball = null;

	protected override void OnStart()
	{
		base.OnStart();
	}

	private void UpdateSpellCastDirection()
	{
		if (_fireball != null)
			_fireball.CastDirection = GetDirectionToPlayerEyes().Normal;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (_fireball != null)
		{
			UpdateSpellCastDirection();
			_fireball.OnUpdate();
		}

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool ShouldCastFireball()
	{
		return !_passive &&
			   _fireball == null &&
			   PlayerInRange(AttackRangeMax) &&
			   !PlayerObscured() &&
			   CanAttack();
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if (_fireball != null)
		{
			_fireball.OnFixedUpdate();

			bool shouldFinishCasting = true;
			if (shouldFinishCasting)
			{
				_fireball.FinishCasting();

				if (_enemyManager != null)
					_enemyManager.AddCastSpell(_fireball);

				_fireball = null;
			}
		}
		else if (ShouldCastFireball())
		{
			_fireball =
				(FireballSpell)PlayerSpellcastingController.CreateSpell(
					GameObject, BaseSpell.SpellType.Fireball
				);
			_fireball.CasterEyeOrigin = EyePosition;
			// TODO: prediction?
			UpdateSpellCastDirection();
			_fireball.StartCasting();

			SetAttackCooldown();
		}
	}
}
