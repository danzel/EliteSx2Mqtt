using System.Xml.Serialization;

namespace EliteSx;

[Serializable]
public enum ZoneState
{
	[XmlEnum("0")]
	Sealed,
	[XmlEnum("1")]
	Unsealed,
	[XmlEnum("2")]
	Bypassed,
	[XmlEnum("3")]
	EntryDelay,
	[XmlEnum("4")]
	Alarm24hr,
	[XmlEnum("5")]
	Alarm,
	[XmlEnum("6")]
	SealedLowBattery
}
