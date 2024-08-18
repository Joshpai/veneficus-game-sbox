public class LevelStats
{
	public float LevelStartTime { get; set; } = 0.0f;
	public int EnemiesKilled { get; set; } = 0;
	public int DeathCount { get; set; } = 0;
}

public class LevelManagerStaticStore
{
	public static LevelManager Instance { get; set; }
	public static SceneFile ActiveScene { get; set; }
	public static bool IsLoading { get; set; }
	public static SaveData SaveDataInstance { get; set; }
	public static LevelStats Stats { get; set; }
}

public sealed class LevelManager : Component
{
	[Property]
	public SceneFile StartingLevel { get; set; }

	[Property]
	public GameObject LoadingScreen { get; set; }

	public LevelManager()
		: base()
	{
		LevelManagerStaticStore.Stats = new LevelStats();
		LevelManagerStaticStore.SaveDataInstance = new SaveData();
		SaveData.Load();
	}

	public static void LoadLevelImmediate(SceneFile newScene,
										  bool showLoadingScreen)
	{
		var instance = LevelManagerStaticStore.Instance;

		if (instance.Scene.IsLoading)
			return;

		float timescale =
			instance.LoadLevelInternal(newScene, showLoadingScreen);

		LevelManagerStaticStore.IsLoading = false;
		instance.Scene.TimeScale = timescale;
	}

	public static float? LoadLevel(SceneFile newScene, bool showLoadingScreen)
	{
		var instance = LevelManagerStaticStore.Instance;

		if (instance.Scene.IsLoading)
			return null;

		float timescale =
			instance.LoadLevelInternal(newScene, showLoadingScreen);

		return timescale;
	}

	PlayerMovementController SpawnPlayer()
	{
		var player = new GameObject(true, "Player");
		player.SetPrefabSource("prefabs/player.prefab");
		player.UpdateFromPrefab();

		var controller = player.Components.Get<PlayerMovementController>();
		if (controller == null)
			return null;

		var spawnPoints = Scene.GetAllComponents<SpawnPoint>();
		foreach (var spawn in spawnPoints)
		{
		    controller.Transform.Position = spawn.Transform.Position;
		    controller.EyeAngles = spawn.Transform.Rotation.Angles();
			break;
		}

		return controller;
	}

	private float LoadLevelInternal(SceneFile newScene, bool showLoadingScreen)
	{
		LevelManagerStaticStore.IsLoading = true;

		var oldTimeScale = newScene.SceneProperties.ContainsKey("TimeScale")
						 ? newScene.SceneProperties["TimeScale"].GetValue<float>()
						 : 1.0f;
		newScene.SceneProperties.Remove("TimeScale");
		Scene.TimeScale = 0.0f;

		GameObject loadingScreen = null;
		if (showLoadingScreen)
		{
			loadingScreen = LoadingScreen.Clone();
			loadingScreen.Flags |= GameObjectFlags.DontDestroyOnLoad;
		}

		SceneLoadOptions options = new SceneLoadOptions();
		options.SetScene(newScene);
		options.ShowLoadingScreen = false;
		options.DeleteEverything = false;

		Scene.Load(options);

		var playerController = SpawnPlayer();
		playerController.SetPlayerNotStarted();

		if (showLoadingScreen)
			loadingScreen.DestroyImmediate();

		LevelManagerStaticStore.ActiveScene = newScene;

		return oldTimeScale;
	}

	protected override void OnStart()
	{
		LevelManagerStaticStore.Instance = this;
		GameObject.Flags |= GameObjectFlags.DontDestroyOnLoad;
		if (!Scene.IsLoading && StartingLevel != null)
		{
			LoadLevelImmediate(StartingLevel, true);
		}
	}
}
