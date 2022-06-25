using ReactiveState.ComplexState;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ReactiveState
{
	public static class Tools
	{
		public static IEnumerable<Reducer<TState, IAction>> Reducers<TState>(this Assembly assembly)
			=> assembly.GetTypes().SelectMany(_ => _.Reducers<TState>());

		public static IEnumerable<Reducer<TState, IAction>> Reducers<TState>(this Type type)
			=> ReadonlyStaticFields(type)
			.Where(fi => fi.FieldType.LikeReducer<TState>())
			.Select(_ => _.GetValue(null)!)
			.Select(_ => ReducerWrapper<TState>(_));

		public static bool LikeReducer<T>(this Type type) => type.Like<Reducer<T, IAction>>();

		public static bool LikeReducer(this Type type) => type.LikeReducer<object>();

		public static Reducer<TState, IAction> Wrap<TState, TAction>(this Reducer<TState, TAction> reducer)
			where TAction: IAction
			=> ReducerWrapper<TState>(reducer);

		public static Reducer<TState, IAction> ReducerWrapper<TState>(object reducer)
		{
			var type = reducer.GetType();

			if (type == typeof(Reducer<TState, IAction>))
				return (Reducer<TState, IAction>)reducer;

			var actualStateType = type.GetGenericArguments()[0];
			var actionType = type.GetGenericArguments()[1];

			Expression reducerExpression = Expression.Constant(reducer);

			var state = Expression.Parameter(typeof(TState), "state");
			var action = Expression.Parameter(typeof(IAction), "action");

			var mi = new object().GetType().GetMethod(nameof(object.GetType))!;
			var isAssignableFrom = typeof(Type).GetMethod(nameof(Type.IsAssignableFrom))!;
			var isAssignableFromExpression =
				Expression.Call(
					Expression.Constant(actionType),
					isAssignableFrom,
					Expression.Call(action, mi)
					);

			var ifExpression = Expression.Condition(
				isAssignableFromExpression,
				Expression.Invoke(reducerExpression, state, Expression.Convert(action, actionType)),
				state);

			var wrapper = Expression.Lambda<Reducer<TState, IAction>>(ifExpression, state, action);

			return wrapper.Compile();
		}

		public static Reducer<IState, IAction> BuildComplexReducer(params object[] reducers)
		{
			var stateParam = Expression.Parameter(typeof(IState), "state");
			var actionParam = Expression.Parameter(typeof(IAction), "action");

			var mutableState = Expression.Variable(typeof(IMutableState), "mutable");
			var assignMutableState = Expression.Assign(mutableState,
				Expression.Call(stateParam, typeof(IState).GetMethod(nameof(IState.BeginTransaction))!));

			//var actualActionType = Expression.Variable(typeof(Type), "actualActionType");
			//var assignActualActionType = Expression.Assign(actualActionType,
			//	Expression.Call(actionParam, typeof(object).GetMethod(nameof(object.GetType))!));

			var reducerDescriptors = reducers.Select(_ => _)
				.Select(_ => new
				{
					StateType = _.GetType().GetGenericArguments()[0],
					ActionType = _.GetType().GetGenericArguments()[1],
					Method = _
				})
				.GroupBy(_ => _.ActionType)
				.Select(_ => new
				{
					ActionType = _.Key,
					Reducers = _.ToList()
				});

			var parameters = new List<ParameterExpression>();
			var invocations = new List<Expression>();

			var ifInvocations = reducerDescriptors.Select(r =>
			{
				//var condition = Expression.Call(
				//	Expression.Constant(r.ActionType),
				//	typeof(Type).GetMethod(nameof(Type.IsAssignableFrom))!,
				//	actualActionType
				//	);

				// var condition = Expression.TypeIs(actionParam, r.ActionType);

				var typedAction = Expression.Variable(r.ActionType);
				var assignTypedAction = Expression.Assign(typedAction, Expression.TypeAs(actionParam, r.ActionType));
				var condition = Expression.NotEqual(typedAction, Expression.Constant(null, r.ActionType));

				parameters.Add(typedAction);
				invocations.Add(assignTypedAction);

				var calls = r.Reducers.Select(rd =>
				{
					var key = Expression.Constant(rd.StateType.FullName);
					var getMethodInfo = typeof(IPersistentState)
						.GetMethod(nameof(IPersistentState.Get))!
						.MakeGenericMethod(rd.StateType);

					var getValue = Expression.Call(mutableState,
						getMethodInfo,
						//nameof(IMutableState.Get),
						//new[] { rd.StateType },
						key);

					var invokeReducer = Expression.Invoke(Expression.Constant(rd.Method),
						getValue,
						typedAction
						);

					var invokeSetter = Expression.Call(mutableState,
						nameof(IMutableState.Set),
						new[] { rd.StateType },
						key,
						invokeReducer);

					return invokeSetter;
				})
				.ToList();

				var ifExpression = Expression.IfThen(condition,
					Expression.Block(calls));

				return ifExpression;
			})
			.ToList();

			var commitExpression = Expression.Call(mutableState,
				typeof(IMutableState).GetMethod(nameof(IMutableState.Commit))!);

			//var returnTarget = Expression.Label(typeof(IState));
			//var returnExpression = Expression.Return(returnTarget,
			//	commitExpression,
			//	typeof(IState));

			//var returnLabel = Expression.Label(returnTarget, Expression.Constant(null, typeof(IState)));

			//invocations.Add(assignActualActionType);
			invocations.Add(assignMutableState);

			invocations.AddRange(ifInvocations);

			invocations.Add(commitExpression);
			//invocations.Add(returnExpression);
			//invocations.Add(returnLabel);
			parameters.Add(mutableState);
			//parameters.Add(actualActionType);

			var body = Expression.Block(/*new ParameterExpression[]
				{
					mutableState,
					actualActionType
				},*/
				parameters,
				invocations
			);

			var reducerExpression = Expression.Lambda<Reducer<IState, IAction>>(body,
				stateParam,
				actionParam);

			return reducerExpression.Compile();
		}

		public static IEnumerable<Func<IObservable<(TState, IAction)>, IObservable<IAction>>> ObservableEffects<TState>(this Assembly assembly)
			=> ReadonlyStaticFields<Func<IObservable<(TState, IAction)>, IObservable<IAction>>>(assembly);

		public static IEnumerable<Func<IObservable<(TState, IAction)>, IObservable<IAction>>> ObservableEffects<TState>(this Type type)
			=> ReadonlyStaticFields<Func<IObservable<(TState, IAction)>, IObservable<IAction>>>(type);

		public static IEnumerable<Func<TContext, Task<IAction?>>> Effects<TContext, TState>(this Type type)
			where TContext : IDispatchContext<TState>
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeEffect<TContext, TState>())
			.Select(_ => _.GetValue(null)!)
			.Select(_ => EffectWrapper<TContext, TState>(_))
			;

		public static bool LikeEffect<TContext, TState>(this Type type)
			where TContext : IDispatchContext<TState>
		{
			var looksLikeEffect = 
				type.Like<Func<TContext, object, IAction, IAction>>()       ||
				type.Like<Func<TContext, object, IAction, Task<IAction>>>() ||
				type.Like<Func<          object, IAction, IAction>>()       ||
				type.Like<Func<          object, IAction, Task<IAction>>>();

			if (looksLikeEffect == false)
				return false;

			var funcGenericArguments = type.GetGenericArguments();
			var actualStateType = funcGenericArguments.Length == 4 ? funcGenericArguments[1] : funcGenericArguments[0];

			if (actualStateType == typeof(TState))
				return true;

			return false;
		}
		public static IEnumerable<Func<IDispatchContext<TState>, TState, Task<IAction>>> StateEffects<TState>(this Type type)
			=> StateEffects<IDispatchContext<TState>, TState>(type);

		public static IEnumerable<Func<TStoreContext, TState, Task<IAction>>> StateEffects<TStoreContext, TState>(this Type type)
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeStateEffect<TStoreContext, TState>())
			.Select(_ => _.GetValue(null)!)
			.Select(_ => StateEffectWrapper<TStoreContext, TState>(_))
			;

		public static IEnumerable<Func<TStoreContext, IObservable<TState>, IObservable<IAction>>> ObservableStateEffects<TStoreContext, TState>(this Type type)
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeObservableStateEffect<TStoreContext, TState>())
			.Select(_ => _.GetValue(null)!)
			.Select(_ => ObservableStateEffectWrapper<TStoreContext, TState>(_))
			;

		public static bool LikeStateEffect<TContext, TState>(this Type type)
		{
			var looksLikeEffect =
				type.Like<Func<TContext, object, IAction>>()       ||
				type.Like<Func<TContext, object, Task<IAction>>>() ||
				type.Like<Func<          object, IAction>>()       ||
				type.Like<Func<          object, Task<IAction>>>()
				;

			if (looksLikeEffect == false)
				return false;

			var funcGenericArguments = type.GetGenericArguments();
			var actualStateType = funcGenericArguments.Length == 3 ? funcGenericArguments[1] : funcGenericArguments[0];

			if (actualStateType == typeof(TState))
				return true;

			return false;
		}

		public static bool LikeObservableStateEffect<TContext, TState>(this Type type)
		{
			var looksLikeEffect =
				type.Like<Func<TContext, IObservable<object>, IObservable<IAction>>>() ||
				type.Like<Func<          IObservable<object>, IObservable<IAction>>>()
				;

			if (looksLikeEffect == false)
				return false;

			var funcGenericArguments = type.GetGenericArguments();
			var actualStateType = (funcGenericArguments.Length == 3 ? funcGenericArguments[1] : funcGenericArguments[0])
				.GetGenericArguments()[0];

			if (actualStateType == typeof(TState))
				return true;


			return false;
		}

		public static IEnumerable<Func<TContext, Task<IAction?>>> Effects<TContext, TState>(this Assembly assembly)
			where TContext : IDispatchContext<TState>
			=> assembly.GetTypes().SelectMany(x => x.Effects<TContext, TState>());

		public static IEnumerable<Func<TContext, TState, Task<IAction>>> StateEffects<TContext, TState>(this Assembly assembly)
			=> assembly.GetTypes().SelectMany(x => x.StateEffects<TContext, TState>());

		public static IEnumerable<Func<TContext, IObservable<TState>, IObservable<IAction>>> ObservableStateEffects<TContext, TState>(this Assembly assembly)
			=> assembly.GetTypes().SelectMany(x => x.ObservableStateEffects<TContext, TState>());


		public static Func<TContext, Task<IAction?>> Wrap<TContext, TState, TAction, TResult>(this Func<TState, TAction, TResult> func)
			where TContext : IDispatchContext<TState>
			where TAction : IAction
			=> EffectWrapper<TContext, TState>(func);


		public static Func<TContext, Task<IAction?>> Wrap<TContext, TState, TAction, TResult>(this Func<TContext, TState?, TAction, TResult?> func)
			where TContext : IDispatchContext<TState>
			where TAction : IAction
			=> EffectWrapper<TContext, TState>(func);

		public static Func<TContext, Task<IAction?>> EffectWrapper<TContext, TState>(object func)
			where TContext : IDispatchContext<TState>
		{
			var type = func.GetType();

			if (type == typeof(Func<TContext, Task<IAction>>))
				return (Func<TContext, Task<IAction?>>)func;

			var wrapperContextParam = Expression.Parameter(typeof(TContext), "context");

			var funcGenericArguments = type.GetGenericArguments();
			var funcExpression = Expression.Constant(func);
			var currentAction = Expression.Property(wrapperContextParam, nameof(IDispatchContext<TState>.Action));

			var invokeParams = new List<Expression>();

			for (var i = 0; i < funcGenericArguments.Length - 1; i++)
			{
				var invokeParamType = funcGenericArguments[i];
				Expression? invokeParam = null;

				if (invokeParamType.Like<IAction>())
				{
					invokeParam = Expression.Condition(Expression.TypeIs(currentAction, invokeParamType),
						Expression.Convert(currentAction, invokeParamType),
						Expression.Constant(null, invokeParamType)
						);
				}
				else if (invokeParamType.Like<TContext>())
				{
					invokeParam = wrapperContextParam;
				}
				else if (invokeParamType.Like<TState>())
				{
					invokeParam = Expression.Property(wrapperContextParam, nameof(IDispatchContext<TState>.OriginalState));
				}
				else if (invokeParamType.GetCustomAttribute<SubStateAttribute>() != null)
				{
					if (!typeof(TState).Like<IState>())
						throw new InvalidOperationException($"{typeof(TState).FullName} should be derived from {typeof(IState).FullName} to access substate");

					var subState = invokeParamType.GetCustomAttribute<SubStateAttribute>()!;
					string? key = subState.Key;
					if (string.IsNullOrEmpty(key))
						key = invokeParamType.FullName;

					invokeParam = Expression.Call(Expression.Property(wrapperContextParam, nameof(IDispatchContext<TState>.OriginalState)),
						nameof(IState.Get),
						new[] { typeof(string), invokeParamType },
						Expression.Constant(key!)
						);
				}
				else if(typeof(TContext).Like<IServiceProvider>())
				{
					invokeParam = Expression.Call(Expression.Convert(wrapperContextParam, typeof(IServiceProvider)),
						nameof(IServiceProvider.GetService),
						null,
						Expression.Constant(invokeParamType)
						);
				}

				if (invokeParam == null)
					throw new InvalidOperationException($"Unable to wrap effect parameter for type {invokeParamType.FullName}");

				if (invokeParam.Type != invokeParamType)
					invokeParam = Expression.Convert(invokeParam, invokeParamType);

				invokeParams.Add(invokeParam);
			};

			Expression invokeExpression = Expression.Invoke(funcExpression, invokeParams);

			var funcReturnType  = funcGenericArguments.Last();
			if (funcReturnType.Like(typeof(Task<IAction>)))
			{
				var actionType = funcReturnType.GetGenericArguments()[0];
				if (actionType != typeof(IAction))
				{
					var p = Expression.Parameter(funcReturnType, "_");

					invokeExpression = Expression.Call(invokeExpression,
						nameof(Task.ContinueWith),
						new[] { typeof(IAction) },
						Expression.Lambda(Expression.Convert(Expression.Property(p, nameof(Task<IAction>.Result)), typeof(IAction)), p));
				}
			}

			if (funcReturnType.Like(typeof(IAction)))
			{
				invokeExpression = Expression.Call(typeof(Task),
					nameof(Task.FromResult),
					new[] { typeof(IAction) },
					Expression.Convert(invokeExpression, typeof(IAction)));
			}

			var mi = new object().GetType().GetMethod(nameof(object.GetType))!;
			var iActionTypes = funcGenericArguments.Reverse().Skip(1)
				.Where(_ => _.Like<IAction>())
				.ToList();

			Expression testExpression = iActionTypes.Count > 0
				? Expression.TypeIs(currentAction, iActionTypes[0])
				: Expression.Constant(true);

			for (var i = 1; i < iActionTypes.Count; i++)
				testExpression = Expression.OrElse(testExpression, Expression.TypeIs(currentAction, iActionTypes[i]));

			var iifExpression = Expression.Condition(testExpression,
				invokeExpression,
				Expression.Constant(Task. FromResult<IAction?>(null), typeof(Task<IAction>)));

			var body = iActionTypes.Count == 0
				? invokeExpression
				: iifExpression;

			var wrapper = Expression.Lambda<Func<TContext, Task<IAction?>>>(body, wrapperContextParam);

			return wrapper.Compile();
		}

		public static Func<TContext, TState, Task<IAction>> StateEffectWrapper<TContext, TState>(object func)
		{
			var type = func.GetType();

			if (type == typeof(Func<TContext, TState, Task<IAction>>))
				return (Func<TContext, TState, Task<IAction>>)func;

			var funcGenericArguments = type.GetGenericArguments();

			if (funcGenericArguments.Length < 2 || funcGenericArguments.Length > 3)
				throw new InvalidOperationException($"Unexpected func type: {type.Name}");

			var funcReturnType  = funcGenericArguments.Last();
			var funcStateType   = funcGenericArguments.Length == 3 ? funcGenericArguments[1] : funcGenericArguments[0];
			var funcContextType = funcGenericArguments.Length == 3 ? funcGenericArguments[0] : null;

			var wrapperContextParam = Expression.Parameter(typeof(TContext), "context");
			var wrapperStateParam   = Expression.Parameter(typeof(TState),   "state");

			var invokeStateParam = funcStateType == typeof(TState)
				? (Expression)wrapperStateParam
				: funcStateType.IsAssignableFrom(typeof(TState))
					? Expression.Convert(wrapperStateParam, funcStateType)
					: null;

			if (!funcStateType.IsAssignableFrom(typeof(TState)))
				throw new InvalidOperationException("Unconvinient function type");

			var invokeContextParam = funcContextType == typeof(TContext) || funcContextType == null
				? (Expression)wrapperContextParam
				: Expression.Convert(wrapperContextParam, funcContextType);

			var funcExpression = Expression.Constant(func);

			Expression invokeExpression = funcContextType == null
				? Expression.Invoke(funcExpression,                     invokeStateParam!)
				: Expression.Invoke(funcExpression, invokeContextParam, invokeStateParam!);

			if (funcReturnType.Like(typeof(Task<IAction>)))
			{
				var actionType = funcReturnType.GetGenericArguments()[0];
				if (actionType != typeof(IAction))
				{
					var p = Expression.Parameter(funcReturnType, "_");

					invokeExpression = Expression.Call(invokeExpression,
						nameof(Task.ContinueWith),
						new[] { typeof(IAction) },
						Expression.Lambda(Expression.Convert(Expression.Property(p, nameof(Task<IAction>.Result)), typeof(IAction)), p));
				}
			}

			if (funcReturnType.Like(typeof(IAction)))
			{
				invokeExpression = Expression.Call(typeof(Task),
					nameof(Task.FromResult),
					new[] { typeof(IAction) },
					Expression.Convert(invokeExpression, typeof(IAction)));
			}

			var mi = new object().GetType().GetMethod(nameof(object.GetType));

			var body = invokeExpression;

			var wrapper = Expression.Lambda<Func<TContext, TState, Task<IAction>>>(body, wrapperContextParam, wrapperStateParam);

			return wrapper.Compile();
		}

		public static Func<TContext, IObservable<TState>, IObservable<IAction>> ObservableStateEffectWrapper<TContext, TState>(object func)
		{
			var type = func.GetType();

			if (type == typeof(Func<TContext, IObservable<TState>, IObservable<IAction>>))
				return (Func<TContext, IObservable<TState>, IObservable<IAction>>)func;

			var funcGenericArguments = type.GetGenericArguments();

			if (funcGenericArguments.Length < 2 || funcGenericArguments.Length > 3)
				throw new InvalidOperationException($"Unexpected func type: {type.Name}");

			var funcReturnType     = funcGenericArguments.Last();
			var funcObservableType = funcGenericArguments.Length == 3 ? funcGenericArguments[1] : funcGenericArguments[0];
			var funcStateType      = funcObservableType.GetGenericArguments()[0];
			var funcContextType    = funcGenericArguments.Length == 3 ? funcGenericArguments[0] : null;

			var wrapperContextParam = Expression.Parameter(typeof(TContext),              "context");
			var wrapperStateParam   = Expression.Parameter(typeof(IObservable<TState>),   "states");

			var invokeStateParam = funcStateType == typeof(TState)
				? (Expression)wrapperStateParam
				: funcStateType.IsAssignableFrom(typeof(TState))
					? Expression.Convert(wrapperStateParam, typeof(IObservable<TState>))
					: null;

			if (!funcStateType.IsAssignableFrom(typeof(TState)))
				throw new InvalidOperationException("Unconvinient function type");

			var invokeContextParam = funcContextType == typeof(TContext) || funcContextType == null
				? (Expression)wrapperContextParam
				: Expression.Convert(wrapperContextParam, funcContextType);

			var funcExpression = Expression.Constant(func);

			Expression invokeExpression = funcContextType == null
				? Expression.Invoke(funcExpression,                     invokeStateParam!)
				: Expression.Invoke(funcExpression, invokeContextParam, invokeStateParam!);

			//if (funcReturnType.Like(typeof(Task<IAction>)))
			//{
			//	var actionType = funcReturnType.GetGenericArguments()[0];
			//	if (actionType != typeof(IAction))
			//	{
			//		var p = Expression.Parameter(funcReturnType, "_");

			//		invokeExpression = Expression.Call(invokeExpression,
			//			nameof(Task.ContinueWith),
			//			new[] { typeof(IAction) },
			//			Expression.Lambda(Expression.Convert(Expression.Property(p, nameof(Task<IAction>.Result)), typeof(IAction)), p));
			//	}
			//}

			//if (funcReturnType.Like(typeof(IAction)))
			//{
			//	invokeExpression = Expression.Call(typeof(Task),
			//		nameof(Task.FromResult),
			//		new[] { typeof(IAction) },
			//		Expression.Convert(invokeExpression, typeof(IAction)));
			//}

			var mi = new object().GetType().GetMethod(nameof(object.GetType));

			var body = invokeExpression;

			var wrapper = Expression.Lambda<Func<TContext, IObservable<TState>, IObservable<IAction>>>(body, wrapperContextParam, wrapperStateParam);

			return wrapper.Compile();
		}

		public static IEnumerable<FieldInfo> ReadonlyStaticFields(this Type type) =>
			type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
			.Where(_ => _.IsInitOnly);

		public static IEnumerable<T> ReadonlyStaticFields<T>(this Type type)
			=> ReadonlyStaticFields(type).Where(_ => _.FieldType == typeof(T))
			.Select(_ => (T)_.GetValue(null)!);

		public static IEnumerable<T> ReadonlyStaticFields<T>(this Assembly assembly)
			=> assembly.GetTypes().SelectMany(_ => _.ReadonlyStaticFields<T>());

		public static string GetActionTypeName(this IAction action)
			=> GetActionTypeName(action.GetType());

		private static ConcurrentDictionary<Type, string> _actionTypes = new ConcurrentDictionary<Type, string>();
		public static string GetActionTypeName(this Type type)
			=> _actionTypes.GetOrAdd(type, tp =>
			{
				if (tp.IsGenericType)
				{
					var gt = string.Join(",",
						tp.GenericTypeArguments.Select(_ => _.Name));

					return $"{tp.Namespace}.{tp.Name.Substring(0, tp.Name.IndexOf('`'))}[{gt}]";
				}

				return tp.FullName!;
			});

		private static ConcurrentDictionary<(Type targetType, Type patternType), bool> _likes = new ConcurrentDictionary<(Type targetType, Type patternType), bool>();
		public static bool Like(this Type targetType, Type patternType)
		{
			return _likes.GetOrAdd((targetType, patternType), (pair) => LikeInternal(pair.targetType, pair.patternType));
		}

		public static bool Like<TPattern>(this Type targetType) => Like(targetType, typeof(TPattern));

		private static bool LikeInternal(Type targetType, Type patternType)
		{
			if (patternType == targetType)
				return true;

			if (targetType.IsGenericTypeDefinition && patternType.IsGenericTypeDefinition)
				return targetType.BaseType?.Like(patternType) ?? false;

			if (patternType.IsGenericType == false)
			{
				if (patternType.IsInterface == true)
					if (targetType.GetInterfaces().Contains(patternType))
						return true;

				if (targetType.BaseType != null)
					return targetType.BaseType.Like(patternType);

				return false;
			}

			if (targetType.IsGenericType == true)
			{
				var targetGenericParematers = targetType.GenericTypeArguments;
				var patternGenericParameters = patternType.GenericTypeArguments;

				if (patternType.IsInterface)
				{
					var isAny = targetType.GetInterfaces().Any(x => x.Like(patternType));
					if (!isAny && !targetType.GetGenericTypeDefinition().Like(patternType.GetGenericTypeDefinition()))
						return false;
				}
				else if (!targetType.GetGenericTypeDefinition().Like(patternType.GetGenericTypeDefinition()))
					return false;

				if (targetGenericParematers.Length != patternGenericParameters.Length)
					return false;

				for (var i = 0; i < patternGenericParameters.Length; i++)
					if (!targetGenericParematers[i].Like(patternGenericParameters[i]))
						return false;

				return true;
			}

			if (targetType.BaseType != null)
				if (targetType.BaseType.Like(patternType))
					return true;

			if (patternType.IsInterface)
				foreach (var i in targetType.GetInterfaces())
					if (i.Like(patternType))
						return true;

			return false;
		}

		public static TField? ValueOrDefault<TObject, TField>(this TObject obj, Func<TObject, TField> getter, TField? def = default(TField))
			=> obj == null ? def : getter(obj);
	}
}
