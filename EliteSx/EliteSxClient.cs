using EliteSx.Responses;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Xml.Serialization;

namespace EliteSx;
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

public interface IEliteSxClient
{
	Task IsInitialized { get; }

	Task ControlOutput(int outputIndex, DesiredOutputState desired);
	Task ControlPartition(int partitionIndex, DesiredPartitionState desired);

	Task EnsureAuthenticated();

	Task<SystemInformationResponse> GetSystemInformation();
	Task<PrivilegesResponse> GetPrivileges();

	Task<PartitionNamesResponse> GetPartitionNames();
	Task<ZoneNamesResponse> GetZoneNames();
	Task<OutputNamesResponse> GetOutputNames();

	Task<PartitionStatusResponse> GetPartitionStatus();
	Task<ZoneStatusResponse> GetZoneStatus();
	Task<OutputStatusResponse> GetOutputStatus();

	/// <summary>
	/// Get the config (as fetched on Settings - Config files)
	/// </summary>
	/// <param name="users">How many users to fetch. As we don't decode users set it to 1 to run quickest. Set to 0 to fetch them all</param>
	Task<ConfigResponse> GetConfig(int users);
}

public class EliteSxClient : BackgroundService, IEliteSxClient
{
	private readonly ILogger<EliteSxClient> _logger;
	private readonly HttpClient _httpClient;
	private readonly EliteSxClientOptions _options;

	private readonly SemaphoreSlim _authLock = new(1);
	private readonly SemaphoreSlim _httpLock = new(1);
	private string? _guid;

	private int? _authExpireTimeSeconds;
	private Stopwatch? _authExpireAge;
	private Stopwatch? _timeSinceLastPoll;

	private readonly TaskCompletionSource _hasLoggedInOnce = new();
	public Task IsInitialized => _hasLoggedInOnce.Task;

	public EliteSxClient(ILogger<EliteSxClient> logger, IOptions<EliteSxClientOptions> options, HttpClient httpClient)
	{
		_logger = logger;
		_httpClient = httpClient;
		_options = options.Value;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await EnsureAuthenticated();
			}
			catch (Exception ex)
			{
				_logger.LogWarning(ex, "Auth failed");
			}
			await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
		}
	}

	private string GetUrl(string path)
	{
		return $"http://{_options.IpAddress}/{path}?guid={_guid}";
	}

	public async Task EnsureAuthenticated()
	{
		await _authLock.WaitAsync();
		try
		{
			if (_guid == null)
			{
				try
				{
					_logger.LogInformation("Attempting authentication");
					await LogIn();

					_logger.LogDebug("Polling authentication");
					await PollAuth("poll.xml");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Failed to Authenticate");
					throw;
				}
			}
			else
			{
				try
				{

					if (_authExpireTimeSeconds == null || _authExpireAge == null || _timeSinceLastPoll == null)
					{
						throw new Exception("Got guid but not auth expire?");
					}
					else
					{
						if (TimeSpan.FromSeconds(_authExpireTimeSeconds.Value) - _authExpireAge.Elapsed <= _options.AuthRefreshTime)
						{
							_logger.LogInformation("Refreshing authentication");
							await PollAuth("refr.xml");
						}
						//UI hits this every 5 seconds, if you don't you get 404 (I assume we get logged out)
						else if (_timeSinceLastPoll.Elapsed >= TimeSpan.FromSeconds(5))
						{
							_logger.LogDebug("Polling authentication");
							await PollAuth("poll.xml");
						}
					}
				}
				catch (Exception ex)
				{
					_logger.LogWarning(ex, "Failed to poll/refresh, will try Login again");

					_guid = null;
					_authExpireAge = null;
					_authExpireTimeSeconds = null;
					_timeSinceLastPoll = null;

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
			}
		}
		finally
		{
			_authLock.Release();
		}
	}

	private async Task LogIn()
	{
		await _httpLock.WaitAsync();
		try
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

			_hasLoggedInOnce.TrySetResult();
		}
		finally
		{
			_httpLock.Release();
		}
	}

	private async Task PollAuth(string path)
	{
		var response = await Get<PollResponse>(path, "poll auth");

		_authExpireTimeSeconds = response.TimeSeconds;
		_authExpireAge = Stopwatch.StartNew();
		_timeSinceLastPoll = Stopwatch.StartNew();
	}

	private async Task<T> Get<T>(string path, string textForError) where T : class
	{
		await _httpLock.WaitAsync();
		try
		{
			using var response = await _httpClient.GetStreamAsync(GetUrl(path));
			var result = new XmlSerializer(typeof(T)).Deserialize(response) as T;

			if (result == null)
				throw new Exception($"Failed to fetch {textForError}");

			return result;
		}
		finally
		{
			_httpLock.Release();
		}
	}

	public async Task<PrivilegesResponse> GetPrivileges()
	{
		return await Get<PrivilegesResponse>("priv.xml", "privileges");
	}

	public async Task<SystemInformationResponse> GetSystemInformation()
	{
		return await Get<SystemInformationResponse>("sysinfo.xml", "system information");
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

	/// <inheritdoc/>
	public async Task<ConfigResponse> GetConfig(int users)
	{
		return await Get<ConfigResponse>("config.cfx?n=" + users, "config");
	}

	private async Task Post(string path, string content)
	{
		await _httpLock.WaitAsync();
		try
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
		finally
		{
			_httpLock.Release();
		}
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
