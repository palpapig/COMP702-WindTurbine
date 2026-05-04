using System.Globalization;

namespace Simulator.Server.Services;

public sealed class CsvReplayReader
{
    private readonly string _csvPath;
    private readonly Dictionary<string, int> _columnMap = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string[]> _rows = new();
    private int _index;

    public CsvReplayReader(string csvPath)
    {
        _csvPath = csvPath;
    }

    public IReadOnlyDictionary<string, int> ColumnMap => _columnMap;

    public async Task LoadAsync(CancellationToken ct)
    {
        _rows.Clear();
        _columnMap.Clear();
        _index = 0;

        using var fs = new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs);

        var header = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(header)) return;
        var cols = header.Split(',');
        for (var i = 0; i < cols.Length; i++) _columnMap[cols[i].Trim()] = i;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;
            _rows.Add(line.Split(','));
        }
    }

    public bool TryNext(out DateTime timestampUtc, out Dictionary<string, double> values)
    {
        timestampUtc = default;
        values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        if (_rows.Count == 0) return false;

        var row = _rows[_index];
        _index = (_index + 1) % _rows.Count;

        if (!DateTime.TryParse(row[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ts)) return false;
        timestampUtc = DateTime.SpecifyKind(ts, DateTimeKind.Utc);

        foreach (var kv in _columnMap)
        {
            if (kv.Key.Equals("Timestamp", StringComparison.OrdinalIgnoreCase)) continue;
            var idx = kv.Value;
            if (idx < 0 || idx >= row.Length) continue;
            if (!double.TryParse(row[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) continue;
            if (double.IsNaN(v) || double.IsInfinity(v)) continue;
            values[kv.Key] = v;
        }

        return true;
    }
}
