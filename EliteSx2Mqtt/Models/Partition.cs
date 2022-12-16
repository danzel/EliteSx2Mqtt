using ToMqttNet;

namespace EliteSx2Mqtt.Models;
public class Partition
{
	public string Name { get; }
	public int Index { get; }
	public MqttAlarmControlPanelDiscoveryConfig Config { get; }

	public PartitionState? State { get; set; }

	public Partition(string name, int index, MqttAlarmControlPanelDiscoveryConfig config)
	{
		Name = name;
		Index = index;
		Config = config;
	}
}
