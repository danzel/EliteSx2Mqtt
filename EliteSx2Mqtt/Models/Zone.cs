using EliteSx;
using ToMqttNet;

namespace EliteSx2Mqtt.Models;
public class Zone
{
	public string Name { get; }
	public int Index { get; }
	public MqttBinarySensorDiscoveryConfig Config { get; }

	public ZoneState? State { get; set; }

	public Zone(string name, int index, MqttBinarySensorDiscoveryConfig config)
	{
		Name = name;
		Index = index;
		Config = config;
	}
}
