using Moq;
using ThreadDispatcher;

namespace ThreadDispatcherTest;

public class ThreadDispatcherRunMethodTest
{
	private readonly Dispatcher _sut = new();

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
		Assert.Throws<InvalidOperationException>(() => _sut.Run());
	}

	[Fact]
	public async Task ThrowsWhenAlreadyRunning()
	{
		var task = Task.Run(() => _sut.Run());

		await _sut.StartedTask;

		Assert.False(task.IsCompleted);
		Assert.True(_sut.StartedTask.IsCompleted);
		Assert.False(_sut.FinishedTask.IsCompleted);
		Assert.Throws<InvalidOperationException>(() => _sut.Run());
	}

	[Fact]
	public async Task SetsSynchronizationContext()
	{
		var contextMock = new Mock<SynchronizationContext>();
		SynchronizationContext? contextDuringRun = null;

		var task = Task.Run
		(
			() =>
			{
				SynchronizationContext.SetSynchronizationContext(contextMock.Object);
				_sut.Run();
			}
		);

		await _sut.StartedTask;
		_sut.Invoke(action: () => contextDuringRun = SynchronizationContext.Current);
		_sut.RequestStop();
		await _sut.FinishedTask;
		await task;

		Assert.NotNull(contextDuringRun);
		Assert.NotSame(contextDuringRun, contextMock.Object);
		Assert.IsNotType<SynchronizationContext>(contextDuringRun);
		Assert.IsType<DispatcherSynchronizationContext>(contextDuringRun);
	}

	[Fact]
	public async Task RestoresSynchronizationAfterFinishContext()
	{
		var contextMock = new Mock<SynchronizationContext>();
		SynchronizationContext? contextAfterRun = null;

		var task = Task.Run
		(
			() =>
			{
				SynchronizationContext.SetSynchronizationContext(contextMock.Object);
				_sut.Run();
				contextAfterRun = SynchronizationContext.Current;
			}
		);

		await _sut.StartedTask;
		_sut.RequestStop();
		await _sut.FinishedTask;
		await task;

		Assert.NotNull(contextAfterRun);
		Assert.Same(contextAfterRun, contextMock.Object);
	}
}
