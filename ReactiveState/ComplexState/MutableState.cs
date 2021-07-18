using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveState.ComplexState
{
	class MutableState : IMutableState
	{
		private readonly IState _source;
		private readonly ConcurrentDictionary<string, object?> _changes = new ConcurrentDictionary<string, object?>();
		public MutableState(IState store)
		{
			_source = store;
		}

		public IState Commit() => new State(GetResult());

		private IEnumerable<KeyValuePair<string, object?>> GetResult()
		{
			foreach (var p in _changes)
				yield return p;

			foreach (var p in _source.Where(_ => !_changes.ContainsKey(_.Key)))
				yield return p;
		}

		public IMutableState Set<T>(string key, T? value) where T : class
		{
			_changes[key] = value;
			return this;
 		}

		public T? Get<T>(string key) where T: class
		{
			if (_changes.TryGetValue(key, out var res))
				return res as T;

			return _source.Get<T>(key);
		}

		public bool ContainsKey(string key)
			=> _changes.ContainsKey(key) || _source.ContainsKey(key);

		public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
			=> GetResult().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
