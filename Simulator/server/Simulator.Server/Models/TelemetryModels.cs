namespace Simulator.Server.Models;

public sealed record SignalDefinition(
    string Key,
    string GreenbyteTitle,
    string ManufacturerTitle,
    string Unit,
    string NodeName);

public sealed record TelemetryRow(DateTime TimestampUtc, Dictionary<string, double> Values);

public sealed record TurbineDataset(string TurbineId, int Year, IReadOnlyList<TelemetryRow> Rows);

public sealed record ReplayFrame(DateTime Timestamp, Dictionary<string, Dictionary<string, double>> Values);
