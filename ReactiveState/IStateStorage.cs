using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	public interface IStateAccessor: IDisposable
	{
		TState Get<TState>();

		public object GetRoot();

		IObservable<IStateAccessor> States { get; }
	}

	public interface IStateStorage: IStateAccessor
	{
		public void Reduce(IReducer reducer, IAction action);
	}

	public interface IReducer
	{
		Type StateType { get; }
		Type ActionType { get; }
		object? Reduce(object? state, IAction action);
	}

	sealed class ReducerImpl<TState, TAction>: IReducer
		where TAction : IAction
		//where TState: class
	{
		private readonly Reducer<TState, TAction> _reducer;

		public ReducerImpl(Reducer<TState, TAction> reducer)
		{
			_reducer = reducer;
		}

		Type IReducer.StateType => typeof(TState);

		Type IReducer.ActionType => typeof(TAction);

		object? IReducer.Reduce(object? state, IAction action)
			=> _reducer((TState)state, (TAction)action)!;
	}
}
