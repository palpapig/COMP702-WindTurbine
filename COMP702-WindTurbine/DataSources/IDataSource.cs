/*
purpose: defines a common interface for all external data sources (simulated, canary, etc.) each source must be able to fetch a batch of raw telemetry
*/
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.DataSources
{
    public interface IDataSource
    {
        Task<IEnumerable<RawTelemetry>> FetchAsync(CancellationToken cancellationToken); //gets a batch of raw telemetry data
    }
}