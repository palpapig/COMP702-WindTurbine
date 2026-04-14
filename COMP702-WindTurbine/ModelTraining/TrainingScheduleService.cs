using Microsoft.Extensions.Options;

namespace COMP702_WindTurbine.ModelTraining
{
    public class TrainingScheduleService
    {
        private readonly ModelTrainingOptions _options;

        public TrainingScheduleService(IOptions<ModelTrainingOptions> options)
        {
            _options = options.Value;
        }

        public bool IsTrainingEnabled()
        {
            return _options.Enabled;
        }

        public bool IsRetrainingDue()
        {
            if (!_options.Enabled)
                return false;

            if (string.IsNullOrWhiteSpace(_options.LastTrainingUtc))
                return true;

            if (!DateTime.TryParse(_options.LastTrainingUtc, out var lastTrainingUtc))
                return true;

            var nextTrainingUtc = lastTrainingUtc.AddMonths(_options.IntervalMonths);
            return DateTime.UtcNow >= nextTrainingUtc;
        }

        public string GetTurbineId()
        {
            return _options.TurbineId;
        }

        public string GetPythonTrainEndpoint()
        {
            return _options.PythonTrainEndpoint;
        }
    }
}