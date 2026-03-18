using System.Net.Http.Json;
using System.Text.Json;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Prediction;

public sealed class PythonPredictionEngine : IPredictionEngine
{
    private readonly HttpClient _httpClient;

    public PythonPredictionEngine(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PredictionResult> PredictAsync(ProcessedData data, CancellationToken cancellationToken)
    {
        Console.WriteLine(">>> PythonPredictionEngine.PredictAsync was called");

        var request = new
        {
            turbine_id = "T1",
            vibration = 10.5,
            temperature = 85.0
        };

        Console.WriteLine(">>> Sending request to Python");

        var response = await _httpClient.PostAsJsonAsync(
            "predict",
            request,
            cancellationToken);

        Console.WriteLine($">>> Response received: {(int)response.StatusCode} {response.StatusCode}");

        response.EnsureSuccessStatusCode();

        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($">>> Raw response JSON: {rawJson}");

        var result = JsonSerializer.Deserialize<PredictionResult>(
            rawJson,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result is null)
            throw new Exception("No response from Python API");

        Console.WriteLine(">>> Successfully deserialized Python response");

        return result;
    }
}