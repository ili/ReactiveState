using System;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ReactiveState
{
	public static class Middlewares
	{
		public static MiddlewareBuilder<TState, TContext> UseReducers<TContext, TState>(
			this MiddlewareBuilder<TState, TContext> dispatcherBuilder,
			params Reducer<TState, IAction>[] reducers)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(
				next => (context) =>
				{
					var state = context.OriginalState;
					var newState = state;
					foreach (var r in reducers)
						newState = r(newState, context.Action);

					context.NewState = newState;

					return next(context);
				}
			);

		public static MiddlewareBuilder<TState, TContext> UseNotification<TContext, TState>(
			this MiddlewareBuilder<TState, TContext> dispatcherBuilder)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(
				next => (context) =>
				{
					var state = context.OriginalState;
					var newState = context.NewState;

					if (newState?.Equals(state) == false || (newState != null && state == null))
						context.StateEmitter.OnNext(newState);

					return next(context);
				}
			);


		public static MiddlewareBuilder<TState, TContext> UseBeforeHook<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder,
			params Func<TState, IAction, bool>[] hooks)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => (context) =>
			{
				foreach (var h in hooks)
					if (!h(context.OriginalState, context.Action))
						return Task.CompletedTask;

				return next(context);
			});

		public static MiddlewareBuilder<TState, TContext> UseBeforeHook<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder,
			params Func<TState, IAction, Task<bool>>[] hooks)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => async (context) =>
			{
				foreach (var h in hooks)
					if (!await h(context.OriginalState, context.Action))
						return;

				await next(context);
			});

		public static MiddlewareBuilder<TState, TContext> UseAfterHook<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder,
			params Action<TState, TState?, IAction>[] hooks)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => async (context) =>
			{
				await next(context);

				foreach (var h in hooks)
					h(context.OriginalState, context.NewState, context.Action);
			});

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder,
			params Func<IObservable<(TContext Context, TState State, IAction Action)>, IObservable<(TContext Context, IAction Action)>>[] effects)
			where TContext : IDispatchContext<TState>
		{
			var requests = new Subject<(TContext Context, TState State, IAction Action)>();

			dispatcherBuilder.Free.Add(
				effects
					.Select(_ => _(requests))
					.Merge()
					.Where(_ => _.Action != null)
				.Subscribe(async _ => await _.Context.Dispatcher.Dispatch(_.Action)));

			dispatcherBuilder.Free.Add(requests);

			return dispatcherBuilder.Use(next => async (context) =>
			{
				try
				{
					await next(context);
				}
				finally
				{
					requests.OnNext((context, context.OriginalState, context.Action));
				}
			});
		}

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcher, params Func<TState, IAction, Task<IAction>>[] effects)
			where TContext : IDispatchContext<TState>
			=> UseEffects(dispatcher, effects.Select(_ => _.Wrap<TContext, TState, IAction, Task<IAction>>()).ToArray());

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, params Func<TState, IAction, IAction>[] effects)
			where TContext : IDispatchContext<TState>
			=> UseEffects(dispatcherBuilder, effects.Select<Func<TState, IAction, IAction>, Func<TContext, TState, IAction, Task<IAction>>>(e => (TContext c, TState s, IAction a) => Task.FromResult((IAction)e(s, a))).ToArray());

		public static MiddlewareBuilder<TState, TContext> UseEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, params Func<TContext, TState, IAction, Task<IAction>>[] effects)
			where TContext : IDispatchContext<TState>
		{
			return dispatcherBuilder.Use(next => async (context) =>
			{

				try
				{
					await next(context);
				}
				finally
				{
					var state = context.NewState ?? context.OriginalState;
					var newActions = new List<IAction>();

					foreach (var e in effects)
					{
						var newAction = await e(context, state, context.Action);
						if (newAction != null)
							newActions.Add(newAction);
					}

					foreach (var a in newActions)
						await context.Dispatcher.Dispatch(a);
				}
			});
		}

		public static MiddlewareBuilder<TState, TContext> UseStateEffects<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, params Func<TContext, TState, Task<IAction>>[] effects)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(
				next => async (context) =>
				{
					try
					{
						await next(context);
					}
					finally
					{
						var actions = new List<IAction>();
						var state = context.NewState ?? context.OriginalState;

						foreach (var e in effects)
						{
							var newAction = await e(context, state);
							if (newAction != null)
								actions.Add(newAction);
						}

						foreach (var a in actions)
							await context.Dispatcher.Dispatch(a);
					}
				}
			);
		/*

		public static MiddlewareBuilder<TState, TContext> StateEffectMiddleware<TContext, TState>(this MiddlewareBuilder<TState, TContext> dispatcherBuilder, params Func<TContext, IObservable<TState>, IObservable<IAction>>[] effects)
			where TContext : IDispatchContext<TState>
		{
			var source = new Subject<(TContext ctx, TState st)>();
			source
				.SelectMany(s => effects
					.Select(_ => _(s.ctx, s.st))
					.Merge(),)

			dispatcherBuilder.Free.Add(Observable.CombineLatest(
				contexts.DistinctUntilChanged(),
				states
				
				(ctx, a) => (ctx, a))
				.Subscribe(async _ => await _.ctx.Dispatcher.Dispatch(_.a)));

			dispatcherBuilder.Free.Add(states);
			dispatcherBuilder.Free.Add(contexts);

			return dispatcherBuilder.Use(next => async (context) =>
			{
				try
				{
					await next(context);
				}
				finally
				{
					contexts.OnNext(context);
					states.OnNext(context.NewState ?? context.OriginalState);
				}
			});
		}

		

			=> (context) =>
			{
				var states = new Subject<TState>();

				var d = dispatcher ?? context;

				effects
					.Select(_ => _(context, states))
					.Merge()
					.Where(_ => _ != null)
					.Subscribe(async _ => await d.Dispatch(_));

				return next => async (state, action) =>
				{
					var res = state;
					try
					{
						return res = await next(state, action);
					}
					finally
					{
						states.OnNext(res);
					}
				};
			};
		 */
	}
}
