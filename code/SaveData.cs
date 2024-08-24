using System.Text.Json;

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
			var saveFile = $"{SAVE_DIRECTORY}/{save}";
			Log.Info(saveFile);
			Log.Info(save);
			_saveFiles.Add(saveFile);
			_allSaveData.Add(ParseSaveFile(saveFile));
		}
	}

	public static void CreateNewSave()
	{
		String creationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ");
		String saveFile = $"{SAVE_DIRECTORY}/player_save-{creationTime}.json";
		Instance._saveFilePath = saveFile;
		Instance.Data = new SaveDataFormat();
		Instance._saveFiles.Add(saveFile);
		Instance._allSaveData.Add(Instance.Data);
		Instance._selectedSave = Instance._allSaveData.Count - 1;
		Save();
	}

	public static bool SelectSaveIndex(int saveIdx)
	{
		if (saveIdx < 0 || saveIdx >= Instance._saveFiles.Count)
			return false;

		Instance._saveFilePath = Instance._saveFiles[saveIdx];
		Instance.Data = Instance._allSaveData[saveIdx];
		Instance._selectedSave = saveIdx;
		return true;
	}

	public static void Save()
	{
		if (Instance._saveFilePath != "" && Instance.Data != null)
		{
			Instance._allSaveData[Instance._selectedSave] = Instance.Data;
			// NOTE: this brokey, no worky
			// FileSystem.Data.WriteJson<SaveDataFormat>(Instance._saveFilePath, Instance.Data);
			string contents = JsonSerializer.Serialize(Instance.Data);
			FileSystem.Data.WriteAllText(Instance._saveFilePath, contents);
		}
	}

	private static SaveDataFormat ParseSaveFile(String path)
	{
		Log.Info(path);
		return FileSystem.Data.ReadJsonOrDefault<SaveDataFormat>(
			path,
			new SaveDataFormat()
		);
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
