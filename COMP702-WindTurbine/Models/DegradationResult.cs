namespace COMP702_WindTurbine.models;
public sealed class DegradationResult
{
    public required float Region2Score { get; set; }
    public required float Region2Point5Score { get; set; }
    public required DateTime TimeStamp { get; set; }
    public required DateTime TimeRangeStart { get; set; }
    public required DateTime TimeRangeEnd { get; set; }
}
