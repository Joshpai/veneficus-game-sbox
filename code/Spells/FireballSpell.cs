public class FireballSpell : BaseSpell
{
	public override float ManaCost => 50.0f;
	public override float Cooldown => 2.0f;
	public override float CastTime => 0.3f;
	public override float MaxChargeTime => 0.5f;
	public override float SpellMass => 100.0f;
	public override float SpellSpeed => 300.0f;

	public override event EventHandler OnDestroy;

	private GameObject _fireballObject;
	private TimeSince _timeSinceFireballSpawn;

	const float START_OFFSET = 50.0f;
	const float DURATION = 5.0f;

	public FireballSpell(GameObject caster)
		: base(caster)
	{

	}

	public override void OnStartCasting()
	{
		// TODO: is there a better way to use a prefab programatically?
		_fireballObject = new GameObject();
		_fireballObject.SetPrefabSource("prefabs/fireball.prefab");
		_fireballObject.UpdateFromPrefab();
		_fireballObject.Transform.Scale = 0.1f;

		// NOTE: collision is handled in FireballCollisionComponent.

		_fireballObject.SetParent(_caster);
		_fireballObject.Transform.LocalPosition =
			CasterEyeOrigin + CastDirection * START_OFFSET;
	}

	public override void OnFinishCasting()
	{
		_timeSinceFireballSpawn = 0.0f;
		_fireballObject.SetParent(null, true);
		// Reparenting currently messes with interpolation is weird ways that I
		// haven't bothered reading enough about to understand. But without the
		// below line, the object will be set to the origin for a few frames
		// and looks rubbish as it jumps around.
		_fireballObject.Transform.ClearInterpolation();

		// Update the fireball damage according to the charge amount
		// TODO: get this earlier and only enable it now? Or better handle this
		// destroying us before the spellcasting controller expects.
		var collisionComponent = _fireballObject.Components.Get<FireballCollisionComponent>();
		collisionComponent.DamageMultiplier *= (1 + GetChargeAmount());
	}

	public override void OnUpdate()
	{
		if (!HasFinishedCasting)
		{
			_fireballObject.Transform.LocalPosition =
				CasterEyeOrigin + CastDirection * START_OFFSET;
		}
	}

	public override void OnFixedUpdate()
	{
		if (!HasFinishedCasting)
		{
			// TODO: is there a nicer way to create animations
			if (WasCancelled)
			{
				_fireballObject.Transform.LocalScale -= 3.0f * Time.Delta;
				// Because the scale is the same on all axes, just check one.
				if (_fireballObject.Transform.LocalScale.x < 0.05f)
				{
					OnDestroy?.Invoke(this, EventArgs.Empty);
					_fireballObject.Destroy();
				}
			}
			else if (!IsFullyCharged())
			{
				_fireballObject.Transform.LocalScale += 1.0f * Time.Delta;
			}
		}
		else
		{
			// Despawn after 5 seconds
			if (_timeSinceFireballSpawn >= DURATION)
			{
				OnDestroy?.Invoke(this, EventArgs.Empty);
				_fireballObject.Destroy();
			}

			if (_fireballObject != null)
			{
				_fireballObject.Transform.Position +=
					CastDirection * SpellSpeed * Time.Delta;
			}
		}
	}

	public override BaseSpell.SpellType GetSpellType()
	{
		return BaseSpell.SpellType.Fireball;
	}
}
