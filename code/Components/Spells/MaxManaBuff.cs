public sealed class MaxManaBuff : Component, Component.ITriggerListener
{
	[Property]
	public float MaxManaValue { get; set; } = 25.0f;

	private bool _hasBeenCollected;

	protected override void OnStart()
	{
		base.OnStart();

		_hasBeenCollected =
			SaveData.Instance.Data.ConsumedMapItems.Contains(GameObject.Id);

		if (_hasBeenCollected)
		{
			ModelRenderer modelRenderer =
				GameObject.Components.GetInDescendantsOrSelf<SkinnedModelRenderer>();
			if (modelRenderer == null)
				modelRenderer =
					GameObject.Components.GetInDescendantsOrSelf<ModelRenderer>();

			if (modelRenderer != null)
			{
				modelRenderer.Tint = modelRenderer.Tint.WithAlpha(0.25f);
			}
		}
	}

	public void OnTriggerEnter( Collider collider )
	{
		var other = collider.GameObject.Components;
		var player =
			other.GetInDescendantsOrSelf<PlayerSpellcastingController>();

		if (player != null)
		{
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
