public abstract class ProjectileSpell : BaseSpell
{
	public override bool IsStateful => false;
	public override event EventHandler OnDestroy;

	public abstract String ProjectilePrefabPath { get; }
	public abstract float ProjectileScale { get; }
	public abstract float StartOffset { get; }
	public abstract float Duration { get; }

	protected GameObject _projectileObject;
	protected ProjectileSpellCollisionComponent _collisionComponent;
	protected TimeSince _timeSinceProjectileSpawn;

	public ProjectileSpell(GameObject caster)
		: base(caster)
	{

	}

	public override void OnStartCasting()
	{
		_projectileObject = new GameObject();
		_projectileObject.SetPrefabSource(ProjectilePrefabPath);
		_projectileObject.UpdateFromPrefab();
		_projectileObject.Transform.Scale = ProjectileScale;
		_projectileObject.Transform.LocalRotation =
			CastDirection.EulerAngles;


		// NOTE: collision is handled in ProjectileSpellCollisionComponent.
		_collisionComponent =
			_projectileObject.Components.Get<ProjectileSpellCollisionComponent>();
		if (_collisionComponent != null)
			_collisionComponent.Enabled = false;

		_projectileObject.SetParent(_caster);
		_projectileObject.Transform.LocalPosition =
			CasterEyeOrigin + CastDirection * StartOffset;
	}

	public override void OnFinishCasting()
	{
		_timeSinceProjectileSpawn = 0.0f;
		_projectileObject.SetParent(null, true);
		// Reparenting currently messes with interpolation is weird ways that I
		// haven't bothered reading enough about to understand. But without the
		// below line, the object will be set to the origin for a few frames
		// and looks rubbish as it jumps around.
		_projectileObject.Transform.ClearInterpolation();

		// Update the fireball damage according to the charge amount
		if (_collisionComponent != null)
		{
			_collisionComponent.Enabled = true;
			_collisionComponent.DamageMultiplier *= (1 + GetChargeAmount());
		}
	}

	public override void OnUpdate()
	{
		if (!HasFinishedCasting)
		{
			_projectileObject.Transform.LocalPosition =
				CasterEyeOrigin + CastDirection * StartOffset;
		}
	}

	public override void OnFixedUpdate()
	{
		if (!HasFinishedCasting)
		{
			// TODO: is there a nicer way to create animations
			if (WasCancelled)
			{
				_projectileObject.Transform.LocalScale -= 3.0f * Time.Delta;
				// Because the scale is the same on all axes, just check one.
				if (_projectileObject.Transform.LocalScale.x < 0.05f)
				{
					OnDestroy?.Invoke(this, EventArgs.Empty);
					_projectileObject.Destroy();
				}
			}
			else if (!IsFullyCharged())
			{
				_projectileObject.Transform.LocalScale += 1.0f * Time.Delta;
			}
		}
		else
		{
			// Despawn after 5 seconds
			if (_timeSinceProjectileSpawn >= Duration)
			{
				OnDestroy?.Invoke(this, EventArgs.Empty);
				_projectileObject.Destroy();
			}

			if (_projectileObject != null)
			{
				_projectileObject.Transform.Position +=
					CastDirection * SpellSpeed * Time.Delta;
			}
		}
	}
}
