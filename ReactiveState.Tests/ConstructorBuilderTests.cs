using NUnit.Framework;
using System;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ConstructorBuilderTests
	{
		public class ComplexObject
		{
			public ComplexObject(int intValue, string stringValue)
			{
				IntValue = intValue;
				StringValue = stringValue;
			}

			public int IntValue { get; }
			public string StringValue { get; }
		}

		public class BigComplexObject
		{
			public BigComplexObject(long p1, long p2, long p3, long p4, long p5, long p6, long p7, long p8, long p9, long p10, long p11)
			{
				P1 = p1;
				P2 = p2;
				P3 = p3;
				P4 = p4;
				P5 = p5;
				P6 = p6;
				P7 = p7;
				P8 = p8;
				P9 = p9;
				P10 = p10;
				P11 = p11;
			}

			public readonly long P1;
			public readonly long P2;
			public readonly long P3;
			public readonly long P4;
			public readonly long P5;
			public readonly long P6;
			public readonly long P7;
			public readonly long P8;
			public readonly long P9;
			public readonly long P10;
			public readonly long P11;

			public long Control => P1 * P2 * P3 * P4 * P5 * P6 * P7 * P8 * P9 * P10 * P11;
		}


		[Test]
		public void BuildTest1()
		{
			var cb = new ConstructorCloneBuilder<ComplexObject>();
			cb.Add(_ => _.IntValue);

			var builded = cb.Build();
			Assert.NotNull(builded);

			var ex = builded.Compile();

			Assert.AreEqual(typeof(Func<ComplexObject, int, ComplexObject>), ex.GetType());
		}

		[Test]
		public void BuildTest2()
		{
			var cb = new ConstructorCloneBuilder<ComplexObject>();
			cb.Add(_ => _.IntValue);
			cb.Add(_ => _.StringValue);

			var builded = cb.Build();
			Assert.NotNull(builded);

			var ex = builded.Compile();

			Assert.AreEqual(typeof(Func<ComplexObject, int, string, ComplexObject>), ex.GetType());
		}



		[Test]
		public void TypedTest()
		{
			//var simple = new [] { 1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };

			var source = new BigComplexObject(1, 2, 3, 5, 7, 11, 13, 17, 19, 23, 29);

			var builder0 = Builder.Clone<BigComplexObject>();
			var next = builder0.Build().Compile()(source);
			Assert.AreNotEqual(0, next.Control);

			var builder1 = builder0.Add(_ => _.P1);
			Func<BigComplexObject, long, BigComplexObject> cloner1 = builder1.Build().Compile();
			next = cloner1(source, 1);
			Assert.AreEqual(source.Control / source.P1, next.Control);
			source = next;

			var builder2 = builder1.Add(_ => _.P2);
			next = builder2.Build().Compile()(source, 1, 1);
			Assert.AreEqual(source.Control / source.P2, next.Control);
			source = next;

			var builder3 = builder2.Add(_ => _.P3);
			next = builder3.Build().Compile()(source, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P3, next.Control);
			source = next;

			var builder4 = builder3.Add(_ => _.P4);
			next = builder4.Build().Compile()(source, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P4, next.Control);
			source = next;

			var builder5 = builder4.Add(_ => _.P5);
			next = builder5.Build().Compile()(source, 1, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P5, next.Control);
			source = next;

			var builder6 = builder5.Add(_ => _.P6);
			next = builder6.Build().Compile()(source, 1, 1, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P6, next.Control);
			source = next;

			var builder7 = builder6.Add(_ => _.P7);
			next = builder7.Build().Compile()(source, 1, 1, 1, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P7, next.Control);
			source = next;

			var builder8 = builder7.Add(_ => _.P8);
			next = builder8.Build().Compile()(source, 1, 1, 1, 1, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P8, next.Control);
			source = next;

			var builder9 = builder8.Add(_ => _.P9);
			next = builder9.Build().Compile()(source, 1, 1, 1, 1, 1, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P9, next.Control);
			source = next;

			var builder10 = builder9.Add(_ => _.P10);
			next = builder10.Build().Compile()(source, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1);
			Assert.AreEqual(source.Control / source.P10, next.Control);
			source = next;

			var builder11 = builder10.Add(_ => _.P11);
			next = (BigComplexObject) builder11.Build().Compile().DynamicInvoke(new object[] { source, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });
			Assert.AreEqual(source.Control / source.P11, next.Control);
			source = next;
		}
	}
}
