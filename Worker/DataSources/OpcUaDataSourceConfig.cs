namespace COMP702_WindTurbine.DataSources;

public sealed class DataSourceSelectorConfig
{
    public string Type { get; set; } = "Simulated";
}

public sealed class OpcUaDataSourceConfig
{
    public string EndpointUrl { get; set; } = "opc.tcp://localhost:4840";
    public string TurbineId { get; set; } = "WT-004";
    public string RootNode { get; set; } = "Turbines";
    public int RequestTimeoutMs { get; set; } = 8000;
    public Dictionary<string, string> Nodes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WindSpeed"] = "WindSpeed",
        ["ActivePower"] = "ActivePower",
        ["RotorSpeed"] = "RotorSpeed",
        ["PitchAngle"] = "PitchAngle",
        ["GearboxOilTemp"] = "GearboxOilTemp"
    };
}
