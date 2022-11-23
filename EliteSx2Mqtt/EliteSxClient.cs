using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EliteSx2Mqtt;
public class EliteSxClientOptions
{
	public string IpAddress { get; set; } = null!;
	public string UserName { get; set; } = null!;
	public string Password { get; set; } = null!;
}

public class EliteSxClient
{
	private readonly ILogger<EliteSxClient> _logger;
	private readonly HttpClient _httpClient;
	private readonly EliteSxClientOptions _options;

	private readonly SemaphoreSlim _authLock = new(1);
	private string _guid;

	public EliteSxClient(ILogger<EliteSxClient> logger, IOptions<EliteSxClientOptions> options, HttpClient httpClient)
	{
		_logger = logger;
		_httpClient = httpClient;
		_options = options.Value;
	}

	private string GetUrl(string path)
	{
		return $"http://{_options.IpAddress}/{path}?guid={_guid}";
	}

	public async Task LogIn()
	{
		await _authLock.WaitAsync();
		try
		{
			_guid = "GUID-" + Guid.NewGuid().ToString().ToLowerInvariant();

			var content = new FormUrlEncodedContent(
				new Dictionary<string, string>
				{
					{ "user", _options.UserName },
					{ "pass", _options.Password },
					{ "guid", _guid }
				});
			var response = await _httpClient.PostAsync($"http://{_options.IpAddress}/li.php?nc={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds}", content);
			var responseStr = await response.Content.ReadAsStringAsync();

			if (responseStr != "ok")
				throw new Exception($"Login failed. Expected 'ok', received '{responseStr}");
		}
		finally
		{
			_authLock.Release();
		}
	}

	private async Task<T> Get<T>(string path, string textForError) where T : class
	{
		using var response = await _httpClient.GetStreamAsync(GetUrl(path));
		var result = new XmlSerializer(typeof(T)).Deserialize(response) as T;

		if (result == null)
			throw new Exception($"Failed to fetch {textForError}");

		return result;
	}

	public async Task<PrivilegesResponse> GetPrivileges()
	{
		return await Get<PrivilegesResponse>("priv.xml", "privileges");
	}

	public async Task<PartitionNamesResponse> GetPartitionNames()
	{
		return await Get<PartitionNamesResponse>("pnames.xml", "partition names");
	}

	public async Task<OutputNamesResponse> GetOutputNames()
	{
		return await Get<OutputNamesResponse>("onames.xml", "output names");
	}

	public async Task<ZoneNamesResponse> GetZoneNames()
	{
		return await Get<ZoneNamesResponse>("znames.xml", "zone names");
	}
}

[XmlRoot(ElementName = "priv")]
public class PrivilegesResponse
{
	[XmlElement(ElementName = "v")]
	public int Version { get; set; }

	[XmlElement(ElementName = "pt")]
	public PrivilegeElement[] Pt { get; set; }
}

public class PrivilegeElement
{
	[XmlAttribute("xd")]
	public string Xd { get; set; }

	[XmlText]
	public int Contents { get; set; }

	public override string ToString()
	{
		return $"{Xd}: {Contents}";
	}
}

[XmlRoot(ElementName = "pname")]
public class PartitionNamesResponse
{
	[XmlElement("pn")]
	public NameElement[] Names { get; set; }
}

public class NameElement
{
	[XmlAttribute("xd")]
	public int Index { get; set; }

	[XmlElement(ElementName = "name")]
	public string Name { get; set; }
}

[XmlRoot(ElementName = "oname")]
public class OutputNamesResponse
{
	[XmlElement("pn")]
	public NameElement[] Names { get; set; }
}

[XmlRoot(ElementName = "zname")]
public class ZoneNamesResponse
{
	[XmlElement("pn")]
	public NameElement[] Names { get; set; }
}