namespace COMP702_WindTurbine.Infrastructure;

public sealed class PollingScheduler
{
    private readonly RetryPolicy _retryPolicy;
    private readonly ILogger<PollingScheduler> _logger;
    private readonly TimeSpan _interval;

    public PollingScheduler(RetryPolicy retryPolicy, ILogger<PollingScheduler> logger, IConfiguration configuration)
    {
        _retryPolicy = retryPolicy;
        _logger = logger;

        var seconds = configuration.GetValue<int?>("Monitoring:IntervalSeconds") ?? 5;
        _interval = TimeSpan.FromSeconds(seconds);
    }

    public async Task RunAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _retryPolicy.ExecuteAsync(action, cancellationToken);

            _logger.LogDebug("Polling scheduler waiting {IntervalSeconds} seconds", _interval.TotalSeconds);
            await Task.Delay(_interval, cancellationToken);
        }
    }
}
