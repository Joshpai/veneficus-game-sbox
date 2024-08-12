public sealed class LevelManager : Component
{
	[Property]
	public SceneFile StartingLevel { get; set; }

	[Property]
	public GameObject LoadingScreen { get; set; }

	private static LevelManager _instance;
	public static bool IsLoading = false;

	public static void LoadLevel(SceneFile newScene)
	{
		_instance.LoadLevelInternal(newScene);
	}

	private void LoadLevelInternal(SceneFile newScene)
	{
		IsLoading = true;

		Log.Info(newScene.SceneProperties);
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

		loadingScreen.DestroyImmediate();
		IsLoading = false;
		Scene.TimeScale = oldTimeScale;
		
		// TODO: automatically add MapPlayerSpawner component

	}

	protected override void OnStart()
	{
		_instance = this;
		GameObject.Flags |= GameObjectFlags.DontDestroyOnLoad;
		LoadLevelInternal(StartingLevel);
	}
}
