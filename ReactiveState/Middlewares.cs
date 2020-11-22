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
		public static DispatcherBuilder<TState, TContext> UseReducers<TContext, TState>(
			this DispatcherBuilder<TState, TContext> dispatcherBuilder,
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

		public static DispatcherBuilder<TState, TContext> UseNotification<TContext, TState>(
			this DispatcherBuilder<TState, TContext> dispatcherBuilder)
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


		public static DispatcherBuilder<TState, TContext> UseBeforeHook<TContext, TState>(this DispatcherBuilder<TState, TContext> dispatcherBuilder,
			params Func<TState, IAction, bool>[] hooks)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => (context) =>
			{
				foreach (var h in hooks)
					if (!h(context.OriginalState, context.Action))
						return Task.CompletedTask;

				return next(context);
			});

		public static DispatcherBuilder<TState, TContext> UseBeforeHook<TContext, TState>(this DispatcherBuilder<TState, TContext> dispatcherBuilder,
			params Func<TState, IAction, Task<bool>>[] hooks)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => async (context) =>
			{
				foreach (var h in hooks)
					if (!await h(context.OriginalState, context.Action))
						return;

				await next(context);
			});

		public static DispatcherBuilder<TState, TContext> UseAfterHook<TContext, TState>(this DispatcherBuilder<TState, TContext> dispatcherBuilder,
			params Action<TState, TState?, IAction>[] hooks)
			where TContext : IDispatchContext<TState>
			=> dispatcherBuilder.Use(next => async (context) =>
			{
				await next(context);

				foreach (var h in hooks)
					h(context.OriginalState, context.NewState, context.Action);
			});

		/*
		public static Middleware<TContext, TState> EffectMiddleware<TContext, TState>(IDispatcher dispatcher, params Func<IObservable<(TState State, IAction Action)>, IObservable<IAction>>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> (context) =>
			{
				var subject = new Subject<(TState, IAction)>();

				effects
					.Select(_ => _(subject))
					.Merge()
					.Subscribe(async _ => await (dispatcher ?? context).Dispatch(_));

				return next => async (state, action) =>
				{
					var res = state;
					try
					{
						return res = await next(state, action);
					}
					finally
					{
						subject.OnNext((res, action));
					}
				};
			};

		public static Middleware<TContext, TState> EffectMiddleware<TContext, TState>(IDispatcher dispatcher, IStateTree<TState> stateTree, params Func<TState, IAction, IAction>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> EffectMiddleware(dispatcher, effects.Select(_ => _.Wrap<TContext, TState, IAction, IAction>(stateTree)).ToArray());

		public static Middleware<TContext, TState> EffectMiddleware<TContext, TState>(IDispatcher dispatcher, IStateTree<TState> stateTree, params Func<TState, IAction, Task<IAction>>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> EffectMiddleware(dispatcher, effects.Select(_ => _.Wrap<TContext, TState, IAction, Task<IAction>>(stateTree)).ToArray());

		public static Middleware<TContext, TState> EffectMiddleware<TContext, TState>(IDispatcher dispatcher, IStateTree<TState> stateTree, params Func<TContext, TState, IAction, IAction>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> EffectMiddleware(dispatcher, effects.Select(_ => _.Wrap<TContext, TState, IAction, IAction>(stateTree)).ToArray());

		public static Middleware<TContext, TState> EffectMiddleware<TContext, TState>(IDispatcher dispatcher, params Func<TContext, TState, IAction, Task<IAction>>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> (context) =>
			{
				return next => async (state, action) =>
				{
					var res = state;
					try
					{
						return res = await next(state, action);
					}
					finally
					{
						foreach (var e in effects)
						{
							var newAction = await e(context, res, action);
							if (newAction != null)
								await (dispatcher ?? context).Dispatch(newAction);
						}
					}
				};
			};

		public static Middleware<TContext, TState> StateEffectMiddleware<TContext, TState>(IDispatcher dispatcher, params Func<TContext, TState, Task<IAction>>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> (context) =>
			{
				return next => async (state, action) =>
				{
					var res = state;
					try
					{
						return res = await next(state, action);
					}
					finally
					{
						var actions = new List<IAction>();
						foreach (var e in effects)
						{
							var newAction = await e(context, res);
							if (newAction != null)
								actions.Add(newAction);
						}

						var d = dispatcher ?? context;
						foreach (var a in actions)
							await d.Dispatch(a);
					}
				};
			};

		public static Middleware<TContext, TState> StateEffectMiddleware<TContext, TState>(IDispatcher dispatcher, params Func<TContext, IObservable<TState>, IObservable<IAction>>[] effects)
			where TContext : IStoreContext, IDispatcher
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
