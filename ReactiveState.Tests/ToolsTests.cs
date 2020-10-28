using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ReactiveState.Tests
{
	public class ToolsTests
	{
		private static readonly int Value = 101;
#pragma warning disable CS0169
		private static int NonReadOnlyValue;
#pragma warning restore CS0169

		private static readonly Reducer<int, IAction> SomeReducer = (a, b) => ++a;
		private static readonly Reducer<long, IAction> LongReducer1 = (a, b) => ++a;
		private static readonly Reducer<long, MyAction> LongReducer2 = (a, b) => 100;

		private static readonly Func<       int, IAction, IAction>          Effect01 = (a, b)    => null;
		private static readonly Func<       int, IAction, Task<IAction>>    Effect02 = (a, b)    => Task.FromResult<IAction>(null);
		private static readonly Func<Store, int, IAction, IAction>          Effect03 = (a, b, c) => null;
		private static readonly Func<Store, int, IAction, Task<IAction>>    Effect04 = (a, b, c) => Task.FromResult<IAction>(null);

		private static readonly Func<       int, MyAction, IAction>         Effect05 = (a, b)    => null;
		private static readonly Func<       int, MyAction, Task<IAction>>   Effect06 = (a, b)    => Task.FromResult<IAction>(null);
		private static readonly Func<Store, int, MyAction, IAction>         Effect07 = (a, b, c) => null;
		private static readonly Func<Store, int, MyAction, Task<IAction>>   Effect08 = (a, b, c) => Task.FromResult<IAction>(null);

		private static readonly Func<       int, IAction, MyAction>         Effect09 = (a, b)    => null;
		private static readonly Func<       int, IAction, Task<MyAction>>   Effect10 = (a, b)    => Task.FromResult<MyAction>(null);
		private static readonly Func<Store, int, IAction, MyAction>         Effect11 = (a, b, c) => null;
		private static readonly Func<Store, int, IAction, Task<MyAction>>   Effect12 = (a, b, c) => Task.FromResult<MyAction>(null);

		private static readonly Func<       int, MyAction, MyAction>        Effect13 = (a, b)    => null;
		private static readonly Func<       int, MyAction, Task<MyAction>>  Effect14 = (a, b)    => Task.FromResult<MyAction>(null);
		private static readonly Func<Store, int, MyAction, MyAction>        Effect15 = (a, b, c) => null;
		private static readonly Func<Store, int, MyAction, Task<MyAction>>  Effect16 = (a, b, c) => Task.FromResult<MyAction>(null);

		private static readonly Func<       int,  IAction>          StateEffect01 = (b)    => null;
		private static readonly Func<       int,  Task<IAction>>    StateEffect02 = (b)    => Task.FromResult<IAction>(null);
		private static readonly Func<Store, int,  IAction>          StateEffect03 = (a, b) => null;
		private static readonly Func<Store, int,  Task<IAction>>    StateEffect04 = (a, b) => Task.FromResult<IAction>(null);

		private static readonly Func<       int, MyAction>          StateEffect05 = (b)    => null;
		private static readonly Func<       int, Task<MyAction>>    StateEffect06 = (b)    => Task.FromResult<MyAction>(null);
		private static readonly Func<Store, int, MyAction>          StateEffect07 = (a, b) => null;
		private static readonly Func<Store, int, Task<MyAction>>    StateEffect08 = (a, b) => Task.FromResult<MyAction>(null);

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
			var field = GetType().Reducers<int>(null).Single();
			Assert.AreEqual(2, field(1, null));
		}

		[Test]
		public void ReducerLikeTest()
		{
			var field = GetType().Reducers<long>(null).ToArray();
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

		[Test]
		public void LikeTest()
		{
			AssertLike(typeof(object),       typeof(object));
			AssertLike(typeof(ToolsTests),   typeof(object));
			AssertLike(typeof(ActionBase),   typeof(IAction));
			AssertLike(typeof(SampleAction), typeof(IAction));
			AssertLike(typeof(string),       typeof(object));

			AssertLike(typeof(Action<string>),      typeof(Action<object>));
			AssertLike(typeof(Action<ActionBase>),  typeof(Action<IAction>));
			AssertLike(typeof(IMarker<ActionBase>), typeof(IMarker<IAction>));
			AssertLike(typeof(IMarker<ActionBase>), typeof(IMarker));
			AssertLike(typeof(MarkerOfString),      typeof(IMarker));
			AssertLike(typeof(MarkerOfString),      typeof(IMarker<object>));


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
			public Store(int initialState, params Middleware<Store<int>, int>[] middlewares) : base(initialState, middlewares)
			{
			}
		}

		[Test]
		public async Task EffectWrapperTest()
		{
			var store = new Store(0);

			{
				Func<IStoreContext, int, MyAction, Task<IAction>> func = (a, b, c) => Task.FromResult((IAction)new MyAction());
				var wrapped = func.Wrap(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<IStoreContext, int, MyAction, Task<MyAction>> func = (a, b, c) => Task.FromResult(new MyAction());
				var wrapped = func.Wrap(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<int, MyAction, Task<IAction>> func = (b, c) => Task.FromResult((IAction)new MyAction());
				var wrapped = func.Wrap<Store, int, MyAction, Task<IAction>>(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<int, MyAction, Task<MyAction>> func = (b, c) => Task.FromResult(new MyAction());
				var wrapped = func.Wrap<Store, int, MyAction, Task<MyAction>>(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<IStoreContext, int, MyAction, IAction> func = (a, b, c) => new MyAction();
				var wrapped = func.Wrap(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<IStoreContext, int, MyAction, MyAction> func = (a, b, c) => new MyAction();
				var wrapped = func.Wrap(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<int, MyAction, IAction> func = (b, c) => new MyAction();
				var wrapped = func.Wrap<Store, int, MyAction, IAction>(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}

			{
				Func<int, MyAction, MyAction> func = (b, c) => new MyAction();
				var wrapped = func.Wrap<Store, int, MyAction, MyAction>(null);

				Assert.IsInstanceOf<MyAction>(await wrapped(store, 0, new MyAction()));
				Assert.IsNull                (await wrapped(store, 0, new SampleAction()));
			}
		}

		[Test]
		public void LikeEffectTest()
		{
			var type = GetType();

			var expected = Enumerable.Range(1, 16).Select(_ => $"Effect{_:00}").ToArray();

			var fields = type.ReadonlyStaticFields()
				.Where(_ => _.FieldType.LikeEffect<Store, int>(null))
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

			Assert.AreEqual(16, type.Effects<Store,  int>(null).Count());
			Assert.AreEqual(0,  type.Effects<Store, long>(null).Count());
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
			Assert.True(typeof(Reducer<int, IAction> ).LikeReducer<int>(null));
			Assert.True(typeof(Reducer<int, MyAction>).LikeReducer<int>(null));

			var stateTree = new StateTreeBuilder<ComplexState>()
				.With(_ => _.IntValue, (a, b) => a)
				.Build();

			Assert.True (typeof(Reducer<ComplexState, IAction> ).LikeReducer<ComplexState>(null));
			Assert.True (typeof(Reducer<ComplexState, MyAction>).LikeReducer<ComplexState>(null));
			Assert.True (typeof(Reducer<int,          IAction> ).LikeReducer<ComplexState>(stateTree));
			Assert.True (typeof(Reducer<int,          MyAction>).LikeReducer<ComplexState>(stateTree));
			Assert.False(typeof(Reducer<int,          IAction> ).LikeReducer<ComplexState>(null));
			Assert.False(typeof(Reducer<int,          MyAction>).LikeReducer<ComplexState>(null));

		}

		[Test]
		public void LikeEffectTest2()
		{
			var stateTree = new StateTreeBuilder<ComplexState>()
				.With(_ => _.IntValue, (a, b) => a)
				.Build();

			Assert.True (typeof(Func<                     ComplexState,  IAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));
			Assert.True (typeof(Func<                     ComplexState, MyAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));
			Assert.True (typeof(Func<Store<ComplexState>, ComplexState,  IAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));
			Assert.True (typeof(Func<Store<ComplexState>, ComplexState, MyAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));

			Assert.False(typeof(Func<                     int,  IAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));
			Assert.False(typeof(Func<                     int, MyAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));
			Assert.False(typeof(Func<Store<ComplexState>, int,  IAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));
			Assert.False(typeof(Func<Store<ComplexState>, int, MyAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(null));

			Assert.True (typeof(Func<                     int,  IAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			Assert.True (typeof(Func<                     int, MyAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			Assert.True (typeof(Func<Store<ComplexState>, int,  IAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			Assert.True (typeof(Func<Store<ComplexState>, int, MyAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));

			Assert.False(typeof(Func<                     string,  IAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			Assert.False(typeof(Func<                     string, MyAction,  IAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			Assert.False(typeof(Func<Store<ComplexState>, string,  IAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
			Assert.False(typeof(Func<Store<ComplexState>, string, MyAction, MyAction>).LikeEffect<Store<ComplexState>, ComplexState>(stateTree));
		}

		public class StoreAction: ActionBase
		{
			public int Value;
		}

		[Test]
		public void EffectWrapperTest2()
		{
			var stateTree = new StateTreeBuilder<ComplexState>()
				.With(_ => _.IntValue, (a, b) => a)
				.Build();

			Func<                     int,  IAction,  IAction> effect1 = (     a, b) => new StoreAction { Value = a };
			Func<                     int, MyAction,  IAction> effect2 = (     a, b) => b;
			Func<Store<ComplexState>, int,  IAction, MyAction> effect3 = (ctx, a, b) => new MyAction();
			Func<Store<ComplexState>, int, MyAction, MyAction> effect4 = (ctx, a, b) => b;

			Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect1, stateTree));
			Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect2, stateTree));
			Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect3, stateTree));
			Assert.NotNull(Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect4, stateTree));

			var wrapped = Tools.EffectWrapper<Store<ComplexState>, ComplexState>(effect1, stateTree);
			var action = wrapped(null, new ComplexState()
				{
					IntValue = 999
				}, new MyAction()).Result as StoreAction;

			Assert.NotNull(action);
			Assert.AreEqual(999, action.Value);
		}

		[Test]
		public void OrDefaultTest()
		{
			ComplexState state = null;

			Assert.AreEqual(0, state.GetOrDefault(_ => _.IntValue));

			state = new ComplexState();
			state.IntValue = 8890;

			Assert.AreEqual(state.IntValue, state.GetOrDefault(_ => _.IntValue));
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
		public void LikeStateEffectTest()
		{
			var type = GetType();

			var expected = Enumerable.Range(1, 8).Select(_ => $"StateEffect{_:00}").ToArray();

			var fields = type.ReadonlyStaticFields()
				.Where(_ => _.FieldType.LikeStateEffect<Store, int>(null))
				.Select(_ => _.Name)
				.OrderBy(_ => _)
				.ToArray();

			Assert.AreEqual(
				string.Join(", ", expected),
				string.Join(", ", fields));
		}

		[Test]
		public void StateEffectsTest()
		{
			var type = GetType();

			Assert.AreEqual(8, type.StateEffects<Store,  int>(null).Count());
			Assert.AreEqual(0, type.StateEffects<Store, long>(null).Count());
		}

		[Test]
		public void LikeObservableStateEffectTest()
		{
			var type = GetType();

			var expected = Enumerable.Range(1, 2).Select(_ => $"ObservableStateEffect{_:00}").ToArray();

			var fields = type.ReadonlyStaticFields()
				.Where(_ => _.FieldType.LikeObservableStateEffect<Store, int>(null))
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

			Assert.AreEqual(2, type.ObservableStateEffects<Store,  int>(null).Count());
			Assert.AreEqual(0, type.ObservableStateEffects<Store, long>(null).Count());
		}

		[Test]
		public void ObservableStateEffectWrapperTest()
		{
			var stateTree = new StateTreeBuilder<ComplexState>()
				.With(_ => _.IntValue, (a, b) => a)
				.Build();

			Func<IObservable<ComplexState>, IObservable<IAction>>  effect1 = (a) => Observable.Empty<IAction> ();
			Func<IObservable<int>,          IObservable<MyAction>> effect2 = (a) => Observable.Empty<MyAction>();

			Func<Store<ComplexState>, IObservable<ComplexState>, IObservable<IAction>>  effect3 = (ctx, a) => Observable.Empty<IAction>();
			Func<Store<ComplexState>, IObservable<int>,          IObservable<MyAction>> effect4 = (ctx, a) => Observable.Empty<MyAction>();

			Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect1, stateTree));
			Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect2, stateTree));
			Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect3, stateTree));
			Assert.NotNull(Tools.ObservableStateEffectWrapper<Store<ComplexState>, ComplexState>(effect4, stateTree));
		}
	}
}
