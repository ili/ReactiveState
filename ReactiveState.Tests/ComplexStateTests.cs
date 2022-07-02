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
		public class SubState4
		{
			public int Sum;
		}

		private static readonly Reducer<SubState1, IncrementAction> SubState1OnIncrement = (x, a) => new SubState1() { Counter = x?.Counter ?? 0 + 1 };
		private static readonly Expression<Reducer<SubState2, IncrementAction>> SubState2OnIncrement = (x, a) => new SubState2() { Counter = (x != null ? x.Counter : 0) + 1 };
		private static readonly Reducer<IPersistentState, IncrementAction> IStateOnIncrement = (x, a) => State.Build(new SubState3()
		{
			Sum = (x?.Get<SubState1>()?.Counter ?? 0) + (x?.Get<SubState2>()?.Counter ?? 0)
		});
		private static readonly Expression<Reducer<IPersistentState, IAction>> LastAction = (x, a) => State.Build(a);
		private static readonly Expression<Reducer<IMutableState, IncrementAction>> IMutableStateOnIncrement = (x, a) => x.Set(new SubState4()
		{
			Sum = (x.Get<SubState1>().Counter) + (x.Get<SubState2>().Counter)
		});

		[Test]
		public async Task ReducerTest()
		{
			var store = new Store<IState>(new State(), new MiddlewareBuilder<IState, DispatchContext<IState>>()
				.UseReducers(Tools.BuildComplexReducer(typeof(ComplexStateTests).
					ReadonlyStaticFields()
					.Where(fi => fi.FieldType.LikeReducer())
					.Select(_ => _.GetValue(null)!)
					.ToArray())!)
				.UseNotification()
				.Build()
				);

			var a = new IncrementAction();

			var st = await store.Dispatch(a);

			Assert.NotNull(st!.Get<SubState1>());
			Assert.NotNull(st!.Get<SubState2>());

			Assert.AreEqual(1, st.Get<SubState1>()!.Counter);
			Assert.AreEqual(1, st.Get<SubState2>()!.Counter);
			Assert.AreEqual(0, st.Get<SubState3>()!.Sum);
			Assert.AreEqual(2, st.Get<SubState4>()!.Sum);
			Assert.AreEqual(a, st.Get<IAction>());

			var st2 = await store.Dispatch(a);
			Assert.AreEqual(2, st2.Get<SubState3>()!.Sum);
			Assert.AreEqual(3, st2.Get<SubState4>()!.Sum);
		}
	}
}
