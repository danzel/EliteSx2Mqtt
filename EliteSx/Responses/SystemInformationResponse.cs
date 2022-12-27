using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "sysinfo")]
public class SystemInformationResponse
{
    [XmlElement(ElementName = "info")]
    public SystemInformationElement[] Info = null!;
}

public class SystemInformationElement
{
    [XmlElement("lbl")]
    public string Lbl { get; set; } = null!;

    [XmlElement("val")]
    public string Val { get; set; } = null!;
}
