using System.Diagnostics;

namespace ThreadDispatcher;

public partial class Dispatcher
{
	private sealed class SyncWork<T>(Func<T> func) : IDispatcherWork
	{
		private readonly ManualResetEventSlim _completed = new(false);

		private Exception? _exception;
		private T? _result;

		public void Execute()
		{
			try
			{
				_result = func();
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

		public T WaitForResult()
		{
			_completed.Wait();

			if (_exception is not null)
			{
				throw _exception;
			}

			Debug.Assert(_result is not null);
			
			return _result;
		}
	}
}
