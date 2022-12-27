using System.Xml.Serialization;

namespace EliteSx.Responses;

[XmlRoot("poll")]
public class PollResponse
{
    [XmlElement("t")]
    public int TimeSeconds { get; set; }
}
