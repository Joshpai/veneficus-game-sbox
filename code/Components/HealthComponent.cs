using Sandbox;
using System;

public sealed class HealthComponent : Component
{
	[Property]
	public float MaxHealth { get; set; } = 100.0f;

	[Property]
	public float DamageMultiplier { get; set; } = 1.0f;

	[Property]
	public float HealMultiplier { get; set; } = 1.0f;

	public float Health { get; private set; }

	public bool Alive { get; private set; } = false;

	public event Action<float> OnHealthChanged;
	public event Action OnDeath;

	protected override void OnStart()
	{
		Health = MaxHealth;
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

		GameObject.Destroy();
	}
}
