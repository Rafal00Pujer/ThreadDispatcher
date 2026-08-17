namespace ThreadDispatcher;

public partial class Dispatcher
{
	private sealed class TaskWork<T>(Func<Task<T>> func, CancellationToken cancellationToken) : IDispatcherWork
	{
		private readonly TaskCompletionSource<T> _tcs = new();

		public Task<T> Task => _tcs.Task;

		public async void Execute()
		{
			try
			{
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
				}
				catch (OperationCanceledException e)
				{
					_tcs.SetCanceled(e.CancellationToken);

					return;
				}

				var task = func();

				try
				{
					await task;
					_tcs.SetFromTask(task);
				}
				catch when (task.IsCompleted)
				{
					_tcs.SetFromTask(task);
				}
			}
			catch (Exception e)
			{
				_tcs.SetException(e);
			}
		}
	}
}
