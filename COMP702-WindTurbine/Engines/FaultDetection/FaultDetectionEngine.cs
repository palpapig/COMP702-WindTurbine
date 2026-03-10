/*
purpose: implements the fault detection engine. it will hopefully detect gearbox oil temperature using a trained model & raise alarms if residuals exceed thresholds. currently a stub
what will be added later:
- fetch training data via IDataAccessor to build/update the model
- detect gearbox oil temp
- compute residual and ewma
- check a1/a2 alarm conditions
- store results via WriteAnalysisResultsAsync and possibly create Alert records
*/
using System;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Engines.FaultDetection
{
    public class FaultDetectionEngine : IFaultDetectionEngine
    {
        public Task ProcessAsync(CleanedTelemetry data) => throw new NotImplementedException();
    }
}