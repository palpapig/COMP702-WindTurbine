namespace COMP702_WindTurbine.models;
public sealed class BenchmarkResult
{
    public long Id { get; set; }
    public required float DeviationScore { get; set; } //average of deviation bins, weighted by wind speed bin frequency
    public required DateTime TimeRangeStart { get; set; }
    public required DateTime TimeRangeEnd { get; set; }





    public required ICollection<PowerBinDeviation> DeviationBins { get; set; } //difference from expected bins
    public required ICollection<PowerBinMeasured> PowerBins { get; set; } //average power in each 0.5m/s bin of windspeed
    public required Turbine Turbine { get; set; }
}
