namespace COMP702_WindTurbine.models;
public sealed class BenchmarkResult
{
    public long Id { get; set; }
    public float? DeviationScore { get; set; } //average of deviation bins, weighted by wind speed bin frequency
    //TODO: should be required, but is not so the logic in Benchmarker.cs for creating and filling in the BenchmarkResult is easier.
    public required DateTime TimeRangeStart { get; set; }
    public required DateTime TimeRangeEnd { get; set; }





    public  ICollection<PowerBinDeviation> DeviationBins { get; set; } = []; //reference for convenience, not a real foreign key
    //difference from expected bins
    public  ICollection<PowerBinMeasured> PowerBins { get; set; } = []; //reference for convenience, not a real foreign key
    //average power in each 0.5m/s bin of windspeed
    public required Turbine Turbine { get; set; } //FK
}
