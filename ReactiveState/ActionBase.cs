namespace ReactiveState
{
	public abstract class ActionBase : IAction
	{
		protected string _type = null;
		public virtual string Type => _type ?? (_type = this.GetActionTypeName());
	}
}
