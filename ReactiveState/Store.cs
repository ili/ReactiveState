using System;

namespace ReactiveState
{
	public class Store<T> : StoreBase<T, DispatchContext<T>>
	{
		public Store(T initialState,
			Middleware<T, DispatchContext<T>> dispatcher
			)
			: base(initialState, dispatcher)
		{
		}

		protected override DispatchContext<T> CreateContext(IAction action, T currentState)
			=> new DispatchContext<T>(action, currentState, this, this);
	}
}
