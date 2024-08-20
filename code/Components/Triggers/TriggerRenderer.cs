public sealed class TriggerRenderer : Component, Component.ITriggerListener
{
	[Property]
	public Color PassiveColour { get; set; } = Color.White;

	[Property]
	public Color ActiveColour { get; set; } = Color.Red;

	public static bool ShowTriggers { get; set; } = false;

	private int InsideCount = 0;
	private ModelRenderer _modelRenderer;

	protected override void OnStart()
	{
		_modelRenderer =
			GameObject.Components.GetInDescendantsOrSelf<ModelRenderer>();
	}

	[ConCmd("toggle_show_triggers")]
	public static void ToggleShowTriggers()
	{
		ShowTriggers = !ShowTriggers;
	}

	public void OnTriggerEnter(Collider other)
	{
		InsideCount++;
	}

	public void OnTriggerExit(Collider other)
	{
		InsideCount--;
	}

	protected override void OnUpdate()
	{
#if DEBUG
		_modelRenderer.Enabled = TriggerRenderer.ShowTriggers;
		_modelRenderer.Tint = (InsideCount > 0) ? ActiveColour : PassiveColour;
#endif
	}
}
