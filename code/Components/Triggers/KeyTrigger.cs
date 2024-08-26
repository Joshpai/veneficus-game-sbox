public sealed class KeyTrigger : Component, Component.ITriggerListener
{
	public void OnTriggerEnter(Collider other)
	{
		var spellcasting =
			other.Components.GetInDescendantsOrSelf<PlayerSpellcastingController>();
		if (spellcasting == null)
			return;

		LevelManagerStaticStore.UsedObjects.Add(GameObject.Parent.Id);
		GameObject.Parent.Destroy();
	}
}
