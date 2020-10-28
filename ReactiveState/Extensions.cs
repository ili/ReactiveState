using System;
using System.Linq;
using System.Reactive.Linq;

namespace ReactiveState
{
	public static class Extensions
	{
		public static IObservable<IAction> Actions(this IObservable<IAction> actions) => actions;


		public static IObservable<IAction> OfType(this IObservable<IAction> actions, params string[] types) 
			=> actions.Where(_ => types.Contains(_.Type));

	}
}
