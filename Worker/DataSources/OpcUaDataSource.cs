using COMP702_WindTurbine.models;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace COMP702_WindTurbine.DataSources;

public sealed class OpcUaDataSource : IDataSource
{
    private readonly ILogger<OpcUaDataSource> _logger;
    private readonly OpcUaDataSourceConfig _config;
    private readonly ApplicationConfiguration _appConfig;
    private readonly Dictionary<string, string> _signalToNodePath;

    public OpcUaDataSource(ILogger<OpcUaDataSource> logger, IConfiguration configuration)
    {
        _logger = logger;
        _config = configuration.GetSection("OpcUaDataSource").Get<OpcUaDataSourceConfig>() ?? new OpcUaDataSourceConfig();
        _signalToNodePath = BuildSignalMap(_config);
        _appConfig = BuildAppConfig();
        EnsureApplicationCertificate(_appConfig);
    }

    public async Task<RawData> FetchAsync(CancellationToken cancellationToken)
    {
        var endpoint = CoreClientUtils.SelectEndpoint(_config.EndpointUrl, useSecurity: false);
        var endpointConfig = EndpointConfiguration.Create(_appConfig);
        endpointConfig.OperationTimeout = _config.RequestTimeoutMs;
        var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfig);

        using var session = await Session.Create(
            _appConfig,
            configuredEndpoint,
            false,
            "COMP702.Worker.OpcUaDataSource",
            (uint)_config.RequestTimeoutMs,
            null,
            null);

        var readNodes = new ReadValueIdCollection();
        var signals = new[] { "WindSpeed", "ActivePower", "RotorSpeed", "PitchAngle", "GearboxOilTemp" };
        foreach (var signal in signals)
        {
            readNodes.Add(new ReadValueId
            {
                NodeId = new NodeId(_signalToNodePath[signal], 2),
                AttributeId = Attributes.Value
            });
        }

        session.Read(
            null,
            0,
            TimestampsToReturn.Both,
            readNodes,
            out var values,
            out _);

        static double ToDouble(DataValue v) => Convert.ToDouble(v.Value ?? 0d);
        var windSpeed = ToDouble(values[0]);
        var activePower = ToDouble(values[1]);
        var rotorSpeed = ToDouble(values[2]);
        var pitchAngle = ToDouble(values[3]);
        var gearboxOilTemp = ToDouble(values[4]);
        var ts = values[0].SourceTimestamp != DateTime.MinValue ? values[0].SourceTimestamp : DateTime.UtcNow;

        return new RawData
        {
            TurbineId = _config.TurbineId,
            Timestamp = ts.ToUniversalTime(),
            WindSpeed = windSpeed,
            ActivePower = activePower,
            RotorSpeed = rotorSpeed,
            PitchAngle = pitchAngle,
            GearboxOilTemp = gearboxOilTemp,
            Vibration = 0,
            Temperature = gearboxOilTemp
        };
    }

    private static Dictionary<string, string> BuildSignalMap(OpcUaDataSourceConfig cfg)
    {
        string GetNodeName(string key, string fallback) =>
            cfg.Nodes.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["WindSpeed"] = $"{cfg.RootNode}/{cfg.TurbineId}/{GetNodeName("WindSpeed", "WindSpeed")}",
            ["ActivePower"] = $"{cfg.RootNode}/{cfg.TurbineId}/{GetNodeName("ActivePower", "Power")}",
            ["RotorSpeed"] = $"{cfg.RootNode}/{cfg.TurbineId}/{GetNodeName("RotorSpeed", "RotorSpeed")}",
            ["PitchAngle"] = $"{cfg.RootNode}/{cfg.TurbineId}/{GetNodeName("PitchAngle", "PitchAngle")}",
            ["GearboxOilTemp"] = $"{cfg.RootNode}/{cfg.TurbineId}/{GetNodeName("GearboxOilTemp", "GearboxOilTemp")}"
        };
    }

    private static ApplicationConfiguration BuildAppConfig()
    {
        var app = new ApplicationConfiguration
        {
            ApplicationName = "COMP702.Worker",
            ApplicationUri = "urn:comp702:worker",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = "pki/own",
                    SubjectName = "CN=COMP702.Worker"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/trusted"
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = "Directory",
                    StorePath = "pki/rejected"
                },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = false
            },
            TransportQuotas = new TransportQuotas
            {
                OperationTimeout = 8000
            },
            ClientConfiguration = new ClientConfiguration
            {
                DefaultSessionTimeout = 60000
            }
        };
        app.Validate(ApplicationType.Client).GetAwaiter().GetResult();
        return app;
    }

    private static void EnsureApplicationCertificate(ApplicationConfiguration appConfig)
    {
        var app = new ApplicationInstance
        {
            ApplicationName = appConfig.ApplicationName,
            ApplicationType = ApplicationType.Client,
            ApplicationConfiguration = appConfig
        };
        app.CheckApplicationInstanceCertificate(false, 2048).GetAwaiter().GetResult();
    }
}
