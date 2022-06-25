using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class EffectMiddlewareTests
	{
		private static void WriteLine(string format, params object[] args)
		{
			Console.WriteLine("{0:HH:mm:ss.fff}: {1}", DateTime.Now, string.Format(format, args));
		}

		[Test]
		public async Task Test1()
		{
			var autoResetEvent = new AutoResetEvent(false);
			Func<IObservable<(DispatchContext<int>, int, IAction)>, IObservable<(DispatchContext<int>, IAction)>> effect =
				x => x
					.Where(_ => _.Item3 is IncrementAction)
					.Do(_ => WriteLine("Effect: increment"))
					.Delay(TimeSpan.FromMilliseconds(100))
					.Do(_ => WriteLine("Effect: delayed"))
					//.Do(_ => autoResetEvent.WaitOne())
					.Select(_ => (_.Item1, (IAction)new DecrementAction()))
					.Do(_ => WriteLine("Effect: decrement"))
					.Do(_ => autoResetEvent.Set())
					;

			var dispatcher = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification()
				.Build();

			var store = new Store<int>(0, dispatcher);

			int value = 0;
			store.States().Subscribe(_ => value = _);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, value);

			autoResetEvent.WaitOne();
			await Task.Delay(100);
			Assert.AreEqual(0, value);

			await store.Dispatch(new IncrementAction());
			Assert.AreEqual(1, value);

			autoResetEvent.WaitOne();
			await Task.Delay(100);
			Assert.AreEqual(0, value);
		}

		[Test]
		public async Task Test2()
		{
			Func<IObservable<(DispatchContext<int>, int, IAction)>, IObservable<(DispatchContext<int>, IAction)>> effect1 =
				x => x
					.Where(_ => _.Item3 is IncrementAction)
					.Take(1)
					.RepeatWhen(_ => _.Select(n => x.Select(i => i.Item3 is StopAction).Where(_ => _)).Switch())
					.Do(_ => Debug.WriteLine("Begin DEcrement"))
					//.RetryWhen(_ => x.Select(i => i.Item3 is StopAction).Where(_ => _))
					.SelectMany(_ => 
						Observable.Interval(TimeSpan.FromMilliseconds(100))
							.Do(_ => Debug.WriteLine("	On DEcrement Timer"))
							.TakeUntil(x.Select(i => i.Item3 is StopAction).Where(_ => _).Take(1).Do(_ => Debug.WriteLine("Stop DEcrement")))
							.Select(t => _.Item1))
					//.Switch()
					.Select(_ =>
					{
						Debug.WriteLine("		Send DecrementAction Effect");
						return (_, (IAction)new DecrementAction());
					})
					;

			Func<IObservable<(DispatchContext<int>, int, IAction)>, IObservable<(DispatchContext<int>, IAction)>> effect2 =
				x => x
					.Where(_ => _.Item3 is DecrementAction)
					.Take(1)
					.RepeatWhen(_ => _.Select(n => x.Select(i => i.Item3 is StopAction).Where(_ => _)).Switch())
					.Do(_ => Debug.WriteLine("Begin INcrement"))
					.SelectMany(_ => Observable.Interval(TimeSpan.FromMilliseconds(100))
							.Do(_ => Debug.WriteLine("	On INcrement Timer"))
							.TakeUntil(x.Select(i => i.Item3 is StopAction).Where(_ => _).Do(_ => Debug.WriteLine("Stop INcrement")))
							.Select(t => _.Item1))
					//.Switch()
					.Select(_ =>
					{
						Debug.WriteLine("		Send IncrementAction Effect");
						return (_, (IAction)new IncrementAction());
					});

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(
					effect1,
					effect2
				)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.Use(c =>
				{
					Console.WriteLine($"{c.Action}: {c.OriginalState} -> {c.NewState}");
				})
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

			int value = 0;
			store.States().Subscribe(_ => { value = _; Console.WriteLine("value: {0}", _); });

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
			await store.States()/*.Skip(1)*/.FirstAsync();
			Assert.AreEqual(curValue + 1, value);

			var res3 = await store.States().Skip(1).FirstAsync();
			Assert.AreEqual(curValue, value, "Third wait");

			await store.States().Skip(2).FirstAsync();
			await store.Dispatch(new StopAction());
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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect, effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());


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
			Func<DispatchContext<int>, int, IAction, Task<IAction>> effect =
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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect, effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());
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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());

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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseEffects(effect)
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();

			var store = new Store<int>(0, builder.Build());


			//Assert.Throws<InvalidOperationException>(() => effect(1, null));

			Assert.ThrowsAsync<InvalidOperationException>(async () => await store.Dispatch(new IncrementAction()));
		}
	}
}
