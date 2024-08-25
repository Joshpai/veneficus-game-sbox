public class LevelSummaryData
{
	public int LevelIndex { get; set; } = -1;

	public String CompletionTime { get; set; } = "00:00.000";
	public String CompletionTimeRank { get; set; } = "D";
	public String EnemiesKilledRank { get; set; } = "D";
	public String DeathRank { get; set; } = "D";

	// TODO: the below may not be included :)
	public bool ChallengeCompleted { get; set; } = false;
	public int SecretCount { get; set; } = 0;
	public uint SecretCollectedBitmask { get; set; } = 0;

	public String FinalRank { get; set; } = "D";
	public int FinalRankValue { get; set; } = -1;
}

public sealed class LevelEnd : InteractableComponent
{
	[Property]
	public int LevelIndex { get; set; } = -1;

	[Property]
	public SceneFile NextLevel { get; set; }

	[Property]
	public SceneFile MainMenu { get; set; }

	// Relevant for later scenes. For example, if we want to send players to
	// the ethereal tavern at the end, but don't necessarily want to recreate
	// the tavern scene for each individual time we use it. In such cases:
	// - Set NextLevel to the tavern scene; and
	// - Set DeferredNextLevel to the "real" next level
	[Property]
	public SceneFile DeferredNextLevel { get; set; }

	[Property]
	public GameObject LevelTransition { get; set; }

	[Property]
	public float TargetFinishTime_S { get; set; }
	[Property]
	public float TargetFinishTime_A { get; set; }
	[Property]
	public float TargetFinishTime_B { get; set; }
	[Property]
	public float TargetFinishTime_C { get; set; }

	[Property]
	public float TargetEnemiesKilledPercent_S { get; set; } = 1.0f;
	[Property]
	public float TargetEnemiesKilledPercent_A { get; set; } = 0.9f;
	[Property]
	public float TargetEnemiesKilledPercent_B { get; set; } = 0.75f;
	[Property]
	public float TargetEnemiesKilledPercent_C { get; set; } = 0.5f;

	[Property]
	public int DeathCount_S { get; set; } = 0;
	[Property]
	public int DeathCount_A { get; set; } = 2;
	[Property]
	public int DeathCount_B { get; set; } = 5;
	[Property]
	public int DeathCount_C { get; set; } = 10;

	protected override void OnStart()
	{
		LevelTransition.Enabled = false;
	}

	public override void Interact(GameObject interacter)
	{
		var completionTime =
			Time.Now - LevelManagerStaticStore.Stats.LevelStartTime;
		var completionTimeSpan = TimeSpan.FromSeconds(completionTime);
		// Cheat and add an extra "D" on the end because I don't want to
		// bounds check after doing an operation I know can overflow.
		String[] ranks = {"S", "A", "B", "C", "D", "D"};
		float[] finishTimes = {
			TargetFinishTime_S,
			TargetFinishTime_A,
			TargetFinishTime_B,
			TargetFinishTime_C
		};
		float[] killPercents = {
			TargetEnemiesKilledPercent_S,
			TargetEnemiesKilledPercent_A,
			TargetEnemiesKilledPercent_B,
			TargetEnemiesKilledPercent_C
		};
		float killPercent = 1.0f;
		int[] deathCounts = {
			DeathCount_S,
			DeathCount_A,
			DeathCount_B,
			DeathCount_C
		};
		int i;
		int totalRank = 0;

		// We don't want a level transition, so create a "level transition"
		// prefab, which we place in the level (somewhere). Delete the player
		// now, and the camera should hopefully jump to the level end. That
		// prefab will have a screen component attached.
		LevelTransition.Enabled = true;
		LevelManagerStaticStore.Player.Enabled = false;

		// NOTE: this camera used to be parented to the player, but with the
		// addition of moving platforms, it was parented to the scene itself
		// to work around some weirdness. There shouldn't be any other cameras
		// in the scene that are direct children of the scene.
		var camera = Scene.Components.GetInChildren<CameraComponent>();
		if (camera != null)
			camera.Destroy();

		foreach (var obj in LevelTransition.GetAllObjects(true))
		{
			if (obj.Name == "CameraContainer")
			{
				var newCamera = obj.Components.Create<CameraComponent>();
				newCamera.IsMainCamera = true;
				newCamera.FieldOfView = 110.0f;
				newCamera.ZNear = 1.0f;
				newCamera.ZFar = 6210.0f;
			}
		}

		LevelSummaryData summaryData = new LevelSummaryData();
		summaryData.CompletionTime =
			completionTimeSpan.ToString(@"mm\:ss\.fff");
		summaryData.ChallengeCompleted =
			LevelManagerStaticStore.Stats.ChallengeCompleted;
		summaryData.SecretCount = LevelManagerStaticStore.Stats.SecretCount;
		summaryData.SecretCollectedBitmask =
			LevelManagerStaticStore.Stats.SecretCollectedBitmask;

		for (i = 0; i < finishTimes.Length; i++)
			if (completionTime < finishTimes[i])
				break;
		summaryData.CompletionTimeRank = ranks[i];
		totalRank += i;

		var kills = LevelManagerStaticStore.Stats.EnemiesKilled;
		var maxKills = LevelManagerStaticStore.Stats.MaxEnemies;
		if (maxKills != 0)
			killPercent = (float)kills / maxKills;
		for (i = 0; i < killPercents.Length; i++)
			if (killPercent >= killPercents[i])
				break;
		summaryData.EnemiesKilledRank = ranks[i];
		totalRank += i;

		for (i = 0; i < deathCounts.Length; i++)
			if (LevelManagerStaticStore.Stats.DeathCount <= deathCounts[i])
				break;
		summaryData.DeathRank = ranks[i];
		totalRank += i;

		int finalRank = (int)MathF.Ceiling(totalRank / 3.0f);
		summaryData.FinalRank = ranks[finalRank];
		summaryData.FinalRankValue = finalRank;

		var finishSummary =
			LevelTransition.Components
						   .GetInDescendantsOrSelf<LevelFinishSummary>();
		if (finishSummary != null)
		{
			finishSummary.CompletionTime = summaryData.CompletionTime;
			finishSummary.CompletionTimeRank = summaryData.CompletionTimeRank;

			finishSummary.EnemiesKilled = kills;
			finishSummary.MaxEnemies = maxKills;
			finishSummary.EnemiesKilledRank = summaryData.EnemiesKilledRank;

			finishSummary.DeathCount =
				LevelManagerStaticStore.Stats.DeathCount;
			finishSummary.DeathRank = summaryData.DeathRank;

			finishSummary.ChallengeDescription =
				LevelManagerStaticStore.Stats.ChallengeDescription;
			finishSummary.ChallengeCompleted = summaryData.ChallengeCompleted;

			finishSummary.SecretCount = summaryData.SecretCount;
			finishSummary.SecretCollectedBitmask =
				summaryData.SecretCollectedBitmask;

			finishSummary.FinalRank = summaryData.FinalRank;

			finishSummary.OnRetryLevel += RestartCurrentLevel;
			finishSummary.OnMainMenu += ReturnToMainMenu;
			finishSummary.OnNextLevel += StartNextLevel;
		}

		LevelManagerStaticStore.Stats = new LevelStats();

		UpdateSaveDataLevelSummary(summaryData);
	}

	private void UpdateSaveDataLevelSummary(LevelSummaryData summaryData)
	{
		// If there's already data here, only overwrite it if the final rank
		// is better than the existing one.
		if (SaveData.Instance.Data.CompletedLevelData.ContainsKey(LevelIndex))
		{
			var last = SaveData.Instance.Data.CompletedLevelData[LevelIndex];
			if (last.FinalRankValue > summaryData.FinalRankValue)
				return;
			SaveData.Instance.Data.CompletedLevelData[LevelIndex] = summaryData;
		}
		else
		{
			SaveData.Instance.Data.CompletedLevelData.Add(LevelIndex, summaryData);
		}

		SaveData.Instance.Data.GreatestCompletedLevel =
			(LevelIndex > SaveData.Instance.Data.GreatestCompletedLevel)
			? LevelIndex : SaveData.Instance.Data.GreatestCompletedLevel;
		SaveData.Save();
	}

	private void RestartCurrentLevel()
	{
		LevelManagerStaticStore.Stats = new LevelStats();
		LevelManager.LoadLevelImmediate(LevelManagerStaticStore.ActiveScene, true, true);
	}

	private void ReturnToMainMenu()
	{
		if (MainMenu != null)
			LevelManager.LoadLevelImmediate(MainMenu, true, false);
	}

	private void StartNextLevel()
	{
		// TODO: convert "NextLevel" to a stack of next levels. (maybe?)
		// We want to be able to go to a level summary screen first.
		LevelManagerStaticStore.Stats = new LevelStats();
		if (NextLevel != null)
			LevelManager.LoadLevelImmediate(NextLevel, true, true);
	}
}
