/*
purpose: common interface for all outlier detection algorithms. 
each detector takes a batch of raw telemetry & returns the same batch with outlier flags set
*/
using System.Collections.Generic;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Preprocessing.OutlierDetection
{
    public interface IOutlierDetector
    {
        IEnumerable<RawTelemetry> Detect(IEnumerable<RawTelemetry> data); //process a batch & set flags
    }
}