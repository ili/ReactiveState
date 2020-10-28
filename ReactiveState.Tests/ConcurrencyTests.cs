using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ConcurrencyTests
	{
		public class IncrementAction: ActionBase
		{
		}
		public class IncrementEffectAction: ActionBase
		{
		}

		[Test]
		public async Task ConcurrentCounterTest()
		{
			var store = new Store<int>(0,
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s
				));

			var increments = new Task[100];

			for (var i = 0; i < 100; i++)
				increments[i] = store.Dispatch(new IncrementAction());

			Task.WaitAll(increments);

			var st = await store.States().FirstAsync();

			Assert.AreEqual(100, st);
		}

		[Test]
		public async Task ConcurrentCounterWithEffectTest()
		{
			Func<int, IAction, Task<IAction>> effect =
				async (_, a) =>
				{
					if (a is IncrementAction)
					{
						await Task.Delay(5);
						return new IncrementEffectAction();
					}
					return null;
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is IncrementEffectAction ? s + 1 : s
				));

			var increments = new Task[100];

			for (var i = 0; i < 100; i++)
				increments[i] = store.Dispatch(new IncrementAction());

			Task.WaitAll(increments);

			var st = await store.States().FirstAsync();

			Assert.AreEqual(200, st);
		}

		[Test]
		public async Task ConcurrentCounterStateEffectTest()
		{
			Func<Store<int>, int, Task<IAction>> effect =
				async (c, s) =>
				{
					if (s % 2 == 1)
					{
						await Task.Delay(5);
						return new IncrementEffectAction();
					}
					return null;
				};

			var store = new Store<int>(0,
				Middlewares.StateEffectMiddleware<Store<int>, int>(null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is IncrementEffectAction ? s + 1 : s
				));

			var increments = new Task[100];

			for (var i = 0; i < 100; i++)
				increments[i] = store.Dispatch(new IncrementAction());

			Task.WaitAll(increments);

			var st = await store.States().FirstAsync();

			Assert.AreEqual(200, st);
		}
	}
}
