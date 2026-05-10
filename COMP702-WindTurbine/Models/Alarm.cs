namespace COMP702_WindTurbine.models;

public class Alarm
{
    public double Residual { get; set; }
    public double EWMA { get; set; }
    public double UCL { get; set; }
    public double LCL { get; set; }
    public bool A1Triggered { get; set; }
    public bool A2Triggered { get; set; }
    public int ConsecutiveA1Count { get; set; }
}