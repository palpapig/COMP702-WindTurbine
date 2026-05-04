using System.Globalization;
using Simulator.Server.Config;
using Simulator.Server.Models;

namespace Simulator.Server.Services;

public sealed class CsvCatalog
{
    private readonly string _dataRoot;
    private readonly SignalMapper _mapper;
    private readonly SimulatorSettings _settings;
    private readonly Dictionary<(string turbineId, int year), TurbineDataset> _datasets = new();

    public CsvCatalog(string dataRoot, SignalMapper mapper, SimulatorSettings settings)
    {
        _dataRoot = dataRoot;
        _mapper = mapper;
        _settings = settings;
    }

    public IReadOnlyCollection<SignalDefinition> SignalDefinitions => _mapper.GetAll();
    public IReadOnlyCollection<string> TurbineIds => _datasets.Keys.Select(k => k.turbineId).Distinct().OrderBy(x => x).ToList();

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var targetYears = new HashSet<int> { _settings.Data.ReplayYear, _settings.Data.HistoryYear };
        var allFiles = Directory.GetFiles(_dataRoot, $"{_settings.Data.TurbineFilePrefix}*.csv");

        var selected = new List<(string path, string turbineId, int year)>();
        foreach (var file in allFiles)
        {
            var meta = ParseFileMeta(file);
            if (meta is null) continue;
            if (!targetYears.Contains(meta.Value.year)) continue;
            selected.Add((file, meta.Value.turbineId, meta.Value.year));
        }

        selected = selected
            .OrderBy(x => x.year)
            .ThenBy(x => x.turbineId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Console.WriteLine($"[Load] Selected files: {selected.Count} (years: {string.Join(", ", targetYears.OrderBy(x => x))})");

        for (var i = 0; i < selected.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = selected[i];
            Console.WriteLine($"[Load] ({i + 1}/{selected.Count}) {Path.GetFileName(item.path)} -> {item.turbineId}, {item.year}");

            var rows = await ReadRowsAsync(item.path, cancellationToken, item.turbineId, item.year);
            _datasets[(item.turbineId, item.year)] = new TurbineDataset(item.turbineId, item.year, rows);

            Console.WriteLine($"[Load] Done {item.turbineId} {item.year}: {rows.Count} rows");
        }
    }

    public TurbineDataset GetDataset(string turbineId, int year) => _datasets[(turbineId, year)];

    private static (string turbineId, int year)? ParseFileMeta(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var parts = name.Split('_');
        if (parts.Length < 8) return null;

        var turbineNo = parts[3];
        var yearPart = parts[4];
        if (!int.TryParse(turbineNo, out var turbine)) return null;
        if (!int.TryParse(yearPart.Split('-')[0], out var year)) return null;

        return ($"WT-{turbine:D3}", year);
    }

    private static async Task<IReadOnlyList<TelemetryRow>> ReadRowsAsync(string file, CancellationToken ct, string turbineId, int year)
    {
        var rows = new List<TelemetryRow>(capacity: 200_000);
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);

        var header = await reader.ReadLineAsync(ct);
        if (header is null) return rows;

        var cols = header.Split(',');
        var tsIndex = Array.FindIndex(cols, c => c.Contains("Timestamp", StringComparison.OrdinalIgnoreCase));
        if (tsIndex < 0) tsIndex = 0;

        long lineNo = 0;
        const long progressEvery = 100_000;

        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrWhiteSpace(line)) continue;
            lineNo++;

            var parts = line.Split(',');
            if (parts.Length <= tsIndex) continue;
            if (!DateTime.TryParse(parts[tsIndex], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ts)) continue;

            var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < cols.Length && i < parts.Length; i++)
            {
                if (i == tsIndex) continue;
                if (double.TryParse(parts[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                {
                    values[cols[i]] = v;
                }
            }

            rows.Add(new TelemetryRow(DateTime.SpecifyKind(ts, DateTimeKind.Utc), values));

            if (lineNo % progressEvery == 0)
            {
                Console.WriteLine($"[Load] {turbineId} {year}: parsed {lineNo:N0} lines, kept {rows.Count:N0} rows");
            }
        }

        return rows;
    }
}
