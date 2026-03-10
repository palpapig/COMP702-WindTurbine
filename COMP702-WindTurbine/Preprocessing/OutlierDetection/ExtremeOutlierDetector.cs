/*
purpose: implements rule‑based outlier detection (negative power, pitch >20°, rotor speed <11 rpm). 
sets ExtremeOutlierFlag on raw telemetry
what will be added later:
- iterate through the raw telemetry list
- apply the rules & set the flag accordingly
- return the same list with flags updated
*/
using System;
using System.Collections.Generic;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Preprocessing.OutlierDetection
{
    public class ExtremeOutlierDetector : IOutlierDetector
    {
        public IEnumerable<RawTelemetry> Detect(IEnumerable<RawTelemetry> data)
        {
            //placeholder - will later check each record & set extremeoutlierflag
            throw new NotImplementedException();
        }
    }
}