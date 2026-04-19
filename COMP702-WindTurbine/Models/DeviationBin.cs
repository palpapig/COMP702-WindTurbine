namespace COMP702_WindTurbine.models;
public sealed class DeviationBin
{
    public required BenchmarkResult Benchmark { get; set; }
    public required float WindSpeed { get; set; }
    public required float Power { get; set; }
}
