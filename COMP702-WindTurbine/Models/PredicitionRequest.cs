using COMP702_WindTurbine.models;

namespace COMP702_WindTurbine.models;

public sealed class PredictRequest
{
    public required string TurbineId { get; init; }
    public DateTime Timestamp { get; init; }
    public double? ActualTargetValue { get; init; }
    public required Dictionary<string, double?> Values { get; init; }
}

public static class PredictionRequestMapper
{
    public static PredictRequest FromRawData(RawData raw)
    {

        Console.WriteLine($"TurbineId: {raw.TurbineId}");
        Console.WriteLine($"Timestamp: {raw.Timestamp}");
        Console.WriteLine($"GearboxOilTemp (Target): {raw.GearboxOilTemp}");

        Console.WriteLine($"GearOilInletTemp: {raw.GearOilInletTemp}");
        Console.WriteLine($"GeneratorBearingFrontTemp: {raw.GeneratorBearingFrontTemp}");
        Console.WriteLine($"RearBearingTemp: {raw.RearBearingTemp}");
        Console.WriteLine($"GearOilPumpPressure: {raw.GearOilPumpPressure}");
        Console.WriteLine($"GearOilInletPressure: {raw.GearOilInletPressure}");
        Console.WriteLine($"NacelleTemp: {raw.NacelleTemp}");

        return new PredictRequest
        {
            TurbineId = raw.TurbineId,
            Timestamp = raw.Timestamp,
            ActualTargetValue = raw.GearboxOilTemp,
            Values = new Dictionary<string, double?>
            {
                ["GearOilInletTemp"] = raw.GearOilInletTemp,
                ["GeneratorBearingFrontTemp"] = raw.GeneratorBearingFrontTemp,
                ["RearBearingTemp"] = raw.RearBearingTemp,
                ["GearOilPumpPressure"] = raw.GearOilPumpPressure,
                ["GearOilInletPressure"] = raw.GearOilInletPressure,
                ["NacelleTemp"] = raw.NacelleTemp
            }
        };
    }
}