namespace ReactiveState
{
	public class SetStateAction<TState> : ActionBase
	{
		public SetStateAction(TState? state)
		{
			State = state;
		}

		public TState? State { get; }

		public static readonly Reducer<TState?, SetStateAction<TState>> Reducer = (st, a) =>  a.State;
	}
}
