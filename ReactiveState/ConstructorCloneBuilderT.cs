using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ReactiveState
{
	public class ConstructorCloneBuilder<T>
	{
		private readonly List<MemberExpression> _expressions = new List<MemberExpression>();

		public ConstructorCloneBuilder<T> Add<TF>(Expression<Func<T, TF>> expression)
		{
			if (expression.NodeType != ExpressionType.Lambda)
				throw new ArgumentException("Expression should be Lambda");

			var la = (LambdaExpression)expression;

			if (la.Parameters.Count > 1 || la.Parameters[0].Type != typeof(T))
				throw new ArgumentException($"Wrong lambda parameters single of {typeof(T).FullName} expected");

			if (la.Body.NodeType != ExpressionType.MemberAccess)
				throw new ArgumentException("Wrong lambda body, member access expected");

			_expressions.Add((MemberExpression)expression.Body);

			return this;
		}

		public LambdaExpression Build()
		{
			var pars = _expressions.Select(_ => Expression.Parameter(_.Type, _.Member.Name))
				.ToList();
			pars.Insert(0, Expression.Parameter(typeof(T), "source"));

			var constuctor = typeof(T).GetConstructors().Single();

			var constructorParameters = constuctor.GetParameters();
			var invokeParameters = new Expression[constructorParameters.Length];

			for (var i = 0; i < constructorParameters.Length; i++)
			{
				var constructorParameter = constructorParameters[i];
				Expression invokeParameter = pars.Where(_ => _.Name.Equals(constructorParameter.Name, StringComparison.OrdinalIgnoreCase))
					.FirstOrDefault();

				if (invokeParameter != null)
					invokeParameters[i] = invokeParameter;
				else
					invokeParameter = GetOrDefault(pars[0], constructorParameter.ParameterType, typeof(T), constructorParameter.Name);

				invokeParameters[i] = invokeParameter;
			}

			return Expression.Lambda(
				Expression.New(constuctor, invokeParameters),
				pars
				);
		}

		internal static Expression GetOrDefault(ParameterExpression source, Type parameterType, Type objectType, string name)
		{
			var member = objectType.GetMembers().Single(_ => _.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

			Expression def = Expression.Default(parameterType);
			if (parameterType.IsArray)
			{
				var elementType = parameterType.GetElementType();
				def = Expression.NewArrayInit(elementType);
			}

			return Expression.Condition(
				Expression.Equal(source, Expression.Default(source.Type)),
				def,
				Expression.PropertyOrField(source, member.Name));
		}

	}
}
