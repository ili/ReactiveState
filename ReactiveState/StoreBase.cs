using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveState
{
	public abstract class StoreBase : IStore
	{
		private readonly Dispatcher _dispatcher;
		private readonly IStateAccessor _stateAccessor;
		private readonly ConcurrentQueue<IAction> _dispatchActions = new ConcurrentQueue<IAction>();

		public StoreBase(IStateAccessor initialState, params Func<Dispatcher, Dispatcher>[] middlewares)
			: this(initialState, new DispatcherBuilder().Use(middlewares))
		{}

		public StoreBase(IStateAccessor initialState, DispatcherBuilder dispatcherBuilder)
			: this(initialState, dispatcherBuilder.Build())
		{}

		public StoreBase(IStateAccessor initialState, Dispatcher dispatcher)
		{
			_dispatcher = dispatcher;
			_stateAccessor = initialState;
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
					await _dispatcher(a);

					if (Interlocked.Decrement(ref _counter) == 0)
						break;
				}
			}
		}

		public IObservable<IStateAccessor> States() => _stateAccessor.States;

		#region IDisposable Support
		private bool _disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_stateAccessor.Dispose();
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
