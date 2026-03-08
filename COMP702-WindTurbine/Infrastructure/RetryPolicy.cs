namespace COMP702_WindTurbine.Infrastructure;

public sealed class RetryPolicy
{
    private readonly ILogger<RetryPolicy> _logger;
    private readonly int _maxAttempts;

    public RetryPolicy(ILogger<RetryPolicy> logger, int maxAttempts = 3)
    {
        _logger = logger;
        _maxAttempts = maxAttempts;
    }

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                await operation(cancellationToken);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < _maxAttempts)
            {
                var backoff = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning(ex, "Operation failed at attempt {Attempt}. Retrying in {Delay}s", attempt, backoff.TotalSeconds);
                await Task.Delay(backoff, cancellationToken);
            }
        }

        await operation(cancellationToken);
    }
}
