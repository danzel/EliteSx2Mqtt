namespace EliteSx.Tests;

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

	[Fact]
	public async Task CanDeserializePartitionStatus()
	{
		var result = await XmlGet("pstats.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="pstat.xsl"?>
<pstat><pn xd="1" xo="143" xt="0" xs="0">0</pn><pn xd="2" xo="143" xt="0" xs="0">0</pn><pn xd="3" xo="143" xt="0" xs="1">1</pn></pstat>
""", c => c.GetPartitionStatus());

		Assert.Equal(3, result.Statuses.Length);

		Assert.Equal(1, result.Statuses[0].Index);
		Assert.Equal(PartitionState.Disarmed, result.Statuses[0].State);

		Assert.Equal(3, result.Statuses[2].Index);
		Assert.Equal(PartitionState.AwayArmed, result.Statuses[2].State);
	}

	[Fact]
	public async Task CanDeserializeOutputStatus()
	{
		var result = await XmlGet("ostats.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="ostat.xsl"?>
<ostat><os xd="1" xo="0">1</os><os xd="2" xo="0">0</os><os xd="4" xo="3">0</os><os xd="9" xo="0">0</os><os xd="10" xo="0">0</os><os xd="12" xo="3">0</os></ostat>
""", c => c.GetOutputStatus());

		Assert.Equal(6, result.Statuses.Length);

		Assert.Equal(1, result.Statuses[0].Index);
		Assert.Equal(OutputState.Active, result.Statuses[0].State);

		Assert.Equal(12, result.Statuses[5].Index);
		Assert.Equal(OutputState.Idle, result.Statuses[5].State);
	}

	[Fact]
	public async Task CanDeserializeZoneStatus()
	{
		var result = await XmlGet("zstats.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="zstat.xsl"?>
<zstat><zs xd="1" xo="0">1</zs><zs xd="2" xo="0">0</zs><zs xd="3" xo="0">0</zs><zs xd="4" xo="0">0</zs><zs xd="6" xo="0">0</zs><zs xd="7" xo="0">0</zs><zs xd="8" xo="0">0</zs><zs xd="9" xo="0">0</zs><zs xd="10" xo="0">0</zs><zs xd="11" xo="2">0</zs><zs xd="12" xo="2">0</zs><zs xd="13" xo="2">0</zs></zstat>
""", c => c.GetZoneStatus());

		Assert.Equal(12, result.Statuses.Length);

		Assert.Equal(1, result.Statuses[0].Index);
		Assert.Equal(ZoneState.Unsealed, result.Statuses[0].State);

		Assert.Equal(13, result.Statuses[11].Index);
		Assert.Equal(ZoneState.Sealed, result.Statuses[11].State);
	}

	[Fact]
	public async Task CanDeserializeSystemInformation()
	{
		var result = await XmlGet("sysinfo.xml", """
<?xml version="1.0" encoding="ISO-8859-1" ?>
<?xml-stylesheet type="text/xsl" href="sysinfo.xsl"?>
<sysinfo><info><lbl>System</lbl><val>Elite-SX (12345678) Ver 10.0.307</val></info><info><lbl>Mains</lbl><val>OK</val></info><info><lbl>Battery</lbl><val>OK</val></info><info><lbl>DHCP</lbl><val>Enabled</val></info><info><lbl>IP Address</lbl><val>1.1.1.4 (DHCP lease)</val></info><info><lbl>Default Gateway</lbl><val>1.1.1.2 (DHCP lease)</val></info><info><lbl>DNS1</lbl><val>1.1.1.1 (DHCP lease)</val></info><info><lbl>DNS2</lbl><val>1.1.1.5 (DHCP lease)</val></info><info><lbl>NTP1</lbl><val>host (1.1.1.7) OK</val></info><info><lbl>NTP2</lbl><val>host2 (1.1.1.8) OK</val></info><info><lbl>IP Reporting</lbl><val>Ch1 1.1.1.9 (1.1.1.9)</val></info><info><lbl>Zone Expander</lbl><val>Id.1 Ver 0.1.30 (12345678) Online</val></info><info><lbl>Output Expander</lbl><val>Id.3 Ver 0.1.15 (12345678) Online</val></info><info><lbl>File Status</lbl><val>1.256.496.2</val></info><info><lbl>File Status</lbl><val>2.400.696.5</val></info><info><lbl>File Status</lbl><val>3.64.496.10</val></info><info><lbl>File Status</lbl><val>4.224.496.20</val></info><info><lbl>File Status</lbl><val>5.576.1000.1</val></info></sysinfo>
""", c => c.GetSystemInformation());

		Assert.Equal(18, result.Info.Length);

		Assert.Equal("System", result.Info[0].Lbl);
		Assert.Equal("Elite-SX (12345678) Ver 10.0.307", result.Info[0].Val);
	}

	[Fact]
	public async Task CanDeserializeConfig()
	{
		var result = await XmlGet("config.cfx?n=1", """
<ELITE-SX><zones><zone id="1"><name>Lounge</name><partns><pn id="1" /></partns></zone></zones></ELITE-SX>
""", c => c.GetConfig(1));

		var z = Assert.Single(result.Zones);
		Assert.Equal("Lounge", z.Name);
		Assert.Equal(1, z.Id);

		var pn = Assert.Single(z.PartitionNumbers);
		Assert.Equal(1, pn.Id);
	}
}