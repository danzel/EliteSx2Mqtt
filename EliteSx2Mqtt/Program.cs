using EliteSx2Mqtt;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(services =>
	{
		services.AddHostedService<EliteSxPoller>();
	})
	.Build();

await host.RunAsync();
