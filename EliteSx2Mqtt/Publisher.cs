using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToMqttNet;

namespace EliteSx2Mqtt;
internal class Publisher : BackgroundService
{
	private readonly ILogger<Publisher> _logger;
	private readonly IMqttConnectionService _mqtt;

	public Publisher(ILogger<Publisher> logger, IMqttConnectionService mqtt)
	{
		_logger = logger;
		_mqtt = mqtt;
	}

	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_mqtt.OnApplicationMessageReceived += Mqtt_OnApplicationMessageReceived;
		_mqtt.OnConnect += Mqtt_OnConnect;
		_mqtt.OnDisconnect += Mqtt_OnDisconnect;

		return Task.CompletedTask;
	}

	private void Mqtt_OnDisconnect(object? sender, EventArgs e)
	{
		_logger.LogInformation("Disconnected");
	}

	private void Mqtt_OnConnect(object? sender, EventArgs e)
	{
		_logger.LogInformation("Connected");

		var device = new MqttDiscoveryDevice
		{
			Identifiers = new List<string> { "MyDevice" },
			Name = "MyDevice",
			Model = "MyModel",
			Manufacturer = "MyMan"
		};

		//Publish all of our components
		var config = new MqttBinarySensorDiscoveryConfig
		{
			Name = "Test Sensor",
			DeviceClass = "motion",
			UniqueId = "motion1",
			Device = device,
		}.PopulateStateTopic(_mqtt);

		_mqtt.PublishDiscoveryDocument(config).Wait();

		Task.Run(async () =>
		{
			await Task.Delay(2000);
			while (true)
			{
				await _mqtt.PublishAsync(new MQTTnet.MqttApplicationMessage
				{
					Payload = Encoding.UTF8.GetBytes("ON"),
					Topic = config.StateTopic
				});
				await Task.Delay(5000);

				await _mqtt.PublishAsync(new MQTTnet.MqttApplicationMessage
				{
					Payload = Encoding.UTF8.GetBytes("OFF"),
					Topic = config.StateTopic
				});
				await Task.Delay(5000);
			}
		});
	}

	private void Mqtt_OnApplicationMessageReceived(object? sender, MQTTnet.MqttApplicationMessageReceivedEventArgs e)
	{
		_logger.LogInformation("Message {msg}", e);
	}
}
