using System;
using System.Collections.Generic;
using System.Text;

namespace ReactiveState.ComplexState
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public sealed class SubStateAttribute : Attribute
	{
		public string? Key { get; set; }
	}
}
