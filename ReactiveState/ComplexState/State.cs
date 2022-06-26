using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveState.ComplexState
{
	public class State : IState
	{
		private readonly Dictionary<string, object?> _values;

		public State(IEnumerable<KeyValuePair<string, object?>> source)
		{
#if NET461 || NETSTANDARD2_0
			_values = new Dictionary<string, object?>();
			foreach (var v in _values)
				_values[v.Key] = v.Value;
#else
			_values = new Dictionary<string, object?>(source);
#endif
		}

		
		public State() : this(Enumerable.Empty<KeyValuePair<string, object?>>()) { }

		public IMutableState BeginTransaction()
		{
			return new MutableState(this);
		}

		public bool ContainsKey(string key)
			=> _values.ContainsKey(key);

		public T? Get<T>(string key) where T : class
			=> _values.TryGetValue(key, out var res) ? res as T : null as T;

		public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
			=> _values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static IEnumerable<KeyValuePair<string, object?>> ToKvp(IEnumerable<object> values)
			=> values.Select(x => new KeyValuePair<string, object?>(StateExtensions.Key(x.GetType()), x));

		public static IEnumerable<KeyValuePair<string, object?>> ToNulls(IEnumerable<string> values)
			=> values.Select(x => new KeyValuePair<string, object?>(x, null));
		public static IEnumerable<KeyValuePair<string, object?>> ToNulls(IEnumerable<Type> values)
			=> values.Select(x => new KeyValuePair<string, object?>(StateExtensions.Key(x), null));

		public static IPersistentState Build(params object[] values)
		{
			return new State(ToKvp(values));
		}

		public static IPersistentState Build(IEnumerable<string> nulls, params object[] values)
		{
			return new State(ToNulls(nulls).Concat(ToKvp(values)));
		}

		public static IPersistentState Build(IEnumerable<Type> nulls, params object[] values)
		{
			return new State(ToNulls(nulls).Concat(ToKvp(values)));
		}

	}
}
