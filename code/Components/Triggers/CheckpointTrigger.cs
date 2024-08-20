public sealed class CheckpointTrigger : Component, Component.ITriggerListener
{
	[Property]
	public GameObject RespawnPointMarker { get; set; }

	public void OnTriggerEnter(Collider other)
	{
		Log.Info("Checkpoint");

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
