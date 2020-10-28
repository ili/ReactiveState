using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ReactiveState
{

	public class StateTreeBuilder<TObject>
	{
		private readonly Type _rootType;
		private List<IStateTreeNode> _subNodes = new List<IStateTreeNode>();

		public StateTreeBuilder()
		{
			_rootType = typeof(TObject);
		}

		public StateTreeBuilder<TObject> With<TProperty>(Expression<Func<TObject, TProperty>> getter, Expression<Func<TObject, TProperty, TObject>> composer, IStateTree<TProperty> stateTree)
		{
			var node = new Node<TObject, TProperty>(getter, composer, stateTree.Childs);
			_subNodes.Add(node);

			return this;
		}

		public StateTreeBuilder<TObject> With<TNode>(Expression<Func<TObject, TNode>> getter, Expression<Func<TObject, TNode, TObject>> composer)
		{
			_subNodes.Add(new Node<TObject, TNode>(getter, composer,  Enumerable.Empty<IStateTreeNode>()));
			return this;
		}

		public IStateTree<TObject> Build()
		{
			return new Node<TObject, TObject>(_ => _, (_, __) => __, _subNodes);
		}
	}


}
