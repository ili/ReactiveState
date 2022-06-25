using ReactiveState.ComplexState.StateTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState.ComplexState
{
	public abstract class State<TRoot> : IState
	{
		static IStateTree<TRoot>? _stateTree;
		public static void Init(IStateTree<TRoot> stateTree)
		{
			_stateTree = stateTree;
		}

		protected State()
		{
			if (_stateTree == null)
				throw new InvalidOperationException("Initialize state tree with static Init method before creating object");
		}

		public IMutableState BeginTransaction()
		{
			throw new NotImplementedException();
		}

		public bool ContainsKey(string key)
		{
			throw new NotImplementedException();
		}

		public TState Get<TState>(string key) where TState : class
		{
			throw new NotImplementedException();
		}

		public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
		{
			throw new NotImplementedException();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new NotImplementedException();
		}
	}
}
