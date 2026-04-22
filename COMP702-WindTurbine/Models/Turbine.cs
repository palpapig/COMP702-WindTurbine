using System.ComponentModel.DataAnnotations.Schema;

namespace COMP702_WindTurbine.models;
public sealed class Turbine
{
    public required string TurbineId { get; set; }
    public required string Name { get; set; }
    public required string Location { get; set; }
    public required string Status { get; set; } // Running, Alarm, Offline    
    public DateTime? LastTelemetryTime { get; set; } // UTC



    public ICollection<TurbineTelemetry> TelemetryHistories { get; set; } = [];
    public ICollection<Alert> Alerts { get; set; } = [];



    public TurbineModel? TurbineModel { get; set; } //TODO have a way to add turbine model details to database. Just manually through supabase?
    public DegradationModelDetails? DegradationModelDetails { get; set; }
    public ICollection<BenchmarkResult> BenchmarkResults { get; set; } = [];
    public ICollection<DegradationResult> DegradationResults { get; set; } = [];
}