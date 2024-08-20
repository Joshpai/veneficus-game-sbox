public sealed class RockArmourCollisionManager
	: Component, Component.ITriggerListener
{
	[Property]
	public float ContactDamage { get; set; } = 20.0f;

	[Property]
	public float KnockbackForce { get; set; } = 1500.0f;

	[Property]
	public float KnockupForce { get; set; } = 500.0f;

	[Property]
	public List<String> IgnoreTags { get; set; } = new List<String>();

	private HashSet<String> _ignoreTags = new HashSet<String>();

	protected override void OnStart()
	{
		base.OnStart();

		_ignoreTags = new HashSet<String>(IgnoreTags);
	}

	public void OnTriggerEnter(Collider other)
	{
		if (other == null || other.GameObject == null || _ignoreTags == null ||
			other.GameObject.Tags.HasAny(_ignoreTags))
			return;

		Vector3 knockbackDirection =
			(other.Transform.Position - Transform.Position).Normal;
		Vector3 knockback =
			knockbackDirection * KnockbackForce +
			Vector3.Up * KnockupForce;

		var player =
			other.GameObject.Components
							.GetInDescendantsOrSelf<PlayerMovementController>();
		if (player != null)
		{
			player.Controller.Punch(knockback);
		}
		else
		{
			var enemy =
				other.GameObject.Components
								.GetInDescendantsOrSelf<BaseEnemyAI>();
			if (enemy != null && enemy.Agent != null)
			{
				// TODO: this doesn't always work, and also doesn't really do
				// any knockup - debug this at some point
				enemy.Agent.Velocity = knockback;
			}
		}

		var health =
			other.GameObject.Components
							.GetInDescendantsOrSelf<HealthComponent>();
		if (health != null)
		{
			health.Damage(ContactDamage);
		}
	}
}
