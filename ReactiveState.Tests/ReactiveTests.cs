using NUnit.Framework;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ReactiveTests
	{
		class MyClass
		{
			public int Counter;
		}

		[Test]
		public void EventTest()
		{
			var subject = new Subject<MyClass>();

			for (var i = 0; i < 100; i++)
				subject.Subscribe(_ => { _.Counter += 1; });

			var obj = new MyClass();
			subject.OnNext(obj);

			Assert.AreEqual(100, obj.Counter);


		}

		[Test]
		public void SleepEventTest()
		{
			var subject = new Subject<MyClass>();

			for (var i = 0; i < 100; i++)
				subject.Subscribe(_ => { System.Threading.Thread.Sleep(10); _.Counter += 1; });

			var obj = new MyClass();
			subject.OnNext(obj);

			Assert.AreEqual(100, obj.Counter);
		}

		[Test]
		public void AsyncEventTest()
		{
			var subject = new Subject<MyClass>();

			for (var i = 0; i < 100; i++)
				subject.Subscribe(async _ => { await Task.Delay(1); _.Counter += 1; });

			var obj = new MyClass();
			subject.OnNext(obj);

			//await Task.Delay(5000);

			Assert.AreNotEqual(100, obj.Counter);
		}
	}
}
