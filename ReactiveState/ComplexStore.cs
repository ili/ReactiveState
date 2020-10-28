namespace ReactiveState
{
	public class ComplexStore<TComplesState> : ComplexStoreBase<TComplesState, ComplexStore<TComplesState>>
	{
		public ComplexStore(IStateTree<TComplesState> stateTree, TComplesState initialState, params Middleware<ComplexStore<TComplesState>, TComplesState>[] middlewares) : base(stateTree, initialState, middlewares)
		{
		}
	}
}
