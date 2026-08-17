using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MainThreadDispatcherHost;

public class IndependentHostedService(ILogger<IndependentHostedService> logger) : IHostedLifecycleService
{
	public async Task StartingAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(100, cancellationToken);

		logger.LogInformation
			("Independent service starting on thread: {threadId}", Environment.CurrentManagedThreadId);
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(100, cancellationToken);

		logger.LogInformation("Independent service start on thread: {threadId}", Environment.CurrentManagedThreadId);
	}

	public async Task StartedAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(100, cancellationToken);

		logger.LogInformation("Independent service started on thread: {threadId}", Environment.CurrentManagedThreadId);
	}

	public async Task StoppingAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(100, cancellationToken);

		logger.LogInformation
			("Independent service stopping on thread: {threadId}", Environment.CurrentManagedThreadId);
	}

	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(100, cancellationToken);

		logger.LogInformation("Independent service stop on thread: {threadId}", Environment.CurrentManagedThreadId);
	}

	public async Task StoppedAsync(CancellationToken cancellationToken)
	{
		await Task.Delay(100, cancellationToken);

		logger.LogInformation("Independent service stopped on thread: {threadId}", Environment.CurrentManagedThreadId);
	}
}
