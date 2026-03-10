/*
purpose: an interface (for abstraction) defines the contract for the benchmarking engine. any benchmarking implementation must have a ProcessAsync method that takes cleaned telemetry
*/

using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Engines.Benchmarking
{
    public interface IBenchmarkingEngine
    {
        Task ProcessAsync(CleanedTelemetry data); //process a single cleaned telemetry record
    }
}