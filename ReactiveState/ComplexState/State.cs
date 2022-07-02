using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReactiveState.ComplexState
{
	public class State : IState
	{
		private readonly IDictionary<string, object> _values;

		public State(IEnumerable<KeyValuePair<string, object>> source)
		{
#if NET461 || NETSTANDARD2_0
			_values = new Dictionary<string, object>();
			foreach (var v in source)
				_values[v.Key] = v.Value;
#else
			_values = new Dictionary<string, object>(source);
#endif
		}

		public State() : this(Enumerable.Empty<KeyValuePair<string, object>>()) { }

		public State(IDictionary<string, object> values)
			=> _values = values;

		public virtual IMutableState BeginTransaction()
		{
			return new MutableState(this, (s, x) => new State(ApplyChanges(x)));
		}

		protected IDictionary<string, object> ApplyChanges(IEnumerable<KeyValuePair<string, object?>> changes)
		{
			var values = new Dictionary<string, object>(_values);

			foreach (var v in changes)
			{
				if (v.Value == null)
				{
					if (values.ContainsKey(v.Key))
						values.Remove(v.Key);
				}
				else
					values[v.Key] = v.Value!;
			}

			return values;
		}

		public static State ApplyChanges(State source, IEnumerable<KeyValuePair<string, object?>> changes)
		{
			return new State(source.ApplyChanges(changes));
		}

		public bool ContainsKey(string key)
			=> _values.ContainsKey(key);

		public T? Get<T>(string key) where T : class
			=> _values.TryGetValue(key, out var res) ? res as T : null as T;

		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
			=> _values.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public static IEnumerable<KeyValuePair<string, object>> ToKvp(IEnumerable<object> values)
			=> values.Select(x => new KeyValuePair<string, object>(StateExtensions.Key(x.GetType()), x));

		public static State Build(params object[] values)
		{
			return new State(ToKvp(values));
		}

		public static State Build(params KeyValuePair<string, object?>[] values)
			=> new State(values.Where(_ => _.Value != null).OfType<KeyValuePair<string, object>>());

		public static State Build(params KeyValuePair<Type, object?>[] values)
			=> new State(values.Where(_ => _.Value != null).Select(_ => new KeyValuePair<string, object>(StateExtensions.Key(_.Key), _.Value!)));

		public static State Build<T>(T value)
			=> Build(new KeyValuePair<Type, object?>(typeof(T), value));

		public static State Build<T1, T2>(T1? value1, T2? value2)
			=> Build(new KeyValuePair<Type, object?>(typeof(T1), value1),
				new KeyValuePair<Type, object?>(typeof(T2), value2)
				);

		public static State Build<T1, T2, T3>(T1? value1, T2? value2, T3? value3)
			=> Build(new KeyValuePair<Type, object?>(typeof(T1), value1),
				new KeyValuePair<Type, object?>(typeof(T2), value2),
				new KeyValuePair<Type, object?>(typeof(T3), value3)
				);

		public static State Build<T1, T2, T3, T4>(T1? value1, T2? value2, T3? value3, T4? value4)
			=> Build(new KeyValuePair<Type, object?>(typeof(T1), value1),
				new KeyValuePair<Type, object?>(typeof(T2), value2),
				new KeyValuePair<Type, object?>(typeof(T3), value3),
				new KeyValuePair<Type, object?>(typeof(T4), value4)
				);
	}
}
