namespace ReactiveState
{
	public abstract class ActionBase : IAction
	{
		private string _type;

		public ActionBase()
		{
			_type = this.GetActionTypeName();
		}

		public virtual string Type => _type;
	}
}
