public class LevelManagerStaticStore
{
	public static LevelManager Instance { get; set; }
	public static bool IsLoading { get; set; }
}

public sealed class LevelManager : Component
{
	[Property]
	public SceneFile StartingLevel { get; set; }

	[Property]
	public GameObject LoadingScreen { get; set; }

	public static void LoadLevel(SceneFile newScene)
	{
		LevelManagerStaticStore.Instance.LoadLevelInternal(newScene);
	}

	void SpawnPlayer()
	{
		var player = new GameObject();
		player.SetPrefabSource("prefabs/player.prefab");
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

	private void LoadLevelInternal(SceneFile newScene)
	{
		if (Scene.IsLoading)
			return;

		LevelManagerStaticStore.IsLoading = true;

		var oldTimeScale = newScene.SceneProperties.ContainsKey("TimeScale")
						 ? newScene.SceneProperties["TimeScale"].GetValue<float>()
						 : 1.0f;
		newScene.SceneProperties.Remove("TimeScale");
		Scene.TimeScale = 0.0f;

		var loadingScreen = LoadingScreen.Clone();
		loadingScreen.Flags |= GameObjectFlags.DontDestroyOnLoad;

		SceneLoadOptions options = new SceneLoadOptions();
		options.SetScene(newScene);
		options.ShowLoadingScreen = false;
		options.DeleteEverything = false;

		Scene.Load(options);

		SpawnPlayer();

		loadingScreen.DestroyImmediate();
		LevelManagerStaticStore.IsLoading = false;
		Scene.TimeScale = oldTimeScale;
	}

	protected override void OnStart()
	{
		LevelManagerStaticStore.Instance = this;
		GameObject.Flags |= GameObjectFlags.DontDestroyOnLoad;
		if (!Scene.IsLoading && StartingLevel != null)
		{
			LoadLevelInternal(StartingLevel);
		}
	}
}
