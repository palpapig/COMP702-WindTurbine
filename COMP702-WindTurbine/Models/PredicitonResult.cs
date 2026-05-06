namespace COMP702_WindTurbine.models;

public class PredictionResult
{
    public string TurbineId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    public double? PredictedValue { get; set; }
    public double? ActualValue { get; set; }

    public double? Residual { get; set; } // difference between predicted and actual value

    public bool? IsFaulty { get; set; } // true if the model predicts a fault, false otherwise

    public int? AlarmLvl { get; set; } //  0 mean no alarm, 1 means warning, 2 means critical
}
