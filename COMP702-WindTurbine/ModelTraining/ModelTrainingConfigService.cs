using System.Text.Json;

namespace COMP702_WindTurbine.ModelTraining
{
    public class ModelTrainingConfigService
    {
        private readonly string _configPath;

        public ModelTrainingConfigService()
        {
            _configPath = Path.Combine(AppContext.BaseDirectory, "ModelTraining", "model_training_settings.json");
        }

        public ModelTrainingOptions Load()
        {
            if (!File.Exists(_configPath))
                throw new FileNotFoundException($"Training config file not found: {_configPath}");

            var json = File.ReadAllText(_configPath);

            var options = JsonSerializer.Deserialize<ModelTrainingOptions>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (options is null)
                throw new InvalidOperationException("Failed to load model training settings.");

            return options;
        }

        public void Save(ModelTrainingOptions options)
        {
            var json = JsonSerializer.Serialize(
                options,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            File.WriteAllText(_configPath, json);
        }

        public void UpdateLastTrainingUtc(string turbineId, DateTime utcTime)
        {
            var options = Load();

            options.Turbines ??= new List<TurbineTrainingInfo>();

            var turbine = options.Turbines.FirstOrDefault(t => t.TurbineId == turbineId);
            if (turbine is null)
            {
                options.Turbines.Add(new TurbineTrainingInfo
                {
                    TurbineId = turbineId,
                    LastTrainingUtc = utcTime.ToString("O")
                });

                Console.WriteLine($"Added new turbine: {turbineId}");
            }
            else
            {
                turbine.LastTrainingUtc = utcTime.ToString("O");
                Console.WriteLine($"Updated existing turbine: {turbineId}");
            }

            Save(options);
        }
    }
}