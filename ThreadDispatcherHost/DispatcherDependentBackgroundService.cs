using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThreadDispatcher;

namespace MainThreadDispatcherHost;

public class DispatcherDependentBackgroundService
	(Dispatcher dispatcher, ILogger<DispatcherDependentBackgroundService> logger)
	: BackgroundService, IHostedLifecycleService
{
	public Task StartingAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent background service starting on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}

	public Task StartedAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent background service starting on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var contextInstaller = dispatcher.InstallSynchronizationContext();

		try
		{
			logger.LogInformation
			(
				"Dependent background service execute before yield on thread: {threadId}",
				Environment.CurrentManagedThreadId
			);

			await Task.Yield();

			logger.LogInformation
			(
				"Dependent background service execute after yield on thread: {threadId}",
				Environment.CurrentManagedThreadId
			);

			while (!stoppingToken.IsCancellationRequested)
			{
				logger.LogInformation
				(
					"Dependent background service execute on thread: {threadId}, at: {datetime}",
					Environment.CurrentManagedThreadId, DateTime.UtcNow
				);

				await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
			}
		}
		finally
		{
			contextInstaller.Uninstall();
		}
	}

	public Task StoppingAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent background service stopping on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}

	public Task StoppedAsync(CancellationToken cancellationToken)
	{
		return dispatcher.InvokeAsync
		(
			() => logger.LogInformation
				("Dependent background service stopped on thread: {threadId}", Environment.CurrentManagedThreadId),
			cancellationToken
		);
	}
}
