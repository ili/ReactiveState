using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveState
{
	public abstract class StoreBase<TState, TContext> : IStore<TState>, IStateEmitter<TState>, IDispatcher<TState>
		where TContext : IDispatchContext<TState>
	{
		protected readonly BehaviorSubject<TState> _states;
		private readonly Middleware<TState, TContext> _dispatcher;
		private readonly ContextFactory<TState, TContext> _contextFactory;

		public StoreBase(TState initialState, ContextFactory<TState, TContext> contextFactory, Middleware<TState, TContext> dispatcher)
		{
			_dispatcher = dispatcher;
			_contextFactory = contextFactory;
			_states = new BehaviorSubject<TState>(initialState);
		}

		public async Task<TState?> Dispatch(IAction action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			var context = _contextFactory(action, _states.Value, this, this);
			return await _dispatcher.Dispatch(context);
		}


		public IObservable<TState> States() => _states.DistinctUntilChanged();

		void IStateEmitter<TState>.OnNext(TState? state)
			=> _states.OnNext(state!); //TODO test null state

		#region IDisposable Support
		private bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_states.OnCompleted();
					_dispatcher.Dispose();
				}

				_disposed = true;
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}
		#endregion

	}
}
