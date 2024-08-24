public class SaveDataFormat
{
	public UInt64 UnlockedSpells { get; set; } = 0x2ul;
	public float MaxMana { get; set; } = 100.0f;
	// TODO: keep state of collected "single-time" secrets. In particular, we
	// would care about max mana buffs for example, as we don't want players
	// to replay a level and get infinite max mana.
	public HashSet<Guid> ConsumedMapItems { get; set; } = new HashSet<Guid>();

	public Dictionary<int, LevelSummaryData> CompletedLevelData { get; set; } =
		new Dictionary<int, LevelSummaryData>();
	public int GreatestCompletedLevel { get; set; } = -1;
}

public class SaveData
{
	public static SaveData Instance;
	public SaveDataFormat Data;

	private const String SAVE_DIRECTORY = "saves";
	private String _saveFilePath = "";
	private int _selectedSave = -1;

	private List<String> _saveFiles;
	private List<SaveDataFormat> _allSaveData;

	public SaveData()
	{
		Instance = this;
		Data = new SaveDataFormat();
		_saveFiles = new List<String>();
		_allSaveData = new List<SaveDataFormat>();

		FileSystem.Data.CreateDirectory(SAVE_DIRECTORY);
		var saves =
			FileSystem.Data.FindFile(SAVE_DIRECTORY, "player_save-*.json");
		foreach (var save in saves)
		{
			_saveFiles.Add(save);
			_allSaveData.Add(ParseSaveFile(save));
		}
	}

	public static void CreateNewSave()
	{
		String creationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
		String saveFile = $"{SAVE_DIRECTORY}/player_save-{creationTime}.json";
		Instance._saveFiles.Add(saveFile);
		Instance._selectedSave = Instance._saveFiles.Count;
		Instance._saveFilePath = saveFile;
		Instance.Data = new SaveDataFormat();
		Save();
	}

	public static bool SelectSaveIndex(int saveIdx)
	{
		if (saveIdx < 0 || saveIdx >= Instance._saveFiles.Count)
			return false;

		Instance._saveFilePath = $"{SAVE_DIRECTORY}/{Instance._saveFiles[saveIdx]}";
		Instance._selectedSave = saveIdx;
		return true;
	}

	public static void Save()
	{
		if (Instance._saveFilePath != "" && Instance.Data != null)
		{
			Instance._allSaveData[Instance._selectedSave] = Instance.Data;
			FileSystem.Data.WriteJson(Instance._saveFilePath, Instance.Data);
		}
	}

	private static SaveDataFormat ParseSaveFile(String path)
	{
		return FileSystem.Data.ReadJsonOrDefault<SaveDataFormat>(
			path,
			new SaveDataFormat()
		);
	}

	public static void Load()
	{
		// Not sure if ReadJsonOrDefault handles bad file names.
		if (Instance._saveFilePath == "")
		{
			Instance.Data = new SaveDataFormat();
			return;
		}

		Instance.Data = ParseSaveFile(Instance._saveFilePath);
	}

	public static int GetSaveCount()
	{
		return Instance._allSaveData.Count;
	}

	public static SaveDataFormat GetSave(int saveIdx)
	{
		if (saveIdx < 0 || saveIdx > Instance._allSaveData.Count)
			return null;

		return Instance._allSaveData[saveIdx];
	}
}
