using System.Text.Json.Serialization;

namespace COMP702_WindTurbine.ModelTraining
{
    public sealed class ModelTrainingOptions
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("interval_months")]
        public int IntervalMonths { get; set; }

        [JsonPropertyName("python_train_endpoint")]
        public string PythonTrainEndpoint { get; set; } = "train";

        [JsonPropertyName("turbines")]
        public List<TurbineTrainingInfo> Turbines { get; set; } = new();
    }

    public sealed class TurbineTrainingInfo
    {
        [JsonPropertyName("turbine_id")]
        public string TurbineId { get; set; } = string.Empty;

        [JsonPropertyName("last_training_utc")]
        public string LastTrainingUtc { get; set; } = string.Empty;
    }
}