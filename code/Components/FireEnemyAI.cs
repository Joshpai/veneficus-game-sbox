public sealed class FireEnemyAI : BaseEnemyAI
{
	private FireballSpell _fireball = null;

	protected override void OnStart()
	{
		base.OnStart();
	}

	private void UpdateSpellCastDirection()
	{
		// NOTE: unlike the player controller, we just want to cast spells
		// "forwards" as we will always try to face the place we want to shoot
		// with our body (this is seperate in the player).
		if (_fireball != null)
			_fireball.CastDirection = Transform.Rotation.Forward;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		// NOTE: because we are always shooting forwards (see above), we don't
		// actually need to update the fireball, and this will actually save
		// us from the double spinning issue.
		// if (_fireball != null)
		// {
		// 	UpdateSpellCastDirection();
		// 	_fireball.OnUpdate();
		// }

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool ShouldCastFireball()
	{
		return _fireball == null &&
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
