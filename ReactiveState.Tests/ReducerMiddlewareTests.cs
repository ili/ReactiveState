using NUnit.Framework;
using System.Reactive.Linq;
using System;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ReducerMiddlewareTests
	{
		[Test]
		public async System.Threading.Tasks.Task Test()
		{
			var store = new Store<int>(0, Middlewares.ReducerMiddleware
				<Store<int>, int>(
				(s, a) => a is IncrementAction? s+1: s,
				(s, a) => a is DecrementAction? s-1: s
				));

			int? value = null;
			store.States().Subscribe(x => value = x);

			Assert.AreEqual(0, value);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, value);

			await store.Dispatch(new DecrementAction());
			Assert.AreEqual(0, value);

		}
	}
}
