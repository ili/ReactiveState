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

			var builder = new MiddlewareBuilder<int, DispatchContext<int>>()
				.UseAfterHook((o, n, a) =>
				{
					old = o;
					@new = n;
					action = a;
				})
				.UseReducers(
					(s, a) => a is IncrementAction ? s + 1 : s,
					(s, a) => a is DecrementAction ? s - 1 : s
				)
				.UseNotification();


			var store = new Store<int>(0, builder.Build());

			await store.Dispatch(new IncrementAction());

			Assert.NotNull(action);
			Assert.AreEqual(0, old);
			Assert.AreEqual(1, @new);
		}
	}
}
