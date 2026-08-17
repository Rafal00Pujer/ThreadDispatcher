namespace ThreadDispatcher;

public partial class Dispatcher
{
	private interface IDispatcherWork
	{
		public void Execute();
	}
}
