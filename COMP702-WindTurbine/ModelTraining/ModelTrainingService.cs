using System.Net.Http.Json;
using System.Text.Json;
using COMP702_WindTurbine.database;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace COMP702_WindTurbine.ModelTraining
{
    public class ModelTrainingService
    {
        private readonly MonitoringDbContext _context;
        private readonly TrainingScheduleService _scheduleService;
        private readonly ModelTrainingConfigService _configService;
        private readonly HttpClient _httpClient;

        public ModelTrainingService(
            MonitoringDbContext context,
            TrainingScheduleService scheduleService,
            ModelTrainingConfigService configService,
            HttpClient httpClient)
        {
            _context = context;
            _scheduleService = scheduleService;
            _configService = configService;
            _httpClient = httpClient;
        }

        public async Task<bool> RunTrainingForTurbineAsync(string turbineId, CancellationToken cancellationToken = default)
        {
            var fromDate = GetFromDateUtc(turbineId);

            var data = await _context.TurbineData
                .Where(x => x.TurbineId == turbineId && x.Timestamp >= fromDate)
                .OrderBy(x => x.Timestamp)
                .ToListAsync(cancellationToken);



            if (data.Count == 0)
            {
                Console.WriteLine($"No training rows found for turbine {turbineId}.");
                return false;
            }

            var rows = data.Select(d => new
            {
                timestamp = d.Timestamp,
                values = new Dictionary<string, object?>
        {
            { "windSpeed", d.WindSpeed },
            { "rotorSpeed", d.RotorSpeed },
            { "power", d.PowerOutput },
            { "temperature", d.Temperature },
            { "pitchAngle", d.PitchAngle },
            { "gearOilTemp", d.GearboxOilTemp }
        }
            }).ToList();

            var request = new
            {
                turbineId = turbineId,
                rows = rows,
                targetColumn = "power", // chnage to gear temp if needed but then remove ear temp from feature coloumns
                featureColumns = new[]
                {
            "windSpeed", // these features to be changed
            "rotorSpeed",
            "temperature",
            "pitchAngle",
            "gearOilTemp"
        },
                forceRetrain = false
            };

            var endpoint = _scheduleService.GetPythonTrainEndpoint();

            Console.WriteLine($"Starting model training for {turbineId} with {rows.Count} rows.");
            Console.WriteLine($"Training endpoint: {endpoint}");

            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

            Console.WriteLine($"Python /train response: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine($"Raw /train response JSON: {rawJson}");

            response.EnsureSuccessStatusCode();

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


                _configService.UpdateLastTrainingUtc(turbineId, DateTime.UtcNow);
                Console.WriteLine($"Training completed successfully for turbine {turbineId}.");
                return true;
            }

            Console.WriteLine($"Python training API returned unsuccessful result for turbine {turbineId}.");

            return false;
        }

        private DateTime GetFromDateUtc(string turbineId)
        {
            var options = _scheduleService.GetOptions();
            var turbine = options.Turbines.FirstOrDefault(t => t.TurbineId == turbineId);

            if (turbine is null || string.IsNullOrWhiteSpace(turbine.LastTrainingUtc))
                return DateTime.UtcNow.AddMonths(-options.IntervalMonths);

            if (!DateTimeOffset.TryParse(
                    turbine.LastTrainingUtc,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal,
                    out var dto))
            {
                return DateTime.UtcNow.AddMonths(-options.IntervalMonths);
            }

            var utc = dto.UtcDateTime;
            Console.WriteLine($"GetFromDateUtc => {utc:o}, Kind={utc.Kind}");
            return utc;
        }
    }
}