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

		public StoreBase(TState initialState, Middleware<TState, TContext> dispatcher)
		{
			_dispatcher = dispatcher;
			_states = new BehaviorSubject<TState>(initialState);
		}

		protected abstract TContext CreateContext(IAction action, TState currentState);

		public async Task<TState?> Dispatch(IAction action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			var context = CreateContext(action, _states.Value);
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
