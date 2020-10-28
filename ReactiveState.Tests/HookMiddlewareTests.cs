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
			var store = new Store<int>(0, 
				Middlewares.BeforeHookMiddleware<Store<int>, int>((s, a) => s >= 0),
				Middlewares.ReducerMiddleware<Store<int>, int>(
				(s, a) => a is IncrementAction? s+1: s,
				(s, a) => a is DecrementAction? s-1: s
				),
				Middlewares.AfterHookMiddleware<Store<int>, int>((s1, s2, a) => counter++)
				);

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
			Assert.AreEqual(-1, value);

			Assert.AreEqual(3, counter);
		}
	}
}
