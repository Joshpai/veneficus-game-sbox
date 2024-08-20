public sealed class DeathTrigger : Component, Component.ITriggerListener
{
	public void OnTriggerEnter(Collider other)
	{
		var health =
			other.GameObject.Components.GetInDescendantsOrSelf<HealthComponent>();
		if (health != null)
			health.Kill();
	}
}
