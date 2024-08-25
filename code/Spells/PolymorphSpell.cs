public class PolymorphSpell : BaseSpell
{
	const float COOLDOWN_INTO_FROG = 0.5f;
	const float COOLDOWN_INTO_HUMAN = 2.0f;
	private float _cooldown = COOLDOWN_INTO_FROG;

	public override float ManaCost => 25.0f;
	public override float Cooldown => _cooldown;
	public override float CastTime => 0.0f;
	public override float MaxChargeTime => 0.0f;
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 0.0f;
	public override bool IsStateful => true;
	public override String IconPath =>
		"materials/PlayerMaterials/Spells/polymorph.png";
	public override String SpellSound => "sounds/Spells/polymorph";
	public override ManaTakeTime TakeManaTime => ManaTakeTime.OnStartCasting;

	public override event EventHandler OnDestroy;

	private PlayerMovementController _playerMovementController;
	private ModelRenderer _modelRenderer;
	private String _modelPath = "models/PlayerModels/Frog/playerfrog.vmdl";
	private Model _nextModel;
	private Model _currentModel;

	public PolymorphSpell(GameObject caster)
		: base(caster)
	{
		if (caster == null)
			return;

		_playerMovementController =
			_caster.Components
				   .GetInDescendantsOrSelf<PlayerMovementController>();

		_modelRenderer = _caster.Components
								.GetInDescendantsOrSelf<ModelRenderer>();
		_currentModel = _modelRenderer.Model;
		_nextModel = Model.Load(_modelPath);
	}

	private void ChangeModel(Model to)
	{
		if (_modelRenderer == null)
			return;

		_modelRenderer.Model = to;

		// TODO: would be nice if the smoke puff followed the player.
		var _smokePuff = new GameObject(true, "PolymorphSmokePuff");
		_smokePuff.Transform.Position = _caster.Transform.Position;
		_smokePuff.SetPrefabSource("prefabs/particles/SmokePuff.prefab");
		_smokePuff.UpdateFromPrefab();

		if (_playerMovementController != null)
			_playerMovementController.TogglePolymorph();
	 }

	public override void OnStartCasting()
	{
		// TODO: generic casting animation in BaseSpell?
		// Would give better indication to players for things like this spell
		// that stuff is happening but we don't really have a good way to show
		// this otherwise. Maybe each spell has it's own colour or something
		// to discriminate between them?
	}

	public override bool OnFinishCasting()
	{
		_currentModel = _nextModel;
		_nextModel = _modelRenderer.Model;

		ChangeModel(_currentModel);

		_cooldown = !_playerMovementController.IsPolymorphed
				  ? COOLDOWN_INTO_HUMAN : COOLDOWN_INTO_FROG;

		return true;
	}

	public override void OnCancelCasting()
	{
	}

	public override void OnUpdate()
	{
	}

	public override bool OnFixedUpdate()
	{
		// We don't need to use these update functions, so just leave us alone
		OnDestroy?.Invoke(this, EventArgs.Empty);

		return false;
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.Polymorph;
	}
}
