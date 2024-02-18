using EliteSx;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using System.Threading.Tasks.Dataflow;
using ToMqttNet;

namespace EliteSx2Mqtt;

/// <summary>
/// Listens on MQTT Command topics for commands and interacts with the EliteSxClient to make the changes
/// </summary>
internal class MqttToEliteSx : BackgroundService
{
	private readonly ILogger<MqttToEliteSx> _logger;
	private readonly IEliteSxClient _client;
	private readonly PollToMqtt _pollToMqtt;
	private readonly IMqttConnectionService _mqtt;

	private readonly BufferBlock<MqttApplicationMessageReceivedEventArgs> _messages = new();

	public MqttToEliteSx(ILogger<MqttToEliteSx> logger, IEliteSxClient client, PollToMqtt pollToMqtt, IMqttConnectionService mqtt)
	{
		_logger = logger;
		_client = client;
		_pollToMqtt = pollToMqtt;
		_mqtt = mqtt;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			await _pollToMqtt.PopulatedEverything;

			var topics = new List<MqttTopicFilter>();
			foreach (var p in _pollToMqtt.Partitions!)
				topics.Add(new MqttTopicFilter { Topic = p.Config.CommandTopic });
			foreach (var o in _pollToMqtt.Outputs!)
				topics.Add(new MqttTopicFilter { Topic = o.Config.CommandTopic });

			_mqtt.OnApplicationMessageReceived += Mqtt_OnApplicationMessageReceived;

			await _mqtt.SubscribeAsync(topics.ToArray());
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Failed to initialise");
			return;
		}

		while (!stoppingToken.IsCancellationRequested)
		{
			var e = await _messages.ReceiveAsync(stoppingToken);
			var payload = e.ApplicationMessage.ConvertPayloadToString();

			var partition = _pollToMqtt.Partitions!.SingleOrDefault(p => p.Config.CommandTopic == e.ApplicationMessage.Topic);
			var output = _pollToMqtt.Outputs!.SingleOrDefault(p => p.Config.CommandTopic == e.ApplicationMessage.Topic);
			if (partition != null)
			{
				_logger.LogInformation("Updating partition {name} to {desired}", partition.Name, payload);
				//https://www.home-assistant.io/integrations/alarm_control_panel.mqtt/#payload_arm_away
				try
				{
					switch (payload)
					{
						case "ARM_AWAY":
							await _client.ControlPartition(partition.Index, DesiredPartitionState.Away);
							break;
						case "DISARM":
							await _client.ControlPartition(partition.Index, DesiredPartitionState.Off);
							break;
						case "ARM_HOME":
						case "ARM_NIGHT":
						case "ARM_VACATION":
						case "ARM_CUSTOM_BYPASS":
						case "TRIGGER":
						default:
							_logger.LogWarning("Unexpected partition request {name} to {desired}", partition.Name, payload);
							break;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to ControlPartition {index} to {payload}", partition.Index, payload);
				}
			}
			else if (output != null)
			{
				_logger.LogInformation("Updating output {name} to {desired}", output.Name, payload);
				//https://www.home-assistant.io/integrations/switch.mqtt/#payload_off
				try
				{
					switch (payload)
					{
						case "OFF":
							await _client.ControlOutput(output.Index, DesiredOutputState.Off);
							break;
						case "ON":
							await _client.ControlOutput(output.Index, DesiredOutputState.On);
							break;
						default:
							_logger.LogWarning("Unexpected output request {name} to {desired}", output.Name, payload);
							break;
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to ControlOutput {index} to {payload}", output.Index, payload);
				}
			}
			else
			{
				_logger.LogWarning("Received message on unexpected topic {topic}", e.ApplicationMessage.Topic);
			}
		}
	}

	private void Mqtt_OnApplicationMessageReceived(object? sender, MqttApplicationMessageReceivedEventArgs e)
	{
		_messages.Post(e);
	}
}
