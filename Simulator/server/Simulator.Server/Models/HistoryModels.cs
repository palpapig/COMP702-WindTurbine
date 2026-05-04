namespace Simulator.Server.Models;

public sealed record CsvTimeIndexEntry(DateTime TimestampUtc, long Offset);

public sealed record HistoryQueryRequest(
    DateTime StartUtc,
    DateTime EndUtc,
    IReadOnlyCollection<string>? Signals,
    int MaxValues = 5000,
    long? ContinuationOffset = null);

public sealed record HistoryQueryResult(
    IReadOnlyList<TelemetryRow> Rows,
    long? NextContinuationOffset);
