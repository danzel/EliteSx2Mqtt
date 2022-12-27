using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "zname")]
public class ZoneNamesResponse
{
    [XmlElement("pn")]
    public NameElement[] Names { get; set; } = null!;
}
