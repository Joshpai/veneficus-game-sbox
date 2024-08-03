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
