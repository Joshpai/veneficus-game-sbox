public sealed class LevelEnd : InteractableComponent
{
	[Property]
	public SceneFile NextLevel { get; set; }

	// Relevant for later scenes. For example, if we want to send players to
	// the ethereal tavern at the end, but don't necessarily want to recreate
	// the tavern scene for each individual time we use it. In such cases:
	// - Set NextLevel to the tavern scene; and
	// - Set DeferredNextLevel to the "real" next level
	[Property]
	public SceneFile DeferredNextLevel { get; set; }

	public override void Interact(GameObject interacter)
	{
		Log.Info("Use LevelEnd");
		if (NextLevel != null)
		{
			LevelManager.LoadLevel(NextLevel);
		}
	}
}
