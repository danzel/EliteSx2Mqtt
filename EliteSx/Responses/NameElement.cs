using System.Xml.Serialization;

namespace EliteSx.Responses;

public class NameElement
{
    [XmlAttribute("xd")]
    public int Index { get; set; }

    [XmlElement(ElementName = "name")]
    public string Name { get; set; } = null!;
}
