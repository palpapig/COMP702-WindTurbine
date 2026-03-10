/*
purpose: placeholder for a machine‑learning‑based outlier detector (knn). 
will eventually, hopefully load a pre‑trained model & set KnnOutlierFlag.
what will be added later:
- load a serialised knn model (from database or file)
- for each record, predict whether it's an outlier
- set KnnOutlierFlag accordingly
*/
using System;
using System.Collections.Generic;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Preprocessing.OutlierDetection
{
    public class KnnOutlierDetector : IOutlierDetector
    {
        public IEnumerable<RawTelemetry> Detect(IEnumerable<RawTelemetry> data)
        {
            //placeholder - will later use kNN model to detect outliers
            throw new NotImplementedException();
        }
    }
}