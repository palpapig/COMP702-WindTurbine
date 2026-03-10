/*
purpose: represents an alert (alarm) in the database. corresponds to the Alerts table. links to the cleaned telemetry that triggered it through CleanedTelemetryId
*/
using System;

namespace COMP702_WindTurbine.Models
{
    public class Alert
    {
        public long Id { get; set; } //unique identifier (primary key)
        public string TurbineId { get; set; } = string.Empty; //which turbine this alert belongs to 
        public DateTime Timestamp { get; set; } //when the alert was created 
        public string Type { get; set; } = string.Empty; //e.g. "benchmark deviation" or "high temperature"
        public double Value { get; set; } //the value that triggered the alert
        public string Severity { get; set; } = string.Empty; //info, warning, critical
        public string Status { get; set; } = string.Empty; //active, acknowledged, resolved, cleared
        public DateTime? AcknowledgedAt { get; set; } //when someone acknowledged it
        public DateTime? ResolvedAt { get; set; } //when the condition returned to normal
        public DateTime? ClearedAt { get; set; } //when it was finally cleared
        public DateTime UpdatedAt { get; set; } //last update time (utc)
        public int? CleanedTelemetryId { get; set; } //foreign key to the cleaned telemetry that caused the alert
    }
}