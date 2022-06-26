using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace ReactiveState.ComplexState
{
	public static class StateExtensions
	{
		private static readonly ConcurrentDictionary<Type, string> _defaultKeys = new ConcurrentDictionary<Type, string>();

		public static string Key<T>()
			=> Key(typeof(T));

		public static string Key(Type type)
			=> _defaultKeys.GetOrAdd(type, t =>
			{
				var key = t.FullName;
				var attr = t.GetCustomAttribute<SubStateAttribute>();
				if (attr != null && !string.IsNullOrEmpty(attr.Key))
					return attr.Key!;

				return key!;
			});

		public static T? Get<T>(this IPersistentState store) where T : class
			=> store.Get<T>(Key<T>());


		public static IMutableState Set<T>(this IMutableState transaction, T? value) where T : class
			=> transaction.Set(Key<T>(), value);

		public static bool ContainsKey<T>(this IPersistentState store)
			=> store.ContainsKey(Key<T>());
	}
}
