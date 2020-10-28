using NUnit.Framework;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class EffectMiddlewareTests
	{
		[Test]
		public async Task Test1()
		{
			Func<IObservable<(int, IAction)>, IObservable<IAction>> effect =
				x => x
					.Select(_ => _.Item2)
					.OfType<IncrementAction>()
					.Delay(TimeSpan.FromMilliseconds(100))
					.Select(_ => new DecrementAction());

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction? s+1: s,
					(s, a) => a is DecrementAction? s-1: s
				));

			int value = 0;
			store.States().Subscribe(_ => value = _);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, value);

			var res = await store.States().Skip(1).FirstAsync();
			Assert.AreEqual(0, value);

			await store.Dispatch(new IncrementAction());
			await store.States().Skip(1).FirstAsync();
			Assert.AreEqual(1, value);

			var res2 = await store.States().Skip(1).FirstAsync();
			Assert.AreEqual(0, value);
		}

		[Test]
		public async Task Test2()
		{
			Func<IObservable<(int, IAction)>, IObservable<IAction>> effect1 =
				x => x
					.Select(_ => _.Item2)
					.OfType<IncrementAction>()
					.Select(_ => 
						Observable.Interval(TimeSpan.FromMilliseconds(100))
							.TakeUntil(x.Select(i => i.Item2).OfType<StopAction>()).Take(1))
					.Switch()
					.Select(_ => new DecrementAction());

			Func<IObservable<(int, IAction)>, IObservable<IAction>> effect2 =
				x => x
					.Select(_ => _.Item2)
					.OfType<DecrementAction>()
					.Select(_ => Observable.Interval(TimeSpan.FromMilliseconds(100))
							.TakeUntil(x.Select(i => i.Item2).OfType<StopAction>()).Take(1))
					.Switch()
					.Select(_ => new IncrementAction());

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, effect1, effect2),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction? s+1: s,
					(s, a) => a is DecrementAction? s-1: s
				));

			int value = 0;
			store.States().Subscribe(_ => value = _);

			await store.Dispatch(new StopAction());
			Assert.AreEqual(0, value);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, value);

			var res = await store.States().Skip(5).FirstAsync();
			Assert.AreEqual(0, value, "First wait");

			await store.Dispatch(new StopAction());
			Assert.AreEqual(0, value, "Second wait");
			var res2 = await store.States().FirstAsync();

			var curValue = value;
			await store.Dispatch(new IncrementAction());
			await store.States().Skip(1).FirstAsync();
			Assert.AreEqual(curValue+1, value);

			var res3 = await store.States().Skip(1).FirstAsync();
			Assert.AreEqual(curValue, value, "Third wait");
		}

		[Test]
		public async System.Threading.Tasks.Task Test3()
		{
			Func<int, IAction, IAction> effect =
				(st, a) =>
				{
					if (a is IncrementAction)
						return new DecrementAction();

					return null;
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction? s+1: s,
					(s, a) => a is DecrementAction? s-1: s
				));

			int value = 0;
			int counter = 0;
			store.States().Subscribe(_ => { value = _; counter++; });

			await store.Dispatch(new IncrementAction());

			Assert.AreEqual(3, counter);
			Assert.AreEqual(0, value);
		}

		[Test]
		public async Task Test4()
		{
			Func<int, IAction, Task<IAction>> effect =
				async (st, a) =>
				{
					if (a is IncrementAction)
					{
						await Task.Delay(100);
						return new DecrementAction();
					}
					return null;
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction? s+1: s,
					(s, a) => a is DecrementAction? s-1: s
				));

			int value = 0;
			var was1 = false;
			store.States().Subscribe(_ =>
			{
				value = _;
				was1 = was1 || _ == 1;
			});

			await store.Dispatch(new IncrementAction());
			Assert.True(was1);
			Assert.AreEqual(0, value);

			// await is used for effect
			var res = await store.States().FirstAsync();
			Assert.AreEqual(0, value);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(0, value);

			var res2 = await store.States().FirstAsync();
			Assert.AreEqual(0, value);
		}

		[Test]
		public async System.Threading.Tasks.Task DoubleEffect()
		{
			Func<int, IAction, IAction> effect =
				(st, a) =>
				{
					if (a is IncrementAction)
						return new DecrementAction();

					return null;
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, null, effect, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				));

			int value = 0;
			int counter = 0;
			store.States().Subscribe(_ => { value = _; counter++; });

			await store.Dispatch(new IncrementAction());

			Assert.AreEqual( 4, counter);
			Assert.AreEqual(-1, value);
		}

		[Test]
		public async Task DoubleEffectAsync()
		{
			var effectCounter = 0;
			Func<Store<int>, int, IAction, Task<IAction>> effect =
				async (s, st, a) =>
				{
					await Task.Delay(50);
					if (a is IncrementAction)
					{
						Interlocked.Increment(ref effectCounter);
						return new DecrementAction();
					}
					return null;
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, effect, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				));

			int value = 0;
			int counter = 0;
			store.States().Subscribe(_ => { value = _; counter++; });

			await store.Dispatch(new IncrementAction());

			var last = await store.States().Where(_ => _ < 0).FirstAsync();

			Assert.AreEqual(4, counter);
			Assert.AreEqual(-1, value);
		}

		[Test]
		public void ExceptionTest()
		{
			Func<int, IAction, IAction> effect =
				(st, a) =>
				{
					throw new InvalidOperationException();
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				));

			Assert.Throws<InvalidOperationException>(() => effect(1, null));

			Assert.ThrowsAsync<InvalidOperationException>(async () => await store.Dispatch(new IncrementAction()));
		}

		[Test]
		public void ExceptionTestAsync()
		{
			Func<int, IAction, Task<IAction>> effect =
				async (st, a) =>
				{
					await Task.Delay(500);
					throw new InvalidOperationException();
				};

			var store = new Store<int>(0,
				Middlewares.EffectMiddleware<Store<int>, int>(null, null, effect),
				Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				));

			//Assert.Throws<InvalidOperationException>(() => effect(1, null));

			Assert.ThrowsAsync<InvalidOperationException>(async () => await store.Dispatch(new IncrementAction()));
		}
	}
}
