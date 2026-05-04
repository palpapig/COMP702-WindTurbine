using System.Globalization;
using Simulator.Server.Models;

namespace Simulator.Server.Services;

public sealed class CsvIndexedReader
{
    private readonly string _csvPath;
    private readonly List<CsvTimeIndexEntry> _index = new();
    private readonly Dictionary<string, int> _columnMap = new(StringComparer.OrdinalIgnoreCase);

    public CsvIndexedReader(string csvPath)
    {
        _csvPath = csvPath;
    }

    public IReadOnlyDictionary<string, int> ColumnMap => _columnMap;

    public async Task BuildIndexAsync(CancellationToken ct)
    {
        _index.Clear();
        _columnMap.Clear();

        using var fs = new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(fs);

        var headerOffset = fs.Position;
        var header = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(header)) return;

        var cols = header.Split(',');
        for (var i = 0; i < cols.Length; i++) _columnMap[cols[i].Trim()] = i;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var offset = fs.Position;
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split(',');
            if (parts.Length == 0) continue;
            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ts)) continue;
            _index.Add(new CsvTimeIndexEntry(DateTime.SpecifyKind(ts, DateTimeKind.Utc), offset));
        }
    }

    public async Task<HistoryQueryResult> QueryAsync(HistoryQueryRequest request, CancellationToken ct)
    {
        var startOffset = request.ContinuationOffset ?? FindStartOffset(request.StartUtc);
        if (startOffset < 0) return new HistoryQueryResult(Array.Empty<TelemetryRow>(), null);

        var wanted = request.Signals is null || request.Signals.Count == 0
            ? _columnMap.Keys.Where(k => !k.Equals("Timestamp", StringComparison.OrdinalIgnoreCase)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : request.Signals.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rows = new List<TelemetryRow>();
        long? nextOffset = null;

        using var fs = new FileStream(_csvPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Seek(startOffset, SeekOrigin.Begin);
        using var reader = new StreamReader(fs);

        while (rows.Count < request.MaxValues)
        {
            ct.ThrowIfCancellationRequested();
            var currentOffset = fs.Position;
            var line = await reader.ReadLineAsync(ct);
            if (line is null)
            {
                nextOffset = null;
                break;
            }

            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            if (parts.Length == 0) continue;
            if (!DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var tsRaw)) continue;
            var ts = DateTime.SpecifyKind(tsRaw, DateTimeKind.Utc);

            if (ts < request.StartUtc) continue;
            if (ts > request.EndUtc)
            {
                nextOffset = null;
                break;
            }

            var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var signal in wanted)
            {
                if (!_columnMap.TryGetValue(signal, out var idx)) continue;
                if (idx < 0 || idx >= parts.Length) continue;
                if (!double.TryParse(parts[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) continue;
                if (double.IsNaN(v) || double.IsInfinity(v)) continue;
                values[signal] = v;
            }

            rows.Add(new TelemetryRow(ts, values));
            nextOffset = fs.Position;
        }

        if (rows.Count < request.MaxValues) nextOffset = null;
        return new HistoryQueryResult(rows, nextOffset);
    }

    private long FindStartOffset(DateTime startUtc)
    {
        if (_index.Count == 0) return -1;
        var lo = 0;
        var hi = _index.Count - 1;
        var ans = _index.Count;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) / 2);
            if (_index[mid].TimestampUtc >= startUtc)
            {
                ans = mid;
                hi = mid - 1;
            }
            else lo = mid + 1;
        }

        return ans >= _index.Count ? -1 : _index[ans].Offset;
    }
}
