namespace benchmarking_experimenting.models;

public sealed class RawData
{
    public required string TurbineId { get; init; }
    public DateTime Timestamp { get; init; }
    public double WSSensor { get; init; }
    public double RSSensor { get; init; }
    public double POSensor { get; init; }
    public double VibSensor { get; init; }
    public double TempSensor { get; init; }
}
