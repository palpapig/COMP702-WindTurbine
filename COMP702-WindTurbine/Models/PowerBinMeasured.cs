namespace COMP702_WindTurbine.models;
public sealed class PowerBinMeasured
{
    public long Id { get; set; }
    public required BenchmarkResult Benchmark { get; set; }
    public required float WindSpeed { get; set; }
    public required float Power { get; set; }
}
