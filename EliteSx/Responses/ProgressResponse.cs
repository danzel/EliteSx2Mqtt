using System.Xml.Serialization;

namespace EliteSx.Responses;
[XmlRoot("p")]
public class ProgressResponse
{
	[XmlElement("v")]
	public int V { get; set; }

	/// <summary>
	/// How big the file is so far
	/// </summary>
	[XmlElement("d")]
	public int D { get; set; }

	[XmlElement("f")]
	public int F { get; set; }

}
