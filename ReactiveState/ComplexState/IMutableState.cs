namespace ReactiveState.ComplexState
{
	public interface IMutableState: IPersistentState
	{
		IMutableState Set<T>(string key, T? value) where T : class;

		IMutableState Set(IPersistentState state);

		IState Commit();
	}
}
