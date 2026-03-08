using COMP702_WindTurbine.Models;

namespace COMP702_WindTurbine.Processing;

public interface IDataFormatter
{
    Task<ProcessedData> FormatAsync(RawData rawData, CancellationToken cancellationToken);
}
