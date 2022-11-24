using EliteSx2Mqtt;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.Local.json", true))
	.ConfigureServices((context, services) =>
	{
		services.Configure<EliteSxClientOptions>(context.Configuration.GetSection("EliteSx"));
		services.AddHttpClient();
		services.AddSingleton<EliteSxClient>();

		services.AddHostedService<EliteSxPoller>();
	})
	.Build();

await host.RunAsync();
