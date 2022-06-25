namespace ReactiveState
{
	public interface IStateEmitter<in TState>
	{
		void OnNext(TState? state);
	}
}
