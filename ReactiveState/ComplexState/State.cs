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
	}
}
