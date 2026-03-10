/*
purpose: holds the results of benchmarking & fault detection for a cleaned telemetry record. 
one‑to‑one with cleaned telemetry
*/
namespace COMP702_WindTurbine.Models
{
    public class TelemetryAnalysis
    {
        public int Id { get; set; }
        public int CleanedTelemetryId { get; set; } //foreign key to a cleaned telemetry
        //Benchmarking Results
        public double? BenchmarkResidual { get; set; } //difference between actual & expected power (or other)
        public double? BenchmarkConfidenceLower { get; set; } //lower bound of prediction interval
        public double? BenchmarkConfidenceUpper { get; set; } //upper bound of prediction interval (PI)
        public double? PredictedGearboxOilTemp { get; set; } //model-predicted temperature
        public double? ResidualError { get; set; } //actual-predicted temp
        public double? EwmaValue { get; set; } //exponentially weighted moving average of residuals
        public string? FaultAlarmLevel { get; set; } //A1 or A2 or null if no alarm
    }
}