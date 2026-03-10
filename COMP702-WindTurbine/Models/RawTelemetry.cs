/*
purpose: represents raw data as it arrives from external sources. 
includes the seven mandatory fields & a flag for extreme outlier detection
*/
using System;

namespace COMP702_WindTurbine.Models
{
    public class RawTelemetry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string TurbineId { get; set; } = string.Empty;
        public double WindSpeed { get; set; }
        public double ActivePower { get; set; }
        public double PitchAngle { get; set; }
        public double RotorSpeed { get; set; }
        public double GearboxOilTemp { get; set; }
        public bool ExtremeOutlierFlag { get; set; } //true if extreme rule-based detection flagged this record
    }
}