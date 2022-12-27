using System.Xml.Serialization;

namespace EliteSx;

[Serializable]
public enum PartitionState
{
	[XmlEnum("0")]
	Disarmed,
	[XmlEnum("1")]
	AwayArmed,
	[XmlEnum("2")]
	StayArmed,
	[XmlEnum("3")]
	StayExiting,
	[XmlEnum("4")]
	AwayExiting,
	[XmlEnum("5")]
	JuvenileArmed,
	[XmlEnum("6")]
	JuvenileExiting,
	[XmlEnum("7")]
	DisarmedAlarm,
	[XmlEnum("8")]
	DisarmedScheduled,
	[XmlEnum("9")]
	DisArming,
	[XmlEnum("10")]
	UnknownState
}
