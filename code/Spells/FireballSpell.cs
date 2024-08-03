public class FireballSpell : ISpell
{
	public float ManaCost => 50.0f;
	public float Cooldown => 2.0f;
	public float CastTime => 0.3f;

	public event EventHandler OnDestroy;

	private GameObject _fireballObject;
	private TimeSince _timeSincefireballSpawn;

	private Vector3 _direction;

	private bool FinishedCasting = false;

	const float START_OFFSET = 50.0f;
	const float SPEED = 300.0f;
	const float DURATION = 5.0f;

	void ISpell.StartCasting(PlayerController playerController)
	{
		// I would prefer prefab instantiation here instead...
		_fireballObject = new GameObject();
		var model = _fireballObject.Components.Create<ModelRenderer>();
		model.Model = Model.Sphere;
		model.Tint = Color.Red;
		_fireballObject.Transform.Scale = 0.1f;

		_direction = playerController.EyeAngles.Forward;
		_fireballObject.Transform.Position =
			playerController.Body.Transform.Position +
			playerController.EyePosition +
			_direction * START_OFFSET;
	}

	void ISpell.FinishCasting(PlayerController playerController)
	{
		FinishedCasting = true;
		_timeSincefireballSpawn = 0.0f;
	}

	void ISpell.OnFixedUpdate()
	{
		if (!FinishedCasting && _fireballObject != null)
		{
			// Grow in size
			_fireballObject.Transform.Scale += 1.0f * Time.Delta;
		}
		else
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

	}

	ISpell.SpellType ISpell.GetSpellType()
	{
		return ISpell.SpellType.Fireball;
	}
}
