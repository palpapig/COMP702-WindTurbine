namespace COMP702_WindTurbine.ModelTraining
{
    public class TrainingScheduleService
    {
        private readonly ModelTrainingConfigService _configService;

        public TrainingScheduleService(ModelTrainingConfigService configService)
        {
            _configService = configService;
        }

        public bool IsTrainingEnabled()
        {
            return _configService.Load().Enabled;
        }

        public string GetPythonTrainEndpoint()
        {
            return _configService.Load().PythonTrainEndpoint;
        }

        public List<string> GetTurbinesDueForTraining()
        {
            var options = _configService.Load();
            var due = new List<string>();

            if (!options.Enabled)
                return due;

            foreach (var turbine in options.Turbines)
            {
                if (string.IsNullOrWhiteSpace(turbine.LastTrainingUtc))
                {
                    due.Add(turbine.TurbineId);
                    continue;
                }

                if (!DateTime.TryParse(turbine.LastTrainingUtc, out var lastTrainingUtc))
                {
                    due.Add(turbine.TurbineId);
                    continue;
                }

                var nextTrainingUtc = lastTrainingUtc.AddMonths(options.IntervalMonths);

                if (DateTime.UtcNow >= nextTrainingUtc)
                {
                    due.Add(turbine.TurbineId);
                }
            }

            return due;
        }

        public ModelTrainingOptions GetOptions()
        {
            return _configService.Load();
        }
    }
}