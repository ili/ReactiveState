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
		public static Middleware<TContext, TState> ReducerMiddleware<TContext, TState>(params Reducer<TState, IAction>[] reducers)
			where TContext : IStateEmitter<TState>
			=> ReducerMiddleware<TContext, TState>(null, reducers);

		public static Middleware<TContext, TState> ReducerMiddleware<TContext, TState>(IStateEmitter<TState>? stateEmitter, params Reducer<TState, IAction>[] reducers)
			where TContext : IStateEmitter<TState>
			=> (context) =>
			{
				return next => (state, action) =>
				{
					foreach (var r in reducers)
					{
						var newState = r(state, action);

						if (newState?.Equals(state) == false || (newState != null && state == null))
							(stateEmitter ?? context).OnNext(newState);

						state = newState;
					}

					return next(state, action);
				};
			};

		public static Middleware<TContext, TState> BeforeHookMiddleware<TContext, TState>(params Func<TState, IAction, bool>[] hooks)
			where TContext : IStoreContext
			=> (context) =>
			{
				return next => (state, action) =>
				{
					foreach (var h in hooks)
						if (!h(state, action))
							return Task.FromResult(state);

					return next(state, action);
				};
			};

		public static Middleware<TContext, TState> BeforeHookMiddleware<TContext, TState>(params Func<TState, IAction, Task<bool>>[] hooks)
			where TContext : IStoreContext
			=> (context) =>
			{
				return next => async (state, action) =>
				{
					foreach (var h in hooks)
						if (!await h(state, action))
							return state;

					return await next(state, action);
				};
			};

		public static Middleware<TContext, TState> AfterHookMiddleware<TContext, TState>(params Action<TState, TState, IAction>[] hooks)
			where TContext : IStoreContext
			=> (context) =>
			{
				return next => async (state, action) =>
				{
					var res = await next(state, action);

					foreach (var h in hooks)
						h(state, res, action);

					return res;
				};
			};

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
	}
}
