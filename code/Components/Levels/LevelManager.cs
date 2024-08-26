public class LevelStats
{
	public float LevelStartTime { get; set; } = 0.0f;
	public int EnemiesKilled = 0;
	public int MaxEnemies { get; set; } = 0;
	public int DeathCount { get; set; } = 0;
	public String ChallengeDescription { get; set; } = "";
	public bool ChallengeCompleted { get; set; } = false;
	public int SecretCount { get; set; } = 0;
	public uint SecretCollectedBitmask { get; set; } = 0;
}

public class LevelCheckpointData
{
	public GameObject RespawnPoint { get; set; } = null;
	public HashSet<Guid> UsedObjects { get; set; } = new HashSet<Guid>();
	public int EnemiesKilled = 0;
}

public class LevelManagerStaticStore
{
	public static LevelManager Instance { get; set; }
	public static SceneFile ActiveScene { get; set; }
	public static bool IsLoading { get; set; }
	public static SaveData SaveDataInstance { get; set; }
	public static LevelStats Stats { get; set; }
	public static GameObject Player { get; set; }
	public static LevelCheckpointData CheckpointData { get; set; }
	public static HashSet<Guid> UsedObjects { get; set; }
}

public sealed class LevelManager : Component
{
	[Property]
	public SceneFile StartingLevel { get; set; }

	[Property]
	public bool StartingLevelSpawnsPlayer { get; set; }

	[Property]
	public GameObject LoadingScreen { get; set; }

	public LevelManager()
		: base()
	{
		LevelManagerStaticStore.Stats = new LevelStats();
		LevelManagerStaticStore.CheckpointData = new LevelCheckpointData();
		LevelManagerStaticStore.UsedObjects = new HashSet<Guid>();
		LevelManagerStaticStore.SaveDataInstance = new SaveData();
	}

	public static void LoadLevelImmediate(SceneFile newScene,
										  bool showLoadingScreen,
										  bool spawnPlayer)
	{
		var instance = LevelManagerStaticStore.Instance;

		if (instance.Scene.IsLoading)
			return;

		float timescale =
			instance.LoadLevelInternal(newScene, showLoadingScreen, spawnPlayer);

		LevelManagerStaticStore.IsLoading = false;
		instance.Scene.TimeScale = timescale;
	}

	public static float? LoadLevel(SceneFile newScene, bool showLoadingScreen,
								   bool spawnPlayer)
	{
		var instance = LevelManagerStaticStore.Instance;

		if (instance.Scene.IsLoading)
			return null;

		float timescale =
			instance.LoadLevelInternal(newScene, showLoadingScreen, spawnPlayer);

		return timescale;
	}

	PlayerMovementController SpawnPlayer()
	{
		var player = new GameObject(true, "Player");
		player.SetPrefabSource("prefabs/objects/player.prefab");
		player.UpdateFromPrefab();

		LevelManagerStaticStore.Player = player;

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

	private float LoadLevelInternal(SceneFile newScene, bool showLoadingScreen,
									bool spawnPlayer)
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

		if (spawnPlayer)
		{
			var playerController = SpawnPlayer();
			playerController.SetPlayerNotStarted();
		}

		if (showLoadingScreen)
			loadingScreen.DestroyImmediate();

		LevelManagerStaticStore.ActiveScene = newScene;

		LevelManagerStaticStore.Stats.MaxEnemies = 0;
		foreach (var _ in Scene.GetAllComponents<BaseEnemyAI>())
			LevelManagerStaticStore.Stats.MaxEnemies++;

		Log.Info(LevelManagerStaticStore.Stats.MaxEnemies);

		return oldTimeScale;
	}

	protected override void OnStart()
	{
		LevelManagerStaticStore.Instance = this;
		GameObject.Flags |= GameObjectFlags.DontDestroyOnLoad;
		if (!Scene.IsLoading && StartingLevel != null)
		{
			LoadLevelImmediate(StartingLevel, true, StartingLevelSpawnsPlayer);
		}
	}
}
