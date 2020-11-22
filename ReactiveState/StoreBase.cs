using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveState
{
	public abstract class StoreBase<TState, TContext> : IStore<TState>, IStateEmitter<TState>, IDispatcher
		where TContext : IDispatchContext<TState>
	{
		protected readonly BehaviorSubject<TState> _states;
		private readonly Dispatcher<TState, TContext> _dispatcher;
		private readonly ContextFactory<TState, TContext> _contextFactory;
		private readonly ConcurrentQueue<IAction> _dispatchActions = new ConcurrentQueue<IAction>();

		public StoreBase(TState initialState, ContextFactory<TState, TContext> contextFactory, Dispatcher<TState, TContext> dispatcher)
		{
			_dispatcher = dispatcher;
			_contextFactory = contextFactory;
			_states = new BehaviorSubject<TState>(initialState);
		}

		volatile int _counter = 0;
		public async Task Dispatch(IAction action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_dispatchActions.Enqueue(action);
			if (Interlocked.Increment(ref _counter) == 1)
			{
				while (_dispatchActions.TryDequeue(out var a))
				{
					var context = _contextFactory(a, _states.Value, this, this);
					await _dispatcher(context);

					if (Interlocked.Decrement(ref _counter) == 0)
						break;
				}
			}
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
