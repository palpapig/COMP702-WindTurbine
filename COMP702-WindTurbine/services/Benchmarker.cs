namespace benchmarking_experimenting.services;
using benchmarking_experimenting.models;


public sealed class Benchmarker
{
    public TurbineTelemetry BenchmarkData(TurbineTelemetry telemetry)
    {
        var rand = new Random();
        telemetry.Efficiency = Math.Round(rand.NextDouble()*100, 2);
        return telemetry;
    }
}