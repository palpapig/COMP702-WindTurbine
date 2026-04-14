using COMP702_WindTurbine.models;

namespace COMP702_WindTurbine.DataSources;

public interface IDataSource
{
    Task<RawData> FetchAsync(CancellationToken cancellationToken);
}
