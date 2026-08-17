using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThreadDispatcher;

namespace MainThreadDispatcherHost;

public class DispatcherDependentHostedService
	(Dispatcher dispatcher, ILogger<DispatcherDependentHostedService> logger) : IHostedLifecycleService
{
	public Task StartingAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent service starting on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent service starting on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}
	
	public Task StartedAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent service started on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}

	public Task StoppingAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent service stoping on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent service stop on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}
	
	public Task StoppedAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent service stopped on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}
}
