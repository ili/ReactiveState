using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace ReactiveState
{
	public sealed class Middleware<TState, TContext> : IDisposable
		where TContext: IDispatchContext<TState>
	{
		private readonly IDisposable _free;
		public Dispatcher<TState, TContext> Dispatch { get; }
		public Middleware(Dispatcher<TState, TContext> dispatch,
			IDisposable free) => (Dispatch, _free) = (dispatch, free);

		public void Dispose() => _free.Dispose();
	}
}
