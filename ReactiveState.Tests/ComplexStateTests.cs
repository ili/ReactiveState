using NUnit.Framework;
using ReactiveState.ComplexState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ComplexStateTests
	{
		public class SubState1
		{
			public int Counter;
		}

		public class SubState2
		{
			public int Counter;
		}
		public class SubState3
		{
			public int Sum;
		}


		private static readonly Reducer<SubState1, IncrementAction> SubState1OnIncrement = (x, a) => new SubState1() { Counter = x?.Counter ?? 0 + 1 };
		private static readonly Expression<Reducer<SubState2, IncrementAction>> SubState2OnIncrement = (x, a) => new SubState2() { Counter = (x != null ? x.Counter : 0) + 1 };
		private static readonly Reducer<IPersistentState, IncrementAction> IStateOnIncrement = (x, a) => State.Build(new SubState3()
		{
			Sum = (x.Get<SubState1>()?.Counter ?? 0) + (x.Get<SubState2>()?.Counter ?? 0)
		});

		[Test]
		public async Task ReducerTest()
		{
			var store = new Store<IState>(new State(), new MiddlewareBuilder<IState, DispatchContext<IState>>()
				.UseReducers(Tools.BuildComplexReducer(typeof(ComplexStateTests).
					ReadonlyStaticFields()
					.Where(fi => fi.FieldType.LikeReducer())
					.Select(_ => _.GetValue(null)!)
					.ToArray()))
				.Build()
				);

			var st = await store.Dispatch(new IncrementAction());

			Assert.NotNull(st!.Get<SubState1>());
			Assert.NotNull(st!.Get<SubState2>());

			Assert.AreEqual(1, st.Get<SubState1>()!.Counter);
			Assert.AreEqual(1, st.Get<SubState2>()!.Counter);
			Assert.AreEqual(2, st.Get<SubState3>()!.Sum);
		}
	}
}
