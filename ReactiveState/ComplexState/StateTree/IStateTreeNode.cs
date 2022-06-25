using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ReactiveState.ComplexState.StateTree
{

	public interface IStateTreeNode
	{
		Type Type { get; }

		IEnumerable<IStateTreeNode> Childs { get; }

		Expression? FindGetter(Type type);
		Expression? FindComposer(Type type);
	}


}
