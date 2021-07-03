using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class HookMiddlewareTests
	{
		[Test]
		public async Task Test()
		{
			int counter = 0;
			var dispatcher = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseBeforeHook((s, a) => s >= 0)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification()
				.UseAfterHook((s1, s2, a) => counter++)
				.Build()
				;

			var store = new Store<int>(0, dispatcher);

			int? value = null;
			store.States().Subscribe(x => value = x);

			Assert.AreEqual(0, value);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, value);

			await store.Dispatch(new DecrementAction());
			Assert.AreEqual(0, value);

			await store.Dispatch(new DecrementAction());
			await store.Dispatch(new DecrementAction());
			await store.Dispatch(new DecrementAction());
			Assert.AreEqual(-1, value, "Before hook not working");

			Assert.AreEqual(3, counter, "After hook is not working");
		}
	}
}
