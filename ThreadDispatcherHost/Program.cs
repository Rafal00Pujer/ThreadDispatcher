using MainThreadDispatcherHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ThreadDispatcher;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<IndependentHostedService>()
	   .AddHostedService<DispatcherDependentHostedService>()
	   .AddHostedService<DispatcherDependentBackgroundService>();

using var dispatcher = new Dispatcher();

builder.Services.AddSingleton(dispatcher);

var app = builder.Build();

var appTask = dispatcher.InvokeAsync
(
	async () =>
	{
		await app.RunAsync();
		dispatcher.RequestStop();
	}
);

dispatcher.Run();

await appTask;
