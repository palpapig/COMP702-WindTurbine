using Simulator.Server.Config;
using Simulator.Server.Models;

namespace Simulator.Server.Services;

public sealed class ReplayEngine
{
    private readonly CsvCatalog _catalog;
    private readonly SimulatorSettings _settings;
    private int _index;

    public ReplayEngine(CsvCatalog catalog, SimulatorSettings settings)
    {
        _catalog = catalog;
        _settings = settings;
    }

    public ReplayFrame NextFrame()
    {
        var perTurbine = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        DateTime ts = DateTime.UtcNow;

        foreach (var turbineId in _catalog.TurbineIds)
        {
            var ds = _catalog.GetDataset(turbineId, _settings.Data.ReplayYear);
            if (ds.Rows.Count == 0) continue;
            var row = ds.Rows[_index % ds.Rows.Count];
            ts = row.TimestampUtc;
            perTurbine[turbineId] = row.Values;
        }

        _index++;
        return new ReplayFrame(ts, perTurbine);
    }
}
