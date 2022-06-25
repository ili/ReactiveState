using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ReactiveState.ComplexState.StateTree
{
	class Node<TObject, TProperty> : IStateTreeNode<TObject, TProperty>, IStateTree<TObject>
	{

		private static Expression<Func<TObject, TProperty>> GetterOrDefault(Expression<Func<TObject, TProperty>> getter)
		{
			var parameter = Expression.Parameter(typeof(TObject));

			return Expression.Lambda<Func<TObject, TProperty>>(
				Expression.Condition(
						Expression.Equal(parameter, Expression.Default(parameter.Type)),
						Expression.Default(getter.ReturnType),
						Expression.Invoke(getter, parameter)
						),
				parameter);
		}

		private readonly Dictionary<Type, IStateTreeNode> _childs;

		public Node(Expression<Func<TObject, TProperty>> getter, Expression<Func<TObject, TProperty, TObject>> composer, IEnumerable<IStateTreeNode> childs)
		{
			Getter = GetterOrDefault(getter);
			Composer = composer;
			_childs = childs.ToDictionary(_ => _.Type);
		}

		public Expression<Func<TObject, TProperty>> Getter { get; }

		public Expression<Func<TObject, TProperty, TObject>> Composer { get; }

		public Type Type => typeof(TProperty);

		public IEnumerable<IStateTreeNode> Childs => _childs.Values;

		public Expression? FindGetter(Type type)
		{
			if (type == Type)
				return Getter;

			foreach (var node in _childs.Values)
			{
				var getter = node.FindGetter(type);
				if (getter != null)
				{
					var par = Expression.Parameter(typeof(TObject));

					return Expression.Lambda(
						Expression.Invoke(getter,
							Expression.Invoke(Getter, par)
						), par);
				}
			}

			return null;
		}

		public Expression? FindComposer(Type type)
		{
			if (type == Type)
				return Composer;

			foreach (var node in _childs.Values)
			{
				var composer = node.FindComposer(type);

				if (composer != null)
				{
					var root = Expression.Parameter(typeof(TObject));
					var par = Expression.Parameter(type);

					return Expression.Lambda(
						Expression.Invoke(Composer, root,
						Expression.Invoke(composer,
							Expression.Invoke(Getter, root),
							par
						)), root, par);
				}
			}

			return null;
		}

		public Func<TObject, TSubState>? FindGetter<TSubState>()
		{
			var exp = FindGetter(typeof(TSubState));
			if (exp == null)
				return null;

			return (Func<TObject, TSubState>)((LambdaExpression)exp).Compile();
		}

		public Func<TObject, TSubState, TObject>? FindComposer<TSubState>()
		{
			var exp = FindComposer(typeof(TSubState));
			if (exp == null)
				return null;

			return (Func<TObject, TSubState, TObject>)((LambdaExpression)exp).Compile();
		}
	}
}
