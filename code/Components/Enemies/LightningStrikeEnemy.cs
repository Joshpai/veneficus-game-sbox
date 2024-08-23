public sealed class LightningStrikeEnemyAI : BaseEnemyAI
{
	[Property]
	public HealthComponent Health { get; set; }

	[Property, Group("Combat")]
	public float DamageRadius = 100.0f;

	[Property, Group("Combat")]
	public float DamageHeight = 100.0f;

	[Property, Group("Combat")]
	public float Damage = 60.0f;

	[Property, Group("Combat")]
	public float IndicatorFollowStrength = 3.0f;

	[Property, Group("Combat")]
	public float IndicateDuration = 3.0f;

	[Property, Group("Combat")]
	public float PreCastPause = 0.3f;

	[Property, Group("Combat")]
	public float StrikeLifetime = 0.7f;

	[Property, Group("Combat")]
	public GameObject LightningStrikeEnemyIndicator = null;

	[Property, Group("Combat")]
	public GameObject LightningPrefab = null;

	private GameObject _indicator = null;
	private GameObject _lightning = null;

	private float _attackStartTime = 0.0f;

	private void CleanupSpell()
	{
		if (_indicator != null)
			_indicator.Destroy();
	}

	protected override void OnStart()
	{
		base.OnStart();

		Health.OnDeath += CleanupSpell;

		if (LightningStrikeEnemyIndicator != null)
		{
			_indicator = LightningStrikeEnemyIndicator.Clone();
			_indicator.Enabled = false;
		}
	}

	private bool IsAttacking()
	{
		return _attackStartTime > 0.0f;
	}

	private bool IsIndicatingAttack()
	{
		return _attackStartTime > 0.0f &&
			   _attackStartTime + IndicateDuration >= Time.Now;
	}

	private bool IsAttackLockedIn()
	{
		return _attackStartTime + IndicateDuration <= Time.Now &&
			   _attackStartTime + IndicateDuration + PreCastPause >= Time.Now;
	}

	private bool IsAttackReady()
	{
		var start = _attackStartTime + IndicateDuration + PreCastPause;
		return start <= Time.Now && start + StrikeLifetime >= Time.Now;
	}

	private bool IsAttackDone()
	{
		var endTime =
			_attackStartTime + IndicateDuration + PreCastPause + StrikeLifetime;
		return endTime <= Time.Now;
	}

	private Vector3? SnapVectorToGround(Vector3 pos)
	{
		var startPos = pos + Vector3.Up * 5.0f;
		var endPos = startPos + Vector3.Down * 1000.0f;
		var tr = Scene.Trace.Ray(startPos, endPos).Run();

		return (tr.Hit) ? (tr.HitPosition + Vector3.Up * 0.1f) : null;
	}

	private void UpdateIndicatorPosition()
	{
		var startPos =
			_indicator.Transform.Position.WithZ(_player.Transform.Position.z);
		var pos =
			Vector3.Lerp(
				startPos,
				_player.Transform.Position + Vector3.Up * 0.1f,
				IndicatorFollowStrength * Time.Delta
			);
		
		var groundPos = SnapVectorToGround(pos);
		if (groundPos != null)
			_indicator.Transform.Position = groundPos.Value;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (IsIndicatingAttack())
		{
			UpdateIndicatorPosition();
		}

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool ShouldCastLightningStrike()
	{
		return !_passive &&
			   _lightning == null &&
			   PlayerInRange(AttackRangeMax) &&
			   !PlayerObscured() &&
			   CanAttack();
	}

	private void HandleMovement()
	{
		if (_passive)
			return;

		if (!PlayerInRange(AttackRangeMax))
		{
			MoveToPlayer();
		}
		else
		{
			TurnToFacePlayer();
		}
	}

	private void HandleAttacks()
	{
		if (IsAttacking())
		{
			if (IsIndicatingAttack())
			{
				// Log.Info("Indicating");
			}
			else if (IsAttackLockedIn())
			{
				// Log.Info("Locked in");
			}
			else if (IsAttackReady() && _indicator.Enabled)
			{
				// Log.Info("Attacking");
				_indicator.Enabled = false;

				if (LightningPrefab != null)
				{
					_lightning = LightningPrefab.Clone();
					_lightning.Transform.Position =
						_indicator.Transform.Position;
					// TODO: give it a random rotation?
					// _lightning.Transform.Rotation = Rotation.Random;
				}

				var capBottom = _indicator.Transform.Position;
				var capTop = capBottom + Vector3.Up * DamageHeight;
				var cap = new Capsule(capBottom, capTop, DamageRadius);
				var trace = Scene.Trace
					.Capsule(cap)
					.HitTriggers()
					.RunAll();
				foreach (var hit in trace)
				{
					if (!hit.Hit)
						continue;

					var hp = hit.GameObject.Components
						.GetInDescendantsOrSelf<HealthComponent>();
					if (hp != null)
						hp.Damage(Damage);
				}
			}
			else if (IsAttackDone())
			{
				_attackStartTime = 0.0f;
				if (_lightning != null)
				{
					_lightning.Destroy();
					_lightning = null;
				}
				SetAttackCooldown();
			}
		}
		else if (ShouldCastLightningStrike())
		{
			_attackStartTime = Time.Now;
			_indicator.Enabled = true;
			var pos = _player.Transform.Position + Vector3.Up * 0.1f;
			var groundPos = SnapVectorToGround(pos);
			_indicator.Transform.Position =
				(groundPos != null) ? groundPos.Value : pos;
			_indicator.Transform.ClearInterpolation();
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		HandleMovement();
		HandleAttacks();
	}
}
