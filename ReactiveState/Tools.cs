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
		public static IEnumerable<Reducer<TState, IAction>> Reducers<TState>(this Assembly assembly, IStateTree<TState> stateTree)
			=> assembly.GetTypes().SelectMany(_ => _.Reducers<TState>(stateTree));

		public static IEnumerable<Reducer<TState, IAction>> Reducers<TState>(this Type type, IStateTree<TState> stateTree)
			=> ReadonlyStaticFields(type)
			.Where(fi => fi.FieldType.LikeReducer(stateTree))
			.Select(_ => _.GetValue(null))
			.Select(_ => ReducerWrapper(_, stateTree));

		public static bool LikeReducer<TState>(this Type type, IStateTree<TState> stateTree)
		{
			if (!type.Like<Reducer<object, IAction>>())
				return false;

			var actualStateType = type.GetGenericArguments()[0];

			if (actualStateType == typeof(TState))
				return true;

			if (stateTree == null)
				return false;

			return stateTree.FindGetter(actualStateType) != null;
		}

		public static Reducer<TState, IAction> Wrap<TState, TAction>(this Reducer<TState, TAction> reducer)
			where TAction: IAction
			=> ReducerWrapper<TState>(reducer, null);

		public static Reducer<TState, IAction> Wrap<TState, TSubState, TAction>(this Reducer<TSubState, TAction> reducer, IStateTree<TState> stateTree)
			where TAction: IAction
			=> ReducerWrapper<TState>(reducer, stateTree);

		public static Reducer<TState, IAction> ReducerWrapper<TState>(object arg, IStateTree<TState> stateTree)
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
					stateTree.FindGetter(actualStateType),
					stateParameter);
				if (getterExpression == null)
					throw new InvalidOperationException($"{typeof(TState).FullName} does not have subtree of {actualStateType.FullName} type");

				var composer = stateTree.FindComposer(actualStateType);

				reducerExpression = Expression.Lambda(
					Expression.Invoke(composer,
						stateParameter,
						Expression.Invoke(reducerExpression, 
							getterExpression,
							actionParametrer)),
					stateParameter,
					actionParametrer);
			}

			var mi = new object().GetType().GetMethod(nameof(object.GetType));
			var isAssignableFrom = typeof(Type).GetMethod(nameof(Type.IsAssignableFrom));
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

		public static IEnumerable<Func<IObservable<(TState, IAction)>, IObservable<IAction>>> ObservableEffects<TState>(this Assembly assembly)
			=> ReadonlyStaticFields<Func<IObservable<(TState, IAction)>, IObservable<IAction>>>(assembly);

		public static IEnumerable<Func<IObservable<(TState, IAction)>, IObservable<IAction>>> ObservableEffects<TState>(this Type type)
			=> ReadonlyStaticFields<Func<IObservable<(TState, IAction)>, IObservable<IAction>>>(type);

		public static IEnumerable<Func<TStoreContext, TState, IAction, Task<IAction>>> Effects<TStoreContext, TState>(this Type type, IStateTree<TState> stateTree)
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeEffect<TStoreContext, TState>(stateTree))
			.Select(_ => _.GetValue(null))
			.Select(_ => EffectWrapper<TStoreContext, TState>(_, stateTree))
			;

		public static bool LikeEffect<TContext, TState>(this Type type, IStateTree<TState> stateTree)
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

			if (stateTree == null)
				return false;

			return stateTree.FindGetter(actualStateType) != null;
		}

		public static IEnumerable<Func<TStoreContext, TState, Task<IAction>>> StateEffects<TStoreContext, TState>(this Type type, IStateTree<TState> stateTree)
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeStateEffect<TStoreContext, TState>(stateTree))
			.Select(_ => _.GetValue(null))
			.Select(_ => StateEffectWrapper<TStoreContext, TState>(_, stateTree))
			;

		public static IEnumerable<Func<TStoreContext, IObservable<TState>, IObservable<IAction>>> ObservableStateEffects<TStoreContext, TState>(this Type type, IStateTree<TState> stateTree)
			=> type.ReadonlyStaticFields()
			.Where (_ => _.FieldType.LikeObservableStateEffect<TStoreContext, TState>(stateTree))
			.Select(_ => _.GetValue(null))
			.Select(_ => ObservableStateEffectWrapper<TStoreContext, TState>(_, stateTree))
			;

		public static bool LikeStateEffect<TContext, TState>(this Type type, IStateTree<TState> stateTree)
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

			if (stateTree == null)
				return false;

			return stateTree.FindGetter(actualStateType) != null;
		}

		public static bool LikeObservableStateEffect<TContext, TState>(this Type type, IStateTree<TState> stateTree)
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

			if (stateTree == null)
				return false;

			return stateTree.FindGetter(actualStateType) != null;
		}

		public static IEnumerable<Func<TContext, TState, IAction, Task<IAction>>> Effects<TContext, TState>(this Assembly assembly, IStateTree<TState> stateTree)
			=> assembly.GetTypes().SelectMany(x => x.Effects<TContext, TState>(stateTree));

		public static IEnumerable<Func<TContext, TState, Task<IAction>>> StateEffects<TContext, TState>(this Assembly assembly, IStateTree<TState> stateTree)
			=> assembly.GetTypes().SelectMany(x => x.StateEffects<TContext, TState>(stateTree));

		public static IEnumerable<Func<TContext, IObservable<TState>, IObservable<IAction>>> ObservableStateEffects<TContext, TState>(this Assembly assembly, IStateTree<TState> stateTree)
			=> assembly.GetTypes().SelectMany(x => x.ObservableStateEffects<TContext, TState>(stateTree));


		public static Func<TContext, TState, IAction, Task<IAction>> Wrap<TContext, TState, TAction, TResult>(this Func<TState, TAction, TResult> func, IStateTree<TState> stateTree)
			=> EffectWrapper<TContext, TState>(func, stateTree);

		public static Func<TContext, TState, IAction, Task<IAction>> Wrap<TContext, TState, TAction, TResult>(this Func<TContext, TState, TAction, TResult> func, IStateTree<TState> stateTree)
			=> EffectWrapper<TContext, TState>(func, stateTree);

		public static Func<TContext, TState, IAction, Task<IAction>> EffectWrapper<TContext, TState>(object func, IStateTree<TState> stateTree)
		{
			var type = func.GetType();

			if (type == typeof(Func<TContext, TState, IAction, Task<IAction>>))
				return (Func<TContext, TState, IAction, Task<IAction>>)func;

			var funcGenericArguments = type.GetGenericArguments();

			if (funcGenericArguments.Length < 3 || funcGenericArguments.Length > 4)
				throw new InvalidOperationException($"Unexpected func type: {type.Name}");

			var funcReturnType  = funcGenericArguments.Last();
			var funcActionType  = funcGenericArguments[funcGenericArguments.Length - 2];
			var funcStateType   = funcGenericArguments.Length == 4 ? funcGenericArguments[1] : funcGenericArguments[0];
			var funcContextType = funcGenericArguments.Length == 4 ? funcGenericArguments[0] : null;

			var wrapperContextParam = Expression.Parameter(typeof(TContext), "context");
			var wrapperStateParam   = Expression.Parameter(typeof(TState),   "state");
			var wrapperActionParam  = Expression.Parameter(typeof(IAction),  "action");

			var invokeActionParam = funcActionType == typeof(IAction)
				? (Expression)wrapperActionParam
				: Expression.Convert(wrapperActionParam, funcActionType);

			var invokeStateParam = funcStateType == typeof(TState)
				? (Expression)wrapperStateParam
				: funcStateType.IsAssignableFrom(typeof(TState))
					? Expression.Convert(wrapperStateParam, funcStateType)
					: null;

			if (!funcStateType.IsAssignableFrom(typeof(TState)))
			{
				if (stateTree == null)
					throw new ArgumentNullException(nameof(stateTree));

				var getterExpression = stateTree.FindGetter(funcStateType);
				if (getterExpression == null)
					throw new InvalidOperationException($"{typeof(TState).FullName} does not have subtree of {funcStateType.FullName} type");

				invokeStateParam = Expression.Invoke(getterExpression, wrapperStateParam);
			}

			var invokeContextParam = funcContextType == typeof(TContext) || funcContextType == null
				? (Expression)wrapperContextParam
				: Expression.Convert(wrapperContextParam, funcContextType);

			var funcExpression = Expression.Constant(func);

			Expression invokeExpression = funcContextType == null
				? Expression.Invoke(funcExpression,                     invokeStateParam, invokeActionParam)
				: Expression.Invoke(funcExpression, invokeContextParam, invokeStateParam, invokeActionParam);

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

			var iifExpression = Expression.Condition(
				Expression.Equal(Expression.Call(wrapperActionParam, mi), Expression.Constant(funcActionType)),
				invokeExpression,
				Expression.Constant(Task.FromResult<IAction>(null), typeof(Task<IAction>)));

			var body = funcActionType == typeof(IAction)
				? invokeExpression
				: iifExpression;

			var wrapper = Expression.Lambda<Func<TContext, TState, IAction, Task<IAction>>>(body, wrapperContextParam, wrapperStateParam, wrapperActionParam);

			return wrapper.Compile();
		}

		public static Func<TContext, TState, Task<IAction>> StateEffectWrapper<TContext, TState>(object func, IStateTree<TState> stateTree)
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
			{
				if (stateTree == null)
					throw new ArgumentNullException(nameof(stateTree));

				var getterExpression = stateTree.FindGetter(funcStateType);
				if (getterExpression == null)
					throw new InvalidOperationException($"{typeof(TState).FullName} does not have subtree of {funcStateType.FullName} type");

				invokeStateParam = Expression.Invoke(getterExpression, wrapperStateParam);
			}

			var invokeContextParam = funcContextType == typeof(TContext) || funcContextType == null
				? (Expression)wrapperContextParam
				: Expression.Convert(wrapperContextParam, funcContextType);

			var funcExpression = Expression.Constant(func);

			Expression invokeExpression = funcContextType == null
				? Expression.Invoke(funcExpression,                     invokeStateParam)
				: Expression.Invoke(funcExpression, invokeContextParam, invokeStateParam);

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

		public static Func<TContext, IObservable<TState>, IObservable<IAction>> ObservableStateEffectWrapper<TContext, TState>(object func, IStateTree<TState> stateTree)
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
			{
				if (stateTree == null)
					throw new ArgumentNullException(nameof(stateTree));

				var getterExpression = stateTree.FindGetter(funcStateType);
				if (getterExpression == null)
					throw new InvalidOperationException($"{typeof(TState).FullName} does not have subtree of {funcStateType.FullName} type");

				// var selectorType = typeof(Func<,>).MakeGenericType(typeof(TState), funcStateType);
				var getter = ((LambdaExpression)getterExpression).Compile();

				invokeStateParam = Expression.Call(typeof(Observable), nameof(Observable.Select),
					new[] { typeof(TState), funcStateType },
					wrapperStateParam, Expression.Constant(getter));
			}

			var invokeContextParam = funcContextType == typeof(TContext) || funcContextType == null
				? (Expression)wrapperContextParam
				: Expression.Convert(wrapperContextParam, funcContextType);

			var funcExpression = Expression.Constant(func);

			Expression invokeExpression = funcContextType == null
				? Expression.Invoke(funcExpression,                     invokeStateParam)
				: Expression.Invoke(funcExpression, invokeContextParam, invokeStateParam);

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
			.Select(_ => (T)_.GetValue(null));

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

				return tp.FullName;
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

				if (!targetType.GetGenericTypeDefinition().Like(patternType.GetGenericTypeDefinition()))
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

		public static TField GetOrDefault<TObject, TField>(this TObject obj, Func<TObject, TField> getter, TField def = default(TField))
			=> obj == null ? def : getter(obj);

		public static StateTreeBuilder<TObject> With<TObject, TNode>(this StateTreeBuilder<TObject> builder, Expression<Func<TObject, TNode>> getter)
			=> builder.With(getter, Builder.Clone<TObject>().Add(getter).Build());

		public static StateTreeBuilder<TObject> With<TObject, TNode>(this StateTreeBuilder<TObject> builder, Expression<Func<TObject, TNode>> getter, IStateTree<TNode> nodeTree)
			=> builder.With(getter, Builder.Clone<TObject>().Add(getter).Build(), nodeTree);
	}
}
