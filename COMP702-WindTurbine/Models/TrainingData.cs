/*
purpose: stores references to historical data used to train models. 
the DataSnapshot could be a json list of raw telemetry ID's or a file path to a csv
*/
using System;

namespace COMP702_WindTurbine.Models
{
    public class TrainingData
    {
        public int Id { get; set; }
        public string TurbineId { get; set; } = string.Empty; //which turbine this training set belongs to
        public DateTime StartDate { get; set; } //start of the training period
        public DateTime EndDate { get; set; }
        public string DataSnapshot { get; set; } = string.Empty; //reference to the actual data (json,path etc.)
    }
}