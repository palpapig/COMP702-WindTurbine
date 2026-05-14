using Simulator.Server.Config;
using Simulator.Server.Services;
using Simulator.Server.Models;

static string ResolveSimulatorRoot()
{
    var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "Simulator");
    if (Directory.Exists(cwdCandidate)) return cwdCandidate;
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
        var c = Path.Combine(dir.FullName, "Simulator");
        if (Directory.Exists(c)) return c;
        dir = dir.Parent;
    }
    throw new DirectoryNotFoundException("Cannot locate Simulator folder from current context.");
}

var simulatorRoot = ResolveSimulatorRoot();
var configPath = Path.Combine(simulatorRoot, "config", "simulator.settings.json");
var settings = SimulatorSettings.Load(configPath);

var csvPath = Path.IsPathRooted(settings.Data.CsvPath)
    ? settings.Data.CsvPath
    : Path.GetFullPath(Path.Combine(simulatorRoot, settings.Data.CsvPath));

if (!File.Exists(csvPath))
{
    Console.WriteLine($"[HA] CSV source not found: {csvPath}");
    return;
}

const int turbineCount = 1;
var signalCount = 0;

var haReader = new CsvIndexedReader(csvPath);
Console.WriteLine("[HA] Building CSV time index...");
await haReader.BuildIndexAsync(CancellationToken.None);
signalCount = Math.Max(0, haReader.ColumnMap.Count - 1);
Console.WriteLine($"Loaded turbines: {turbineCount}");
Console.WriteLine($"Signals (full): {signalCount}");
Console.WriteLine("Simulator.Server CSV mode ready.");
Console.WriteLine($"[HA] Index ready. Columns: {haReader.ColumnMap.Count}");

var opcNodes = haReader.ColumnMap.Keys
    .Where(x => !x.Equals("Timestamp", StringComparison.OrdinalIgnoreCase))
    .Select(x => new OpcNodeDefinition(x, x))
    .ToList();
Console.WriteLine($"[OPC] Node definitions prepared: {opcNodes.Count}");

var replay = new CsvReplayReader(csvPath);
await replay.LoadAsync(CancellationToken.None);
Console.WriteLine($"[Replay] Loaded rows for replay. Columns: {replay.ColumnMap.Count}");

if (DateTime.TryParse(settings.Historical.ProbeStartUtc, null, System.Globalization.DateTimeStyles.AssumeUniversal, out var configuredStart))
{
    var configuredStartUtc = configuredStart.Kind == DateTimeKind.Utc ? configuredStart : configuredStart.ToUniversalTime();
    if (replay.SeekToStartUtc(configuredStartUtc, out var actualStartUtc))
    {
        Console.WriteLine($"[Replay] Start positioned to configured probeStartUtc={configuredStartUtc:O} (actual row ts={actualStartUtc:O})");
    }
    else
    {
        Console.WriteLine($"[Replay] Warning: probeStartUtc={configuredStartUtc:O} is outside CSV range; replay starts from first row.");
    }
}
else
{
    Console.WriteLine($"[Replay] Warning: invalid probeStartUtc '{settings.Historical.ProbeStartUtc}'; replay starts from first row.");
}

static string? PickColumn(IReadOnlyDictionary<string, int> columns, params string[] candidates)
{
    foreach (var c in candidates)
    {
        var match = columns.Keys.FirstOrDefault(k => k.Equals(c, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(match)) return match;
    }
    return null;
}

var signalMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
void TryMap(string opcName, params string[] candidates)
{
    var col = PickColumn(replay.ColumnMap, candidates);
    if (!string.IsNullOrWhiteSpace(col))
    {
        signalMap[opcName] = col;
    }
}

TryMap("WindSpeed", "WindSpeed", "Wind Speed", "windspeed");
TryMap("Power", "Power", "ActivePower", "PowerOutput", "Active Power", "power");
TryMap("RotorSpeed", "RotorSpeed", "Rotor Speed", "rotorspeed");
TryMap("PitchAngle", "PitchAngle", "Pitch Angle", "pitchangle");
TryMap("GearboxOilTemp", "GearboxOilTemp", "Gear Oil Temp", "Temperature", "temp");

if (signalMap.Count == 0)
{
    Console.WriteLine("[OPC] No canonical signal mappings found from CSV headers. Server will publish raw CSV column names.");
}
else
{
    Console.WriteLine($"[OPC] Canonical mappings ready: {string.Join(", ", signalMap.Select(x => $"{x.Key}<={x.Value}"))}");
    var canonicalNodes = signalMap.Keys
        .Where(k => !opcNodes.Any(n => n.Name.Equals(k, StringComparison.OrdinalIgnoreCase)))
        .Select(k => new OpcNodeDefinition(k, k))
        .ToList();
    opcNodes = opcNodes.Concat(canonicalNodes).ToList();
    Console.WriteLine($"[OPC] Combined node definitions prepared: {opcNodes.Count} (raw + canonical)");
}

var opcServer = new OpcUaHostedServer(opcNodes, haReader, settings.Opcua.NamespaceUri);
await opcServer.StartAsync(settings.Opcua.Endpoint, CancellationToken.None);
Console.WriteLine($"[OPC] Endpoint started: {settings.Opcua.Endpoint}");

Console.WriteLine("Simulator.Server running. Press Ctrl+C to stop.");
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, settings.Replay.IntervalSeconds)));

try
{
    while (await timer.WaitForNextTickAsync(cts.Token))
    {
        Dictionary<string, double>? publishedValues = null;

        if (settings.Replay.Enabled && replay.TryNext(out var ts, out var values))
        {
            if (signalMap.Count > 0)
            {
                var merged = new Dictionary<string, double>(values, StringComparer.OrdinalIgnoreCase);
                foreach (var map in signalMap)
                {
                    if (values.TryGetValue(map.Value, out var v))
                    {
                        merged[map.Key] = v;
                    }
                }
                publishedValues = merged;
                opcServer.PublishLiveValues(ts, merged);
            }
            else
            {
                publishedValues = values;
                opcServer.PublishLiveValues(ts, values);
            }
        }

        Console.WriteLine($"[Heartbeat] {DateTime.UtcNow:O} | turbines={turbineCount} signals={signalCount} | live={opcServer.LiveSnapshot()}");
        if (publishedValues is { Count: > 0 })
        {
            var preview = string.Join(", ", publishedValues
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => $"{x.Key}={x.Value:F3}"));
            Console.WriteLine($"[LiveValues] {preview}");
        }
    }
}
catch (OperationCanceledException)
{
    Console.WriteLine("Simulator.Server stopping...");
}
finally
{
    await opcServer.StopAsync();
    Console.WriteLine("Simulator.Server stopped.");
}
