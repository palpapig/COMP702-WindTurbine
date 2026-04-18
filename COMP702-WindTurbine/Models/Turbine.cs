namespace COMP702_WindTurbine.models;
public sealed class Turbine
{
    public required string TurbineId { get; set; }
    public required string Name { get; set; }
    public required TurbineModel Model { get; set; }
    public required string Location { get; set; }
    public required string Status { get; set; } // Running, Alarm, Offline

    
    public DateTime? LastTelemetryTime { get; set; } // UTC

    public ICollection<TurbineTelemetry> TelemetryHistories { get; set; } = new List<TurbineTelemetry>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();


    public DegradationModelDetails? DegradationModel { get; set; }
}
