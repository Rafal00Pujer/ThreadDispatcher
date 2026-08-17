namespace ThreadDispatcher;

public partial class Dispatcher
{
	private sealed class AsyncWork(Action action, CancellationToken cancellationToken) : IDispatcherWork
	{
		private readonly TaskCompletionSource _tcs = new();

		public Task Task => _tcs.Task;

		public void Execute()
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				action();
				_tcs.SetResult();
			}
			catch (OperationCanceledException e)
			{
				_tcs.SetCanceled(e.CancellationToken);
			}
			catch (Exception e)
			{
				_tcs.SetException(e);
			}
		}
	}
}
