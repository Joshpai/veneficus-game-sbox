public sealed class RendingGaleEnemyAI : BaseEnemyAI
{
	[Property, Group("Combat")]
	public float AttackDamage { get; set; } = 15.0f;

	private enum State
	{
		Chase,
		Attack,
		BackOff
	}

	private State _state;
	private HealthComponent _playerHealth;

	protected override void OnStart()
	{
		base.OnStart();
		_state = State.Chase;
		if (_player != null && _player.IsValid)
			_playerHealth =
				_player.Components.GetInDescendantsOrSelf<HealthComponent>();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if (!_passive)
			TurnToFacePlayer();
	}

	private bool ShouldAttack()
	{
		return !_passive &&
				_state == State.Attack &&
				!PlayerObscured() &&
				CanAttack();
	}

	private void HandleMovement()
	{
		if (_passive)
			return;

		switch (_state)
		{
		case State.Chase:
			{
				if (!PlayerInRange(AttackRangeIdeal))
				{
					MoveToPlayer();
				}
				else
				{
					Agent.Velocity = Vector3.Zero;
					_state = State.Attack;
				}

				break;
			}
		case State.BackOff:
			{
				if (PlayerInRange(AttackRangeMax))
				{
					Vector3 moveDir =
						Transform.Position - _player.Transform.Position;
					Vector3 dest = moveDir.Normal * (AttackRangeMax + 50.0f);
					MoveTo(dest);
				}
				else
				{
					_state = State.Chase;
				}
				break;
			}
		}
	}

	private void HandleAttacks()
	{
		if (ShouldAttack())
		{
			// TODO: melee multiple times?
			// TODO: this also need some kind of animation
			if (_playerHealth != null)
				_playerHealth.Damage(AttackDamage);
			SetAttackCooldown();
			_state = State.BackOff;
		}
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		HandleMovement();
		HandleAttacks();
	}
}
