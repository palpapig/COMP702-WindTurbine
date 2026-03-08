using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.DataSources;

public interface IDataSource
{
    Task<RawData> FetchAsync(CancellationToken cancellationToken);
}
