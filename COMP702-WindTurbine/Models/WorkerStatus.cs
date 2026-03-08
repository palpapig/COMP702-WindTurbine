namespace COMP702_WindTurbine.Models;

public sealed class WorkerStatus
{
    public required string WorkerId { get; set; }
    public DateTime LastHeartbeat { get; set; } // UTC
    public required string Status { get; set; } // Healthy, Error
    public string? LastError { get; set; }
    public DateTime? LastDataFetchTime { get; set; } // UTC
}
