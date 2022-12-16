using EliteSx2Mqtt;
using ToMqttNet;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureAppConfiguration(c => c.AddJsonFile("appsettings.Local.json", true))
	.ConfigureServices((context, services) =>
	{
		services.Configure<EliteSxClientOptions>(context.Configuration.GetSection("EliteSx"));

		services.AddHttpClient();

		services.AddSingleton<IEliteSxClient, EliteSxClient>();
		services.AddHostedService(s => (BackgroundService)s.GetRequiredService<IEliteSxClient>());

		services.AddHostedService<EliteSxPoller>();

		services.AddHostedService<Publisher>();

		services.AddMqttConnection(c => context.Configuration.GetSection("Mqtt").Bind(c));
	})
	.Build();

await host.RunAsync();
