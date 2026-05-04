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
        if (settings.Replay.Enabled && replay.TryNext(out var ts, out var values))
        {
            opcServer.PublishLiveValues(ts, values);
        }

        Console.WriteLine($"[Heartbeat] {DateTime.UtcNow:O} | turbines={turbineCount} signals={signalCount} | live={opcServer.LiveSnapshot()}");
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
