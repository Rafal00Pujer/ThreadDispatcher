using System.Collections.Concurrent;

namespace ThreadDispatcher;

public sealed partial class Dispatcher : IDisposable
{
	private const int NotStartedThreadId = -1;

	private readonly BlockingCollection<IDispatcherWork> _queue = new();
	private readonly TaskCompletionSource _startedCompletionSource = new();
	private readonly TaskCompletionSource _finishedCompletionSource = new();

	private volatile int _threadId = NotStartedThreadId;

	public Task StartedTask => _startedCompletionSource.Task;
	public Task FinishedTask => _finishedCompletionSource.Task;

	public void Invoke(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		ThrowIfFinished();

		var work = new SyncWork(action);
		EnqueueOrExecuteWork(work);

		work.Wait();
	}

	public T Invoke<T>(Func<T> func)
	{
		ArgumentNullException.ThrowIfNull(func);

		ThrowIfFinished();

		var work = new SyncWork<T>(func);
		EnqueueOrExecuteWork(work);

		return work.WaitForResult();
	}

	public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(action);

		ThrowIfFinished();

		var work = new AsyncWork(action, cancellationToken);
		EnqueueOrExecuteWork(work, cancellationToken);

		return work.Task;
	}

	public Task<T> InvokeAsync<T>(Func<T> func, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(func);

		ThrowIfFinished();

		var work = new AsyncWork<T>(func, cancellationToken);
		EnqueueOrExecuteWork(work, cancellationToken);

		return work.Task;
	}

	public Task InvokeAsync(Func<Task> func, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(func);

		ThrowIfFinished();

		var work = new TaskWork(func, cancellationToken);
		EnqueueOrExecuteWork(work, cancellationToken);

		return work.Task;
	}

	public Task<T> InvokeAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(func);

		ThrowIfFinished();

		var work = new TaskWork<T>(func, cancellationToken);
		EnqueueOrExecuteWork(work, cancellationToken);

		return work.Task;
	}

	public SynchronizationContextInstaller InstallSynchronizationContext()
		=> SynchronizationContextInstaller.Install(this);

	public void Run()
	{
		ThrowIfFinished();

		if (Interlocked.CompareExchange(ref _threadId, Environment.CurrentManagedThreadId, NotStartedThreadId)
			!= NotStartedThreadId)
		{
			throw new InvalidOperationException("This dispatcher is already running.");
		}

		_startedCompletionSource.TrySetResult();

		using (InstallSynchronizationContext())
		{
			ProcessWork();
		}

		_finishedCompletionSource.TrySetResult();
	}

	public void RequestStop()
	{
		if (_isDisposed)
		{
			return;
		}

		_queue.CompleteAdding();
	}

	private void ThrowIfFinished()
	{
		if (_isDisposed || _queue.IsAddingCompleted)
		{
			throw new InvalidOperationException("This dispatcher finished its work.");
		}
	}

	private bool IsDispatcherThread => _threadId == Environment.CurrentManagedThreadId;

	private void EnqueueOrExecuteWork(IDispatcherWork work, CancellationToken cancellationToken = default)
	{
		if (IsDispatcherThread)
		{
			work.Execute();

			return;
		}

		_queue.Add(work, cancellationToken);
	}

	private void ProcessWork()
	{
		foreach (var work in _queue.GetConsumingEnumerable())
		{
			work.Execute();
		}
	}

	private bool _isDisposed;

	public void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		RequestStop();
		FinishedTask.Wait();
		_queue.Dispose();
		_isDisposed = true;
	}
}
