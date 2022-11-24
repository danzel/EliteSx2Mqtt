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
		await _client.LogIn();

		foreach (var name in (await _client.GetOutputNames()).Names)
			Console.WriteLine($"{name.Index}: {name.Name}");

		foreach (var status in (await _client.GetOutputStatus()).Statuses)
			Console.WriteLine($"{status.Index}: {status.State}");

		await _client.ControlOutput(4, DesiredOutputState.On);

		await Task.Delay(1000, stoppingToken);

		foreach (var status in (await _client.GetOutputStatus()).Statuses)
			Console.WriteLine($"{status.Index}: {status.State}");

		await _client.ControlOutput(4, DesiredOutputState.Off);

		await Task.Delay(1000, stoppingToken);

		foreach (var status in (await _client.GetOutputStatus()).Statuses)
			Console.WriteLine($"{status.Index}: {status.State}");

		while (!stoppingToken.IsCancellationRequested)
		{

			_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
			await Task.Delay(1000, stoppingToken);
		}
	}
}
