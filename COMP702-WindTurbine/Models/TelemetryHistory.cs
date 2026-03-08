namespace COMP702_WindTurbine.Models;

public sealed class TelemetryHistory
{
    public long Id { get; set; }
    public required string TurbineId { get; set; }
    public DateTime Timestamp { get; set; } // UTC
    public double? WindSpeed { get; set; }
    public double? RotorSpeed { get; set; }
    public double? PowerOutput { get; set; }
    public double? Vibration { get; set; }
    public double? Temperature { get; set; }

    public Turbine? Turbine { get; set; }
}
