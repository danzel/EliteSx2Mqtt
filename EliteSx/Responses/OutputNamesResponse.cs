using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "oname")]
public class OutputNamesResponse
{
    [XmlElement("pn")]
    public NameElement[] Names { get; set; } = null!;
}
