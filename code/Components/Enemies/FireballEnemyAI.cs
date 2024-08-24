public sealed class FireballEnemyAI : BaseEnemyAI
{
	[Property]
	public float AttackAnimationLength { get; set; } = 1.67f;

	private FireballSpell _fireball = null;

	private float _attackFinishTime = 0.0f;

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

			bool shouldFinishCasting = _attackFinishTime < Time.Now;
			if (shouldFinishCasting)
			{
				_fireball.FinishCasting();
				_modelRenderer.SceneModel.CurrentSequence.Name = "";

				SetAttackCooldown();

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
			_modelRenderer.SceneModel.CurrentSequence.Name = "EFB_Attack";
			_attackFinishTime = Time.Now + AttackAnimationLength;
		}
	}
}
