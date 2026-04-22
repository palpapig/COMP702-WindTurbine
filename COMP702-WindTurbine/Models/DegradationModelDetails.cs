namespace COMP702_WindTurbine.models;
using System.ComponentModel.DataAnnotations.Schema;
public sealed class DegradationModelDetails
{
    public long Id { get; set; }
    public required float Offset { get; set; }
    public required string Filepath { get; set; }



[ForeignKey("TurbineId")] //Says that this is the child in the one-to-one relationship
    public required Turbine Turbine { get; set; }
}
