using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot(ElementName = "priv")]
public class PrivilegesResponse
{
    [XmlElement(ElementName = "v")]
    public int Version { get; set; }

    [XmlElement(ElementName = "pt")]
    public PrivilegeElement[] Pt { get; set; } = null!;
}

public class PrivilegeElement
{
    [XmlAttribute("xd")]
    public string Xd { get; set; } = null!;

    [XmlText]
    public int Contents { get; set; }

    public override string ToString()
    {
        return $"{Xd}: {Contents}";
    }
}
