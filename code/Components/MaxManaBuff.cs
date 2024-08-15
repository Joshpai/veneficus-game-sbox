public sealed class MaxManaBuff : Component, Component.ITriggerListener
{
	[Property]
	public float MaxManaValue { get; set; } = 25.0f;

	public void OnTriggerEnter( Collider collider )
	{
		var other = collider.GameObject.Components;
		var player =
			other.GetInDescendantsOrSelf<PlayerSpellcastingController>();

		if (player != null)
		{
			player.MaxMana += MaxManaValue;
			GameObject.Destroy();
		}
	}
}
