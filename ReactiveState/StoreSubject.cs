using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace ReactiveState
{
	public sealed class StoreSubject : IObservable<IStateStorage>
	{
		private readonly BehaviorSubject<IStateStorage> _subject;

		public StoreSubject(IStateStorage stateAccessor)
		{
			_subject = new BehaviorSubject<IStateStorage>(stateAccessor);
		}

		public IDisposable Subscribe(IObserver<IStateStorage> observer)
			=> _subject.Subscribe(observer);
	}
}
