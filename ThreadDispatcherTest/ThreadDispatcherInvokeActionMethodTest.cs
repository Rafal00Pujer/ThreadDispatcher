using ThreadDispatcher;

namespace ThreadDispatcherTest;

public class ThreadDispatcherInvokeActionMethodTest
{
	private readonly Dispatcher _sut = new();

	[Fact]
	public void ThrowsWhenActionIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => _sut.Invoke(null!));
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
		Assert.Throws<InvalidOperationException>(() => _sut.Invoke(() => throw new Exception("test")));
	}

	[Fact]
	public async Task InvokesAction()
	{
		var task = Task.Run(() => _sut.Run());
		var invokedFlag = false;
		Action action = () => invokedFlag = true;

		await _sut.StartedTask;
		_sut.Invoke(action);

		Assert.True(invokedFlag);
	}

	[Fact]
	public async Task ThrowsActionException()
	{
		var task = Task.Run(() => _sut.Run());
		Action action = () => throw new Exception("test");

		await _sut.StartedTask;

		Assert.Throws<Exception>(() => _sut.Invoke(action));
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
				_sut.Invoke(childAction);
			}
		}

		await _sut.StartedTask;
		_sut.Invoke(ParentAction);

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
			() =>
			{
				resetEvent2.Set();
				_sut.Invoke(action);
			}
		);

		var task2 = Task.Run
		(
			() =>
			{
				resetEvent3.Set();
				_sut.Invoke(action1);
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
}
