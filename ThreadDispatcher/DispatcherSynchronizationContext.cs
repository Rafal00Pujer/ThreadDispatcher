namespace ThreadDispatcher;

internal sealed class DispatcherSynchronizationContext(Dispatcher dispatcher) : SynchronizationContext
{
	public override void Post(SendOrPostCallback d, object? state) => _ = dispatcher.InvokeAsync(() => d(state));
	public override void Send(SendOrPostCallback d, object? state) => dispatcher.Invoke(() => d(state));
	public override SynchronizationContext CreateCopy() => new DispatcherSynchronizationContext(dispatcher);
}
