namespace COMP702_WindTurbine.models;

public sealed class TurbineTelemetry
{
    public long Id { get; set; }
    public required string TurbineId { get; set; }
    public DateTime Timestamp { get; set; } // UTC


    public double? WindSpeed { get; set; }
    public double? RotorSpeed { get; set; }
    public double? PowerOutput { get; set; }
    public double? Vibration { get; set; }
    public double? Temperature { get; set; }
    // new fields for extended analysis

    public double? PitchAngle { get; set; } // degrees
    public double? GearboxOilTemp { get; set; } // °C
    //public double? GearOilInletTemp { get; set; } // °C
    //public double? GeneratorBearingFrontTemp { get; set; } // °C
    //public double? RearBearingTemp { get; set; } // °C
    //public double? RotorBearingTemp { get; set; } // °C
  
    public double? CorrectedWindSpeed { get; set; } 

    public double GeneratorSpeed { get; set; } //rpm
    public double MinimumPowerOutput { get; set; }


    public double? Efficiency { get; set; }
    public bool? StartedAlert { get; set; }


    public Turbine? Turbine { get; set; }
}
