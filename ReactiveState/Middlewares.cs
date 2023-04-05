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
		public static Middleware<TContext, TState> ReducerMiddleware<TContext, TState>(params Reducer<TState, IAction>[] reducers)
			where TContext : IStateEmitter<TState>
			=> ReducerMiddleware<TContext, TState>(null, reducers);

		public static Middleware<TContext, TState> ReducerMiddleware<TContext, TState>(IStateEmitter<TState>? stateEmitter, params Reducer<TState, IAction>[] reducers)
			where TContext : IStateEmitter<TState>
			=> (context) =>
			{
				var stateParam = Expression.Parameter(typeof(TState), "state");
				var actionParam = Expression.Parameter(typeof(IAction), "action");

				var previousState = Expression.Variable(typeof(TState), "previousState");
				var newState = Expression.Variable(typeof(TState), "newState");
				var stateEmitterParam = Expression.Constant(stateEmitter ?? context, typeof(IStateEmitter<TState>));

				var assignPreviousState = Expression.Assign(previousState, stateParam);
				var calls = reducers.SelectMany(_ => new Expression[]
				{
					Expression.Assign(newState, Expression.Invoke(Expression.Constant(_), previousState, actionParam)),

					Expression.IfThen(
						Expression.Call(typeof(Middlewares), nameof(StateChanged), new [] {typeof(TState)}, newState, previousState),
						Expression.Call(stateEmitterParam, nameof(IStateEmitter<TState>.OnNext), Array.Empty<Type>(), newState)
					),

					Expression.Assign(previousState, newState)
				}
				);

				var returnTarget = Expression.Label(typeof(TState));
				var returnExpression = Expression.Return(returnTarget, newState, typeof(TState));
				var returnLabel = Expression.Label(returnTarget, newState);

				var body = new List<Expression>();
				body.Add(assignPreviousState);
				body.AddRange(calls);
				body.Add(returnExpression);
				body.Add(returnLabel);


				var complexReducerExpression = Expression.Lambda<Func<TState, IAction, TState>>(Expression.Block(new[] { newState, previousState }, body), stateParam, actionParam);

				var complexReducer = complexReducerExpression.Compile();

				return next => (state, action) =>
				{
					state = complexReducer(state, action);

					return next(state, action);
				};
			};

		public static bool StateChanged<TState>(TState newState, TState previousState) =>
			newState?.Equals(previousState) == false || (newState != null && previousState == null);

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

		//Expression<Func<Task<IAction>>> ToExpression(Func<Task<IAction>> action) => async () => await action();

		public static Middleware<TContext, TState> StateEffectMiddleware<TContext, TState>(IDispatcher dispatcher, params Func<TContext, TState, Task<IAction>>[] effects)
			where TContext : IStoreContext, IDispatcher
			=> (context) =>
			{
				/*
				var results = Expression.Variable(typeof(List<Task<IAction>>));

				var contextParam = Expression.Parameter(typeof(TContext), "context");
				var stateParam = Expression.Parameter(typeof(TState), "state");

				var assigns = effects.Select(_ => Expression.Call(results, "Add", Array.Empty<Type>(),
					Expression.Invoke(Expression.Constant(_), contextParam, stateParam))
				);


				var returnTarget = Expression.Label(typeof(List<Task<IAction>>));
				var returnExpression = Expression.Return(returnTarget, results, typeof(List<Task<IAction>>));
				var returnLabel = Expression.Label(returnTarget, results);

				var body = new List<Expression>();
				body.Add(Expression.Assign(results, Expression.New(typeof(List<Task<IAction>>))));
				body.AddRange(assigns);
				body.Add(returnExpression);
				body.Add(returnLabel);


				var complexEffectExpression = Expression.Lambda<Func<TContext, TState, List<Task<IAction>>>>(Expression.Block(new[] { results }, body), contextParam, stateParam);

				var complexEffect = complexEffectExpression.Compile();

				*/

				return next => async (state, action) =>
				{
					var res = state;
					try
					{
						return res = await next(state, action);
					}
					finally
					{
						//var start = Task.FromResult(new List<IAction>());

						//foreach (var e in effects)
						//{
						//	//start.ContinueWith(st =>
						//	//{
						//	//	var list = st.Result;
						//	//	return list;
						//	//});

						//	var x = start.ContinueWith(st =>
						//	{
						//		var list = st.Result;
						//		return e(context, res).ContinueWith(r =>
						//		{
						//			if (r.Result != null)
						//				list.Add(r.Result);

						//			return st;
						//		});
						//	}
						//	);
						//}


						var actions = new List<IAction>();
						foreach (var e in effects)
						{
							var newAction = await e(context, res);
							if (newAction != null)
							{
								actions.Add(newAction);
								//break;
							}
						}

						//var actions = await Task.WhenAll(complexEffect(context, res));

						var d = dispatcher ?? context;
						foreach (var a in actions.Where(_ => _ != null))
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
