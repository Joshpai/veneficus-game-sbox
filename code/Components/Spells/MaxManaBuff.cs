public sealed class MaxManaBuff : Component, Component.ITriggerListener
{
	[Property]
	public float MaxManaValue { get; set; } = 25.0f;

	[Property]
	public String PickupSound { get; set; } = "sounds/World/manacrystalpickedup";
	[Property]
	public String PickupSoundMixerName { get; set; } = "Game";

	private bool _hasBeenCollected;

	protected override void OnStart()
	{
		base.OnStart();

		Sound.Preload(PickupSound);

		if (SaveData.Instance == null || SaveData.Instance.Data == null ||
			SaveData.Instance.Data.ConsumedMapItems == null ||
			GameObject == null || !GameObject.IsValid)
			return;

		_hasBeenCollected =
			SaveData.Instance.Data.ConsumedMapItems.Contains(GameObject.Id);

		if (_hasBeenCollected)
		{
			ModelRenderer modelRenderer =
				GameObject.Components.GetInDescendantsOrSelf<SkinnedModelRenderer>();
			if (modelRenderer == null)
				modelRenderer =
					GameObject.Components.GetInDescendantsOrSelf<ModelRenderer>();

			if (modelRenderer != null && modelRenderer.IsValid)
			{
				modelRenderer.Tint = modelRenderer.Tint.WithAlpha(0.25f);
			}
		}
	}

	public void OnTriggerEnter( Collider collider )
	{
		if (collider.GameObject == null || !collider.GameObject.IsValid)
			return;

		var other = collider.GameObject.Components;
		var player =
			other.GetInDescendantsOrSelf<PlayerSpellcastingController>();

		if (player != null)
		{
			var mixerPickupSound =
				Sandbox.Audio.Mixer.FindMixerByName(PickupSoundMixerName);
			SoundHandle sound;
			if (mixerPickupSound != null)
				sound = Sound.Play($"{PickupSound}.sound", mixerPickupSound);
			else
				sound = Sound.Play($"{PickupSound}.sound");

			if (sound != null)
				sound.Position = Transform.Position;

			if (!_hasBeenCollected)
			{
				player.MaxMana += MaxManaValue;
				SaveData.Instance.Data.ConsumedMapItems.Add(GameObject.Id);
				SaveData.Save();
			}
			GameObject.Destroy();
		}
	}
}
