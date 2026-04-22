namespace COMP702_WindTurbine.models;
public sealed class PowerBinExpected
{
    public long Id { get; set; }
    public required float WindSpeed { get; set; }
    public required float Power { get; set; }
}
