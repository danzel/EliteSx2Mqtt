namespace EliteSx2Mqtt;

public class EliteSxPoller : BackgroundService
{
	private readonly ILogger<EliteSxPoller> _logger;
	private readonly EliteSxClient _client;

	public EliteSxPoller(ILogger<EliteSxPoller> logger, EliteSxClient client)
	{
		_logger = logger;
		_client = client;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await _client.IsInitialized;

		while (!stoppingToken.IsCancellationRequested)
		{
			_logger.LogInformation("Still alive {now}", DateTimeOffset.Now);
			await _client.EnsureAuthenticated();
			await _client.GetZoneStatus();

			await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
		}
	}
}
