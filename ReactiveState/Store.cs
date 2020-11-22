using System;

namespace ReactiveState
{
	public class Store<T> : StoreBase<T, DispatchContext<T>>
	{
		public Store(T initialState,
			Dispatcher<T, DispatchContext<T>> dispatcher)
			: base(initialState, (a, s, se, d) => new DispatchContext<T>(a, s, se, d), dispatcher)
		{
		}
	}
}
