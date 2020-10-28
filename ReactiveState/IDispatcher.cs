using System.Threading.Tasks;

namespace ReactiveState
{

	public interface IDispatcher
	{
		Task Dispatch(IAction action);
	}
}
