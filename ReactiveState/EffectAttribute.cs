using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState
{
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class EffectAttribute: Attribute
	{
	}
}
