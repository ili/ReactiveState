using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveState.ComplexState
{
	public class MutableState : IMutableState
	{
		private readonly IState _source;
		private readonly Func<IState, IDictionary<string, object?>, IState> _commiter;
		private readonly IDictionary<string, object?> _changes = new Dictionary<string, object?>();
		public MutableState(IState store, Func<IState, IDictionary<string, object?>, IState> commiter)
		{
			_source = store;
			_commiter = commiter;
		}

		public IState Commit() => _commiter(_source, _changes);

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

		public IMutableState Set(IPersistentState state)
		{
			if (!ReferenceEquals(state, this))
				foreach (var p in state)
					Set(p.Key, p.Value);

			return this;
		}
	}
}
