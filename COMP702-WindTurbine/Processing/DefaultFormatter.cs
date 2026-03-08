using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Processing;

public sealed class DefaultFormatter : IDataFormatter
{
    private readonly ILogger<DefaultFormatter> _logger;

    public DefaultFormatter(ILogger<DefaultFormatter> logger)
    {
        _logger = logger;
    }

    public Task<ProcessedData> FormatAsync(RawData rawData, CancellationToken cancellationToken)
    {
        var processed = new ProcessedData
        {
            TurbineId = rawData.TurbineId,
            Timestamp = rawData.Timestamp,
            Vibration = rawData.Vibration,
            Temperature = rawData.Temperature,
            WindSpeed = rawData.WindSpeed
        };

        _logger.LogDebug("Formatted data for {TurbineId}", processed.TurbineId);
        return Task.FromResult(processed);
    }
}
