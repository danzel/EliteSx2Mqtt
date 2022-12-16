using ToMqttNet;

namespace EliteSx2Mqtt.Models;
public class Output
{
	public string Name { get; }
	public int Index { get; }
	public MqttSwitchDiscoveryConfig Config { get; }

	public OutputState? State { get; set; }

	public Output(string name, int index, MqttSwitchDiscoveryConfig config)
	{
		Name = name;
		Index = index;
		Config = config;
	}
}
