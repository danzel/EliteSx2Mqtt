using EliteSx;
using ToMqttNet;

namespace EliteSx2Mqtt.Models;
public class Zone
{
	public string Name { get; }
	public int Index { get; }

	/// <summary>
	/// What partition(s) this zone is attached to
	/// </summary>
	public int[] Partitions { get; }

	public MqttBinarySensorDiscoveryConfig Config { get; }

	public ZoneState? State { get; set; }

	public Zone(string name, int index, int[] partitions, MqttBinarySensorDiscoveryConfig config)
	{
		Name = name;
		Index = index;
		Partitions = partitions;
		Config = config;
	}
}
