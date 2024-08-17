public class SaveDataFormat
{
	public UInt64 UnlockedSpells { get; set; } =
#if DEBUG
		0xfffffffffffffffful;
#else
		0x2ul;
#endif
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

	private const string SAVE_FILE_PATH = "player_save.json";

	public SaveData()
	{
		Instance = this;
	}

	public static void Save()
	{
		FileSystem.Data.WriteJson(SAVE_FILE_PATH, Instance.Data);
	}

	public static void Load()
	{
		var data = FileSystem.Data.ReadJsonOrDefault<SaveDataFormat>(
			SAVE_FILE_PATH,
			new SaveDataFormat()
		);

		// TODO: data validation?

		Instance.Data = data;
	}
}
