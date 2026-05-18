namespace COMP702_WindTurbine.models;

public class FailureDetectionSettings
{
    public OnnxModelSettings OnnxModel { get; set; } = new();
    public AlarmSettings Alarm { get; set; } = new();
}

public class OnnxModelSettings
{
    public string Path { get; set; } = "";
    public double Rmse { get; set; }
    public double R2 { get; set; }
    public double ResidualStd { get; set; }
}

public class AlarmSettings
{
    public double EwmaLambda { get; set; }
    public double ControlLimitK { get; set; }
    public int A1RequiredCount { get; set; }
    public double ResidualStd { get; set; }

    public Dictionary<string, double> TurbineResidualBiases { get; set; } = new();
}