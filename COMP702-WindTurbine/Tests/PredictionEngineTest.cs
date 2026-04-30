

/*using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Prediction;

public sealed class PredictionEngineTest
{
    private readonly IPredictionEngine _predictionEngine;

    public PredictionEngineTest(IPredictionEngine predictionEngine)
    {
        _predictionEngine = predictionEngine;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine(">>> Starting PredictionEngineTest");

        var dummyData = new ProcessedData
        {
            TurbineId = "WT-TEST",
            Timestamp = DateTimeOffset.UtcNow,
            Vibration = 10.5,
            Temperature = 85.0
        };

        var result = await _predictionEngine.PredictAsync(dummyData, cancellationToken);

        Console.WriteLine(">>> RESULT:");
        Console.WriteLine($"TurbineId: {result.TurbineId}");
        Console.WriteLine($"Timestamp: {result.Timestamp}");
        Console.WriteLine($"IsAnomaly: {result.IsAnomaly}");
        Console.WriteLine($"Reason: {result.Reason}");
    }
}
*/