public sealed class PlayerDeathManager : Component
{
	[Property]
	public HealthComponent PlayerHealthComponent { get; set; }

	[Property]
	public GameObject DeathScreen { get; set; }

	protected override void OnStart()
	{
		if (PlayerHealthComponent != null)
			PlayerHealthComponent.OnDeath += HandleDeath;
	}

	private void HandleDeath()
	{
		LevelManagerStaticStore.Stats.DeathCount++;

		// NOTE: The death screen is responsible for it's own lifetime. In
		// actuality, it will outlive us, so we can't be responsible for it.
		var deathScreen = DeathScreen.Clone();
		deathScreen.Flags |= GameObjectFlags.DontDestroyOnLoad;

		// TODO: delay of some kind? for artistic effect?
		float? timeScale =
			LevelManager.LoadLevel(LevelManagerStaticStore.ActiveScene, false, true);

		// Load the checkpoint data
		// NOTE: we currently no longer exist (as a game object) so lets
		// piggyback on top of the player's scene reference
		foreach (var obj in LevelManagerStaticStore.Player.Scene.GetAllObjects(false))
		{
			if (LevelManagerStaticStore.CheckpointData.UsedObjects.Contains(obj.Id))
			{
				obj.Destroy();
			}
		}

		LevelManagerStaticStore.UsedObjects =
			new HashSet<Guid>(LevelManagerStaticStore.CheckpointData.UsedObjects);
		LevelManagerStaticStore.Stats.EnemiesKilled =
			LevelManagerStaticStore.CheckpointData.EnemiesKilled;

		if (LevelManagerStaticStore.CheckpointData.RespawnPoint != null)
		{
			var controller =
				LevelManagerStaticStore.Player.Components
				.GetInDescendantsOrSelf<PlayerMovementController>();
			if (controller != null)
			{
				LevelManagerStaticStore.Player.Transform.Position =
					LevelManagerStaticStore.CheckpointData.RespawnPoint.Transform.Position;
				controller.EyeAngles =
					LevelManagerStaticStore.CheckpointData.RespawnPoint.Transform.Rotation;
			}
		}

		var deathScreenComponent =
			deathScreen.Components.GetInDescendantsOrSelf<DeathScreen>();
		if (deathScreenComponent != null)
		{
			deathScreenComponent.TimeScale = timeScale;
			// TODO: make this more random and related to the death reason.
			deathScreenComponent.DeathReason = "L + Ratio + Dead + your mum";
		}
	}

	protected override void OnFixedUpdate()
	{
		if (Input.Pressed("Reload") && PlayerHealthComponent != null)
		{
			// NOTE: we definitely don't want to destroy the player on death,
			// at least because it would cause an error here. But we will end
			// up destroying them in reloading the level anyway.
			// PlayerHealthComponent.DestroyOnDeath = false;
			PlayerHealthComponent.Kill();
		}
	}
}
