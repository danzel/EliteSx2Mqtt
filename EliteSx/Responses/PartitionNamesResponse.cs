using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "pname")]
public class PartitionNamesResponse
{
    [XmlElement("pn")]
    public NameElement[] Names { get; set; } = null!;
}
