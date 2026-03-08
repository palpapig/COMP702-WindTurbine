using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.DataSources;

public sealed class MockDataSource : IDataSource
{
    private readonly ILogger<MockDataSource> _logger;
    private readonly Random _random = new();

    public MockDataSource(ILogger<MockDataSource> logger)
    {
        _logger = logger;
    }

    public Task<RawData> FetchAsync(CancellationToken cancellationToken)
    {
        var data = new RawData
        {
            TurbineId = $"WT-{_random.Next(1, 6):00}",
            Timestamp = DateTimeOffset.UtcNow,
            Vibration = Math.Round(_random.NextDouble() * 12, 2),
            Temperature = Math.Round(40 + _random.NextDouble() * 60, 2),
            WindSpeed = Math.Round(3 + _random.NextDouble() * 22, 2)
        };

        _logger.LogInformation(
            "Fetched raw data for {TurbineId}: vibration={Vibration}, temp={Temperature}, wind={WindSpeed}",
            data.TurbineId,
            data.Vibration,
            data.Temperature,
            data.WindSpeed);

        return Task.FromResult(data);
    }
}
