/*
purpose: stores a trained regression model for a specific performance curve (e.g. power‑wind speed). each turbine can have multiple curves
*/
using System;

namespace COMP702_WindTurbine.Models
{
    public class BaselineCurve
    {
        public int Id { get; set; } //primary key
        public string TurbineId { get; set; } = string.Empty; //which turbine this curve belongs to 
        public string CurveType { get; set; } = string.Empty; //"power-wind" or "pitch-wind" or "rotor-wind"
        public string ModelData { get; set; } = string.Empty; //serialised model (json,binary etc.)
        public DateTime CreatedAt { get; set; } //when this curve was created/updated
    }
}