using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class StoreBuilderTests
	{
		public class Action: ActionBase
		{

		}
		public class ServiceProvider : IServiceProvider
		{
			public object GetService(Type serviceType)
			{
				throw new NotImplementedException();
			}
		}

		[Test]
		public async Task SimpleDispatcher()
		{
			var i = 0;
			var j = 0;

			var builder = new StoreBuilder(new ServiceProvider());
			builder
				.Use(next => a => { i++; return next(a); })
				.Use(next => a => { j++; return next(a); })
				;

			var dispatcher = builder.Build();
			await dispatcher(new Action());

			Assert.AreEqual(1, i, "i");
			Assert.AreEqual(1, j, "j");

			await dispatcher(new Action());

			Assert.AreEqual(2, i, "i");
			Assert.AreEqual(2, j, "j");
		}

		[Test]
		public async Task NextDispatcherControlsPrevious()
		{
			var i = 0;
			var j = 0;
			var k = 0;

			var builder = new StoreBuilder(new ServiceProvider());
			builder
				.Use(next => a => { i++; return next(a); })
				.Use(next => a => { j++; return next(a); })
				.Use(next => a => { k++; return k > 1 ? Task.CompletedTask : next(a); })
				;

			var dispatcher = builder.Build();

			await dispatcher(new Action());
			await dispatcher(new Action());

			Assert.AreEqual(1, i, "i");
			Assert.AreEqual(1, j, "j");
			Assert.AreEqual(2, k, "k");
		}

		[Test]
		public async Task UseDispatcherTest()
		{
			var i = 0;
			var j = 0;

			var builder = new StoreBuilder(new ServiceProvider());
			builder
				.Use(a => { i++; return Task.CompletedTask; })
				.Use(a => j++)
				;

			var dispatcher = builder.Build();
			await dispatcher(new Action());

			Assert.AreEqual(1, i, "i");
			Assert.AreEqual(1, j, "j");

			await dispatcher(new Action());

			Assert.AreEqual(2, i, "i");
			Assert.AreEqual(2, j, "j");
		}
	}
}
