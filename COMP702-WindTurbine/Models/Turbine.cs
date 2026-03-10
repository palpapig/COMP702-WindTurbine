/*
purpose: represents a wind turbine. stores metadata like name, location & current status
*/
using System;

namespace COMP702_WindTurbine.Models
{
    public class Turbine
    {
        public string TurbineId { get; set; } = string.Empty; //external identifiers (primary key)
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; //site location
        public string Status { get; set; } = string.Empty; //running, alarm, offline
        public DateTime? LastTelemetryTime { get; set; } //time of the most recent telemetry received
    }
}