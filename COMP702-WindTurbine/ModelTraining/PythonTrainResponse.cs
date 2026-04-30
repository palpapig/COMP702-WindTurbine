namespace COMP702_WindTurbine.ModelTraining
{
    public sealed class PythonTrainResponse
    {
        public bool Success { get; set; }
        public string? TurbineId { get; set; }
        public int RowsUsed { get; set; }
        public string? Message { get; set; }
    }
}