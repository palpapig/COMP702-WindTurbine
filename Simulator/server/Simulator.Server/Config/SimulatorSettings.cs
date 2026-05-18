namespace Simulator.Server.Config;

public sealed class SimulatorSettings
{
    public ReplaySettings Replay { get; set; } = new();
    public OpcUaSettings Opcua { get; set; } = new();
    public HistoricalSettings Historical { get; set; } = new();
    public DataSettings Data { get; set; } = new();

    public static SimulatorSettings Load(string path)
    {
        var json = File.ReadAllText(path);
        var settings = System.Text.Json.JsonSerializer.Deserialize<SimulatorSettings>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return settings ?? new SimulatorSettings();
    }
}

public sealed class ReplaySettings
{
    public bool Enabled { get; set; } = true;
    public string Mode { get; set; } = "accelerated";
    public int Speed { get; set; } = 10;
    public bool Loop { get; set; } = true;
    public string TimeColumn { get; set; } = "Timestamp";
    public string Timezone { get; set; } = "UTC";
    public int IntervalSeconds { get; set; } = 2;
}

public sealed class OpcUaSettings
{
    public string Endpoint { get; set; } = "opc.tcp://localhost:4840";
    public string NamespaceUri { get; set; } = "urn:comp702:windturbine:simulator";
    public string NodePrefix { get; set; } = "Turbine";
}

public sealed class HistoricalSettings
{
    public bool Enabled { get; set; } = true;
    public int MaxRowsPerRequest { get; set; } = 5000;
    public bool ProbeEnabled { get; set; } = true;
    public int ProbeIntervalSeconds { get; set; } = 30;
    public string ProbeSignal { get; set; } = "WindSpeed";
    public int ProbeMaxValues { get; set; } = 20;
    public string ProbeStartUtc { get; set; } = "2019-01-01T00:00:00Z";
    public string ProbeEndUtc { get; set; } = "2019-01-01T01:00:00Z";
}

public sealed class DataSettings
{
    public int ReplayYear { get; set; } = 2024;
    public int HistoryYear { get; set; } = 2023;
    public string TurbineFilePrefix { get; set; } = "Turbine_Data_Kelmarsh_";
    public string CsvPath { get; set; } = "data/turbine4_2017-2022.csv";
    public string TurbineId { get; set; } = "WT-004";
}
