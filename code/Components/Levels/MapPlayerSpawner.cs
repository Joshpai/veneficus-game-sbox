// NOTE: can be used as a quick hack, but best just use LevelManager
public sealed class MapPlayerSpawner : Component
{
	private MapInstance _map;

	protected override void OnEnabled()
	{
		if (this == null)
			return;

		_map = Components.Get<MapInstance>();

		if (_map != null)
		{
			_map.OnMapLoaded += SpawnPlayer;

			if (_map.IsLoaded)
			{
				SpawnPlayer();
			}
		}
	}

	protected override void OnDisabled()
	{
		if (_map != null)
		{
			_map.OnMapLoaded -= SpawnPlayer;
		}
	}

	void SpawnPlayer()
	{
		var player = new GameObject(true, "Player");
		player.SetPrefabSource("prefabs/objects/player.prefab");
		player.UpdateFromPrefab();

		var controller = player.Components.Get<PlayerMovementController>();
		if (controller == null)
			return;

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>();
		foreach (var spawn in spawnPoints)
		{
		    controller.Transform.Position = spawn.Transform.Position;
		    controller.EyeAngles = spawn.Transform.Rotation.Angles();
			break;
		}
	}
}
