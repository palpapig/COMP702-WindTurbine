using System.Net.Http.Json;
using COMP702_WindTurbine.models;

namespace COMP702_WindTurbine.services;

public sealed class FailureDetection
{
    private readonly HttpClient _httpClient;

    public FailureDetection(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TurbineTelemetry> FaultDetectAsync(RawData rawdata,
      TurbineTelemetry telemetry,
        CancellationToken ct)
    {
        var predictRequest = PredictionRequestMapper.FromRawData(rawdata);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("predict", predictRequest, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"Python error: {response.StatusCode} - {error}");
                return telemetry;
            }

            var result = await response.Content.ReadFromJsonAsync<PredictionResult>(
                cancellationToken: ct
            );

            if (result == null)
                return telemetry;

            // later you can copy result values into telemetry here
            return telemetry;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Python service is not available: {ex.Message}");
            return telemetry;
        }
        catch (TaskCanceledException ex)
        {
            Console.WriteLine($"Python request timed out: {ex.Message}");
            return telemetry;
        }
    }
}

