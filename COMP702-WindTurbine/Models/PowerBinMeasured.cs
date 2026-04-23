namespace COMP702_WindTurbine.models;
public sealed class PowerBinMeasured
{
    public long Id { get; set; }
    public required float WindSpeed { get; set; }
    public required float Power { get; set; }



    public required BenchmarkResult BenchmarkResult { get; set; }
}
