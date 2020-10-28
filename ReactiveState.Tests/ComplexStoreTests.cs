using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	[TestFixture]
	public class ComplexStoreTests
	{
		public class ComplexState
		{
			public int I;
			public long L;
		}

		[Test]
		public async Task Test()
		{
			var stateTree = new StateTreeBuilder<ComplexState>()
				.With(_ => _ != null ? _.I : default(int), (state, field) => new ComplexState()
				{
					I = field,
					L = state != null ? state.L : 0
				})
				.With(_ => _ != null ? _.L : default(long), (state, field) => new ComplexState()
				{
					I = state != null ? state.I : 0,
					L = field
				})
				.Build();

			var reducerMiddleware = Middlewares.ReducerMiddleware<ComplexStore<ComplexState>, ComplexState>(
				((Reducer<int, IAction>)((int s, IAction a) => ++s)).Wrap<ComplexState, int, IAction>(stateTree),
				((Reducer<long, IAction>)((long s, IAction a) => ++s)).Wrap<ComplexState, long, IAction>(stateTree)
				);


			var complexStote = new ComplexStore<ComplexState>(
				stateTree,
				null,
				reducerMiddleware
				);

			int intValue = 0;
			long longValue = 0;
			int counter = 0;
			ComplexState complexState = null;

			complexStote.States().Subscribe(_ => { counter++; complexState = _; });

			complexStote.States<int>().Subscribe(_ => intValue = _);
			complexStote.States<long>().Subscribe(_ => longValue = _);

			await complexStote.Dispatch(new IncrementAction());

			Assert.AreEqual(1, intValue);
			Assert.AreEqual(1, longValue);
			Assert.AreEqual(3, counter);

			Assert.IsNotNull(complexState);
			Assert.AreEqual(1, complexState.I);
			Assert.AreEqual(1, complexState.L);
		}
	}
}
