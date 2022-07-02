using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	public interface IDispatchContext<TState>: ISetStateContext<TState>
	{
		IAction Action { get; }

		TState? OriginalState { get; }

		TState? NewState { get; }

		IStateEmitter<TState> StateEmitter { get; }

		IDispatcher<TState> Dispatcher { get; }
	}

	public interface ISetStateContext<TState>
	{
		void SetState(TState? state);
	}
}
