namespace ReactiveState.ComplexState
{
	public static class StateExtensions
	{
		public static string Key<T>()
			=> typeof(T).FullName!;

		public static T? Get<T>(this IState store) where T : class
			=> store.Get<T>(Key<T>());
		public static IMutableState Set<T>(this IMutableState transaction, T? value) where T : class
			=> transaction.Set(Key<T>(), value);

		public static bool ContainsKey<T>(this IState store)
			=> store.ContainsKey(Key<T>());
	}
}
