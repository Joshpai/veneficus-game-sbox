public class RendingGaleSpell : BaseSpell
{
	public override float ManaCost => 10.0f;
	public override float Cooldown => 1.0f;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.0f;
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 0.0f;
	public override bool IsStateful => false;
	public override ManaTakeTime TakeManaTime => ManaTakeTime.OnStartCasting;

	public override event EventHandler OnDestroy;

	private const float BOOST_AMOUNT = 500.0f;
	// TODO: calculate this somehow?
	private const float DASH_DURATION = 0.1f;
	private PlayerMovementController _playerMovementController;
	private float _finishDashingTime;

	public RendingGaleSpell(GameObject caster)
		: base(caster)
	{
		// Dash in movement direction
		_playerMovementController =
			_caster.Components
				   .GetInDescendantsOrSelf<PlayerMovementController>();
	}

	public override void OnStartCasting()
	{
	}

	public override bool OnFinishCasting()
	{
		Vector3 direction = _playerMovementController.WishDir;
		Vector3 boost = direction * BOOST_AMOUNT;
		// TODO: also disable gravity for a short period of time
		// Think a bit more about this, as disabling gravity could lead to very
		// funky situations where any external upwards forces are no longer
		// opposed. For example, an enemy sending a fireball or other at you.
		_playerMovementController.Controller.Velocity =
			_playerMovementController.Controller.Velocity.WithZ(0.0f);
		_playerMovementController.Controller.Punch(boost);
		_playerMovementController.IsDashing = true;
		_finishDashingTime = Time.Now + DASH_DURATION;

		return true;
	}

	public override void OnCancelCasting()
	{
	}

	public override void OnUpdate()
	{
	}

	public override void OnFixedUpdate()
	{
		if (_finishDashingTime < Time.Now)
		{
			_playerMovementController.IsDashing = false;
			OnDestroy?.Invoke(this, EventArgs.Empty);
		}
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.RendingGale;
	}
}
