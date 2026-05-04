using Simulator.Server.Config;
using Simulator.Server.Models;

namespace Simulator.Server.Services;

public sealed class HistoricalStore
{
    private readonly CsvCatalog _catalog;
    private readonly SimulatorSettings _settings;

    public HistoricalStore(CsvCatalog catalog, SimulatorSettings settings)
    {
        _catalog = catalog;
        _settings = settings;
    }

    public IReadOnlyList<TelemetryRow> Query(string turbineId, DateTime startUtc, DateTime endUtc, int maxRows)
    {
        var ds = _catalog.GetDataset(turbineId, _settings.Data.HistoryYear);
        return ds.Rows
            .Where(r => r.TimestampUtc >= startUtc && r.TimestampUtc <= endUtc)
            .Take(Math.Min(maxRows, _settings.Historical.MaxRowsPerRequest))
            .ToList();
    }
}
