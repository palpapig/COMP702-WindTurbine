namespace COMP702_WindTurbine.models;
public sealed class DegradationResult
{
    public long Id { get; set; }
    public required float Region2Score { get; set; }
    public required float Region2Point5Score { get; set; }
    public required DateTime TimeRangeStart { get; set; }
    public required DateTime TimeRangeEnd { get; set; }





    public required Turbine Turbine { get; set; }

}
