using NUnit.Framework;
using ReactiveState.ComplexState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	using Ctx = DispatchContext<int>;

	public class ToolsTests
	{
		private static readonly int Value = 101;
#pragma warning disable CS0169
		private static int NonReadOnlyValue;
#pragma warning restore CS0169

		private static readonly Reducer<int,  IAction> SomeReducer = (a, b) => ++a;
		private static readonly Reducer<long, IAction> LongReducer1 = (a, b) => ++a;
		private static readonly Reducer<long, MyAction> LongReducer2 = (a, b) => 100;

		private static readonly Func<     int, IAction, IAction?>          Effect01 = (a, b)    => null;
		private static readonly Func<     int, IAction, Task<IAction?>>    Effect02 = (a, b)    => Task.FromResult<IAction?>(null);
		private static readonly Func<Ctx, int, IAction, IAction?>          Effect03 = (a, b, c) => null;
		private static readonly Func<Ctx, int, IAction, Task<IAction?>>    Effect04 = (a, b, c) => Task.FromResult<IAction?>(null);

		private static readonly Func<     int, MyAction, IAction?>         Effect05 = (a, b)    => null;
		private static readonly Func<     int, MyAction, Task<IAction?>>   Effect06 = (a, b)    => Task.FromResult<IAction?>(null);
		private static readonly Func<Ctx, int, MyAction, IAction?>         Effect07 = (a, b, c) => null;
		private static readonly Func<Ctx, int, MyAction, Task<IAction?>>   Effect08 = (a, b, c) => Task.FromResult<IAction?>(null);

		private static readonly Func<     int, IAction, MyAction?>         Effect09 = (a, b)    => null;
		private static readonly Func<     int, IAction, Task<MyAction?>>   Effect10 = (a, b)    => Task.FromResult<MyAction?>(null);
		private static readonly Func<Ctx, int, IAction, MyAction?>         Effect11 = (a, b, c) => null;
		private static readonly Func<Ctx, int, IAction, Task<MyAction?>>   Effect12 = (a, b, c) => Task.FromResult<MyAction?>(null);

		private static readonly Func<     int, MyAction, MyAction?>        Effect13 = (a, b)    => null;
		private static readonly Func<     int, MyAction, Task<MyAction?>>  Effect14 = (a, b)    => Task.FromResult<MyAction?>(null);
		private static readonly Func<Ctx, int, MyAction, MyAction?>        Effect15 = (a, b, c) => null;
		private static readonly Func<Ctx, int, MyAction, Task<MyAction?>>  Effect16 = (a, b, c) => Task.FromResult<MyAction?>(null);

		private static readonly Func<     int,  IAction?>          StateEffect01 = (b)    => null;
		private static readonly Func<     int,  Task<IAction?>>    StateEffect02 = (b)    => Task.FromResult<IAction?>(null);
		private static readonly Func<Ctx, int,  IAction?>          StateEffect03 = (a, b) => null;
		private static readonly Func<Ctx, int,  Task<IAction?>>    StateEffect04 = (a, b) => Task.FromResult<IAction?>(null);

		private static readonly Func<     int, MyAction?>          StateEffect05 = (b)    => null;
		private static readonly Func<     int, Task<MyAction?>>    StateEffect06 = (b)    => Task.FromResult<MyAction?>(null);
		private static readonly Func<Ctx, int, MyAction?>          StateEffect07 = (a, b) => null;
		private static readonly Func<Ctx, int, Task<MyAction?>>    StateEffect08 = (a, b) => Task.FromResult<MyAction?>(null);

		private static readonly Func<       IObservable<int>, IObservable<IAction>>    ObservableStateEffect01 = (b)    => Observable.Return<IAction>(null);
		private static readonly Func<Store, IObservable<int>, IObservable<IAction>>    ObservableStateEffect02 = (a, b) => Observable.Return<IAction>(null);

		[Test]
		public void ReadonlyStaticFieldsTest()
		{
			var field = GetType().ReadonlyStaticFields<int>().Single();
			Assert.AreEqual(Value, field);
		}

		[Test]
		public void ReducerTest()
		{
			var field = GetType().Reducers<int>().Single();
			Assert.AreEqual(2, field(1, null));
		}

		[Test]
		public void ReducerLikeTest()
		{
			var field = GetType().Reducers<long>().ToArray();
			Assert.AreEqual(2,   field.Length);
			Assert.AreEqual(2,   field[0](1,  null));
			Assert.AreEqual(100, field[1](21, new MyAction()));
		}

		[Test]
		public void ReducerWrapperTest()
		{
			var reducer = LongReducer2.Wrap();

			Assert.AreEqual(0,   reducer(0, new MyGenericAction(0)));
			Assert.AreEqual(100, reducer(0, new MyAction()));

		}

		public class MyAction : IAction
		{
			public string Type => throw new System.NotImplementedException();
		}

		private class MyGenericAction: SetStateAction<int>
		{
			public MyGenericAction(int state) : base(state)
			{
			}
		}

		[Test]
		public void GetActionTypeTest()
		{
			var myActionType = new MyAction().GetActionTypeName();

			Assert.AreEqual("ReactiveState.Tests.ToolsTests+MyAction", myActionType);

			var genericAction = new SetStateAction<int>(1).GetActionTypeName();

			Assert.AreEqual("ReactiveState.SetStateAction[Int32]", genericAction);

			var myGgenericAction = new MyGenericAction(0).GetActionTypeName();
			Assert.AreEqual("ReactiveState.Tests.ToolsTests+MyGenericAction", myGgenericAction);
		}

		private class SampleAction: ActionBase { }

		private interface IMarker { }

		private interface IMarker<T>: IMarker { }

		private class MarkerOfString: IMarker<string>
		{ }

		private interface IDispatchContext2<T> : IDispatchContext<T>
		{

		}

		[Test]
		public void LikeTest()
		{
			AssertLike(typeof(object),       typeof(object));
			AssertLike(typeof(ToolsTests),   typeof(object));
			AssertLike(typeof(ActionBase),   typeof(IAction));
			AssertLike(typeof(SampleAction), typeof(IAction));
			AssertLike(typeof(string),       typeof(object));

			AssertLike(typeof(Action<string>),         typeof(Action<object>));
			AssertLike(typeof(Action<ActionBase>),     typeof(Action<IAction>));
			AssertLike(typeof(IMarker<ActionBase>),    typeof(IMarker<IAction>));
			AssertLike(typeof(IMarker<ActionBase>),    typeof(IMarker));
			AssertLike(typeof(MarkerOfString),         typeof(IMarker));
			AssertLike(typeof(MarkerOfString),         typeof(IMarker<object>));
			AssertLike(typeof(DispatchContext<int>),   typeof(IDispatchContext<int>));
			AssertLike(typeof(IDispatchContext2<int>), typeof(IDispatchContext<int>));

			AssertLike(typeof(Reducer<object, SampleAction>), typeof(Reducer<object, IAction>));

			var e = Enumerable.Empty<string>();
			Assert.True(e is IEnumerable<object>);

			Assert.False(new MarkerOfString() is IMarker<object>);
		}

		private static void AssertLike(Type targetType, Type patternType)
		{
			Assert.True (targetType.Like(patternType), $"{targetType.GetActionTypeName()} should be like {patternType.GetActionTypeName()}");

			if (targetType != patternType)
				Assert.False(patternType.Like(targetType), $"{patternType.GetActionTypeName()} should NOT be like {targetType.GetActionTypeName()}");
		}

		private class Store : Store<int>
		{
			public Store(int initialState, Middleware<int, DispatchContext<int>> middleware) : base(initialState, middleware)
			{
			}
		}

		[Test]
		public async Task EffectWrapperTest()
		{
			var builder = new MiddlewareBuilder<int, DispatchContext<int>>();
			var store = new Store(0, builder.Build());

			{
				Func<IDispatchContext<int>, int, MyAction, Task<IAction>> func = (a, b, c) => Task.FromResult((IAction)new MyAction());
				var wrapped = func.Wrap();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<IDispatchContext<int>, int, MyAction, Task<MyAction>> func = (a, b, c) => Task.FromResult(new MyAction());
				var wrapped = func.Wrap();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<int, MyAction, Task<IAction>> func = (b, c) => Task.FromResult((IAction)new MyAction());
				var wrapped = func.Wrap<DispatchContext<int>, int, MyAction, Task<IAction>>();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<int, MyAction, Task<MyAction>> func = (b, c) => Task.FromResult(new MyAction());
				var wrapped = func.Wrap<IDispatchContext<int>, int, MyAction, Task<MyAction>>();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<IDispatchContext<int>, int, MyAction, IAction> func = (a, b, c) => new MyAction();
				var wrapped = func.Wrap();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<IDispatchContext<int>, int, MyAction, MyAction> func = (a, b, c) => new MyAction();
				var wrapped = func.Wrap();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<int, MyAction, IAction> func = (b, c) => new MyAction();
				var wrapped = func.Wrap<IDispatchContext<int>, int, MyAction, IAction>();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<int, MyAction, MyAction> func = (b, c) => new MyAction();
				var wrapped = func.Wrap<IDispatchContext<int>, int, MyAction, MyAction>();

				Assert.IsInstanceOf<MyAction>(await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsNull                (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}

			{
				Func<int, MyAction, SampleAction, IAction> func = (b, c, d) => (IAction)c ?? d;
				var wrapped = Tools.EffectWrapper<IDispatchContext<int>, int>(func);

				Assert.IsInstanceOf<MyAction>     (await wrapped(new DispatchContext<int>(new MyAction(),     0, store, store)));
				Assert.IsInstanceOf<SampleAction> (await wrapped(new DispatchContext<int>(new SampleAction(), 0, store, store)));
			}
		}

		[Test]
		public void LikeEffectTest()
		{
			var type = GetType();

			var expected = Enumerable.Range(1, 16).Select(_ => $"Effect{_:00}").ToArray();

			var fields = type.ReadonlyStaticFields()
				.Where(_ => _.FieldType.LikeEffect<IDispatchContext<int>, int>())
				.Select(_ => _.Name)
				.OrderBy(_ => _)
				.ToArray();

			Assert.AreEqual(
				string.Join(", ", expected),
				string.Join(", ", fields));
		}

		[Test]
		public void EffectsTest()
		{
			var type = GetType();

			Assert.AreEqual(16, type.Effects<IDispatchContext<int>,   int>().Count());
			Assert.AreEqual(0,  type.Effects<IDispatchContext<long>, long>().Count());
		}

		class ComplexState
		{
			public int IntValue;
#pragma warning disable CS0649
			public string StringValue;
#pragma warning restore CS0649

		}

		[Test]
		public void LikeReducerTest()
		{
			Assert.True(typeof(Reducer<int, IAction> ).LikeReducer<int>());
			Assert.True(typeof(Reducer<int, MyAction>).LikeReducer<int>());


			Assert.True (typeof(Reducer<ComplexState, IAction> ).LikeReducer<ComplexState>());
			Assert.True (typeof(Reducer<ComplexState, MyAction>).LikeReducer<ComplexState>());
			//Assert.True (typeof(Reducer<int,          IAction> ).LikeReducer<ComplexState>(stateTree));
			//Assert.True (typeof(Reducer<int,          MyAction>).LikeReducer<ComplexState>(stateTree));
			Assert.False(typeof(Reducer<int,          IAction> ).LikeReducer<ComplexState>());
			Assert.False(typeof(Reducer<int,          MyAction>).LikeReducer<ComplexState>());

		}

		[Test]
		public void LikeEffectTest2()
		{
			Assert.True (typeof(Func<                                ComplexState,  IAction,  IAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.True (typeof(Func<                                ComplexState, MyAction,  IAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.True (typeof(Func<DispatchContext<ComplexState>,  ComplexState,  IAction, MyAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.True (typeof(Func<DispatchContext<ComplexState>,  ComplexState, MyAction, MyAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.True (typeof(Func<IDispatchContext<ComplexState>, ComplexState,  IAction, MyAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.True (typeof(Func<IDispatchContext<ComplexState>, ComplexState, MyAction, MyAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());

			Assert.False(typeof(Func<                               int,  IAction,  IAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.False(typeof(Func<                               int, MyAction,  IAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.False(typeof(Func<DispatchContext<ComplexState>, int,  IAction, MyAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());
			Assert.False(typeof(Func<DispatchContext<ComplexState>, int, MyAction, MyAction>).LikeEffect<IDispatchContext<ComplexState>, ComplexState>());

			//Assert.True (typeof(Func<                     int,  IAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			//Assert.True (typeof(Func<                     int, MyAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			//Assert.True (typeof(Func<Store<ComplexState>, int,  IAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			//Assert.True (typeof(Func<Store<ComplexState>, int, MyAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));

			//Assert.False(typeof(Func<                     string,  IAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			//Assert.False(typeof(Func<                     string, MyAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			//Assert.False(typeof(Func<Store<ComplexState>, string,  IAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			//Assert.False(typeof(Func<Store<ComplexState>, string, MyAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
		}

		public class StoreAction: ActionBase
		{
			public int Value;
		}

		//[Test]
		//public void EffectWrapperTest2()
		//{
		//	Func<                     int,  IAction,  IAction> effect1 = (     a, b) => new StoreAction { Value = a };
		//	Func<                     int, MyAction,  IAction> effect2 = (     a, b) => b;
		//	Func<Store<ComplexState>, int,  IAction, MyAction> effect3 = (ctx, a, b) => new MyAction();
		//	Func<Store<ComplexState>, int, MyAction, MyAction> effect4 = (ctx, a, b) => b;

		//	Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect1));
		//	Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect2));
		//	Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect3));
		//	Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect4));

		//	var wrapped = Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect1);
		//	var action = wrapped(null, new ComplexState()
		//		{
		//			IntValue = 999
		//		}, new MyAction()).Result as StoreAction;

		//	Assert.NotNull(action);
		//	Assert.AreEqual(999, action.Value);
		//}

		[Test]
		public void OrDefaultTest()
		{
			ComplexState state = null;

			Assert.AreEqual(0, state.ValueOrDefault(_ => _.IntValue));

			state = new ComplexState();
			state.IntValue = 8890;

			Assert.AreEqual(state.IntValue, state.ValueOrDefault(_ => _.IntValue));
		}

		[Test]
		public void IsTest()
		{
#pragma warning disable CS0184, CS0183
			Assert.False(null is IAction);
			Assert.True(1 is object);
#pragma warning restore CS0184, CS0183
		}


		[Test]
		public void StateEffectsTest()
		{
			var type = GetType();

			Assert.AreEqual(16, type.Effects< int>().Count());
			Assert.AreEqual(0,  type.Effects<long>().Count());
		}

		[Test]
		public void LikeObservableStateEffectTest()
		{
			var type = GetType();

			var expected = Enumerable.Range(1, 2).Select(_ => $"ObservableStateEffect{_:00}").ToArray();

			var fields = type.ReadonlyStaticFields()
				.Where(_ => _.FieldType.LikeObservableStateEffect<Store, int>())
				.Select(_ => _.Name)
				.OrderBy(_ => _)
				.ToArray();

			Assert.AreEqual(
				string.Join(", ", expected),
				string.Join(", ", fields));
		}

		[Test]
		public void ObservableStateEffectsTest()
		{
			var type = GetType();

			Assert.AreEqual(2, type.ObservableStateEffects<Store,  int>().Count());
			Assert.AreEqual(0, type.ObservableStateEffects<Store, long>().Count());
		}

		[Test]
		public void ObservableStateEffectWrapperTest()
		{
			Func<IObservable<ComplexState>, IObservable<IAction>>  effect1 = (a) => Observable.Empty<IAction> ();
			Func<IObservable<int>,          IObservable<MyAction>> effect2 = (a) => Observable.Empty<MyAction>();

			Func<Store<ComplexState>, IObservable<ComplexState>, IObservable<IAction>>  effect3 = (ctx, a) => Observable.Empty<IAction>();
			Func<Store<ComplexState>, IObservable<int>,          IObservable<MyAction>> effect4 = (ctx, a) => Observable.Empty<MyAction>();

			Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect1));
			//Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect2));
			Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect3));
			//Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect4));
		}

		public class SubState1 { }
		public class SubState2 { }
		public class SubState3 { }

		public class SubSate1Action : ActionBase { }
		public class SubSate23Action : ActionBase { }

		public class SubState4 : ActionBase
		{
			public SubState4(IAction? p1, IAction? p2, IAction a1, IAction a2)
			{
				P1 = p1;
				P2 = p2;
				A1 = a1;
				A2 = a2;
			}

			public IAction? P1 { get; }
			public IAction? P2 { get; }
			public IAction A1 { get; }
			public IAction A2 { get; }
		}

		[Test]
		public void BuildComplexReducerTest()
		{
			var reducer = Tools.BuildComplexReducer(
				(Expression<Reducer<IAction, IAction>>)((IAction? p, IAction a) => new SubState4(p, p, a, a)),
				(Reducer<SubState1, SubSate1Action>)((SubState1? p, SubSate1Action a) => new SubState1()),
				(Reducer<SubState2, SubSate23Action>)((SubState2? p, SubSate23Action a) => new SubState2()),
				(Reducer<SubState3, SubSate23Action>)((SubState3? p, SubSate23Action a) => new SubState3())
				);

			Assert.NotNull(reducer);

			var orig = new State();
			var res1 = reducer(orig, new SubSate1Action())!;
			Assert.NotNull(res1);
			Assert.AreNotEqual(orig, res1);
			Assert.NotNull(res1.Get<SubState1>());
			Assert.Null(res1.Get<SubState2>());
			Assert.Null(res1.Get<SubState3>());
			Assert.That(res1.Get<IAction>() is SubState4);

			var res2 = reducer(res1, new SubSate23Action())!;
			Assert.NotNull(res2);
			Assert.AreNotEqual(orig, res2);
			Assert.NotNull(res2.Get<SubState1>());
			Assert.NotNull(res2.Get<SubState2>());
			Assert.NotNull(res2.Get<SubState3>());
			Assert.That(res2.Get<IAction>() is SubState4);

		}
	}
}
