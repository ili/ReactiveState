using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	public interface IDispatchContext<TState>
	{
		IAction Action { get; }

		TState? OriginalState { get; }

		TState? NewState { get; }

		IStateEmitter<TState> StateEmitter { get; }

		IDispatcher<TState> Dispatcher { get; }
	}

	public interface IMutableStateContext<TState>: IDispatchContext<TState>
	{
		void SetState(TState? state);
	}
}
