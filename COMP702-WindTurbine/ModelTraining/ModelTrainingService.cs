using System.Net.Http.Json;
using System.Text.Json;
using COMP702_WindTurbine.database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace COMP702_WindTurbine.ModelTraining
{
    public class ModelTrainingService
    {
        private readonly MonitoringDbContext _context;
        private readonly TrainingScheduleService _scheduleService;
        private readonly HttpClient _httpClient;
        private readonly ModelTrainingOptions _options;

        public ModelTrainingService(
            MonitoringDbContext context,
            TrainingScheduleService scheduleService,
            HttpClient httpClient,
            IOptions<ModelTrainingOptions> options)
        {
            _context = context;
            _scheduleService = scheduleService;
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<bool> RunTrainingIfDueAsync(CancellationToken cancellationToken = default)
        {
            if (!_scheduleService.IsTrainingEnabled())
            {
                Console.WriteLine("Model training is disabled.");
                return false;
            }

            if (!_scheduleService.IsRetrainingDue())
            {
                Console.WriteLine("Model retraining is not due yet.");
                return false;
            }

            var turbineId = _scheduleService.GetTurbineId();
            var fromDate = GetFromDateUtc();

            var data = await _context.TurbineData
                .Where(x => x.TurbineId == turbineId && x.Timestamp >= fromDate)
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);

            if (data.Count == 0)
            {
                Console.WriteLine($"No training rows found for turbine {turbineId}.");
                return false;
            }

            var rows = data.Select(d => new Dictionary<string, object?>
            {
                { "timestamp", d.Timestamp },
                { "windSpeed", d.WindSpeed },
                { "rotorSpeed", d.RotorSpeed },
                { "power", d.PowerOutput },
                { "vibration", d.Vibration },
                { "temperature", d.Temperature },
                { "pitchAngle", d.PitchAngle },
                { "gearOilTemp", d.GearboxOilTemp }
            }).ToList();

            var request = new
            {
                turbine_id = turbineId,
                rows = rows
            };

            var endpoint = _scheduleService.GetPythonTrainEndpoint();

            Console.WriteLine($"Starting model training for {turbineId} with {rows.Count} rows.");
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

            Console.WriteLine($"Python /train response: {(int)response.StatusCode} {response.StatusCode}");
            response.EnsureSuccessStatusCode();

            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"Raw /train response JSON: {rawJson}");

            var result = JsonSerializer.Deserialize<PythonTrainResponse>(
                rawJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (result is null)
                throw new Exception("No response from Python training API");

            if (result.Success)
            {
                Console.WriteLine("Training completed successfully.");
                return true;
            }

            Console.WriteLine("Python training API returned unsuccessful result.");
            return false;
        }

        private DateTime GetFromDateUtc()
        {
            if (string.IsNullOrWhiteSpace(_options.LastTrainingUtc))
            {
                return DateTime.UtcNow.AddMonths(-_options.IntervalMonths);
            }

            if (!DateTime.TryParse(_options.LastTrainingUtc, out var lastTrainingUtc))
            {
                return DateTime.UtcNow.AddMonths(-_options.IntervalMonths);
            }

            return lastTrainingUtc;
        }
    }
}