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
		public async Task FuncEffectTest()
		{
			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects((ctx) =>
				{
					if (ctx.NewState > 1)
						return Task.FromResult<IAction?>(new DecrementAction());

					return Task.FromResult<IAction?>(null);
				})
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

			var res1 = await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, res1);

			var res2 = await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, res2);

		}
	}
}
