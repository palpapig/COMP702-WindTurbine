namespace COMP702_WindTurbine.models;
public sealed class Turbine
{
    public required string TurbineId { get; set; }
    public required string Name { get; set; }
    public TurbineModel? Model { get; set; } //TODO have a way to add turbine model details to database. Also an error when trying to benchmark but no model is provided.
    public required string Location { get; set; }
    public required string Status { get; set; } // Running, Alarm, Offline

    
    public DateTime? LastTelemetryTime { get; set; } // UTC

    public ICollection<TurbineTelemetry> TelemetryHistories { get; set; } = new List<TurbineTelemetry>();
    public ICollection<Alert> Alerts { get; set; } = new List<Alert>();


    public DegradationModelDetails? DegradationModel { get; set; }
    public ICollection<BenchmarkResult> BenchmarkResult { get; set; } = new List<BenchmarkResult>();
    public ICollection<DegradationResult> DegradationResult { get; set; } = new List<DegradationResult>();
}