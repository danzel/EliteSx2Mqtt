using System.Xml.Serialization;

namespace EliteSx;

[Serializable]
public enum OutputState
{
	[XmlEnum("0")]
	Idle,
	[XmlEnum("1")]
	Active,
	[XmlEnum("2")]
	IdleFault,
	[XmlEnum("3")]
	ActiveFault,
	[XmlEnum("4")]
	IdleDelay,
	[XmlEnum("5")]
	ActiveDelay,
	[XmlEnum("6")]
	IdleFaultDelay,
	[XmlEnum("7")]
	ActiveFaultDelay,
	[XmlEnum("8")]
	UnknownState
}
