using System;
using System.Threading.Tasks;

namespace ReactiveState
{
	public delegate TState Reducer<TState, in TAction>(TState previousState, TAction action)
		where TAction : IAction
		//where TState : class
		;

	//public delegate TState Reducer<TState>(TState previousState, IAction action);

	public delegate Task Dispatcher(IAction action);

	public delegate Task<TState> Dispatcher<TState>(TState state, IAction Action);

	public delegate Func<Dispatcher<TState>, Dispatcher<TState>> Middleware<in TContext, TState>(TContext context)
		where TContext: IStoreContext;

}
