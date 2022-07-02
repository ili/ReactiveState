using System;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ReactiveState
{
	public static class Middlewares
	{
		public static MiddlewareBuilder<TState, TContext> UseReducers<TContext, TState>(
			this MiddlewareBuilder<TState, TContext> builder,
			params Reducer<TState?, IAction>[] reducers)
			where TContext : IDispatchContext<TState>
			=> builder.Use(
				next => (context) =>
				{
					var state = context.OriginalState;
					var newState = state;
					foreach (var r in reducers)
						newState = r(newState, context.Action);

					context.SetState(newState);

					return next(context);
				}
			);

		public static MiddlewareBuilder<TState, TContext> UseNotification<TContext, TState>(
			this MiddlewareBuilder<TState, TContext> builder)
			where TContext : IDispatchContext<TState>
			=> builder.Use(
				next => (context) =>
				{
					var state = context.OriginalState;
					var newState = context.NewState;

					if (newState?.Equals(state) == false || (newState != null && state == null))
						context.StateEmitter.OnNext(newState);

					return next(context);
				}
			);


		public static MiddlewareBuilder<TState, TContext> UseBeforeHook<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder,
			params Func<TState?, IAction, bool>[] hooks)
			where TContext : IDispatchContext<TState>
			=> builder.Use(next => (context) =>
			{
				foreach (var h in hooks)
					if (!h(context.OriginalState, context.Action))
						return Task.FromResult(context.OriginalState);

				return next(context);
			});

		public static MiddlewareBuilder<TState, TContext> UseBeforeHook<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder,
			params Func<TState?, IAction, Task<bool>>[] hooks)
			where TContext : IDispatchContext<TState>
			=> builder.Use(next => async (context) =>
			{
				foreach (var h in hooks)
					if (!await h(context.OriginalState, context.Action))
						return context.OriginalState;

				return await next(context);
			});

		public static MiddlewareBuilder<TState, TContext> UseAfterHook<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder,
			params Action<TState?, TState?, IAction>[] hooks)
			where TContext : IDispatchContext<TState>
			=> builder.Use(next => async (context) =>
			{
				var res = await next(context);

				foreach (var h in hooks)
					h(context.OriginalState, context.NewState, context.Action);

				return res;
			});

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder,
			params Func<IObservable<TContext>, IObservable<IAction>>[] effects)
			where TContext : IDispatchContext<TState>
		{
			var requests = new Subject<TContext>();
			IDispatcher<TState>? dispatcher = null;

			var actions = effects
					.Select(_ => _(requests))
					.Merge()
					.Where(_ => _ != null)
					.Subscribe(async _ => await dispatcher!.Dispatch(_));

			builder.DisposeWith.Add(requests);

			return builder.Use(next => async (context) =>
			{
				try
				{
					dispatcher = context.Dispatcher;
					return await next(context);
				}
				finally
				{
					requests.OnNext(context);
				}
			});
		}

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder, params Func<TContext, TState?, IAction, Task<IAction?>>[] effects)
			where TContext : IDispatchContext<TState>
			=> UseEffects(builder, effects.Select(x => Tools.EffectWrapper<TContext, TState>(x)).ToArray());

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder, params Func<TState?, IAction, Task<IAction?>>[] effects)
			where TContext : IDispatchContext<TState>
			=> UseEffects(builder, effects.Select(x => Tools.EffectWrapper<TContext, TState>(x)).ToArray());

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder, params Func<TState?, IAction, IAction?>[] effects)
			where TContext : IDispatchContext<TState>
			=> UseEffects(builder, effects.Select(x => Tools.EffectWrapper<TContext, TState>(x)).ToArray());

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> builder, params Func<TContext, Task<IAction?>>[] effects)
			where TContext : IDispatchContext<TState>
		{

			var ctx = Expression.Parameter(typeof(TContext), "ctx");

			var eff = Expression.Convert(Expression.NewArrayInit(typeof(Task<IAction?>),
				effects.Select(e => Expression.Invoke(Expression.Constant(e), ctx))),
				typeof(IEnumerable<Task<IAction>>))
				;

			var body = Expression.Call(typeof(Task), nameof(Task.WhenAll), new[] { typeof(IAction) },
				eff);

			var lambda = Expression.Lambda<Func<TContext, Task<IAction[]>>>(body, ctx);

			var invoker = lambda.Compile();

			return builder.Use(next => async (context) =>
			{
				TState? res;
				try
				{
					res = await next(context);
				}
				finally
				{

					var newActions = await invoker(context);

					//var newActions = new List<IAction>();

					//foreach (var e in effects)
					//{
					//	var newAction = await e(context);
					//	if (newAction != null)
					//		newActions.Add(newAction);
					//}

					foreach (var a in newActions.Where(_ => _ != null))
						res = await context.Dispatcher.Dispatch(a!);
				}

				return res;
			});
		}
	}
}
