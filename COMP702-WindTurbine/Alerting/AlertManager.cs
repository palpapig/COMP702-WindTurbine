using COMP702_WindTurbine.Models;
using COMP702_WindTurbine.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace COMP702_WindTurbine.Alerting;

public sealed class AlertManager
{
    private readonly ILogger<AlertManager> _logger;
    private readonly TimeSpan _autoClearAfter;

    public AlertManager(ILogger<AlertManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        var hours = configuration.GetValue<int?>("Monitoring:AlertAutoClearHours") ?? 24;
        _autoClearAfter = TimeSpan.FromHours(hours);
    }

    public async Task<int> ProcessVibrationAlertAsync(
        MonitoringDbContext db,
        string turbineId,
        DateTime nowUtc,
        double vibration,
        CancellationToken cancellationToken)
    {
        const string alertType = "HighVibration";
        var openAlert = await db.Alerts
            .Where(a => a.TurbineId == turbineId
                && a.Type == alertType
                && (a.Status == "Active" || a.Status == "Acknowledged"))
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        if (vibration > 8.0)
        {
            if (openAlert is null)
            {
                db.Alerts.Add(new Alert
                {
                    TurbineId = turbineId,
                    Timestamp = nowUtc,
                    Type = alertType,
                    Value = vibration,
                    Severity = "Critical",
                    Status = "Active",
                    UpdatedAt = nowUtc
                });
                _logger.LogWarning(
                    "Alert activated for {TurbineId}: {Type} value={Value}",
                    turbineId,
                    alertType,
                    vibration);
                return 1;
            }

            openAlert.Value = vibration;
            openAlert.UpdatedAt = nowUtc;
            return 0;
        }

        if (openAlert is not null)
        {
            openAlert.Status = "Resolved";
            openAlert.ResolvedAt = nowUtc;
            openAlert.UpdatedAt = nowUtc;
            _logger.LogInformation("Alert resolved for {TurbineId}: {Type}", turbineId, alertType);
        }

        await AutoClearResolvedAlertsAsync(db, nowUtc, cancellationToken);
        return 0;
    }

    public Task EvaluateAsync(PredictionResult prediction, CancellationToken cancellationToken)
    {
        if (prediction.IsAnomaly)
        {
            _logger.LogWarning(
                "Anomaly detected for {TurbineId} at {Timestamp}. Reason: {Reason}",
                prediction.TurbineId,
                prediction.Timestamp,
                prediction.Reason);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> TryAcknowledgeAsync(
        MonitoringDbContext db,
        long alertId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var alert = await db.Alerts.FirstOrDefaultAsync(a => a.Id == alertId, cancellationToken);
        if (alert is null || alert.Status != "Active")
        {
            return false;
        }

        alert.Status = "Acknowledged";
        alert.AcknowledgedAt = nowUtc;
        alert.UpdatedAt = nowUtc;
        return true;
    }

    private async Task AutoClearResolvedAlertsAsync(
        MonitoringDbContext db,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var clearBefore = nowUtc - _autoClearAfter;
        var toClear = await db.Alerts
            .Where(a => a.Status == "Resolved"
                && a.ResolvedAt.HasValue
                && a.ResolvedAt.Value <= clearBefore)
            .ToListAsync(cancellationToken);

        foreach (var alert in toClear)
        {
            alert.Status = "Cleared";
            alert.ClearedAt = nowUtc;
            alert.UpdatedAt = nowUtc;
        }
    }
}
