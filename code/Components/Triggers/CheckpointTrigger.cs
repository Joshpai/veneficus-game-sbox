public sealed class CheckpointTrigger : Component, Component.ITriggerListener
{
	[Property]
	public GameObject RespawnPointMarker { get; set; }

	public void OnTriggerEnter(Collider other)
	{
		// TODO: is there a better way to detect a player here?
		var playerController =
			other.Components.GetInDescendantsOrSelf<PlayerMovementController>();
		if (playerController == null)
			return;

		// Log.Info("Checkpoint");

		LevelManagerStaticStore.UsedObjects.Add(GameObject.Parent.Id);

		// Update the save point data
		LevelManagerStaticStore.CheckpointData.UsedObjects =
			new HashSet<Guid>(LevelManagerStaticStore.UsedObjects);
		LevelManagerStaticStore.CheckpointData.RespawnPoint =
			RespawnPointMarker;
		LevelManagerStaticStore.CheckpointData.EnemiesKilled =
			LevelManagerStaticStore.Stats.EnemiesKilled;

		// Trigger only once
		Enabled = false;
	}
}
