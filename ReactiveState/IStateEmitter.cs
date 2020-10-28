namespace ReactiveState
{
	public interface IStateEmitter<TState>: IStoreContext
	{
		void OnNext(TState state);
	}
}
