public sealed class ExplosionManagerComponent : Component
{
	[Property]
	public Vector3 ExplosionOrigin { get; set; } = Vector3.Zero;

	[Property]
	public float ExplosionRadius { get; set; } = 10.0f;

	[Property]
	public float ExplosionDamage { get; set; } = 20.0f;

	[Property]
	public float ImpulseForce { get; set; } = 10000.0f;

	public float DamageMultiplier { get; set; } = 1.0f;

	public void Explode()
	{
		var trace =
			Scene.Trace
			     .Sphere(ExplosionRadius, ExplosionOrigin, ExplosionOrigin)
				 .RunAll();

		// TODO: should we consider "cover" when applying damage? If an entity
		// is on the other side of a thick wall but the explosion radius gets
		// them then they will take full damage.
		// TODO: also add some distance-based damage drop-off?
		// TODO: add some knockback to all hit entities.
		// TODO: create custom particles (using placeholder currently)

		foreach (var hit in trace)
		{
			if (!hit.Hit)
				continue;

			Vector3 explosionToHit = hit.GameObject.Transform.Position
								   - ExplosionOrigin;

			var hp = hit.GameObject.Components
								   .GetInDescendantsOrSelf<HealthComponent>();
			if (hp != null)
				hp.Damage(ExplosionDamage * DamageMultiplier);

			var rigidBody = hit.GameObject.Components
										  .GetInDescendantsOrSelf<Rigidbody>();
			if (rigidBody != null)
				rigidBody.ApplyImpulse(explosionToHit * ImpulseForce);
		}
	}
}
