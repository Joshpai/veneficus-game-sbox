public class PolymorphSpell : BaseSpell
{
	public override float ManaCost => 25.0f;
	public override float Cooldown => 2.0f;
	public override float CastTime => 0.1f;
	public override float MaxChargeTime => 0.3f;
	public override float SpellMass => 0.0f;
	public override float SpellSpeed => 0.0f;

	public override event EventHandler OnDestroy;

	private ModelRenderer _modelRenderer;
	private String _modelPath = "models/citizen_props/beachball.vmdl";
	private Model _model;
	private Model _oldModel;

	private TimeSince _timeSincePolymorphed;
	private float _spellDuration = 3.0f;

	private bool _canPolymorphBack;

	public PolymorphSpell(GameObject caster)
		: base(caster)
	{
		_model = Model.Load(_modelPath);
		_modelRenderer = _caster.Components
								.GetInDescendantsOrSelf<ModelRenderer>();
		_canPolymorphBack = false;
	}

	private void ChangeModel(Model to)
	{
		if (_modelRenderer == null)
			return;

		_oldModel = _modelRenderer.Model;
		_modelRenderer.Model = to;

		// TODO: would be nice if the smoke puff followed the player.
		var _smokePuff = new GameObject();
		_smokePuff.Transform.Position = _caster.Transform.Position;
		_smokePuff.SetPrefabSource("prefabs/SmokePuff.prefab");
		_smokePuff.UpdateFromPrefab();

		// TODO: update camera follow position to be lower
	 }

	public override void OnStartCasting()
	{
		// TODO: generic casting animation in BaseSpell?
		// Would give better indication to players for things like this spell
		// that stuff is happening but we don't really have a good way to show
		// this otherwise. Maybe each spell has it's own colour or something
		// to discriminate between them?
	}

	public override void OnFinishCasting()
	{
		_timeSincePolymorphed = 0.0f;
		_canPolymorphBack = true;
		_spellDuration *= (1 + GetChargeAmount());
		ChangeModel(_model);
	}

	public override void OnUpdate()
	{
	}

	public override void OnFixedUpdate()
	{
		if (!HasFinishedCasting && WasCancelled)
		{
			OnDestroy?.Invoke(this, EventArgs.Empty);
		}
		else if (_timeSincePolymorphed > _spellDuration && _canPolymorphBack)
		{
			ChangeModel(_oldModel);
			_canPolymorphBack = false;
			OnDestroy?.Invoke(this, EventArgs.Empty);
		}
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.Polymorph;
	}
}
