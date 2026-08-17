namespace ThreadDispatcher;

public sealed class SynchronizationContextInstaller : IDisposable
{
	private readonly SynchronizationContext? _previousContext;
	private SynchronizationContext? _installedContext;

	public bool IsInstalled => _installedContext is not null;

	private SynchronizationContextInstaller
		(SynchronizationContext? previousContext, SynchronizationContext? installedContext)
	{
		_previousContext = previousContext;
		_installedContext = installedContext;
	}

	public bool Uninstall()
	{
		if (!IsInstalled)
		{
			return false;
		}

		SynchronizationContext.SetSynchronizationContext(_previousContext);
		_installedContext = null;

		return true;
	}

	internal static SynchronizationContextInstaller Install(Dispatcher dispatcher)
	{
		var current = SynchronizationContext.Current;
		var installed = new DispatcherSynchronizationContext(dispatcher);

		SynchronizationContext.SetSynchronizationContext(installed);

		return new SynchronizationContextInstaller(current, installed);
	}

	public void Dispose() => _ = Uninstall();
}
