namespace COMP702_WindTurbine.Models;

public sealed class WorkerMetrics
{
    public long Id { get; set; }
    public required string WorkerId { get; set; }
    public DateTime Timestamp { get; set; } // UTC
    public int SignalsProcessed { get; set; }
    public int AlarmsTriggered { get; set; }
    public double PipelineLatencyMs { get; set; }
}
