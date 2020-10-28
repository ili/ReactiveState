using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveState
{
	public abstract class StoreBase<TState, TContext> : IStore<TState>, IStateEmitter<TState>
		where TContext : StoreBase<TState, TContext>
	{
		protected readonly BehaviorSubject<TState> _states;
		private readonly Dispatcher<TState> _dispatcher;
		private readonly ConcurrentQueue<IAction> _dispatchActions = new ConcurrentQueue<IAction>();

		public StoreBase(TState initialState, params Middleware<TContext, TState>[] middlewares)
		{
			_dispatcher = ComposeMiddlewares(middlewares);

			_states = new BehaviorSubject<TState>(initialState);
		}

		private Dispatcher<TState> ComposeMiddlewares(Middleware<TContext, TState>[] middlewares)
		{
			Dispatcher<TState> dispatchAction =
				(state, action) => Task.FromResult(DispatchInternal(state, action));

			for (var i = middlewares.Length - 1; i >= 0; i--)
				dispatchAction = middlewares[i]((TContext)this)(dispatchAction);

			return dispatchAction;
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
					await _dispatcher(_states.Value, a);

					if (Interlocked.Decrement(ref _counter) == 0)
						break;
				}
			}
		}

		protected virtual TState DispatchInternal(TState state, IAction action)
		{
			return state;
		}

		public IObservable<TState> States() => _states.DistinctUntilChanged();

		void IStateEmitter<TState>.OnNext(TState state)
			=> _states.OnNext(state);

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
