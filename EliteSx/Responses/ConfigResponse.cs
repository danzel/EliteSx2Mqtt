using System.Xml.Serialization;

namespace EliteSx.Responses;

/// <summary>
/// The config (as fetched on Settings - Config files).
/// We only deserialise the minimum of things we need, everything you can configure is available, add things as needed
/// </summary>
[XmlRoot(ElementName = "ELITE-SX")]
public class ConfigResponse
{
	[XmlArray("zones"), XmlArrayItem("zone")]
	public ConfigZone[] Zones { get; set; } = null!;
}

public class ConfigZone
{
	/// <summary>
	/// The unique id of this zone (Same as <see cref="NameElement.Index"/> and <see cref="ZoneStatus.Index"/>)
	/// </summary>
	[XmlAttribute("id")]
	public int Id { get; set; }

	/// <summary>
	/// The name of this zone
	/// </summary>
	[XmlElement("name"), XmlText]
	public string Name { get; set; } = null!;

	/// <summary>
	/// What partitions (areas) this zone is linked to
	/// </summary>
	[XmlArray("partns"), XmlArrayItem("pn")]
	public ConfigZonePartition[] PartitionNumbers { get; set; } = null!;
}

public class ConfigZonePartition
{
	[XmlAttribute("id")]
	public int Id { get; set; }
}
