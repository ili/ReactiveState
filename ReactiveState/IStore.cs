using System;

namespace ReactiveState
{
	public interface IStore<out TState> : IDisposable
	{
		IObservable<TState> States();
	}
}
