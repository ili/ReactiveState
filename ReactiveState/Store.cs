namespace ReactiveState
{
	public class Store<T> : StoreBase<T, Store<T>>
	{
		public Store(T initialState, params Middleware<Store<T>, T>[] middlewares) : base(initialState, middlewares)
		{
		}
	}
}