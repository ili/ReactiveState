using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReactiveState
{
	class ReducerMiddleware
	{
		private readonly IStateStorage _stateAccessor;
		private readonly IEnumerable<IReducer> _reducers;

		public ReducerMiddleware(IStateStorage stateAccessor, IEnumerable<IReducer> reducers)
		{
			_stateAccessor = stateAccessor;
			_reducers = reducers;
		}

		public void Invoke(IAction action)
		{
			foreach (var r in _reducers.Where(_ => _.ActionType.IsAssignableFrom(action.GetType())))
			{
				_stateAccessor.Reduce(r, action);
			}
		}
	}
}
