using ReactiveState.ComplexState;
using ReactiveState.ComplexState.StateTree;
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

		private class ReducerParameterRewriter : ExpressionVisitor
		{
			private IReadOnlyDictionary<ParameterExpression, Expression> _rewriter;

			public ReducerParameterRewriter(IReadOnlyDictionary<ParameterExpression, Expression> rewriter)
			{
				_rewriter = rewriter;
			}

			protected override Expression VisitParameter(ParameterExpression node)
				=> _rewriter[node];

		}

		public static Reducer<TState, IAction> ReducerWrapper<TState>(object arg, IStateTree<TState>? stateTree)
		{
			var type = arg.GetType();

			if (type == typeof(Reducer<TState, IAction>))
				return (Reducer<TState, IAction>)arg;

			var actualStateType = type.GetGenericArguments()[0];
			var actionType = type.GetGenericArguments()[1];

			if (typeof(TState) != actualStateType && stateTree == null)
				throw new ArgumentNullException(nameof(stateTree));


			Expression reducerExpression = Expression.Constant(arg);

			var state = Expression.Parameter(typeof(TState), "state");
			var action = Expression.Parameter(typeof(IAction), "action");

			if (typeof(TState) != actualStateType)
			{
				var actionParametrer = Expression.Parameter(actionType);
				var stateParameter = Expression.Parameter(typeof(TState));

				var getterExpression = Expression.Invoke(
					stateTree!.FindGetter(actualStateType)!,
					stateParameter);
				if (getterExpression == null)
					throw new InvalidOperationException($"{typeof(TState).FullName} does not have subtree of {actualStateType.FullName} type");

				var composer = stateTree.FindComposer(actualStateType)!;

				reducerExpression = Expression.Lambda(
					Expression.Invoke(composer,
						stateParameter,
						Expression.Invoke(reducerExpression,
							getterExpression,
							actionParametrer)),
					stateParameter,
					actionParametrer);
			}

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
				.Select(_ => {
					var expression = _ as LambdaExpression;

					return new
					{
						StateType = expression?.Parameters[0]?.Type ?? _.GetType().GetGenericArguments()[0],
						ActionType = expression?.Parameters[1]?.Type ?? _.GetType().GetGenericArguments()[1],
						Method = _,
						Expression = expression
					};
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
					ConstantExpression key = Expression.Constant(rd.StateType.FullName);
					var subState = rd.StateType.GetCustomAttribute<SubStateAttribute>();
					if (subState != null && !string.IsNullOrEmpty(subState.Key))
						key = Expression.Constant(subState.Key);

					var getMethodInfo = typeof(IPersistentState)
						.GetMethod(nameof(IPersistentState.Get))!
						.MakeGenericMethod(rd.StateType);

					Expression getValue = Expression.Call(mutableState,
						getMethodInfo,
						key);

					if (rd.Expression == null)
					{
						var invokeReducer = Expression.Invoke(Expression.Constant(rd.Method),
								getValue,
								typedAction
							);

						Expression invokeSetter = Expression.Call(mutableState,
							nameof(IMutableState.Set),
							new[] { rd.StateType },
							key,
							invokeReducer);

						return invokeSetter;
					}
					else
					{
						var st = Expression.Parameter(getValue.Type, key.Value!.ToString()!.Replace(".", "$")!.Replace("+", "$"));
						var assignParamener = Expression.Assign(st, getValue);

						var convertedBody =	new ReducerParameterRewriter(new Dictionary<ParameterExpression, Expression>
							{
								{ rd.Expression.Parameters[0], st },
								{ rd.Expression.Parameters[1], actionParam }
							})
							.Visit(rd.Expression.Body);

						return Expression.Block(new[] {st}, assignParamener,
								Expression.Call(mutableState,
								nameof(IMutableState.Set),
								new[] { rd.StateType },
								key,
								convertedBody));							

					}
				})
				.ToList();

				var ifExpression = Expression.IfThen(condition,
					Expression.Block(calls));

				return ifExpression;
			})
			.ToList();

			var commitExpression = Expression.Call(mutableState,
				typeof(IMutableState).GetMethod(nameof(IMutableState.Commit))!);

			invocations.Add(assignMutableState);

			invocations.AddRange(ifInvocations);

			invocations.Add(commitExpression);
			parameters.Add(mutableState);

			var body = Expression.Block(parameters,
				invocations
			);

			var reducerExpression = Expression.Lambda<Reducer<IState, IAction>>(body,
				stateParam,
				actionParam);

			return reducerExpression.Compile();
		}


		public static IEnumerable<Func<IDispatchContext<TState>, Task<IAction?>>> Effects<TState>(this Type type, params Type[] patterns)
			=> Effects<IDispatchContext<TState>, TState>(type, patterns);

		public static IEnumerable<Func<TContext, Task<IAction?>>> Effects<TContext, TState>(this Type type, params Type[] patterns)
			where TContext : IDispatchContext<TState>
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeEffect<TContext, TState>(patterns))
			.Select(_ => _.GetValue(null)!)
			.Select(_ => EffectWrapper<TContext, TState>(_))
			;


		public static bool LikeEffect<TContext, TState>(this Type type, params Type[] patterns)
			where TContext : IDispatchContext<TState>
		{
			if (patterns.Length == 0)
			{
				patterns = new Type[]
				{
					typeof(Func<TContext, object, IAction, IAction>),
					typeof(Func<TContext, object, IAction, Task<IAction>>),
					typeof(Func<          object, IAction, IAction>),
					typeof(Func<          object, IAction, Task<IAction>>)
				};
			}

			var looksLikeEffect = patterns.Any(pattern => type.Like(pattern));

			if (looksLikeEffect == false)
				return false;

			var funcGenericArguments = type.GetGenericArguments();

			var canNotWrap = funcGenericArguments.Any(_ => CanWrapEffectParameter<TContext, TState>(_) == false);

			return canNotWrap == false;
		}

		private static bool CanWrapEffectParameter<TContext, TState>(Type parameterType) where TContext : IDispatchContext<TState>
		{
			return parameterType.Like<IAction>()
				|| parameterType.Like<Task<IAction>>()
				|| parameterType.Like(typeof(TState))
				|| parameterType.Like(typeof(TContext))
				|| typeof(TState).Like<IState>()
				|| typeof(TContext).Like<IServiceProvider>();
				
		}

		public static IEnumerable<Func<TStoreContext, IObservable<TState>, IObservable<IAction>>> ObservableEffects<TStoreContext, TState>(this Type type)
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeObservableEffect<TStoreContext, TState>())
			.Select(_ => _.GetValue(null)!)
			.Select(_ => ObservableEffectWrapper<TStoreContext, TState>(_))
			;

		public static bool LikeObservableEffect<TContext, TState>(this Type type)
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
		public static IEnumerable<Func<IDispatchContext<TState>, Task<IAction?>>> Effects<TState>(this Assembly assembly, params Type[] patterns)
			=> Effects<IDispatchContext<TState>, TState>(assembly, patterns);

		public static IEnumerable<Func<TContext, Task<IAction?>>> Effects<TContext, TState>(this Assembly assembly, params Type[] patterns)
			where TContext : IDispatchContext<TState>
			=> assembly.GetTypes().SelectMany(x => x.Effects<TContext, TState>(patterns));

		public static IEnumerable<Func<TContext, IObservable<TState>, IObservable<IAction>>> ObservableEffects<TContext, TState>(this Assembly assembly)
			=> assembly.GetTypes().SelectMany(x => x.ObservableEffects<TContext, TState>());


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

		public static Func<TContext, IObservable<TState>, IObservable<IAction>> ObservableEffectWrapper<TContext, TState>(object func)
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
