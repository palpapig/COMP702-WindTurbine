/*
purpose: contract for the fault detection engine
*/
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Engines.FaultDetection
{
    public interface IFaultDetectionEngine
    {
        Task ProcessAsync(CleanedTelemetry data); //process a cleaned telemetry record for fault detection
    }
}