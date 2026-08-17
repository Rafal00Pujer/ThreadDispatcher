namespace ThreadDispatcher;

public partial class Dispatcher
{
	private sealed class SyncWork(Action action) : IDispatcherWork
	{
		private readonly ManualResetEventSlim _completed = new(false);
		
		private Exception? _exception;

		public void Execute()
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				_exception = e;
			}
			finally
			{
				_completed.Set();
			}
		}

		public void Wait()
		{
			_completed.Wait();

			if (_exception is not null)
			{
				throw _exception;
			}
		}
	}
}
