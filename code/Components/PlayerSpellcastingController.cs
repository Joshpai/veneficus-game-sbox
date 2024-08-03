// TODO: move this elsewhere
public interface ISpell
{
	enum SpellType
	{
		Fireball,
		// The below must be at the end, so add new types above here!
		SpellTypeMax
	}

	public float ManaCost { get; }
	public float Cooldown { get; }

	public event EventHandler OnDestroy;

	public void Cast(PlayerController playerController);
	public void OnFixedUpdate();
	// This should just help avoid forgetting to create a SpellType.
	public SpellType GetSpellType();
}

public class FireballSpell : ISpell
{
	public float ManaCost => 50.0f;
	public float Cooldown => 2.0f;

	public event EventHandler OnDestroy;

	private GameObject _fireballObject;
	private TimeSince _timeSincefireballSpawn;

	private Vector3 _direction;

	const float START_OFFSET = 50.0f;
	const float SPEED = 300.0f;
	const float DURATION = 5.0f;

	void ISpell.Cast(PlayerController playerController)
	{
		// I would prefer prefab instantiation here instead...
		_fireballObject = new GameObject();
		var model = _fireballObject.Components.Create<ModelRenderer>();
		model.Model = Model.Sphere;
		model.Tint = new Color(255, 0, 0);
		_timeSincefireballSpawn = 0;

		_direction = playerController.EyeAngles.Forward;
		_fireballObject.Transform.Position =
			playerController.Body.Transform.Position +
			playerController.EyePosition +
			_direction * START_OFFSET;
	}

	void ISpell.OnFixedUpdate()
	{
		// Despawn after 5 seconds
		if (_timeSincefireballSpawn >= DURATION)
		{
			OnDestroy?.Invoke(this, EventArgs.Empty);
			_fireballObject.Destroy();
		}

		if (_fireballObject != null)
		{
			_fireballObject.Transform.Position +=
				_direction * SPEED * Time.Delta;
		}
	}

	ISpell.SpellType ISpell.GetSpellType()
	{
		return ISpell.SpellType.Fireball;
	}
}

public sealed class PlayerSpellcastingController : Component
{
	[Property]
	public PlayerController PlayerController { get; set; }

	private TimeSince _timeSinceLastSpell = 0.0f;

	// NOTE: setting this internally MUST make sure it's valid.
	private ISpell.SpellType _activeSpell = ISpell.SpellType.Fireball;

	// TODO: is there a better data type for this?
	private List<ISpell> castSpells = new List<ISpell>();
	// Defer removal so we can avoid locks (blegh)
	private List<ISpell> deferredRemovals = new List<ISpell>();

	private float[] _spellNextCastTime;

	protected override void OnStart()
	{
		// This should be zeroed by definition.
		_spellNextCastTime = new float[(int)ISpell.SpellType.SpellTypeMax];
	}

	private ISpell CreateSpell(ISpell.SpellType spellType)
	{
		switch (spellType)
		{
		case ISpell.SpellType.Fireball: return new FireballSpell();
		}
		return null;
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		deferredRemovals.Add((ISpell)spell);
	}

	protected override void OnFixedUpdate()
	{
		// TODO: we should also consider mana cost here
		if (Input.Pressed("attack1") &&
			_spellNextCastTime[(int)_activeSpell] <= Time.Now)
		{
			ISpell spell = CreateSpell(_activeSpell);
			spell.Cast(PlayerController);
			spell.OnDestroy += OnSpellDestroyed;
			castSpells.Add(spell);
			_spellNextCastTime[(int)_activeSpell] = Time.Now + spell.Cooldown;
		}

		foreach (ISpell spell in castSpells)
		{
			spell.OnFixedUpdate();
		}

		// spell.OnFixedUpdate() can result in a spell deleting itself, thus
		// invalidating the iterator so we defer real removal until afterwards
		foreach (ISpell spell in deferredRemovals)
		{
			castSpells.Remove(spell);
		}
	}
}
