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
	// D is default, so below C => D.

	public override void Interact(GameObject interacter)
	{
		// We don't want a level transition, so create a "level transition"
		// prefab, which we place in the level (somewhere). Delete the player
		// now, and the camera should hopefully jump to the level end. That
		// prefab will have a screen component attached.

		if (NextLevel != null)
		{
			// TODO: convert "NextLevel" to a stack of next levels.
			// We want to be able to go to a level summary screen first.

			// Ratings: S A B C D
			// Rate player performance on:
			// - Ranges:
			//   - Time taken to complete level
			//	 - Percentage of enemies killed
			//   - Spell variety
			// - Single Values:
			//   - Number of secrets found
			//   - level challenge
			//   - Deathless
			LevelManager.LoadLevelImmediate(NextLevel, true);
		}
	}
}
