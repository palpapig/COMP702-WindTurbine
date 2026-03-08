using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Prediction;

public sealed class RulePredictionEngine : IPredictionEngine
{
    public Task<PredictionResult> PredictAsync(ProcessedData data, CancellationToken cancellationToken)
    {
        var isVibrationAnomaly = data.Vibration > 8;
        var isTemperatureAnomaly = data.Temperature > 80;

        var reason = isVibrationAnomaly && isTemperatureAnomaly
            ? "vibration > 8 and temperature > 80"
            : isVibrationAnomaly
                ? "vibration > 8"
                : isTemperatureAnomaly
                    ? "temperature > 80"
                    : null;

        var result = new PredictionResult
        {
            TurbineId = data.TurbineId,
            Timestamp = data.Timestamp,
            IsAnomaly = reason is not null,
            Reason = reason
        };

        return Task.FromResult(result);
    }
}
