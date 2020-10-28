using System;

namespace ReactiveState
{
	public interface IStore<out TState> : IStoreContext, IDispatcher, IDisposable
	{
		IObservable<TState> States();
	}
}
