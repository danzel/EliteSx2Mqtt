using EliteSx;
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

		services.AddSingleton<PollToMqtt>();
		services.AddHostedService(s => s.GetRequiredService<PollToMqtt>());
		services.AddHostedService<MqttToEliteSx>();

		services.AddMqttConnection(c => context.Configuration.GetSection("Mqtt").Bind(c));
	})
	.ConfigureLogging(c =>
	{
		c.AddSentry();
	})
	.Build();

await host.RunAsync();
