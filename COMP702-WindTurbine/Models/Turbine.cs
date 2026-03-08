namespace COMP702_WindTurbine.Models;

public sealed class Turbine
{
    public required string TurbineId { get; set; }
    public required string Name { get; set; }
    public required string Location { get; set; }
    public required string Status { get; set; } // Running, Alarm, Offline
    public DateTime? LastTelemetryTime { get; set; } // UTC

    public ICollection<TelemetryHistory> TelemetryHistories { get; set; } = new List<TelemetryHistory>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
}
