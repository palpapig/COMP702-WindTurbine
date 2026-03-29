namespace COMP702_WindTurbine.services;
using COMP702_WindTurbine.models;


public sealed class Benchmarker
{
    public TurbineTelemetry BenchmarkData(TurbineTelemetry telemetry)
    {
        var rand = new Random();
        telemetry.Efficiency = Math.Round(rand.NextDouble()*100, 2);
        return telemetry;
    }
}