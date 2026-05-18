namespace COMP702_WindTurbine.models;
using System.ComponentModel.DataAnnotations.Schema;
public sealed class DegradationModelDetails
{
    public long Id { get; set; }
    public required float Region2Offset { get; set; }
    public required float Region2p5Offset { get; set; }
    public required string Region2Filename { get; set; }
    public required string Region2p5Filename { get; set; }

    public required string TurbineId { get; set; } //FK
    public Turbine? Turbine { get; set; }
}
