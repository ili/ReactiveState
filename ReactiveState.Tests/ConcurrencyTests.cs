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
			var store = new Store<int>(0, new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s
				)
				.UseNotification()
				.Build());

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
			Func<DispatchContext<int>, int, IAction, Task<IAction>> effect =
				async (_, __, a) =>
				{
					if (a is IncrementAction)
					{
						await Task.Delay(5);
						return new IncrementEffectAction();
					}
					return null;
				};

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is IncrementEffectAction ? s + 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

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
			Func<DispatchContext<int>, int, IAction, Task<IAction>> effect =
				async (c, s, _) =>
				{
					if (s % 2 == 1)
					{
						await Task.Delay(5);
						return new IncrementEffectAction();
					}
					return null;
				};

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is IncrementEffectAction ? s + 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

			var increments = new Task[100];

			for (var i = 0; i < 100; i++)
				increments[i] = store.Dispatch(new IncrementAction());

			Task.WaitAll(increments);

			var st = await store.States().FirstAsync();

			Assert.AreEqual(200, st);
		}
	}
}
