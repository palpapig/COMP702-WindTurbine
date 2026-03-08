using COMP702_WindTurbine.Alerting;
using COMP702_WindTurbine.DataSources;
using COMP702_WindTurbine.Infrastructure;
using COMP702_WindTurbine.Processing;
using COMP702_WindTurbine.Prediction;

namespace COMP702_WindTurbine.Pipeline;

public sealed class PipelineOrchestrator
{
    private readonly IDataSource _dataSource;
    private readonly IDataFormatter _formatter;
    private readonly IPredictionEngine _predictionEngine;
    private readonly AlertManager _alertManager;
    private readonly MetricsCollector _metrics;
    private readonly HealthService _health;
    private readonly ILogger<PipelineOrchestrator> _logger;

    public PipelineOrchestrator(
        IDataSource dataSource,
        IDataFormatter formatter,
        IPredictionEngine predictionEngine,
        AlertManager alertManager,
        MetricsCollector metrics,
        HealthService health,
        ILogger<PipelineOrchestrator> logger)
    {
        _dataSource = dataSource;
        _formatter = formatter;
        _predictionEngine = predictionEngine;
        _alertManager = alertManager;
        _metrics = metrics;
        _health = health;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var raw = await _dataSource.FetchAsync(cancellationToken);
        _health.UpdateLastDataTimestamp(raw.Timestamp);

        var processed = await _formatter.FormatAsync(raw, cancellationToken);

        var prediction = await _predictionEngine.PredictAsync(processed, cancellationToken);

        await _alertManager.EvaluateAsync(prediction, cancellationToken);

        _metrics.IncrementSignalsProcessed();
        _logger.LogInformation(
            "Pipeline completed for {TurbineId}. Healthy={IsHealthy}, signals={Signals}, alarms={Alarms}",
            raw.TurbineId,
            _health.IsHealthy(),
            _metrics.SignalsProcessed,
            _metrics.AlarmsTriggered);
    }
}
