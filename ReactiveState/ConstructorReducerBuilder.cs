using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ReactiveState
{
	public class ConstructorReducerBuilder<TState, TAction>
		where TAction: IAction
	{
		private readonly List<KeyValuePair<MemberExpression, Expression>> _expressions = new List<KeyValuePair<MemberExpression, Expression>>();

		public ConstructorReducerBuilder<TState, TAction> Add<TValue>(Expression<Func<TState, TValue>> memberExpression, Expression<Func<TState, TAction, TValue>> valueExpression)
		{
			if (memberExpression.Body.NodeType != ExpressionType.MemberAccess)
				throw new ArgumentException("Wrong lambda body, member access expected");

			_expressions.Add(
				new KeyValuePair<MemberExpression, Expression>((MemberExpression)memberExpression.Body,
				valueExpression));

			return this;
		}

		public ConstructorReducerBuilder<TState, TAction> Add<TValue, TValue2>(Expression<Func<TState, TValue>> memberExpression, Expression<Func<TAction, TValue2>> valueExpression)
			where TValue2: TValue
		{
			if (memberExpression.Body.NodeType != ExpressionType.MemberAccess)
				throw new ArgumentException("Wrong lambda body, member access expected");

			_expressions.Add(
				new KeyValuePair<MemberExpression, Expression>((MemberExpression)memberExpression.Body,
				valueExpression));

			return this;
		}

		public ConstructorReducerBuilder<TState, TAction> Add<TValue>(Expression<Func<TState, TValue>> memberExpression, TValue? value)
		{
			if (memberExpression.Body.NodeType != ExpressionType.MemberAccess)
				throw new ArgumentException("Wrong lambda body, member access expected");

			_expressions.Add(
				new KeyValuePair<MemberExpression, Expression>((MemberExpression)memberExpression.Body,
				Expression.Constant(value, typeof(TValue))));

			return this;
		}

		public Expression<Reducer<TState?, TAction>> Build()
		{
			var action = Expression.Parameter(typeof(TAction), "action");
			var state = Expression.Parameter(typeof(TState), "source");

			var constuctors = typeof(TState).GetConstructors().ToList();
			var constuctor = constuctors.Where(_ => _.GetCustomAttributes<ConstructorBuilderAttribute>() != null)
				.FirstOrDefault();

			if (constuctor == null && (constuctors.Count == 0 || constuctors.Count > 1))
				throw new InvalidOperationException($"{typeof(TState)} declares {constuctors.Count} constuctors, but one expected, or use [ConstructorBuilderAttribute] to mark constructor for builder");

			constuctor = constuctor ?? constuctors[0];

			var constructorParameters = constuctor.GetParameters();
			var invokeParameters = new Expression[constructorParameters.Length];

			for (var i = 0; i < constructorParameters.Length; i++)
			{
				var constructorParameter = constructorParameters[i];
				var pair = _expressions
					.Where(_ => _.Key.Member.Name.Equals(constructorParameter.Name, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();

				if (pair.Key != null)
				{
					invokeParameters[i] = pair.Value.NodeType == ExpressionType.Constant
						? pair.Value
						: ((LambdaExpression)pair.Value).Parameters.Count == 1
							? Expression.Invoke(pair.Value, action)
							: Expression.Invoke(pair.Value, state, action);
				}
				else
					invokeParameters[i] = ConstructorCloneBuilder<TState>
						.GetOrDefault(state, constructorParameter.ParameterType, typeof(TState), constructorParameter.Name!);
			}

			return Expression.Lambda<Reducer<TState?, TAction>>(
				Expression.New(constuctor, invokeParameters),
				state, action
				);
		}

		public Reducer<TState?, TAction> Compile() => Build().Compile();
	}

}
