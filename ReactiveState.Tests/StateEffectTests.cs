using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class StateEffectTests
	{
		[Test]
		public Task FuncEffectTest()
		{
			Assert.Fail();
			return Task.CompletedTask;
		}
	}
}
