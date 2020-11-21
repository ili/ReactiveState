using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace ReactiveState
{
	public class ObjectStateAccessor<TState> : IStateStorage
		where TState: class
	{
		private readonly IStateTree<TState> _stateTree;
		private TState _state;

		public ObjectStateAccessor(IStateTree<TState> stateTree, TState initialState)
		{
			_stateTree = stateTree;
			_state = initialState;
		}

		private object GetSubState(Type subStateType)
		{
			if (typeof(TState) == subStateType)
				return _state;

			var getter = ((LambdaExpression)_stateTree.FindGetter(subStateType)).Compile();
			return getter.DynamicInvoke(_state);
		}

		public TSubState Get<TSubState>()
			=> (TSubState)GetSubState(typeof(TSubState));

		public object GetRoot()
			=> _state;

		public void OnNext()
		{
			throw new NotImplementedException();
		}

		public void Reduce(IReducer reducer, IAction action)
		{
			var state = GetSubState(reducer.StateType);
			var newState = reducer.Reduce(state, action)!;
			var composer = ((LambdaExpression)_stateTree.FindComposer(reducer.StateType)).Compile();

			_state = (TState)composer.DynamicInvoke(state, newState);
		}
	}
}
