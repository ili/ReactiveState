namespace ReactiveState.ComplexState
{
	public interface IMutableState: IPersistentState
	{
		IMutableState Set<T>(string key, T? value) where T : class;

		IState Commit();
	}
}
