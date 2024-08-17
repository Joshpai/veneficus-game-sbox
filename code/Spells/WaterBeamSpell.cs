public class WaterBeamSpell : BaseSpell
{
	// BaseSpell
	public override float ManaCost => 0.5f;
	public override float Cooldown => 0.3f;
	public override float CastTime => 0.0f;
	// HACK: this spell doesn't charge but we don't want it to be an "instant"
	// spell, so set a non-zero charge time even though we don't care/use it.
	// We really should just have another property, e.g., bool InstantCast.
	public override float MaxChargeTime => 0.1f;
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 700.0f;
	// TODO: this property might be better being "IsInstance", we don't care
	// here how we're treated, so really the less resource intensive version
	// should be used (i.e., set stateful).
	public override bool IsStateful => true;
	public override ManaTakeTime TakeManaTime => ManaTakeTime.OnStartCasting;

	public override event EventHandler OnDestroy;

	private PlayerMovementController _playerMovementController;
	private PlayerSpellcastingController _playerSpellcastingController;

	private const float MAX_RANGE = 500.0f;
	private const float TRACE_WIDTH = 10.0f;
	private const float TIME_BETWEEN_DAMAGE = 0.05f;
	private const float DAMAGE_PER_PROC = 1.0f;
	private const float START_OFFSET = 1.0f;
	private String WATER_BEAM_PREFAB = "prefabs/water_beam.prefab";

	private float _nextDamageTime = 0.0f;

	private GameObject _waterBeam;

	public WaterBeamSpell(GameObject caster)
		: base(caster)
	{
		_playerMovementController =
			_caster.Components
				   .GetInDescendantsOrSelf<PlayerMovementController>();
		_playerSpellcastingController =
			_caster.Components
				   .GetInDescendantsOrSelf<PlayerSpellcastingController>();
	}

	private void UpdateWaterBeamTransform()
	{
		_waterBeam.Transform.LocalPosition =
			_playerMovementController.EyePosition +
			_playerMovementController.EyeAngles.Forward * START_OFFSET;
		_waterBeam.Transform.Rotation = _playerMovementController.EyeAngles;
	}

	public override void OnStartCasting()
	{
		_waterBeam = new GameObject(true, "WaterBeam");
		_waterBeam.SetPrefabSource(WATER_BEAM_PREFAB);
		_waterBeam.UpdateFromPrefab();
		_waterBeam.Transform.Scale = new Vector3(1.0f, 1.0f, 1.0f);

		_waterBeam.SetParent(_caster);
		_waterBeam.Transform.ClearInterpolation();
		UpdateWaterBeamTransform();
	}

	public override bool OnFinishCasting()
	{
		return true;
	}

	public override void OnCancelCasting()
	{
	}

	public override void OnUpdate()
	{
		UpdateWaterBeamTransform();
		// TODO: update water beam length
	}

	private SceneTraceResult RunEyeTrace()
	{
		Vector3 startPos =
			_caster.Transform.Position + _playerMovementController.EyePosition;
		Vector3 endPos =
			startPos + _playerMovementController.EyeAngles.Forward * MAX_RANGE;

		return _caster.Scene.Trace.Ray(startPos, endPos)
								  .IgnoreGameObjectHierarchy(_caster)
								  .Size(TRACE_WIDTH)
								  .Run();

	}

	public override void OnFixedUpdate()
	{
		if (HasFinishedCasting)
		{
			OnDestroy?.Invoke(this, EventArgs.Empty);
			_waterBeam.Destroy();
		}

		if (_nextDamageTime >= Time.Now)
			return;

		SceneTraceResult tr = RunEyeTrace();
		if (tr.Hit)
		{
			var hp = tr.GameObject.Components
								   .GetInDescendantsOrSelf<HealthComponent>();
			if (hp != null)
				hp.Damage(DAMAGE_PER_PROC);
		}

		_playerSpellcastingController.Mana -= ManaCost;

		if (_playerSpellcastingController.Mana <= 0.0f)
		{
			_playerSpellcastingController.Mana = 0.0f;
			OnFinishCasting();
		}

		_nextDamageTime = Time.Now + TIME_BETWEEN_DAMAGE;
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.WaterBeam;
	}
}
