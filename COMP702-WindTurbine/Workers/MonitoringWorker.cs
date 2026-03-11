using System.Diagnostics;
using COMP702_WindTurbine.Alerting;
using COMP702_WindTurbine.Infrastructure;
using COMP702_WindTurbine.Models;
using Microsoft.EntityFrameworkCore;

namespace COMP702_WindTurbine.Workers;

public sealed class MonitoringWorker : BackgroundService
{
    private const string WorkerId = "worker-01";
    private readonly ILogger<MonitoringWorker> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AlertManager _alertManager;
    private readonly TimeSpan _interval;
    private readonly Random _random = new();

    public MonitoringWorker(
        ILogger<MonitoringWorker> logger,
        IServiceScopeFactory scopeFactory,
        AlertManager alertManager,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _alertManager = alertManager;
        _interval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Monitoring:IntervalSeconds") ?? 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Monitoring worker started with interval {IntervalSeconds}s", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var startedAt = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();
            var alarmsTriggered = 0;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MonitoringDbContext>();

                var turbineId = $"WT-{_random.Next(1, 6):00}";
                var telemetry = new TelemetryHistory
                {
                    TurbineId = turbineId,
                    Timestamp = startedAt,
                    WindSpeed = Math.Round(3 + _random.NextDouble() * 22, 2),
                    RotorSpeed = Math.Round(5 + _random.NextDouble() * 30, 2),
                    PowerOutput = Math.Round(_random.NextDouble() * 5000, 2),
                    Vibration = Math.Round(_random.NextDouble() * 12, 2),
                    Temperature = Math.Round(35 + _random.NextDouble() * 70, 2)
                };

                var turbine = await db.Turbines.FirstOrDefaultAsync(t => t.TurbineId == turbineId, stoppingToken);
                if (turbine is null)
                {
                    turbine = new Turbine
                    {
                        TurbineId = turbineId,
                        Name = $"Turbine {turbineId}",
                        Location = "Site-A",
                        Status = "Running"
                    };
                    db.Turbines.Add(turbine);
                }

                db.TelemetryHistories.Add(telemetry);

                alarmsTriggered = await _alertManager.ProcessVibrationAlertAsync(
                    db,
                    turbineId,
                    startedAt,
                    telemetry.Vibration ?? 0,
                    stoppingToken);
                turbine.Status = alarmsTriggered > 0 || telemetry.Vibration > 8.0 ? "Alarm" : "Running";

                turbine.LastTelemetryTime = startedAt;

                var workerStatus = await db.WorkerStatuses.FirstOrDefaultAsync(w => w.WorkerId == WorkerId, stoppingToken);
                if (workerStatus is null)
                {
                    workerStatus = new WorkerStatus { WorkerId = WorkerId, Status = "Healthy" };
                    db.WorkerStatuses.Add(workerStatus);
                }
                workerStatus.LastHeartbeat = DateTime.UtcNow;
                workerStatus.LastDataFetchTime = startedAt;
                workerStatus.Status = "Healthy";
                workerStatus.LastError = null;

                stopwatch.Stop();
                db.WorkerMetrics.Add(new WorkerMetrics
                {
                    WorkerId = WorkerId,
                    Timestamp = DateTime.UtcNow,
                    SignalsProcessed = 1,
                    AlarmsTriggered = alarmsTriggered,
                    PipelineLatencyMs = stopwatch.Elapsed.TotalMilliseconds
                });

                await db.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Telemetry processed for turbine {TurbineId}", turbineId);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Monitoring cycle failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
