/*
purpose: contract for the data formatter. 
any formatter must have a ProcessAsync method that accepts a batch of raw telemetry
*/
using System.Collections.Generic;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Processing
{
    public interface IDataFormatter
    {
        Task ProcessAsync(IEnumerable<RawTelemetry> rawData); //process a batch of raw data
    }
}