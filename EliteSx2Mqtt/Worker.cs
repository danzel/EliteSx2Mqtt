namespace EliteSx2Mqtt;

public class EliteSxPoller : BackgroundService
{
	private readonly ILogger<EliteSxPoller> _logger;

	public EliteSxPoller(ILogger<EliteSxPoller> logger)
	{
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{

			_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
			await Task.Delay(1000, stoppingToken);
		}
	}
}
