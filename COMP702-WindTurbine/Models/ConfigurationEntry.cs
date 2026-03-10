/*
purpose: a simple key‑value table for storing configuration that can be changed at runtime (e.g. alert thresholds, polling intervals)
*/
namespace COMP702_WindTurbine.Models
{
    public class ConfigurationEntry
    {
        public string Key { get; set; } = string.Empty; //cofiguration key (primary key)
        public string Value { get; set; } = string.Empty; //configuration value
    }
}