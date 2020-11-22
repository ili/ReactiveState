namespace ReactiveState
{
	public interface IStateEmitter<in TState>: IStoreContext
	{
		void OnNext(TState? state);
	}
}
