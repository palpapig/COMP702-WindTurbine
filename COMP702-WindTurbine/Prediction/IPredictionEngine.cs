using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Prediction;

public interface IPredictionEngine
{
    Task<PredictionResult> PredictAsync(ProcessedData data, CancellationToken cancellationToken);
}
