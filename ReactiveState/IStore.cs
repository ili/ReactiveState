using System;

namespace ReactiveState
{
	public interface IStore: IDispatcher
	{
		IObservable<IStateAccessor> States();
	}
		

	public interface IStore<out TState> : IStoreContext, IDispatcher, IDisposable
	{
		IObservable<TState> States();
	}
}
