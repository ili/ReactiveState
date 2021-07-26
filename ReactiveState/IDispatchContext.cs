using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	public interface IDispatchContext<TState>
	{
		IAction Action { get; }

		TState? OriginalState { get; }

		TState? NewState { get; set; }

		IStateEmitter<TState> StateEmitter { get; }

		IDispatcher Dispatcher { get; }
	}
}
