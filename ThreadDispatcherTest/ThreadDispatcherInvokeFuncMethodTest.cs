using ThreadDispatcher;

namespace ThreadDispatcherTest;

public class ThreadDispatcherInvokeFuncMethodTest
{
	private readonly Dispatcher _sut = new();

	[Fact]
	public void ThrowsWhenFuncIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => _sut.Invoke<int>(null!));
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
		Assert.Throws<InvalidOperationException>(() => _sut.Invoke(() => 1));
	}

	[Fact]
	public async Task InvokesFunc()
	{
		var task = Task.Run(() => _sut.Run());
		var action = () => true;

		await _sut.StartedTask;
		var result = _sut.Invoke(action);

		Assert.True(result);
	}

	[Fact]
	public async Task ThrowsFuncException()
	{
		var task = Task.Run(() => _sut.Run());
		Func<int> action = () => throw new Exception("test");

		await _sut.StartedTask;

		Assert.Throws<Exception>(() => _sut.Invoke(action));
	}

	[Fact]
	public async Task InvokesNestedFuncs()
	{
		const int expectedSum = 10;
		var task = Task.Run(() => _sut.Run());
		var childFunc = () => 1;

		int ParentFunc()
		{
			var sum = 0;
			
			for (var i = 0; i < expectedSum; i++)
			{
				sum += _sut.Invoke(childFunc);
			}
			
			return sum;
		}

		await _sut.StartedTask;
		var result = _sut.Invoke(ParentFunc);

		Assert.Equal(expectedSum, result);
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
			() =>
			{
				resetEvent2.Set();
				invokedFlag = _sut.Invoke(func);
			}
		);

		var task2 = Task.Run
		(
			() =>
			{
				resetEvent3.Set();
				invokedFlag1 = _sut.Invoke(func1);
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
