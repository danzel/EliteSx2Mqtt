using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "ostat")]
public class OutputStatusResponse
{
    [XmlElement("os")]
    public OutputStatus[] Statuses { get; set; } = null!;
}

public class OutputStatus
{
    [XmlAttribute("xd")]
    public int Index { get; set; }

    /// <summary>
    /// Flags that control if you can on/off this output
    /// </summary>
    [XmlAttribute("xo")]
    public int Xo { get; set; }

    [XmlText]
    public OutputState State { get; set; }
}