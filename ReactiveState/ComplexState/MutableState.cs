using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveState.ComplexState
{
	class MutableState : IMutableState
	{
		private readonly State _source;
		private readonly ConcurrentDictionary<string, object?> _changes = new ConcurrentDictionary<string, object?>();
		public MutableState(State store)
		{
			_source = store;
		}

		public IState Commit() => State.ApplyChanges(_source, _changes);

		private IEnumerable<KeyValuePair<string, object>> GetResult()
		{
			foreach (var p in _changes.Where(_ => _.Value != null))
				yield return p!;

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

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			=> GetResult().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public IMutableState Merge(IPersistentState state)
		{
			if (!ReferenceEquals(state, this))
				foreach (var p in state)
					Set(p.Key, p.Value);

			return this;
		}
	}
}
