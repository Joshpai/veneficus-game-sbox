public sealed class KeyTrigger : Component, Component.ITriggerListener
{
	public void OnTriggerEnter(Collider other)
	{
		GameObject.Parent.Destroy();
	}
}
