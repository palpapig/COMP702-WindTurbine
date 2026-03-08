namespace COMP702_WindTurbine.Models;

public sealed class PredictionResult
{
    public required string TurbineId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public bool IsAnomaly { get; init; }
    public string? Reason { get; init; }
}
