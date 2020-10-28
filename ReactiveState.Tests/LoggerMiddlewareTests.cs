using NUnit.Framework;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class LoggerMiddlewareTests
	{
		[Test]
		public async Task Test()
		{
			var old = -1;
			var @new = -1;
			IAction action = null;

				var store = new Store<int>(0, 
					Middlewares.AfterHookMiddleware<Store<int>, int>((o, n, a) =>
					{
						old = o;
						@new = n;
						action = a;
					}),
					Middlewares.ReducerMiddleware<Store<int>, int>(
					(s, a) => a is IncrementAction? s+1: s,
					(s, a) => a is DecrementAction? s-1: s
				));

			await store.Dispatch(new IncrementAction());

			Assert.NotNull(action);
			Assert.AreEqual(0, old);
			Assert.AreEqual(1, @new);
		}
	}
}
