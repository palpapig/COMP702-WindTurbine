using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

var endpointUrl = "opc.tcp://localhost:4840";
const string namespaceUri = "urn:comp702:windturbine:simulator";

var pkiRoot = Path.Combine(AppContext.BaseDirectory, "pki");
Directory.CreateDirectory(pkiRoot);

var config = new ApplicationConfiguration
{
    ApplicationName = "HistoryReadClient",
    ApplicationUri = $"urn:{Utils.GetHostName()}:HistoryReadClient",
    ApplicationType = ApplicationType.Client,
    SecurityConfiguration = new SecurityConfiguration
    {
        ApplicationCertificate = new CertificateIdentifier
        {
            StoreType = CertificateStoreType.Directory,
            StorePath = Path.Combine(pkiRoot, "own"),
            SubjectName = "CN=HistoryReadClient, O=COMP702"
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
    TransportConfigurations = new TransportConfigurationCollection(),
    TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
};
await config.Validate(ApplicationType.Client);

var app = new ApplicationInstance
{
    ApplicationName = config.ApplicationName,
    ApplicationType = ApplicationType.Client,
    ApplicationConfiguration = config
};
await app.CheckApplicationInstanceCertificate(false, 2048);

var selectedEndpoint = CoreClientUtils.SelectEndpoint(endpointUrl, useSecurity: false);
var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(config));
using var session = await Session.Create(config, endpoint, false, "HistoryReadClient", 60000, null, null);

Console.WriteLine($"Connected: {endpointUrl}");

var nsIndex = session.NamespaceUris.GetIndex(namespaceUri);
if (nsIndex < 0)
{
    Console.WriteLine($"Namespace URI not found on server: {namespaceUri}");
    return;
}
var rootNode = new NodeId(ObjectIds.ObjectsFolder);
NodeId? turbinesNode = null;
NodeId? wtNode = null;
NodeId? windSpeedNode = null;

ReferenceDescriptionCollection refs;
byte[] cp;
session.Browse(
    null, null, rootNode, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
    (uint)(NodeClass.Object | NodeClass.Variable), out cp, out refs);
turbinesNode = ExpandedNodeId.ToNodeId(refs.FirstOrDefault(r => r.BrowseName.Name == "Turbines")?.NodeId, session.NamespaceUris);
if (turbinesNode is null) { Console.WriteLine("Browse failed: Turbines not found."); return; }

session.Browse(
    null, null, turbinesNode, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
    (uint)(NodeClass.Object | NodeClass.Variable), out cp, out refs);
wtNode = ExpandedNodeId.ToNodeId(refs.FirstOrDefault(r => r.BrowseName.Name == "WT-004")?.NodeId, session.NamespaceUris);
if (wtNode is null) { Console.WriteLine("Browse failed: WT-004 not found."); return; }

session.Browse(
    null, null, wtNode, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
    (uint)(NodeClass.Object | NodeClass.Variable), out cp, out refs);
windSpeedNode = ExpandedNodeId.ToNodeId(refs.FirstOrDefault(r => r.BrowseName.Name == "WindSpeed")?.NodeId, session.NamespaceUris);
if (windSpeedNode is null) { Console.WriteLine("Browse failed: WindSpeed not found."); return; }
var nodeId = windSpeedNode;
Console.WriteLine($"Resolved NodeId: {nodeId}");

var details = new ReadRawModifiedDetails
{
    IsReadModified = false,
    StartTime = new DateTime(2019, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    EndTime = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    NumValuesPerNode = 200,
    ReturnBounds = false
};

var nodesToRead = new HistoryReadValueIdCollection
{
    new HistoryReadValueId { NodeId = nodeId }
};

byte[]? continuation = null;
var page = 0;
var total = 0;
const int maxPages = 10000;

while (true)
{
    if (page >= maxPages)
    {
        Console.WriteLine($"Stopped at maxPages={maxPages} for safety.");
        break;
    }

    if (continuation is not null)
    {
        nodesToRead[0].ContinuationPoint = continuation;
    }

    session.HistoryRead(
        null,
        new ExtensionObject(details),
        TimestampsToReturn.Source,
        releaseContinuationPoints: false,
        nodesToRead,
        out var results,
        out var diagnostics);

    Console.WriteLine($"results.Count={results?.Count ?? 0}, diagnostics.Count={diagnostics?.Count ?? 0}");
    for (var i = 0; i < (results?.Count ?? 0); i++)
    {
        var ri = results[i];
        var hdi = ExtensionObject.ToEncodeable(ri.HistoryData) as HistoryData;
        var rowsi = hdi?.DataValues?.Count ?? 0;
        Console.WriteLine($"result[{i}] status={ri.StatusCode}, rows={rowsi}, cpLen={ri.ContinuationPoint?.Length ?? 0}");
    }

    var result = results[0];
    var historyData = ExtensionObject.ToEncodeable(result.HistoryData) as HistoryData;
    var count = historyData?.DataValues?.Count ?? 0;
    var cpLen = result.ContinuationPoint?.Length ?? 0;
    if (StatusCode.IsBad(result.StatusCode) && count == 0)
    {
        Console.WriteLine($"HistoryRead failed on page {page + 1}: {result.StatusCode}, continuationLen={cpLen}");
        break;
    }

    total += count;
    page++;

    Console.WriteLine($"Page {page}: status={result.StatusCode}, rows={count}, continuationLen={cpLen}");

    continuation = result.ContinuationPoint;
    if (continuation is null || continuation.Length == 0) break;
}

Console.WriteLine($"Done. Total rows={total}");

if (continuation is { Length: > 0 })
{
    nodesToRead[0].ContinuationPoint = continuation;
    session.HistoryRead(
        null,
        new ExtensionObject(details),
        TimestampsToReturn.Source,
        releaseContinuationPoints: true,
        nodesToRead,
        out _,
        out _);
    Console.WriteLine("Released final continuation point.");
}

await session.CloseAsync();
