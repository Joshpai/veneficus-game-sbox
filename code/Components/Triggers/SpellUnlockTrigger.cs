public sealed class SpellUnlockTrigger : Component, Component.ITriggerListener
{
	[Property]
	public BaseSpell.SpellType UnlockSpell { get; set; }

	public void OnTriggerEnter(Collider other)
	{
		var spellcasting =
			other.Components.GetInDescendantsOrSelf<PlayerSpellcastingController>();
		if (spellcasting == null)
			return;

		LevelManagerStaticStore.UsedObjects.Add(GameObject.Parent.Id);
		spellcasting.SetSpellUnlocked(UnlockSpell, true);

		// Trigger only once
		Enabled = false;
	}
}
