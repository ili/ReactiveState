using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveState
{
	public class MiddlewareBuilder<TState, TContext>
		where TContext : IDispatchContext<TState>
	{
		private readonly List<Func<Dispatcher<TState, TContext>, Dispatcher<TState, TContext>>> _middlewares = new List<Func<Dispatcher<TState, TContext>, Dispatcher<TState, TContext>>>();

		public CompositeDisposable DisposeWith { get; } = new CompositeDisposable();

		public MiddlewareBuilder<TState, TContext> Use(params Func<Dispatcher<TState, TContext>, Dispatcher<TState, TContext>>[] middlewares)
		{
			_middlewares.AddRange(middlewares);
			return this;
		}

		public Middleware<TState, TContext> Build()
		{
			Dispatcher<TState, TContext> pipe = context => Task.FromResult(context.NewState ?? context.OriginalState);

			// for (var i = 0; i < _middlewares.Count; i++)
			for (var i = _middlewares.Count-1; i >= 0; i--)
				pipe = _middlewares[i](pipe);

			return  new Middleware<TState, TContext>(pipe, DisposeWith);
		}

	}

	public static class UseExtensions
	{
		/// <summary>
		/// Uses <paramref name="dispatcher"/> as dispatcher and continues pipe
		/// </summary>
		/// <param name="dispatcherBuilder"></param>
		/// <param name="dispatcher"></param>
		public static MiddlewareBuilder<TState, TContext> Use<TState, TContext>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, Dispatcher<TState, TContext> dispatcher)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => context => { dispatcher(context); return next(context); });

		/// <summary>
		/// Uses <paramref name="action"/> as dispatcher and continues pipe
		/// </summary>
		/// <param name="dispatcherBuilder"></param>
		/// <param name="action"></param>
		public static MiddlewareBuilder<TState, TContext> Use<TState, TContext>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, Action<IAction> action)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(a => { action(a.Action); return Task.FromResult(a.OriginalState); });

		public static MiddlewareBuilder<TState, TContext> Use<TState, TContext>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, Action<TContext> action)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(a => { action(a); return Task.FromResult(a.OriginalState); });

	}

}
