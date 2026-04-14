namespace COMP702_WindTurbine.ModelTraining
{
    public sealed class ModelTrainingOptions
    {
        public bool Enabled { get; set; }
        public int IntervalMonths { get; set; }
        public string LastTrainingUtc { get; set; } = string.Empty;
        public string TurbineId { get; set; } = "T1";
        public string PythonTrainEndpoint { get; set; } = "train";
    }
}