using System;

namespace ReactiveState
{
	public interface IStateTree<TObject>: IStateTreeNode
	{
		Func<TObject, TProperty> FindGetter<TProperty>();
		Func<TObject, TProperty, TObject> FindComposer<TProperty>();
	}


}
