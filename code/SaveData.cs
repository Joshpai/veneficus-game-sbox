public class SaveDataFormat
{
	public UInt64 UnlockedSpells { get; set; } = 0x2ul;
	public float MaxMana { get; set; } = 100.0f;
	// TODO: keep state of collected "single-time" secrets. In particular, we
	// would care about max mana buffs for example, as we don't want players
	// to replay a level and get infinite max mana.
	public HashSet<Guid> ConsumedMapItems { get; set; } = new HashSet<Guid>();
}

public class SaveData
{
	public static SaveData Instance;
	public SaveDataFormat Data;

	private const String SAVE_DIRECTORY = "saves";
	private String _saveFilePath = "";

	private List<String> _saveFiles;

	public SaveData()
	{
		Instance = this;
		Data = new SaveDataFormat();
		_saveFiles = new List<String>();

		FileSystem.Data.CreateDirectory(SAVE_DIRECTORY);
		var saves =
			FileSystem.Data.FindFile(SAVE_DIRECTORY, "player_save-*.json");
		foreach (var save in saves)
		{
			_saveFiles.Add(save);
			Log.Info(save);
		}
	}

	public static void CreateNewSave()
	{
		String creationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
		String saveFile = $"{SAVE_DIRECTORY}/player_save-{creationTime}.json";
		Instance._saveFiles.Add(saveFile);

		Instance._saveFilePath = saveFile;
		Instance.Data = new SaveDataFormat();
		Save();
	}

	public static bool SelectSaveIndex(int saveIdx)
	{
		if (saveIdx < 0 || saveIdx >= Instance._saveFiles.Count)
			return false;

		Instance._saveFilePath = Instance._saveFiles[saveIdx];
		return true;
	}

	public static void Save()
	{
		if (Instance._saveFilePath != "" && Instance.Data != null)
			FileSystem.Data.WriteJson(Instance._saveFilePath, Instance.Data);
	}

	public static void Load()
	{
		// Not sure if ReadJsonOrDefault handles bad file names.
		if (Instance._saveFilePath == "")
		{
			Instance.Data = new SaveDataFormat();
			return;
		}

		var data = FileSystem.Data.ReadJsonOrDefault<SaveDataFormat>(
			Instance._saveFilePath,
			new SaveDataFormat()
		);

		// TODO: data validation?

		Instance.Data = data;
	}
}
