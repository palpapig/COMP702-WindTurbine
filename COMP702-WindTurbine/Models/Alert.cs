namespace COMP702_WindTurbine.Models;

public sealed class Alert
{
    public long Id { get; set; }
    public required string TurbineId { get; set; }
    public DateTime Timestamp { get; set; } // UTC (created time)
    public required string Type { get; set; } // e.g. VibrationHigh
    public double Value { get; set; }
    public required string Severity { get; set; } // Info, Warning, Critical
    public required string Status { get; set; } // Active, Acknowledged, Resolved, Cleared
    public DateTime? AcknowledgedAt { get; set; } // UTC
    public DateTime? ResolvedAt { get; set; } // UTC
    public DateTime? ClearedAt { get; set; } // UTC
    public DateTime UpdatedAt { get; set; } // UTC

    public Turbine? Turbine { get; set; }
}
