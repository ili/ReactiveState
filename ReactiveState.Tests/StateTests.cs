using Newtonsoft.Json;
using NUnit.Framework;
using ReactiveState.ComplexState;
using System.Collections.Generic;

namespace ReactiveState.Tests
{
	public class StateTests
	{

		[Test]
		public void KeyNotFound()
		{
			Assert.Throws<KeyNotFoundException>(() => new State().Get<SimpleState>());
		}

		[Test]
		public void NoKey()
		{
			Assert.False(new State().ContainsKey("int"));
		}

		[Test]
		public void Create()
		{
			var st = new SimpleState(11);
			var original = new State();
			var changed = original
				.BeginTransaction()
				.Set(st)
				.Commit();

			Assert.False(original.ContainsKey<SimpleState>());
			Assert.AreNotEqual(original, changed);

			Assert.True(changed.ContainsKey<SimpleState>());
			Assert.AreEqual(st, changed.Get<SimpleState>());
		}

		[Test]
		public void SerializationTest()
		{
			var st = new State()
				.BeginTransaction()
				.Set(new SimpleState(121))
				.Commit();

			var data = JsonConvert.SerializeObject(st, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto
			});

			var st2 = JsonConvert.DeserializeObject<State>(data, new JsonSerializerSettings()
			{
				TypeNameHandling = TypeNameHandling.Auto
			});

			Assert.True(st2.ContainsKey<SimpleState>());
			Assert.AreEqual(st.Get<SimpleState>(), st2.Get<SimpleState>());
		}
	}
}
