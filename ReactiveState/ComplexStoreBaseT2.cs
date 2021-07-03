using System;
using System.Reactive.Linq;

namespace ReactiveState
{
	public abstract class ComplexStoreBase<TState, TContext> : StoreBase<TState, TContext>
		where TContext: IDispatchContext<TState>
	{
		protected ComplexStoreBase(IStateTree<TState> stateTree, TState initialState,
			ContextFactory<TState, TContext> contextFactory,
			Middleware<TState, TContext> dispatcher)
			: base(initialState, contextFactory, dispatcher)
		{
			StateTree = stateTree;
		}

		protected IStateTree<TState> StateTree { get; }

		public IObservable<TSubState> States<TSubState>()
		{
			var getter = StateTree.FindGetter<TSubState>();
			if (getter == null)
				throw new InvalidOperationException($"{typeof(TSubState).FullName} is not registeted in state tree for {typeof(TState).FullName}");

			return States().Select(getter).DistinctUntilChanged();
		}

	}
}
