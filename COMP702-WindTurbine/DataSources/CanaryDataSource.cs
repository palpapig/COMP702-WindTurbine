/*
purpose: a placeholder for connecting to a real canary historian. when implemented, it will fetch live data from the canary API and return it as RawTelemetry objects
what will be added later:
- implement actual http calls to canary
- map the response to RawTelemetry
- handle authentication and errors
*/

using System;
using System.Collections.Generic; //for IEnumerable
using System.Threading; //for cancellationtoken
using System.Threading.Tasks;
using COMP702_WindTurbine.Models; //for rawtelemetry

namespace COMP702_WindTurbine.DataSources
{
    public class CanaryDataSource : IDataSource
    {
        public Task<IEnumerable<RawTelemetry>> FetchAsync(CancellationToken cancellationToken)
        {
            //placeholder - wil later connect to canary theoretically and return real data
            throw new NotImplementedException();
        }
    }
}