public sealed class KeyTrigger : Component, Component.ITriggerListener
{
	public void OnTriggerEnter(Collider other)
	{
		var spellcasting =
			other.Components.GetInDescendantsOrSelf<PlayerSpellcastingController>();
		if (spellcasting == null)
			return;

		GameObject.Parent.Destroy();
	}
}
