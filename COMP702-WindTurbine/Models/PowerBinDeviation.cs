namespace COMP702_WindTurbine.models;
public sealed class PowerBinDeviation
{
    public long Id { get; set; }
    public required BenchmarkResult BenchmarkResult { get; set; }
    public required float WindSpeed { get; set; }
    public required float PowerDifference { get; set; } //difference of measured from expected power
    public required float PowerRatio { get; set; } //ratio of measured from expected power
}
