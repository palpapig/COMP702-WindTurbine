namespace COMP702_WindTurbine.Infrastructure;

public sealed class HealthService
{
    private DateTimeOffset _lastDataTimestamp = DateTimeOffset.MinValue;
    private readonly TimeSpan _maxStaleness;

    public HealthService(TimeSpan? maxStaleness = null)
    {
        _maxStaleness = maxStaleness ?? TimeSpan.FromSeconds(30);
    }

    public DateTimeOffset LastDataTimestamp => _lastDataTimestamp;

    public void UpdateLastDataTimestamp(DateTimeOffset timestamp)
    {
        _lastDataTimestamp = timestamp;
    }

    public bool IsHealthy()
    {
        if (_lastDataTimestamp == DateTimeOffset.MinValue)
        {
            return false;
        }

        return DateTimeOffset.UtcNow - _lastDataTimestamp <= _maxStaleness;
    }
}
