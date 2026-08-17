using ThreadDispatcher;

namespace ThreadDispatcherTest;

public class ThreadDispatcherInvokeAsyncFuncMethodTest
{
	private readonly Dispatcher _sut = new();

	[Fact]
	public async Task ThrowsWhenFuncIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.InvokeAsync(func: (Func<int>)null!));
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
			(() => _sut.InvokeAsync(func: (Func<int>)(() => throw new Exception("test"))));
	}

	[Fact]
	public async Task InvokesFunc()
	{
		var task = Task.Run(() => _sut.Run());
		var func = () => true;

		await _sut.StartedTask;
		var result = await _sut.InvokeAsync(func: func);

		Assert.True(result);
	}

	[Fact]
	public async Task ThrowsFuncException()
	{
		var task = Task.Run(() => _sut.Run());
		Func<bool> func = () => throw new Exception("test");

		await _sut.StartedTask;

		await Assert.ThrowsAsync<Exception>(() => _sut.InvokeAsync(func: func));
	}

	[Fact]
	public async Task InvokesNestedFuncs()
	{
		const int invokedCound = 10;
		var task = Task.Run(() => _sut.Run());
		var childFunc = () => 1;

		int ParentFunc()
		{
			var sum = 0;

			for (var i = 0; i < invokedCound; i++)
			{
				sum += _sut.InvokeAsync(func: childFunc).GetAwaiter().GetResult();
			}

			return sum;
		}

		await _sut.StartedTask;
		var result = await _sut.InvokeAsync(func: ParentFunc);

		Assert.Equal(invokedCound, result);
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
			return true;
		};

		var func1 = () =>
		{
			resetEvent1.Wait();
			return true;
		};

		await _sut.StartedTask;
		var resetEvent2 = new ManualResetEventSlim(false);
		var resetEvent3 = new ManualResetEventSlim(false);

		var task1 = Task.Run
		(
			async () =>
			{
				resetEvent2.Set();
				invokedFlag = await _sut.InvokeAsync(func: func);
			}
		);

		var task2 = Task.Run
		(
			async () =>
			{
				resetEvent3.Set();
				invokedFlag1 = await _sut.InvokeAsync(func: func1);
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

			return true;
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
		var task2 = _sut.InvokeAsync(func: (Func<int>)(() => throw new Exception("test")), tokenSource.Token);

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
}
