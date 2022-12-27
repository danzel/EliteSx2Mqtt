using EliteSx;
using EliteSx2Mqtt.Models;
using MQTTnet;
using System.Text;
using ToMqttNet;

namespace EliteSx2Mqtt;

/// <summary>
/// Performs initial poll of the EliteSx (to get the partitions, zones, outputs with names)
/// Configures these on MQTT
/// Continuously polls the EliteSx to keep the status up to date in MQTT
/// </summary>
public class PollToMqtt : BackgroundService
{
	private readonly ILogger<PollToMqtt> _logger;
	private readonly IEliteSxClient _client;
	private readonly IMqttConnectionService _mqtt;

	private MqttDiscoveryDevice? _device;

	public Partition[]? Partitions { get; private set; }
	public Zone[]? Zones { get; private set; }
	public Output[]? Outputs { get; private set; }

	private readonly TaskCompletionSource _populatedEverything = new();
	public Task PopulatedEverything => _populatedEverything.Task;

	public PollToMqtt(ILogger<PollToMqtt> logger, IEliteSxClient client, IMqttConnectionService mqtt)
	{
		_logger = logger;
		_client = client;
		_mqtt = mqtt;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		try
		{
			await _client.IsInitialized;

			var systemInfo = await _client.GetSystemInformation();
			var system = systemInfo.Info.Single(x => x.Lbl == "System").Val.Split(')', '(').Select(x => x.Trim()).ToArray(); //"Elite-SX (12345678) Ver 10.0.307"
			_device = new MqttDiscoveryDevice
			{
				Name = $"{system[0]} ({system[1]})",
				SoftwareVersion = system[2].Replace("Ver ", ""),
				Identifiers = new List<string> { system[0], system[1] },
			};

			//Load all the bits
			await FetchAndPopulatePartitions();
			await FetchAndPopulateZones();
			await FetchAndPopulateOutputs();
			_populatedEverything.SetResult();

			//Publish them
			await PublishDiscovery();
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Failed to initialise");
			return;
		}


		_logger.LogInformation("Entering polling loop");
		int failuresInARow = 0;
		while (!stoppingToken.IsCancellationRequested)
		{
			//Publish state when it changes
			try
			{
				await UpdatePartitions();
				await UpdateZones();
				await UpdateOutputs();
			}
			catch (Exception ex) when (failuresInARow > 0 && failuresInARow % 10 == 0)
			{
				_logger.LogWarning(ex, "Failed during polling");
				failuresInARow++;
			}
			catch (Exception ex)
			{
				_logger.LogDebug(ex, "Failed during polling");
				failuresInARow++;
			}
			await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
		}
	}

	private async Task FetchAndPopulatePartitions()
	{
		//Fetch partition names and populate Partitions
		_logger.LogInformation("Fetching Partition names");

		var partitionNames = await _client.GetPartitionNames();
		Partitions = new Partition[partitionNames.Names.Length];
		for (int i = 0; i < partitionNames.Names.Length; i++)
		{
			var p = partitionNames.Names[i];
			var config = new MqttAlarmControlPanelDiscoveryConfig
			{
				Name = p.Name,
				Device = _device,
				UniqueId = "partition-" + p.Index,
				Availability = new List<MqttDiscoveryAvailablilty>(),
				CommandTopic = null!,
				StateTopic = null!,
			}.AddDefaultAvailabilityTopic(_mqtt)
			.PopulateStateTopic(_mqtt)
			.PopulateCommandTopic(_mqtt);

			Partitions[i] = new Partition(p.Name, p.Index, config);
		}
	}

	private async Task FetchAndPopulateZones()
	{
		//Fetch zone names and populate Zones
		_logger.LogInformation("Fetching Zone names");

		var zoneNames = await _client.GetZoneNames();
		Zones = new Zone[zoneNames.Names.Length];
		for (var i = 0; i < zoneNames.Names.Length; i++)
		{
			var z = zoneNames.Names[i];
			var config = new MqttBinarySensorDiscoveryConfig
			{
				Name = z.Name,
				Device = _device,
				UniqueId = "zone-" + z.Index,
				DeviceClass = "motion",
				Availability = new List<MqttDiscoveryAvailablilty>(),
				StateTopic = null!,
			}.AddDefaultAvailabilityTopic(_mqtt)
			.PopulateStateTopic(_mqtt);

			Zones[i] = new Zone(z.Name, z.Index, config);
		}
	}

	private async Task FetchAndPopulateOutputs()
	{
		//Fetch output names and populate Outputs
		_logger.LogInformation("Fetching output names");

		var outputNames = await _client.GetOutputNames();
		Outputs = new Output[outputNames.Names.Length];
		for (var i = 0; i < outputNames.Names.Length; i++)
		{
			var o = outputNames.Names[i];
			var config = new MqttSwitchDiscoveryConfig
			{
				Name = o.Name,
				Device = _device,
				UniqueId = "output-" + o.Index,
				Availability = new List<MqttDiscoveryAvailablilty>()
			}.AddDefaultAvailabilityTopic(_mqtt)
			.PopulateStateTopic(_mqtt)
			.PopulateCommandTopic(_mqtt);

			Outputs[i] = new Output(o.Name, o.Index, config);
		}
	}

	private async Task PublishDiscovery()
	{
		_logger.LogInformation("Publishing discovery documents");

		foreach (var partition in Partitions!)
			await _mqtt.PublishDiscoveryDocument(partition.Config);

		foreach (var zone in Zones!)
			await _mqtt.PublishDiscoveryDocument(zone.Config);

		foreach (var output in Outputs!)
			await _mqtt.PublishDiscoveryDocument(output.Config);
	}

	private async Task UpdatePartitions()
	{
		var statuses = await _client.GetPartitionStatus();
		foreach (var s in statuses.Statuses)
		{
			var matching = Partitions!.SingleOrDefault(x => x.Index == s.Index);
			if (matching == null)
			{
				_logger.LogWarning("Found status for an unknown partition {index}", s.Index);
				continue;
			}

			//Publish on state change
			if (matching.State != s.State)
			{
				matching.State = s.State;

				//TODO: What state is alarming!? (Is it just on the zone?)
				string? payload = s.State switch
				{
					PartitionState.Disarmed => "disarmed",
					PartitionState.AwayArmed => "armed_away",
					PartitionState.StayArmed => "armed_home",//Not sure about this one
					PartitionState.StayExiting => "pending",
					PartitionState.AwayExiting => "pending",
					PartitionState.JuvenileArmed => "armed_custom_bypass",//Not sure about this one
					PartitionState.JuvenileExiting => "pending",
					PartitionState.DisarmedAlarm => "disarmed", //TODO: Does this mean we are alarming!?
					PartitionState.DisarmedScheduled => "disarmed",
					PartitionState.DisArming => "disarming",
					PartitionState.UnknownState => "unknown",
					_ => null,
				};

				if (payload == null)
				{
					_logger.LogWarning("Unhandled PartitionState {index} {state}", s.Index, s.State);
				}
				else
				{
					await PublishState(matching.Config, payload);
				}
			}
		}
	}

	private async Task UpdateZones()
	{
		var statuses = await _client.GetZoneStatus();
		foreach (var s in statuses.Statuses)
		{
			var matching = Zones!.SingleOrDefault(x => x.Index == s.Index);
			if (matching == null)
			{
				_logger.LogWarning("Found status for an unknown zone {index}", s.Index);
				continue;
			}

			//Publish on state change
			if (matching.State != s.State)
			{
				matching.State = s.State;

				string? payload = s.State switch
				{
					//Sealed means nothing detected, unsealed means something detected
					//on means 'motion'
					ZoneState.Sealed => "OFF",
					ZoneState.Unsealed => "ON",
					ZoneState.Bypassed => "unavailable",
					ZoneState.EntryDelay => "ON", //Assuming this means something was sensed but we haven't started an alarm yet
					ZoneState.Alarm24hr => "ON", //TODO: Need to check if we can clear alarm from a sensor without resetting the alarm (disarm and arm)
					ZoneState.Alarm => "ON",
					ZoneState.SealedLowBattery => "OFF",
					_ => null
				};

				if (payload == null)
				{
					_logger.LogWarning("Unhandled ZoneState {index} {state}", s.Index, s.State);
				}
				else
				{
					await PublishState(matching.Config, payload);
				}
			}
		}
	}

	private async Task UpdateOutputs()
	{
		var statuses = await _client.GetOutputStatus();
		foreach (var s in statuses.Statuses)
		{
			var matching = Outputs!.SingleOrDefault(x => x.Index == s.Index);
			if (matching == null)
			{
				_logger.LogWarning("Found status for an unknown output {index}", s.Index);
				continue;
			}

			//Publish on state change
			if (matching.State != s.State)
			{
				matching.State = s.State;

				string? payload = s.State switch
				{
					OutputState.Idle => "OFF",
					OutputState.Active => "ON",
					OutputState.IdleFault => "OFF",
					OutputState.ActiveFault => "ON",
					OutputState.IdleDelay => "OFF",
					OutputState.ActiveDelay => "ON",
					OutputState.IdleFaultDelay => "OFF",
					OutputState.ActiveFaultDelay => "ON",
					OutputState.UnknownState => "unknown",
					_ => null
				};

				if (payload == null)
				{
					_logger.LogWarning("Unhandled OutputState {index} {state}", s.Index, s.State);
				}
				else
				{
					await PublishState(matching.Config, payload);
				}
			}
		}
	}

	private async Task PublishState<T>(T config, string payload) where T : MqttDiscoveryConfig, IMqttDiscoveryDeviceWithStateGetter
	{
		await _mqtt.PublishAsync(new MqttApplicationMessage
		{
			Payload = Encoding.UTF8.GetBytes(payload),
			Topic = config.StateTopic,
			Retain = true
		});
	}
}
