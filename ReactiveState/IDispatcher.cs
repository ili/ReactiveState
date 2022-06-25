using System.Threading.Tasks;

namespace ReactiveState
{

	public interface IDispatcher<TState>
	{
		Task<TState?> Dispatch(IAction action);
	}
}
