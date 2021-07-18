namespace ReactiveState.ComplexState
{

	public interface IState: IPersistentState
	{
		IMutableState BeginTransaction();
	}
}
