using System;
using System.Threading.Tasks;

namespace ReactiveState
{
	public delegate TState Reducer<TState, in TAction>(TState? previousState, TAction action)
		where TAction: IAction;

	//public delegate TState Reducer<TState>(TState previousState, IAction action);

	//public delegate Func<Dispatcher<TState, TContext>, Dispatcher<TState, TContext>> Middleware<in TContext, TState>(TContext context)
	//	where TContext : IDispatchContext<TState>;

	public delegate Task Dispatcher<TState, TContext>(TContext context)
		where TContext : IDispatchContext<TState>;

	public delegate TContext ContextFactory<TState, TContext>(IAction action, TState state, IStateEmitter<TState> stateEmitter, IDispatcher dispatcher)
		where TContext : IDispatchContext<TState>;
}
