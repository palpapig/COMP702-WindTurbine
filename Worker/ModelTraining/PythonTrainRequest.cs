namespace COMP702_WindTurbine.ModelTraining
{
    public sealed class PythonTrainRequest
    {
        public string TurbineId { get; set; } = string.Empty;
        public List<Dictionary<string, object?>> Rows { get; set; } = new();
    }
}