

namespace COMP702_WindTurbine.models;

public class FailureDetectionResult
{
    public string TurbineId { get; set; } = "";

    //  public TurbineTelemetry Telemetry { get; set; } = new();

    public DateTime? Timestamp { get; set; }

    public double? Residual { get; set; }

    public bool IsAbnormal { get; set; }

    public int AlarmLvl { get; set; }

    public double? PredictedValue { get; set; }

    public double? ActualValue { get; set; }

    public double? LCL { get; set; }
    public double? UCL { get; set; }
    public double EWMA { get; set; }
    public bool A1Triggered { get; set; }
    public bool A2Triggered { get; set; }


    //public Alarm? Alarm { get; set; }
}
