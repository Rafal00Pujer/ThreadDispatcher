using ThreadDispatcher;

namespace ThreadDispatcherTest;

public class ThreadDispatcherInvokeAsyncActionMethodTest
{
	private readonly Dispatcher _sut = new();

	[Fact]
	public async Task ThrowsWhenFuncIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(() => _sut.InvokeAsync(action: null!));
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
			(() => _sut.InvokeAsync(action: () => throw new Exception("test")));
	}

	[Fact]
	public async Task InvokesAction()
	{
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = false;
		Action action = () => invokedFlag = true;

		await _sut.StartedTask;
		await _sut.InvokeAsync(action: action);

		Assert.True(invokedFlag);
	}

	[Fact]
	public async Task ThrowsActionException()
	{
		var task = Task.Run(() => _sut.Run());
		Action action = () => throw new Exception("test");

		await _sut.StartedTask;

		await Assert.ThrowsAsync<Exception>(() => _sut.InvokeAsync(action: action));
	}

	[Fact]
	public async Task InvokesNestedActions()
	{
		const int invokedCound = 10;
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = 0;
		Action childAction = () => invokedFlag++;

		void ParentAction()
		{
			for (var i = 0; i < invokedCound; i++)
			{
				_sut.InvokeAsync(action: childAction).Wait();
			}
		}

		await _sut.StartedTask;
		await _sut.InvokeAsync(action: ParentAction);

		Assert.Equal(invokedCound, invokedFlag);
	}

	[Fact]
	public async Task InvokesQueuedActionsAfterRequestStop()
	{
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = false;
		var invokedFlag1 = false;
		var resetEvent = new ManualResetEventSlim(false);
		var resetEvent1 = new ManualResetEventSlim(false);

		var action = () =>
		{
			resetEvent.Wait();
			invokedFlag = true;
		};

		var action1 = () =>
		{
			resetEvent1.Wait();
			invokedFlag1 = true;
		};

		await _sut.StartedTask;
		var resetEvent2 = new ManualResetEventSlim(false);
		var resetEvent3 = new ManualResetEventSlim(false);

		var task1 = Task.Run
		(
			async () =>
			{
				resetEvent2.Set();
				await _sut.InvokeAsync(action: action);
			}
		);

		var task2 = Task.Run
		(
			async () =>
			{
				resetEvent3.Set();
				await _sut.InvokeAsync(action: action1);
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

		var action = () =>
		{
			resetEvent.Wait();
		};

		await _sut.StartedTask;
		var resetEvent1 = new ManualResetEventSlim(false);

		var task1 = Task.Run
		(
			async () =>
			{
				resetEvent1.Set();
				await _sut.InvokeAsync(action: action);
			}
		);

		resetEvent1.Wait();

		var tokenSource = new CancellationTokenSource();
		var task2 = _sut.InvokeAsync(action: () => throw new Exception("test"), tokenSource.Token);

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
