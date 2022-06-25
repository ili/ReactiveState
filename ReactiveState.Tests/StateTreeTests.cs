using NUnit.Framework;
using ReactiveState.ComplexState.StateTree;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class StateTreeTest
	{
		public class SimpleState
		{
			public int IntValue { get; set; } = 15;
			public string StringValue { get; set; } = "Hello world";
		}

		public class ComplexState
		{
			public SimpleState Simple { get; set; }
		}

		[Test]
		public void SimpleTest()
		{
			var builder = new StateTreeBuilder<SimpleState>();
			builder.With(x => x.IntValue, (a, b) => new SimpleState()
			{
				IntValue = b,
				StringValue = a != null ? a.StringValue : null
			})
			.With(x => x.StringValue, (a, b) => new SimpleState()
			{
				IntValue = a != null ? a.IntValue : -1,
				StringValue = b
			});

			var tree = builder.Build();

			var intGetter = tree.FindGetter<int>();
			var intComposer = tree.FindComposer<int>();
			var stringGetter = tree.FindGetter<string>();
			var stringComposer = tree.FindComposer<string>();

			Assert.NotNull(intGetter);
			Assert.NotNull(intComposer);
			Assert.NotNull(stringGetter);
			Assert.NotNull(stringComposer);

			var obj = new SimpleState();

			Assert.AreEqual(obj.IntValue, intGetter(obj));
			Assert.AreEqual(obj.StringValue, stringGetter(obj));

			var intComposed = (SimpleState)intComposer(obj, 512);

			Assert.AreEqual(512, intComposed.IntValue);
			Assert.AreEqual(obj.StringValue, intComposed.StringValue);
		}

		[Test]
		public void ComplexTest()
		{
			var subBuilder = new StateTreeBuilder<SimpleState>();
			subBuilder.With(x => x.IntValue, (a, b) => new SimpleState()
			{
				IntValue = b,
				StringValue = a != null ? a.StringValue : null
			})
			.With(x => x.StringValue, (a, b) => new SimpleState()
			{
				IntValue = a != null ? a.IntValue : -1,
				StringValue = b
			});

			var subTree = subBuilder.Build();

			var tree = new StateTreeBuilder<ComplexState>()
				.With(_ => _.Simple, (a, b) => new ComplexState() { Simple = b }, subTree)
				.Build();

			var intGetter = tree.FindGetter<int>();
			var intComposer = tree.FindComposer<int>();
			var stringGetter = tree.FindGetter<string>();
			var stringComposer = tree.FindComposer<string>();

			Assert.NotNull(intGetter);
			Assert.NotNull(intComposer);
			Assert.NotNull(stringGetter);
			Assert.NotNull(stringComposer);

			var obj = new ComplexState()
			{
				Simple = new SimpleState
				{
					IntValue = 954,
					StringValue = "kdjfbsdkljcbvs"
				}
			};

			Assert.AreEqual(obj.Simple.IntValue, intGetter(obj));
			Assert.AreEqual(obj.Simple.StringValue, stringGetter(obj));

			var intComposed = (ComplexState)intComposer(obj, 512);

			Assert.AreEqual(512, intComposed.Simple.IntValue);
			Assert.AreEqual(obj.Simple.StringValue, intComposed.Simple.StringValue);

			Assert.NotNull(intComposer(null, -1));

			Assert.AreEqual(default(int),    intGetter(null));
			Assert.AreEqual(121,             intComposer(null, 121).Simple.IntValue);
			Assert.AreEqual(default(string), intComposer(null, 121).Simple.StringValue);
		}

	}
}
