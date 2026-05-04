using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using Simulator.Server.Models;

namespace Simulator.Server.Services;

public sealed class OpcUaHostedServer
{
    private ApplicationInstance? _application;
    private bool _started;
    private DateTime _lastSourceTs;
    private int _lastCount;
    private readonly IReadOnlyList<OpcNodeDefinition> _nodes;
    private readonly string _namespaceUri;
    private readonly CsvIndexedReader? _historyReader;
    private LiveNodeManager? _nodeManager;

    public OpcUaHostedServer(
        IReadOnlyList<OpcNodeDefinition> nodes,
        CsvIndexedReader? historyReader = null,
        string namespaceUri = "urn:comp702:windturbine:simulator")
    {
        _nodes = nodes;
        _historyReader = historyReader;
        _namespaceUri = namespaceUri;
    }

    public async Task StartAsync(string endpointUrl, CancellationToken ct)
    {
        var pkiRoot = Path.Combine(AppContext.BaseDirectory, "pki");
        Directory.CreateDirectory(pkiRoot);

        var appConfig = new ApplicationConfiguration
        {
            ApplicationName = "Simulator.Server",
            ApplicationUri = $"urn:{Utils.GetHostName()}:Simulator.Server",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(pkiRoot, "own"),
                    SubjectName = "CN=Simulator.Server, O=COMP702"
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(pkiRoot, "trusted")
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = Path.Combine(pkiRoot, "rejected")
                },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { endpointUrl }
            },
            TransportConfigurations = new TransportConfigurationCollection(),
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            TraceConfiguration = new TraceConfiguration()
        };

        await appConfig.Validate(ApplicationType.Server);
        _application = new ApplicationInstance
        {
            ApplicationName = appConfig.ApplicationName,
            ApplicationType = ApplicationType.Server,
            ApplicationConfiguration = appConfig
        };

        await _application.CheckApplicationInstanceCertificate(false, 2048);
        var server = new MinimalOpcUaServer(s => new LiveNodeManager(s, _namespaceUri, _nodes, _historyReader));
        await _application.Start(server);
        _nodeManager = server.NodeManagerInstance;
        _started = true;
    }

    public void PublishLiveValues(DateTime sourceTimestampUtc, IReadOnlyDictionary<string, double> values)
    {
        _lastSourceTs = sourceTimestampUtc;
        _lastCount = values.Count;
        _nodeManager?.Publish(sourceTimestampUtc, values);
    }

    public string LiveSnapshot() => $"srcTs={_lastSourceTs:O}, points={_lastCount}";

    public Task<HistoryQueryResult> ReadHistoryAsync(
        string signal,
        DateTime startUtc,
        DateTime endUtc,
        int maxValues = 5000,
        long? continuationOffset = null,
        CancellationToken ct = default)
    {
        if (_nodeManager is null)
        {
            return Task.FromResult(new HistoryQueryResult(Array.Empty<TelemetryRow>(), null));
        }

        return _nodeManager.ReadHistoryAsync(signal, startUtc, endUtc, maxValues, continuationOffset, ct);
    }

    public Task StopAsync()
    {
        if (!_started || _application is null) return Task.CompletedTask;
        _application.Stop();
        _started = false;
        return Task.CompletedTask;
    }
}

internal sealed class MinimalOpcUaServer : StandardServer
{
    private readonly Func<IServerInternal, LiveNodeManager> _factory;
    public LiveNodeManager? NodeManagerInstance { get; private set; }

    public MinimalOpcUaServer(Func<IServerInternal, LiveNodeManager> factory)
    {
        _factory = factory;
    }

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        NodeManagerInstance = _factory(server);
        return new MasterNodeManager(server, configuration, null, NodeManagerInstance);
    }
}

internal sealed class LiveNodeManager : CustomNodeManager2
{
    private readonly IReadOnlyList<OpcNodeDefinition> _nodes;
    private readonly CsvIndexedReader? _historyReader;
    private readonly Dictionary<string, BaseDataVariableState> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _nodeToCsvSignal = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _fullNodeIdToCsvSignal = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HistoryContinuationState> _continuations = new(StringComparer.Ordinal);

    public LiveNodeManager(IServerInternal server, string namespaceUri, IReadOnlyList<OpcNodeDefinition> nodes, CsvIndexedReader? historyReader)
        : base(server, namespaceUri)
    {
        _nodes = nodes;
        _historyReader = historyReader;
        SystemContext.NodeIdFactory = this;
    }

    public override NodeId New(ISystemContext context, NodeState node)
        => new(Guid.NewGuid(), NamespaceIndex);

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var refs))
        {
            refs = new List<IReference>();
            externalReferences[ObjectIds.ObjectsFolder] = refs;
        }

        var turbines = new FolderState(null)
        {
            SymbolicName = "Turbines",
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId("Turbines", NamespaceIndex),
            BrowseName = new QualifiedName("Turbines", NamespaceIndex),
            DisplayName = "Turbines",
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None
        };
        turbines.AddReference(ReferenceTypes.Organizes, true, ObjectIds.ObjectsFolder);
        refs.Add(new NodeStateReference(ReferenceTypes.Organizes, false, turbines.NodeId));
        AddPredefinedNode(SystemContext, turbines);

        var wt004 = new FolderState(turbines)
        {
            SymbolicName = "WT-004",
            ReferenceTypeId = ReferenceTypes.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId("WT-004", NamespaceIndex),
            BrowseName = new QualifiedName("WT-004", NamespaceIndex),
            DisplayName = "WT-004",
            WriteMask = AttributeWriteMask.None,
            UserWriteMask = AttributeWriteMask.None
        };
        wt004.AddReference(ReferenceTypes.Organizes, true, turbines.NodeId);
        turbines.AddChild(wt004);
        AddPredefinedNode(SystemContext, wt004);

        foreach (var def in _nodes)
        {
            var variable = new BaseDataVariableState(wt004)
            {
                SymbolicName = def.Name,
                ReferenceTypeId = ReferenceTypes.HasComponent,
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                NodeId = new NodeId($"WT-004/{def.Name}", NamespaceIndex),
                BrowseName = new QualifiedName(def.Name, NamespaceIndex),
                DisplayName = def.Name,
                DataType = DataTypeIds.Double,
                ValueRank = ValueRanks.Scalar,
                AccessLevel = AccessLevels.CurrentRead | AccessLevels.HistoryRead,
                UserAccessLevel = AccessLevels.CurrentRead | AccessLevels.HistoryRead,
                Historizing = true,
                Value = 0d,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };
            wt004.AddChild(variable);
            _variables[def.CsvColumn] = variable;
            _nodeToCsvSignal[def.Name] = def.CsvColumn;
            _fullNodeIdToCsvSignal[$"WT-004/{def.Name}"] = def.CsvColumn;
            AddPredefinedNode(SystemContext, variable);
        }
    }

    public void Publish(DateTime sourceTimestampUtc, IReadOnlyDictionary<string, double> values)
    {
        lock (Lock)
        {
            foreach (var kv in values)
            {
                if (!_variables.TryGetValue(kv.Key, out var variable)) continue;
                variable.Value = kv.Value;
                variable.Timestamp = sourceTimestampUtc;
                variable.ClearChangeMasks(SystemContext, false);
            }
        }
    }

    public Task<HistoryQueryResult> ReadHistoryAsync(
        string nodeSignalName,
        DateTime startUtc,
        DateTime endUtc,
        int maxValues,
        long? continuationOffset,
        CancellationToken ct)
    {
        if (_historyReader is null)
        {
            return Task.FromResult(new HistoryQueryResult(Array.Empty<TelemetryRow>(), null));
        }

        if (!_nodeToCsvSignal.TryGetValue(nodeSignalName, out var csvSignal))
        {
            csvSignal = nodeSignalName;
        }

        var req = new HistoryQueryRequest(
            StartUtc: startUtc,
            EndUtc: endUtc,
            Signals: new[] { csvSignal },
            MaxValues: Math.Max(1, maxValues),
            ContinuationOffset: continuationOffset);

        return _historyReader.QueryAsync(req, ct);
    }

    public override void HistoryRead(
        OperationContext context,
        HistoryReadDetails details,
        TimestampsToReturn timestampsToReturn,
        bool releaseContinuationPoints,
        IList<HistoryReadValueId> nodesToRead,
        IList<HistoryReadResult> results,
        IList<ServiceResult> errors)
    {
        Console.WriteLine($"[HistoryRead] Entered. nodes={nodesToRead.Count}, details={details.GetType().Name}, release={releaseContinuationPoints}");
        for (var i = 0; i < nodesToRead.Count; i++)
        {
            var result = new HistoryReadResult { StatusCode = StatusCodes.Good };
            results[i] = result;
            errors[i] = ServiceResult.Good;

            try
            {
                if (_historyReader is null)
                {
                    result.StatusCode = StatusCodes.BadHistoryOperationUnsupported;
                    continue;
                }

                if (details is not ReadRawModifiedDetails raw)
                {
                    result.StatusCode = StatusCodes.BadHistoryOperationUnsupported;
                    continue;
                }

                var node = nodesToRead[i];
                string? continuationKey = null;
                long? continuationOffset = null;

                if (node.ContinuationPoint is { Length: > 0 })
                {
                    continuationKey = System.Text.Encoding.UTF8.GetString(node.ContinuationPoint);
                    lock (Lock)
                    {
                        if (_continuations.TryGetValue(continuationKey, out var state))
                        {
                            continuationOffset = state.Offset;
                        }
                        else
                        {
                            result.StatusCode = StatusCodes.BadContinuationPointInvalid;
                            continue;
                        }
                    }
                }

                if (releaseContinuationPoints && continuationKey is not null)
                {
                    lock (Lock)
                    {
                        _continuations.Remove(continuationKey);
                    }
                    result.ContinuationPoint = null;
                    result.HistoryData = new ExtensionObject(new HistoryData { DataValues = new DataValueCollection() });
                    continue;
                }

                var idText = node.NodeId?.Identifier?.ToString();
                Console.WriteLine($"[HistoryRead] NodeId={node.NodeId}, idText={idText ?? "<null>"}");
                if (string.IsNullOrWhiteSpace(idText))
                {
                    result.StatusCode = StatusCodes.BadNodeIdUnknown;
                    continue;
                }

                string? csvSignal = null;
                if (_fullNodeIdToCsvSignal.TryGetValue(idText, out var fullMapped))
                {
                    csvSignal = fullMapped;
                }
                else
                {
                    var shortSignal = idText.Split('/').LastOrDefault();
                    if (!string.IsNullOrWhiteSpace(shortSignal) && _nodeToCsvSignal.TryGetValue(shortSignal, out var shortMapped))
                    {
                        csvSignal = shortMapped;
                    }
                }

                if (string.IsNullOrWhiteSpace(csvSignal))
                {
                    Console.WriteLine($"[HistoryRead] Unknown NodeId: {node.NodeId}");
                    result.StatusCode = StatusCodes.BadNodeIdUnknown;
                    continue;
                }

                var startUtc = raw.StartTime.Kind == DateTimeKind.Utc ? raw.StartTime : raw.StartTime.ToUniversalTime();
                var endUtc = raw.EndTime.Kind == DateTimeKind.Utc ? raw.EndTime : raw.EndTime.ToUniversalTime();
                if (startUtc == DateTime.MinValue || endUtc == DateTime.MinValue || endUtc < startUtc)
                {
                    result.StatusCode = StatusCodes.BadInvalidArgument;
                    continue;
                }

                var max = raw.NumValuesPerNode > 0 ? (int)raw.NumValuesPerNode : 5000;
                var hist = _historyReader.QueryAsync(
                    new HistoryQueryRequest(startUtc, endUtc, new[] { csvSignal! }, max, continuationOffset),
                    CancellationToken.None).GetAwaiter().GetResult();

                var data = new HistoryData { DataValues = new DataValueCollection() };
                foreach (var row in hist.Rows)
                {
                    if (!row.Values.TryGetValue(csvSignal!, out var v)) continue;
                    data.DataValues.Add(new DataValue
                    {
                        Value = v,
                        SourceTimestamp = row.TimestampUtc,
                        ServerTimestamp = DateTime.UtcNow,
                        StatusCode = StatusCodes.Good
                    });
                }

                result.HistoryData = new ExtensionObject(data);
                result.StatusCode = StatusCodes.Good;

                if (hist.NextContinuationOffset is long next)
                {
                    continuationKey ??= Guid.NewGuid().ToString("N");
                    lock (Lock)
                    {
                        _continuations[continuationKey] = new HistoryContinuationState
                        {
                            Offset = next,
                            Signal = csvSignal!,
                            StartUtc = startUtc,
                            EndUtc = endUtc,
                            ExpiresUtc = DateTime.UtcNow.AddMinutes(10)
                        };
                    }
                    result.ContinuationPoint = System.Text.Encoding.UTF8.GetBytes(continuationKey);
                }
                else if (continuationKey is not null)
                {
                    lock (Lock)
                    {
                        _continuations.Remove(continuationKey);
                    }
                    result.ContinuationPoint = null;
                }

                var rowCount = ((result.HistoryData?.Body as HistoryData)?.DataValues?.Count) ?? 0;
                Console.WriteLine($"[HistoryRead] Completed. status={result.StatusCode}, rows={rowCount}, continuation={(result.ContinuationPoint is { Length: > 0 } ? "yes" : "no")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HistoryRead] Exception for node {nodesToRead[i].NodeId}: {ex.GetType().Name} - {ex.Message}");
                results[i].StatusCode = StatusCodes.BadUnexpectedError;
                errors[i] = new ServiceResult(StatusCodes.BadUnexpectedError, ex.Message);
            }
        }

        CleanupExpiredContinuationPoints();
    }

    private void CleanupExpiredContinuationPoints()
    {
        var now = DateTime.UtcNow;
        lock (Lock)
        {
            var expired = _continuations.Where(kv => kv.Value.ExpiresUtc <= now).Select(kv => kv.Key).ToList();
            foreach (var key in expired) _continuations.Remove(key);
        }
    }
}

internal sealed class HistoryContinuationState
{
    public long Offset { get; set; }
    public string Signal { get; set; } = string.Empty;
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
}
