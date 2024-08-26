public sealed class ArenaManager : Component
{
	[Property]
	public GameObject MagicMissileEnemyPrefab { get; set; } = null;
	[Property]
	public GameObject RockArmourEnemyPrefab { get; set; } = null;
	[Property]
	public GameObject FireballEnemyPrefab { get; set; } = null;
	[Property]
	public GameObject RendingGaleEnemyPrefab { get; set; } = null;
	[Property]
	public GameObject WaterBeamEnemyPrefab { get; set; } = null;
	[Property]
	public GameObject LightningStrikeEnemyPrefab { get; set; } = null;

	[Property]
	public GameObject MaxManaBuff { get; set; } = null;

	private PlayerDeathManager _playerDeath;
	private PlayerMovementController _playerMovement;
	private PlayerHUD _playerHUD;

	private int _waveNumber = 0;
	private int _remainingEnemies = 0;
	private List<BaseSpell.SpellType> _waveEnemies;
	private int _waveSpawnedEnemies = 0;

	protected override void OnStart()
	{
		var playerDeathManagers = Scene.GetAllComponents<PlayerDeathManager>();
		foreach (var manager in playerDeathManagers)
		{
			_playerDeath = manager;
			_playerDeath.IsArena = true;
			break;
		}

		var playerSpellcastingControllers =
			Scene.GetAllComponents<PlayerSpellcastingController>();
		foreach (var spellcastingController in playerSpellcastingControllers)
		{
			spellcastingController.SetAllSpellsUnlocked();
		}

		var playerMovement = Scene.GetAllComponents<PlayerMovementController>();
		foreach (var movement in playerMovement)
		{
			_playerMovement = movement;
			_playerMovement.OnStartLevel += HandleLevelStart;
			break;
		}

		var playerHUD = Scene.GetAllComponents<PlayerHUD>();
		foreach (var hud in playerHUD)
		{
			_playerHUD = hud;
			break;
		}

		_waveNumber = 0;
	}

	private void HandleLevelStart()
	{
		StartNextWave();
	}

	private GameObject CreateEnemyObject(BaseSpell.SpellType enemyType)
	{
		GameObject enemyPrefab = enemyType switch
		{
			BaseSpell.SpellType.MagicMissile => MagicMissileEnemyPrefab,
			BaseSpell.SpellType.RockArmour => RockArmourEnemyPrefab,
			BaseSpell.SpellType.Fireball => FireballEnemyPrefab,
			BaseSpell.SpellType.RendingGale => RendingGaleEnemyPrefab,
			BaseSpell.SpellType.WaterBeam => WaterBeamEnemyPrefab,
			BaseSpell.SpellType.LightningStrike => LightningStrikeEnemyPrefab,
			_ => null
		};

		return (enemyPrefab != null) ? enemyPrefab.Clone() : null;
	}

	private Vector3? GetRandomPointInArena()
	{
		return Scene.NavMesh.GetRandomPoint();
	}

	private bool SpawnEnemy(BaseSpell.SpellType enemyType)
	{
		// TODO: do we care that we might spawn inside the player?
		var spawnPoint = GetRandomPointInArena();
		if (spawnPoint == null)
			return false;

		// TODO: clean up if failed?
		var enemy = CreateEnemyObject(enemyType);
		if (enemy == null)
			return false;

		enemy.Transform.Position = spawnPoint.Value;
		enemy.Transform.ClearInterpolation();

		var enemyAI = enemy.Components.GetInDescendantsOrSelf<BaseEnemyAI>();
		if (enemyAI == null)
			return false;

		enemyAI.AlwaysActive = true;

		var enemyHealth =
			enemy.Components.GetInDescendantsOrSelf<HealthComponent>();
		if (enemyHealth == null)
			return false;

		enemyHealth.OnDeath += HandleEnemyDeath;

		return true;
	}

	private int GetWaveMaxEnemiesTotal(int waveNumber)
	{
		return waveNumber;
	}

	private int GetWaveMaxEnemiesAtOnce(int waveNumber)
	{
		return (waveNumber < 50) ? waveNumber : 50;
	}

	private List<BaseSpell.SpellType> GetWaveEnemies(int waveNumber)
	{
		List<BaseSpell.SpellType> enemies = new List<BaseSpell.SpellType>();
		BaseSpell.SpellType[] enemyTiers = new BaseSpell.SpellType[] {
			BaseSpell.SpellType.MagicMissile,
			BaseSpell.SpellType.RockArmour,
			BaseSpell.SpellType.Fireball,
			BaseSpell.SpellType.RendingGale,
			BaseSpell.SpellType.WaterBeam,
			BaseSpell.SpellType.LightningStrike // TODO: not yet?
		};
		// List of indices into the above for spawn times and amounts
		// NOTE: keep length a power of 2 so modulo isn't slow
		int[] enemyRatios = new int[] {
			0,
			0,
			0, 1,
			1, 0, 1, 2,
			2, 3, 0, 1, 2, 3, 4, 2,
			3, 4, 4, 3, 2, 1, 0, 1, 2, 3, 1, 2, 0, 3, 4, 2
		};

		for (int i = 0; i < GetWaveMaxEnemiesTotal(waveNumber); i++)
		{
			enemies.Add(enemyTiers[enemyRatios[i % enemyRatios.Length]]);
		}

		return enemies;
	}

	private void SpawnNextEnemy()
	{
		// We shouldn't have to loop, but do it just in case
		var enemyType =
			_waveEnemies[_waveSpawnedEnemies % _waveEnemies.Count];
		if (SpawnEnemy(enemyType))
		{
			_waveSpawnedEnemies++;
		}
		else
		{
			_remainingEnemies--;
		}
	}

	private void SpawnMaxManaBuff()
	{
		if (MaxManaBuff == null)
			return;

		var buff = MaxManaBuff.Clone();

		var position = GetRandomPointInArena();
		if (position != null)
			buff.Transform.Position = position.Value + Vector3.Up * 50.0f;

		// Counter the bobbing so we don't go inside the floor
		buff.Transform.Position += Vector3.Up * 50.0f;
	}

	private void StartNextWave()
	{
		_waveNumber++;
		_waveEnemies = GetWaveEnemies(_waveNumber);
		_remainingEnemies = GetWaveMaxEnemiesTotal(_waveNumber);
		_waveSpawnedEnemies = 0;
		int maxAtOnce = GetWaveMaxEnemiesAtOnce(_waveNumber);

		_playerHUD.WaveNumber = _waveNumber;
		_playerDeath.SurvivedWaves = _waveNumber - 1;

		if (_waveNumber > 1)
		{
			SpawnMaxManaBuff();
		}

		for (_waveSpawnedEnemies = 0;
			 _waveSpawnedEnemies <
			 (maxAtOnce < _remainingEnemies ? maxAtOnce : _remainingEnemies);)
		{
			SpawnNextEnemy();
		}
	}

	private void HandleEnemyDeath()
	{
		if (_waveSpawnedEnemies < GetWaveMaxEnemiesTotal(_waveNumber))
		{
			SpawnNextEnemy();
		}

		if (--_remainingEnemies <= 0)
		{
			_remainingEnemies = 0;
			StartNextWave();
		}
	}
}
