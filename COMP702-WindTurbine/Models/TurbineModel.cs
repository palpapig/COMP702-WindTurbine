namespace COMP702_WindTurbine.models;
public sealed class TurbineModel
{
    public required string Name { get; set; }
    public required float CutInWindSpeed { get; set; }
    public required float RatedWindSpeed { get; set; }
    public required float CutOutWindSpeed { get; set; }
    public required ICollection<PowerCurveBin> PowerCurveBins { get; set; }  
}
