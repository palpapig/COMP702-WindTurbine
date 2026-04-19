namespace COMP702_WindTurbine.models;
public sealed class BenchmarkResult
{
    public required string TurbineId { get; set; }
    public required float DeviationScore { get; set; } //average of deviation bins, weighted by wind speed bin frequency
    public required ICollection<PowerCurveBin> DeviationBins { get; set; } //difference from expected bins
    public required ICollection<PowerCurveBin> PowerBins { get; set; } //average power in each 0.5m/s bin of windspeed
    public required DateTime TimeRangeStart { get; set; }
    public required DateTime TimeRangeEnd { get; set; }
}
