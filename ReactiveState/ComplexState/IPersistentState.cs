using System.Collections.Generic;

namespace ReactiveState.ComplexState
{
	public interface IPersistentState : IEnumerable<KeyValuePair<string, object?>>
	{
		T? Get<T>(string key) where T : class;

		bool ContainsKey(string key);
	}
}
