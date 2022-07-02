using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ReactiveState
{
	public delegate TState? Reducer<TState, in TAction>(TState? previousState, TAction action)
		where TAction: IAction;

	//public delegate TState Reducer<TState>(TState previousState, IAction action);

	//public delegate Func<Dispatcher<TState, TContext>, Dispatcher<TState, TContext>> Middleware<in TContext, TState>(TContext context)
	//	where TContext : IDispatchContext<TState>;

	public delegate Task<TState?> Dispatcher<TState, in TContext>(TContext context)
		where TContext : IDispatchContext<TState>;
}
