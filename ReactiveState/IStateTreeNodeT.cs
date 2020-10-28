using System;
using System.Linq.Expressions;

namespace ReactiveState
{

	interface IStateTreeNode<TObject, TProperty> : IStateTreeNode
		{
			Expression<Func<TObject, TProperty>> Getter { get; }
			Expression<Func<TObject, TProperty, TObject>> Composer { get; }
		}


}
