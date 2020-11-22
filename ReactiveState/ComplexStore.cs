using System;

namespace ReactiveState
{
	public class ComplexStore<TComplexState> : ComplexStoreBase<TComplexState, DispatchContext<TComplexState>>
	{
		public ComplexStore(IStateTree<TComplexState> stateTree, TComplexState initialState,
			Dispatcher<TComplexState, DispatchContext<TComplexState>> dispatcher)
			: base(stateTree, initialState, (a, s, se, d) => new DispatchContext<TComplexState>(a, s, se, d), dispatcher)
		{
		}
	}
}
