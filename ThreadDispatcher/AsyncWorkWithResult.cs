namespace ThreadDispatcher;

public partial class Dispatcher
{
	public sealed class AsyncWork<T>(Func<T> func, CancellationToken cancellationToken) : IDispatcherWork
	{
		private readonly TaskCompletionSource<T> _tcs = new();

		public Task<T> Task => _tcs.Task;

		public void Execute()
		{
			try
			{
				cancellationToken.ThrowIfCancellationRequested();
				_tcs.SetResult(func());
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
