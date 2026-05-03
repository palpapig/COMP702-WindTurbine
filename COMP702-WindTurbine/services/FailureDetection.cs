using System.Net.Http.Json;
using COMP702_WindTurbine.models;

namespace COMP702_WindTurbine.Services;

public sealed class FailureDetection
{
    private readonly HttpClient _httpClient;

    public FailureDetection(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TurbineTelemetry> FaultDetectAsync( RawData rawdata,
      TurbineTelemetry telemetry,
        CancellationToken ct)
    {
        var predictRequest = PredictionRequestMapper.FromRawData(rawdata);

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

        telemetry.PredictedGearboxOilTemp = result.PredictedValue;
        telemetry.ActualGearboxOilTemp = result.ActualValue;
        telemetry.Residual = result.Alarm?.Residual;
        telemetry.Ewma = result.Alarm?.Ewma;
        telemetry.FaultAlarmLevel = result.Alarm?.AlarmLevel;
        telemetry.StartedAlert = result.IsAnomaly;

        return telemetry;
    }
}