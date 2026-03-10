/*
purpose: provides simulated live data for testing & development. it will eventually generate the 7 required fields with realistic random values
what will be added later:
- generate random numbers for each field
- return a list of RawTelemetry with ExtremeOutlierFlag = false (or occasionally true to test outlier detection)
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.DataSources
{
    public class MockDataSource : IDataSource
    {
        public Task<IEnumerable<RawTelemetry>> FetchAsync(CancellationToken cancellationToken)
        {
            //placeholder - will later generate realistic dummy/online data
            throw new NotImplementedException();
        }
    }
}