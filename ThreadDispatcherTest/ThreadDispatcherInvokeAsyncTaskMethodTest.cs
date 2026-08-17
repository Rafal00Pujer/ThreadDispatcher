using ThreadDispatcher;

namespace ThreadDispatcherTest;

public class ThreadDispatcherInvokeAsyncTaskMethodTest
{
	private readonly Dispatcher _sut = new();

	[Fact]
	public async Task ThrowsWhenFuncIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.InvokeAsync(func: null!));
	}

	[Fact]
	public async Task ThrowsWhenFinished()
	{
		var task = Task.Run(() => _sut.Run());

		await _sut.StartedTask;
		_sut.RequestStop();
		await _sut.FinishedTask;
		await task;

		Assert.True(task.IsCompleted);
		Assert.True(_sut.StartedTask.IsCompleted);
		Assert.True(_sut.FinishedTask.IsCompleted);

		await Assert.ThrowsAsync<InvalidOperationException>
			(() => _sut.InvokeAsync(func: () => throw new Exception("test")));
	}

	[Fact]
	public async Task InvokesFunc()
	{
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = false;

		var func = () =>
		{
			invokedFlag = true;

			return Task.CompletedTask;
		};

		await _sut.StartedTask;
		await _sut.InvokeAsync(func: func);

		Assert.True(invokedFlag);
	}

	[Fact]
	public async Task ThrowsFuncException()
	{
		var task = Task.Run(() => _sut.Run());
		Func<Task> func = () => throw new Exception("test");

		await _sut.StartedTask;

		await Assert.ThrowsAsync<Exception>(() => _sut.InvokeAsync(func: func));
	}

	[Fact]
	public async Task InvokesNestedFuncs()
	{
		const int invokedCound = 10;
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = 0;

		var childFunc = () =>
		{
			invokedFlag++;

			return Task.CompletedTask;
		};

		async Task ParentFunc()
		{
			for (var i = 0; i < invokedCound; i++)
			{
				await _sut.InvokeAsync(func: childFunc);
			}
		}

		await _sut.StartedTask;
		await _sut.InvokeAsync(func: ParentFunc);

		Assert.Equal(invokedCound, invokedFlag);
	}

	[Fact]
	public async Task InvokesQueuedFuncsAfterRequestStop()
	{
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = false;
		var invokedFlag1 = false;
		var resetEvent = new ManualResetEventSlim(false);
		var resetEvent1 = new ManualResetEventSlim(false);

		var func = () =>
		{
			resetEvent.Wait();
			invokedFlag = true;

			return Task.CompletedTask;
		};

		var func1 = () =>
		{
			resetEvent1.Wait();
			invokedFlag1 = true;

			return Task.CompletedTask;
		};

		await _sut.StartedTask;
		var resetEvent2 = new ManualResetEventSlim(false);
		var resetEvent3 = new ManualResetEventSlim(false);

		var task1 = Task.Run
		(
			async () =>
			{
				resetEvent2.Set();
				await _sut.InvokeAsync(func: func);
			}
		);

		var task2 = Task.Run
		(
			async () =>
			{
				resetEvent3.Set();
				await _sut.InvokeAsync(func: func1);
			}
		);

		resetEvent2.Wait();
		resetEvent3.Wait();
		await Task.Delay(TimeSpan.FromSeconds(1));
		_sut.RequestStop();

		Assert.False(invokedFlag);
		Assert.False(invokedFlag1);
		Assert.False(task1.IsCompleted);
		Assert.False(task2.IsCompleted);
		Assert.False(_sut.FinishedTask.IsCompleted);

		resetEvent.Set();
		resetEvent1.Set();

		await _sut.FinishedTask;
		await task1;
		await task2;
		await task;

		Assert.True(invokedFlag);
		Assert.True(invokedFlag1);
		Assert.True(task.IsCompleted);
		Assert.True(task1.IsCompleted);
		Assert.True(task2.IsCompleted);
		Assert.True(_sut.StartedTask.IsCompleted);
		Assert.True(_sut.FinishedTask.IsCompleted);
	}

	[Fact]
	public async Task ThrowsOperationCanceledWhenCancelledFromToken()
	{
		var task = Task.Run(() => _sut.Run());
		var resetEvent = new ManualResetEventSlim(false);

		var func = () =>
		{
			resetEvent.Wait();

			return Task.CompletedTask;
		};

		await _sut.StartedTask;
		var resetEvent1 = new ManualResetEventSlim(false);

		var task1 = Task.Run
		(
			async () =>
			{
				resetEvent1.Set();
				await _sut.InvokeAsync(func: func);
			}
		);

		resetEvent1.Wait();

		var tokenSource = new CancellationTokenSource();
		var task2 = _sut.InvokeAsync(func: () => throw new Exception("test"), tokenSource.Token);

		Assert.False(task1.IsCompleted);
		Assert.False(task2.IsCompleted);
		Assert.False(_sut.FinishedTask.IsCompleted);

		await tokenSource.CancelAsync();
		resetEvent.Set();
		await task1;

		await Assert.ThrowsAsync<TaskCanceledException>(() => task2);
		Assert.False(task.IsCompleted);
		Assert.True(task1.IsCompleted);
		Assert.True(_sut.StartedTask.IsCompleted);
		Assert.False(_sut.FinishedTask.IsCompleted);
	}

	[Fact]
	public async Task AwaitResumesOnTheSameThread()
	{
		var task = Task.Run(() => _sut.Run());
		var threadIdBefore = -1;
		var threadIdAfter = -1;
		var invokedFlag = false;

		async Task Func()
		{
			threadIdBefore = Environment.CurrentManagedThreadId;
			await Task.Run(() => invokedFlag = true);
			threadIdAfter = Environment.CurrentManagedThreadId;
		}

		await _sut.StartedTask;
		await _sut.InvokeAsync(func: Func);
		
		Assert.Equal(threadIdBefore, threadIdAfter);
		Assert.True(invokedFlag);
		Assert.NotEqual(-1, threadIdBefore);
		Assert.NotEqual(-1, threadIdAfter);
	}
}
