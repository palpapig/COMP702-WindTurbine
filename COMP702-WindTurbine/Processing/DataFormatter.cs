/*
purpose: responsible for converting raw data to standard units, mapping column names & orchestrating outlier detection. 
it then passes data to the data accessor for storage
what will be added later:
- unit conversion (e.g. wind speed to m/s, power to kw)
- call outlier detectors
- store raw telemetry with extreme flags
- for data that passes extreme, run knn & store cleaned telemetry
*/
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Processing
{
    public class DataFormatter : IDataFormatter
    {
        public Task ProcessAsync(IEnumerable<RawTelemetry> rawData)
        {
            //placeholder - will later normalise units, run outlier detectors and store through Data Accessor
            throw new NotImplementedException();
        }
    }
}