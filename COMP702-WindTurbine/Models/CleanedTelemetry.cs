/*
purpose: represents data that has passed extreme outlier removal & has been processed by knn outlier detection. 
it still carries a knn flag (true if knn considers it an outlier). this table is where the engines get their input
*/
using System;

namespace COMP702_WindTurbine.Models
{
    public class CleanedTelemetry
    {
        public int Id { get; set; }
        public int? RawTelemetryId { get; set; } //optional link back to the original raw record
        public DateTime Timestamp { get; set; } //measurement time (utc -> coordinated universal time to avoid timezone confusion  and apparently almost all systems store timestamps in UTC but we can apply local time at the UI layer
        public string TurbineId { get; set; } = string.Empty; //which turbine
        public double WindSpeed { get; set; } //in m/s
        public double ActivePower { get; set; } //kw
        public double PitchAngle { get; set; } //degrees
        public double RotorSpeed { get; set; } //rpm
        public double GearboxOilTemp { get; set; }
        public bool KnnOutlierFlag { get; set; } //true if knn model flagged this as an outlier
    }
}