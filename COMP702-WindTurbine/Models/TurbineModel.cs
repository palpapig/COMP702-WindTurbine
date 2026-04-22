namespace COMP702_WindTurbine.models;
public sealed class TurbineModel
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required float CutInWindSpeed { get; set; } //minimum wind speed for turbine to start spinning. approx. 5 m/s
    public required float SaturationWindSpeed { get; set; } //wind speed at which max rotor speed is reached.  approx. 9 m/s
    public required float RatedWindSpeed { get; set; } //wind speed at which max power generation is reached. approx. 13 m/s
    public required float CutOutWindSpeed { get; set; } //maximum wind speed where turbine must stop spinning.  approx. 22 m/s
    
    
    
    
    public required ICollection<PowerBinExpected> ExpectedPowerBins { get; set; } 
    public required ICollection<Turbine> Turbines { get; set; } = [];
}
