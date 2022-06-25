using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	public class DispatchContext<TState> : IMutableStateContext<TState>
	{
		public DispatchContext(IAction action, TState originalState, IStateEmitter<TState> stateEmitter, IDispatcher<TState> dispatcher)
		{
			Action = action;
			OriginalState = originalState;
			StateEmitter = stateEmitter;
			Dispatcher = dispatcher;
		}

		public IAction Action { get; }

		public TState OriginalState { get; }

		public TState? NewState { get; private set; }

		public IStateEmitter<TState> StateEmitter { get; }

		public IDispatcher<TState> Dispatcher { get; }

		void IMutableStateContext<TState>.SetState(TState? state)
		{
			NewState = state;
		}
	}
}
