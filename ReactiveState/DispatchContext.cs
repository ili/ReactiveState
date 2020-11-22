using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	public class DispatchContext<TState> : IDispatchContext<TState>
	{
		public DispatchContext(IAction action, TState originalState, IStateEmitter<TState> stateEmitter, IDispatcher dispatcher)
		{
			Action = action;
			OriginalState = originalState;
			StateEmitter = stateEmitter;
			Dispatcher = dispatcher;
		}

		public IAction Action { get; }

		public TState OriginalState { get; }

		public TState? NewState { get; set; }

		public IStateEmitter<TState> StateEmitter { get; }

		public IDispatcher Dispatcher { get; }
	}
}
