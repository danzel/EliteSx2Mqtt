using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "pstat")]
public class PartitionStatusResponse
{
    [XmlElement("pn")]
    public PartitionStatus[] Statuses { get; set; } = null!;
}

public class PartitionStatus
{
    [XmlAttribute("xd")]
    public int Index { get; set; }

    /// <summary>
    /// Flags that control if you can off/arm/stay/reset this zone (depending on the zone state too)
    /// </summary>
    [XmlAttribute("xo")]
    public int Xo { get; set; }

    /// <summary>
    /// Only used when disarming, shown as extra text?
    /// </summary>
    [XmlAttribute("xt")]
    public int Xt { get; set; }

    /// <summary>
    /// Unused
    /// </summary>
    [XmlAttribute("xs")]
    public int Xs { get; set; }

    [XmlText]
    public PartitionState State { get; set; }
}