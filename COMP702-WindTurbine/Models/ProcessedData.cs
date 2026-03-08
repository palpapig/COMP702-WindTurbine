namespace COMP702_WindTurbine.Models;

public sealed class ProcessedData
{
    public required string TurbineId { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public double Vibration { get; init; }
    public double Temperature { get; init; }
    public double WindSpeed { get; init; }
}
