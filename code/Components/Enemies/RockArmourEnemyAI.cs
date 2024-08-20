public sealed class RockArmourEnemyAI : BaseEnemyAI
{
	// How much velocity do we put into a charge
	[Property, Group("Combat")]
	public float ChargePower { get; set; } = 400.0f;

	[Property, Group("Combat")]
	public float ChargeStartupDuration { get; set; } = 0.3f;

	[Property, Group("Combat")]
	public float ChargeSlowdownDuration { get; set; } = 1.0f;

	// If the player height is larger than this, then don't charge
	[Property, Group("Combat")]
	public float ChargeMaxHeightDifference { get; set; } = 16.0f;

	private RockArmourCollisionManager _collisionManager = null;

	private float _maxSpeed = 0.0f;

	enum ChargeState
	{
		None,
		Speedup,
		Charge,
		Slowdown
	};

	private ChargeState _chargeState = ChargeState.None;
	private float _chargeTime = 0.0f;

	private float _lastDistanceToPlayer = 0.0f;

	protected override void OnStart()
	{
		base.OnStart();

		// We only want this enabled while we're charging!
		_collisionManager =
			GameObject.Components
					  .GetInDescendantsOrSelf<RockArmourCollisionManager>();
		if (_collisionManager != null)
			_collisionManager.Enabled = false;

		_maxSpeed = Agent.MaxSpeed;
	}

	protected override void OnUpdate()
	{
		// base.OnUpdate();
		Agent.UpdateRotation =
			(_chargeState == ChargeState.None ||
			 _chargeState == ChargeState.Speedup);

		if (!Agent.UpdateRotation)
			LookInDirection(Agent.Velocity);
	}

	private bool ShouldCharge()
	{
		return !_passive &&
				PlayerInRange(AttackRangeMax) &&
				!PlayerObscured() &&
				CanAttack() &&
				(GetDirectionToPlayerEyes().z < ChargeMaxHeightDifference);
	}

	private void HandleMovement()
	{
		if (_passive)
			return;

		var vectorToPlayer = GetDirectionToPlayerEyes().WithZ(0.0f);

		if (_chargeState != ChargeState.None)
		{
			if (_chargeState == ChargeState.Speedup &&
				_chargeTime < Time.Now)
			{
				_chargeState = ChargeState.Charge;
			}
			else if (_chargeState == ChargeState.Charge &&
					 _lastDistanceToPlayer < vectorToPlayer.LengthSquared)
			{
				_chargeState = ChargeState.Slowdown;
				_chargeTime = Time.Now + ChargeSlowdownDuration;
			}
			else if (_chargeState == ChargeState.Slowdown &&
					 _chargeTime < Time.Now)
			{
				_chargeState = ChargeState.None;
			}

			switch (_chargeState)
			{
			case ChargeState.Speedup:
				{
					Vector3 chargeDirection = vectorToPlayer.Normal;
					Vector3 maxCharge = (chargeDirection * ChargePower);
					Vector3 chargeVelocity =
						maxCharge * Time.Delta / ChargeStartupDuration;

					Agent.MaxSpeed = ChargePower;
					Agent.Velocity += chargeVelocity;
					break;
				}
			case ChargeState.Charge:
				{
					// do nothing
					break;
				}
			case ChargeState.Slowdown:
				{
					var maxSpeedAmount =
						(ChargeSlowdownDuration - (_chargeTime - Time.Now))
							/ ChargeSlowdownDuration;
					Agent.MaxSpeed =
						MathX.Lerp(ChargePower, _maxSpeed, maxSpeedAmount);
					if (Agent.Velocity.LengthSquared > 1.0f)
						Agent.Velocity =
							Agent.Velocity * (Agent.MaxSpeed / Agent.Velocity.Length);
					break;
				}
			case ChargeState.None:
				{
					_chargeState = ChargeState.None;
					SetAttackCooldown();
					if (_collisionManager != null)
						_collisionManager.Enabled = false;
					break;
				}
			}
		}
		else if (ShouldCharge())
		{
			_chargeState = ChargeState.Speedup;
			_chargeTime = Time.Now + ChargeStartupDuration;
			if (_collisionManager != null)
				_collisionManager.Enabled = true;
		}
		// TODO: should this be Max?
		else if (!PlayerInRange(AttackRangeIdeal))
		{
			Agent.MaxSpeed = _maxSpeed;
			MoveToPlayer();
		}

		_lastDistanceToPlayer = vectorToPlayer.LengthSquared;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		// Movement is attacking in the eyes of this enemy.
		HandleMovement();
	}
}
