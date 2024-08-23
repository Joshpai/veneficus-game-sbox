public sealed class ProjectileSpellCollisionComponent
	: Component, Component.ICollisionListener, Component.ITriggerListener
{
	[Property]
	public float ContactDamage { get; set; } = 20.0f;

	[Property]
	public float SplashDamage { get; set; } = 20.0f;

	[Property]
	public bool DoesExplode { get; set; } = true;

	public float DamageMultiplier { get; set; } = 1.0f;

	private void HandleCollision(GameObject otherObj)
	{
		if (otherObj == null)
			return;

		// TODO: this can collide with other projectiles. Maybe we should set
		// some "size" value such that bigger projectiles absorb smaller ones?
		var collisionPoint = Transform.Position;
		var hp = otherObj.Components.GetInDescendantsOrSelf<HealthComponent>();
		if (hp != null)
			hp.Damage(ContactDamage * DamageMultiplier);

		if (DoesExplode)
		{
			GameObject explosionObj = new GameObject();
			explosionObj.Transform.Position = collisionPoint;
			explosionObj.SetPrefabSource("prefabs/particles/explosion.prefab");
			explosionObj.UpdateFromPrefab();
			var explosion = explosionObj.Components.Get<ExplosionManagerComponent>();
			explosion.ExplosionOrigin = collisionPoint;
			explosion.ExplosionRadius *= DamageMultiplier;
			explosion.ExplosionDamage = SplashDamage;
			explosion.DamageMultiplier *= DamageMultiplier;
			explosion.Explode();
			// NOTE: the above should clean itself up!
		}

		GameObject.Destroy();
	}

	public void OnTriggerEnter(Collider other)
	{
		HandleCollision(other.GameObject);
	}

	public void OnCollisionStart(Collision collision)
	{
		HandleCollision(collision.Other.GameObject);
	}
}
