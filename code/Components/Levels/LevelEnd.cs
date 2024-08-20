public sealed class LevelEnd : InteractableComponent
{
	[Property]
	public SceneFile NextLevel { get; set; }

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
		// We don't want a level transition, so create a "level transition"
		// prefab, which we place in the level (somewhere). Delete the player
		// now, and the camera should hopefully jump to the level end. That
		// prefab will have a screen component attached.
		LevelTransition.Enabled = true;
		LevelManagerStaticStore.Player.Enabled = false;

		var camera =
			LevelManagerStaticStore.Player.Components
								   .GetInDescendantsOrSelf<CameraComponent>();

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

		var finishSummary =
			LevelTransition.Components
						   .GetInDescendantsOrSelf<LevelFinishSummary>();
		if (finishSummary != null)
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

			finishSummary.CompletionTime =
				completionTimeSpan.ToString(@"mm\:ss\.fff");
			for (i = 0; i < finishTimes.Length; i++)
				if (completionTime < finishTimes[i])
					break;
			finishSummary.CompletionTimeRank = ranks[i];
			totalRank += i;

			finishSummary.EnemiesKilled =
				LevelManagerStaticStore.Stats.EnemiesKilled;
			finishSummary.MaxEnemies =
				LevelManagerStaticStore.Stats.MaxEnemies;
			if (finishSummary.MaxEnemies != 0)
				killPercent =
					finishSummary.EnemiesKilled / finishSummary.MaxEnemies;
			for (i = 0; i < killPercents.Length; i++)
				if (killPercent >= killPercents[i])
					break;
			finishSummary.EnemiesKilledRank = ranks[i];
			totalRank += i;

			finishSummary.DeathCount =
				LevelManagerStaticStore.Stats.DeathCount;
			for (i = 0; i < deathCounts.Length; i++)
				if (finishSummary.DeathCount <= deathCounts[i])
					break;
			finishSummary.DeathRank = ranks[i];
			totalRank += i;

			finishSummary.ChallengeDescription =
				LevelManagerStaticStore.Stats.ChallengeDescription;
			finishSummary.ChallengeCompleted =
				LevelManagerStaticStore.Stats.ChallengeCompleted;

			finishSummary.SecretCount =
				LevelManagerStaticStore.Stats.SecretCount;
			finishSummary.SecretCollectedBitmask =
				LevelManagerStaticStore.Stats.SecretCollectedBitmask;

			int finalRank = (int)MathF.Ceiling(totalRank / 3.0f);
			finishSummary.FinalRank = ranks[finalRank];

			finishSummary.OnRetryLevel += RestartCurrentLevel;
			finishSummary.OnMainMenu += ReturnToMainMenu;
			finishSummary.OnNextLevel += StartNextLevel;
		}
	}

	private void RestartCurrentLevel()
	{
		LevelManagerStaticStore.Stats = new LevelStats();
		LevelManager.LoadLevelImmediate(LevelManagerStaticStore.ActiveScene, true);
	}

	private void ReturnToMainMenu()
	{
		// TODO: once we have a main menu
		Log.Info("main menu in another place");
	}

	private void StartNextLevel()
	{
		// TODO: convert "NextLevel" to a stack of next levels. (maybe?)
		// We want to be able to go to a level summary screen first.
		LevelManagerStaticStore.Stats = new LevelStats();
		if (NextLevel != null)
			LevelManager.LoadLevelImmediate(NextLevel, true);
	}
}
