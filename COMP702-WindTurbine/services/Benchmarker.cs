namespace COMP702_WindTurbine.services;

using COMP702_WindTurbine.models;
using Microsoft.Extensions.Configuration;

public sealed class Benchmarker
{
    private readonly IConfiguration _configuration;

    // We need the power curve parameters from appsettings.json
    public Benchmarker(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TurbineTelemetry BenchmarkData(TurbineTelemetry telemetry)
    {
        double windSpeed = telemetry.WindSpeed ?? 0;
        double actualPower = telemetry.PowerOutput ?? 0;

        // Read power curve parameters from configuration (same as SimulatedLiveDataSource)
        double cutIn = _configuration.GetValue<double>("SimulatedDataSource:Generative:PowerCurve:CutIn");
        double ratedWind = _configuration.GetValue<double>("SimulatedDataSource:Generative:PowerCurve:RatedWind");
        double ratedPower = _configuration.GetValue<double>("SimulatedDataSource:Generative:PowerCurve:RatedPower");
        double cutOut = _configuration.GetValue<double>("SimulatedDataSource:Generative:PowerCurve:CutOut");

        double expectedPower = 0;
        if (windSpeed < cutIn || windSpeed > cutOut)
            expectedPower = 0;
        else if (windSpeed <= ratedWind)
        {
            double ratio = (windSpeed - cutIn) / (ratedWind - cutIn);
            expectedPower = ratio * ratedPower;
        }
        else
        {
            expectedPower = ratedPower;
        }

        double efficiency;
        if (expectedPower > 0)
            efficiency = (actualPower / expectedPower) * 100;
        else
            efficiency = 0;

        // Clamp to 0-100 (but now 100% will only happen if actual == expected)
        telemetry.Efficiency = Math.Clamp(efficiency, 0, 100);

        return telemetry;
    }
}