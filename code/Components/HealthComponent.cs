public sealed class HealthComponent : Component
{
	[Property]
	public float MaxHealth { get; set; } = 100.0f;

	[Property]
	public float DamageMultiplier { get; set; } = 1.0f;

	[Property]
	public float HealMultiplier { get; set; } = 1.0f;

	[Property]
	public bool DestroyOnDeath { get; set; } = true;

	[Property]
	public float HealthRegenRate { get; set; } = 0.0f;

	[Property]
	public float HealthRegenDelay { get; set; } = 0.0f;

	[Property]
	public String HurtSound { get; set; } = null;
	[Property]
	public String HurtSoundMixerName { get; set; } = "Game";

	public float Health { get; private set; }

	public bool InRespawn { get; set; } = false;

	public bool Alive { get; private set; } = false;

	public event Action<float> OnHealthChanged;
	public event Action OnDeath;

	private float _regenStartTime = 0.0f;

	protected override void OnStart()
	{
		Alive = true;
		Health = MaxHealth;
		if (HurtSound != null)
			Sound.Preload(HurtSound);
	}

	private void AddHealth(float amount)
	{
		if (!Alive)
			return;

		Health = Math.Clamp(Health + amount, 0, MaxHealth);

		if (OnHealthChanged != null)
			OnHealthChanged.Invoke(amount);

		if (Health <= 0)
			Kill();
	}

	public void Damage(float amount)
	{
		AddHealth(DamageMultiplier * -amount);
		_regenStartTime = Time.Now + HealthRegenDelay;

		if (HurtSound != null)
		{
			var mixerHurtSound =
				Sandbox.Audio.Mixer.FindMixerByName(HurtSoundMixerName);
			SoundHandle sound;
			if (mixerHurtSound != null)
				sound = Sound.Play($"{HurtSound}.sound", mixerHurtSound);
			else
				sound = Sound.Play($"{HurtSound}.sound");

			if (sound != null && GameObject != null && GameObject.IsValid &&
				GameObject.Transform != null)
				sound.Position = Transform.Position;
		}
	}

	public void Heal(float amount)
	{
		AddHealth(HealMultiplier * amount);
	}

	public void Kill()
	{
		Health = 0.0f;
		Alive = false;

		OnDeath?.Invoke();

		if (DestroyOnDeath)
			GameObject.Destroy();
	}

	public float GetPercentage()
	{
		if (MaxHealth == 0.0f)
			return 0.0f;

		return Health / MaxHealth;
	}

	protected override void OnFixedUpdate()
	{
		if (Health < MaxHealth && _regenStartTime <= Time.Now)
		{
			var healAmount = HealthRegenRate * Time.Delta;
			AddHealth(healAmount);
		}
	}
}
