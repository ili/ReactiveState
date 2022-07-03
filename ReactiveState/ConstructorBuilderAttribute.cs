using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	[AttributeUsage(AttributeTargets.Constructor, Inherited = false)]
	public sealed class ConstructorBuilderAttribute: Attribute
	{
	}
}
