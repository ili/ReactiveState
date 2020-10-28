using NUnit.Framework;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ConstructorReducerBuilderTests
	{
		public class Action: ActionBase
		{
			public readonly int IntValue = 100;
			public string StringValue { get => "Geronimooooooo!!!!!"; }
		}

		public class State
		{
			public State(Action action, int intValue, string stringValue, decimal constValue, float someValue, int summValue)
			{
				Action = action;
				IntValue = intValue;
				StringValue = stringValue;
				ConstValue = constValue;
				SomeValue = someValue;
				SummValue = summValue;
			}

			public Action Action { get; }
			public int IntValue { get; }
			public string StringValue { get; }
			public decimal ConstValue { get; }
			public float SomeValue { get; }
			public int SummValue { get; }
		}

		[Test]
		public void BuilderTest()
		{
			var reducer = new ConstructorReducerBuilder<State, Action>()
				.Add(state => state.Action,      act => act)
				.Add(state => state.IntValue,    act => act.IntValue)
				.Add(state => state.StringValue, act => act.StringValue)
				.Add(state => state.ConstValue,  923)
				.Add(state => state.SummValue,   (s, a) => s.SummValue + a.IntValue)
				.Build()
				.Compile();

			var oldState = new State(null, -10, "nothing", 666, 3.14f, 300);
			var action = new Action();

			var newState = reducer(oldState, action);

			Assert.AreEqual(oldState.SomeValue, newState.SomeValue);
			Assert.AreEqual(action.IntValue, newState.IntValue);
			Assert.AreEqual(action.StringValue, newState.StringValue);
			Assert.AreEqual(action, newState.Action);
			Assert.AreEqual(923, newState.ConstValue);
			Assert.AreEqual(400, newState.SummValue);
			Assert.AreNotEqual(oldState, newState);
		}
	}
}
