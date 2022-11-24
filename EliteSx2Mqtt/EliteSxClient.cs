using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Xml.Serialization;

namespace EliteSx2Mqtt;
public class EliteSxClientOptions
{
	public string IpAddress { get; set; } = null!;
	public string Username { get; set; } = null!;
	public string Password { get; set; } = null!;

	/// <summary>
	/// When the auth timeout is less than this, refresh auth
	/// </summary>
	public TimeSpan AuthRefreshTime { get; set; } = TimeSpan.FromSeconds(65);
}

public class EliteSxClient
{
	private readonly ILogger<EliteSxClient> _logger;
	private readonly HttpClient _httpClient;
	private readonly EliteSxClientOptions _options;

	private readonly SemaphoreSlim _authLock = new(1);
	private string? _guid;

	private int? _authExpireTimeSeconds;
	private Stopwatch? _authExpireAge;

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

	public async Task EnsureAuthenticated()
	{
		//TODO: Be more reliable in here

		await _authLock.WaitAsync();
		try
		{
			if (_guid == null)
			{
				_logger.LogInformation("Attempting authentication");
				await LogIn();
			}
			else if (_authExpireTimeSeconds == null || _authExpireAge == null)
			{
				//TODO: poll.xml shows time until expire
				//To refresh, GET refr.xml which returns the same response and refreshes to 1200 seconds
				await PollAuth("poll.xml");
			}
			else
			{
				if (TimeSpan.FromSeconds(_authExpireTimeSeconds.Value) - _authExpireAge.Elapsed <= _options.AuthRefreshTime)
				{
					//Looks like this URL isn't valid until we near timeout
					_logger.LogInformation("Refreshing authentication");
					await PollAuth("refr.xml");
				}
			}
		}
		catch (Exception ex) when (_guid is null)
		{
			_logger.LogError(ex, "Failed to Authenticate");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to poll/reauthenticate, will try Login again");

			_guid = null;
			_authExpireAge = null;
			_authExpireTimeSeconds = null;

			try
			{
				await LogIn();
			}
			catch (Exception ex2)
			{
				_logger.LogError(ex2, "Failed to Authenticate again");
				throw;
			}
		}
		finally
		{
			_authLock.Release();
		}
	}

	private async Task LogIn()
	{
		_guid = "GUID-" + Guid.NewGuid().ToString().ToLowerInvariant();

		var content = new FormUrlEncodedContent(
			new Dictionary<string, string>
			{
					{ "user", _options.Username },
					{ "pass", _options.Password },
					{ "guid", _guid }
			});
		var response = await _httpClient.PostAsync($"http://{_options.IpAddress}/li.php?nc={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds}", content);
		var responseStr = await response.Content.ReadAsStringAsync();

		if (responseStr != "ok")
			throw new Exception($"Login failed. Expected 'ok', received '{responseStr}");
	}

	private async Task PollAuth(string path)
	{
		var response = await Get<PollResponse>(path, "poll auth");

		_authExpireTimeSeconds = response.TimeSeconds;
		_authExpireAge = Stopwatch.StartNew();
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

	public async Task<PartitionStatusResponse> GetPartitionStatus()
	{
		return await Get<PartitionStatusResponse>("pstats.xml", "partition status");
	}

	public async Task<OutputStatusResponse> GetOutputStatus()
	{
		return await Get<OutputStatusResponse>("ostats.xml", "output status");
	}

	public async Task<ZoneStatusResponse> GetZoneStatus()
	{
		return await Get<ZoneStatusResponse>("zstats.xml", "zone status");
	}

	private async Task Post(string path, string content)
	{
		path = $"http://{_options.IpAddress}/{path}";

		//Cannot use FormUrlEncodedContent here as the fields need to be separated by ?, not by &
		var stringContent = new StringContent(content, MediaTypeHeaderValue.Parse("application/x-www-form-urlencoded; charset=UTF-8"));

		using var response = await _httpClient.PostAsync(path, stringContent);
		response.EnsureSuccessStatusCode();
		var result = await response.Content.ReadAsStringAsync();

		if (result != "Success")
			throw new Exception($"Expected 'Success' but received '{result}'");
	}

	public async Task ControlOutput(int outputIndex, DesiredOutputState desired)
	{
		await Post("ot.php",
			$"op{desired.ToString().ToLowerInvariant()}=op{outputIndex}" +
			$"?GUID={_guid}");
	}

	public async Task ControlPartition(int partitionIndex, DesiredPartitionState desired)
	{
		await Post("pn.php",
			$"pn{desired.ToString().ToLowerInvariant()}=pn{partitionIndex}" +
			$"?GUID={_guid}");
	}
}

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

[XmlRoot(ElementName = "pname")]
public class PartitionNamesResponse
{
	[XmlElement("pn")]
	public NameElement[] Names { get; set; } = null!;
}

public class NameElement
{
	[XmlAttribute("xd")]
	public int Index { get; set; }

	[XmlElement(ElementName = "name")]
	public string Name { get; set; } = null!;
}

[XmlRoot(ElementName = "oname")]
public class OutputNamesResponse
{
	[XmlElement("pn")]
	public NameElement[] Names { get; set; } = null!;
}

[XmlRoot(ElementName = "zname")]
public class ZoneNamesResponse
{
	[XmlElement("pn")]
	public NameElement[] Names { get; set; } = null!;
}

[XmlRoot(ElementName = "pstat")]
public class PartitionStatusResponse
{
	[XmlElement("pn")]
	public PartitionStatus[] Statuses { get; set; } = null!;
}

public class PartitionStatus
{
	[XmlAttribute("xd")]
	public int Index { get; set; }

	/// <summary>
	/// Flags that control if you can off/arm/stay/reset this zone (depending on the zone state too)
	/// </summary>
	[XmlAttribute("xo")]
	public int Xo { get; set; }

	/// <summary>
	/// Only used when disarming, shown as extra text?
	/// </summary>
	[XmlAttribute("xt")]
	public int Xt { get; set; }

	/// <summary>
	/// Unused
	/// </summary>
	[XmlAttribute("xs")]
	public int Xs { get; set; }

	[XmlText]
	public PartitionState State { get; set; }
}

[Serializable]
public enum PartitionState
{
	[XmlEnum("0")]
	Disarmed,
	[XmlEnum("1")]
	AwayArmed,
	[XmlEnum("2")]
	StayArmed,
	[XmlEnum("3")]
	StayExiting,
	[XmlEnum("4")]
	AwayExiting,
	[XmlEnum("5")]
	JuvenileArmed,
	[XmlEnum("6")]
	JuvenileExiting,
	[XmlEnum("7")]
	DisarmedAlarm,
	[XmlEnum("8")]
	DisarmedScheduled,
	[XmlEnum("9")]
	DisArming,
	[XmlEnum("10")]
	UnknownState
}

[XmlRoot(ElementName = "ostat")]
public class OutputStatusResponse
{
	[XmlElement("os")]
	public OutputStatus[] Statuses { get; set; } = null!;
}

public class OutputStatus
{
	[XmlAttribute("xd")]
	public int Index { get; set; }

	/// <summary>
	/// Flags that control if you can on/off this output
	/// </summary>
	[XmlAttribute("xo")]
	public int Xo { get; set; }

	[XmlText]
	public OutputState State { get; set; }
}

[Serializable]
public enum OutputState
{
	[XmlEnum("0")]
	Idle,
	[XmlEnum("1")]
	Active,
	[XmlEnum("2")]
	IdleFault,
	[XmlEnum("3")]
	ActiveFault,
	[XmlEnum("4")]
	IdleDelay,
	[XmlEnum("5")]
	ActiveDelay,
	[XmlEnum("6")]
	IdleFaultDelay,
	[XmlEnum("7")]
	ActiveFaultDelay,
	[XmlEnum("8")]
	UnknownState
}

[XmlRoot(ElementName = "zstat")]
public class ZoneStatusResponse
{
	[XmlElement("zs")]
	public ZoneStatus[] Statuses { get; set; } = null!;
}

public class ZoneStatus
{
	[XmlAttribute("xd")]
	public int Index { get; set; }

	/// <summary>
	/// If 2 this zone is locked (partition it is in is armed?)
	/// </summary>
	[XmlAttribute("xo")]
	public int Xo { get; set; }

	[XmlText]
	public ZoneState State { get; set; }
}

[Serializable]
public enum ZoneState
{
	[XmlEnum("0")]
	Sealed,
	[XmlEnum("1")]
	Unsealed,
	[XmlEnum("2")]
	Bypassed,
	[XmlEnum("3")]
	EntryDelay,
	[XmlEnum("4")]
	Alarm24hr,
	[XmlEnum("5")]
	Alarm,
	[XmlEnum("6")]
	SealedLowBattery
}

[XmlRoot("poll")]
public class PollResponse
{
	[XmlElement("t")]
	public int TimeSeconds { get; set; }
}

public enum DesiredOutputState
{
	Off,
	On
}

public enum DesiredPartitionState
{
	/// <summary>
	/// "Armed"
	/// </summary>
	Away,

	/// <summary>
	/// "Disarmed"
	/// </summary>
	Off
}