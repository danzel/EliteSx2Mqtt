using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "zstat")]
public class ZoneStatusResponse
{
    [XmlElement("zs")]
    public ZoneStatus[] Statuses { get; set; } = null!;
}

public class ZoneStatus
{
    [XmlAttribute("xd")]
    public int Index { get; set; }

    /// <summary>
    /// If 2 this zone is locked (partition it is in is armed?)
    /// </summary>
    [XmlAttribute("xo")]
    public int Xo { get; set; }

    [XmlText]
    public ZoneState State { get; set; }
}