public sealed class EnemyManager : Component
{
	private List<BaseSpell> _castSpells = new List<BaseSpell>();
	private List<BaseSpell> _deferredSpellRemovals = new List<BaseSpell>();

	public void AddCastSpell(BaseSpell spell)
	{
		spell.OnDestroy += OnSpellDestroyed;
		_castSpells.Add(spell);
	}

	private void OnSpellDestroyed(object spell, EventArgs e)
	{
		_deferredSpellRemovals.Add((BaseSpell)spell);
	}

	private void OnUpdateSpells()
	{
		foreach (BaseSpell spell in _castSpells)
			spell.OnUpdate();
	}

	private void OnFixedUpdateSpells()
	{
		foreach (BaseSpell spell in _castSpells)
			spell.OnFixedUpdate();

		foreach (BaseSpell spell in _deferredSpellRemovals)
			_castSpells.Remove(spell);

		_deferredSpellRemovals.Clear();
	}

	protected override void OnUpdate()
	{
		OnUpdateSpells();
	}

	protected override void OnFixedUpdate()
	{
		OnFixedUpdateSpells();
	}
}
