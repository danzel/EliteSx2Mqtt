namespace EliteSx2Mqtt.Tests;

public class GetTests : BaseHelpers
{
	[Fact]
	public async Task CanDeserializePrivileges()
	{
		var result = await XmlGet("priv.xml", """
   <?xml version="1.0" encoding="ISO-8859-1" ?>
   <?xml-stylesheet type="text/xsl" href="priv.xsl"?>
   <priv><v>307</v><pt xd="mista">1</pt><pt xd="miact">1</pt><pt xd="aiarmaway">1</pt><pt xd="aiarmstay">1</pt><pt xd="aidisaway">1</pt><pt xd="aidisstay">1</pt><pt xd="aiguard">0</pt><pt xd="aijuvie">0</pt><pt xd="miprgown">1</pt><pt xd="miprgoth">1</pt><pt xd="miprgful">1</pt><pt xd="miprgphn">1</pt><pt xd="mirtc">0</pt><pt xd="miprgdtmf">0</pt><pt xd="miprgwirl">1</pt><pt xd="micallback">1</pt><pt xd="miuse">0</pt><pt xd="milock">0</pt></priv>
   """, c => c.GetPrivileges());

		Assert.Equal(307, result.Version);
		Assert.NotNull(result.Pt);
	}

	[Fact]
	public async Task CanDeserializePartitionNames()
	{
		var result = await XmlGet("pnames.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="pname.xsl"?>
<pname><pn xd="1"><name>House</name></pn><pn xd="2"><name>Garage</name></pn><pn xd="3"><name>Sleepout</name></pn></pname>
""", c => c.GetPartitionNames());

		Assert.Equal(3, result.Names.Length);

		Assert.Equal(1, result.Names[0].Index);
		Assert.Equal("House", result.Names[0].Name);

		Assert.Equal(3, result.Names[2].Index);
		Assert.Equal("Sleepout", result.Names[2].Name);
	}

	[Fact]
	public async Task CanDeserializeOutputNames()
	{
		var result = await XmlGet("onames.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="oname.xsl"?>
<oname><pn xd="1"><name>External Siren</name></pn><pn xd="2"><name>Internal Siren</name></pn><pn xd="4"><name>Output 4</name></pn><pn xd="9"><name>External Siren</name></pn><pn xd="10"><name>Internal Siren</name></pn><pn xd="12"><name>Garage Door</name></pn></oname>
""", c => c.GetOutputNames());

		Assert.Equal(6, result.Names.Length);

		Assert.Equal(1, result.Names[0].Index);
		Assert.Equal("External Siren", result.Names[0].Name);

		Assert.Equal(12, result.Names[5].Index);
		Assert.Equal("Garage Door", result.Names[5].Name);
	}

	[Fact]
	public async Task CanDeserializeZoneNames()
	{
		var result = await XmlGet("znames.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="zname.xsl"?>
<zname><pn xd="1"><name>Lounge</name></pn><pn xd="2"><name>Hallway</name></pn><pn xd="3"><name>Laundry</name></pn><pn xd="4"><name>Master Bedroom</name></pn><pn xd="6"><name>Lounge Smoke</name></pn><pn xd="7"><name>Hallway Smoke</name></pn><pn xd="8"><name>Kitchen Heat</name></pn><pn xd="9"><name>Garage</name></pn><pn xd="10"><name>Heat (Garage)</name></pn><pn xd="11"><name>Sleepout</name></pn><pn xd="12"><name>Smoke (Sleepout)</name></pn><pn xd="13"><name>Smoke Tmp (Sleepout)</name></pn></zname>
""", c => c.GetZoneNames());

		Assert.Equal(12, result.Names.Length);

		Assert.Equal(1, result.Names[0].Index);
		Assert.Equal("Lounge", result.Names[0].Name);

		Assert.Equal(13, result.Names[11].Index);
		Assert.Equal("Smoke Tmp (Sleepout)", result.Names[11].Name);
	}
}