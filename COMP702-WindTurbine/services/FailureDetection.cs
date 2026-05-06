using System.Net.Http.Json;
using COMP702_WindTurbine.models;
using System.Diagnostics;

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

            /*            new coloumns need to be addewd to db
                        telemetry.PredictedValue = result.PredictedValue;
                        telemetry.ActualValue = result.ActualValue;
                        telemetry.TurbineId = result.TurbineId;
                        telemetry.Residual = result.Residual;
                        telemetry.IsFaulty = result.IsFaulty;
                        telemetry.AlarmLvl = result.AlarmLvl;
            */
            Console.WriteLine(
            $"Prediction Result -> " +
            $"PredictedValue: {result.PredictedValue}, " +
            $"ActualValue: {result.ActualValue}, " +
            $"Residual: {result.Residual}, " +
            $"IsFaulty: {result.IsFaulty}, " +
            $"AlarmLvl: {result.AlarmLvl}"
);
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
    public async Task<bool> GetPythonHealth(CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync("health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }


}

