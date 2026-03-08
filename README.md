# COMP702 Wind Turbine Monitoring

.NET 8 Worker Service for wind turbine monitoring.

## Current architecture

Monitoring loop:

Input -> DataFormatter -> Prediction -> Alert Lifecycle -> Database

Main components:
- Worker: `Workers/MonitoringWorker.cs`
- Alert lifecycle: `Alerting/AlertManager.cs`
- EF Core DbContext: `Infrastructure/MonitoringDbContext.cs`
- Models: `Models/*`

## Database tables

- Turbines
- TelemetryHistories
- Alerts
- WorkerStatuses
- WorkerMetrics

## Alert lifecycle

`Active -> Acknowledged -> Resolved -> Cleared`

Current worker behavior:
- Auto create `Active` when vibration > 8
- Auto resolve when vibration recovers
- Auto clear resolved alerts after configured hours

## Configuration

`appsettings.json`

- `Monitoring:IntervalSeconds` (default 5)
- `Monitoring:AlertAutoClearHours` (default 24)

## Run

```bash
dotnet build
dotnet run --project COMP702-WindTurbine
```

## Team collaboration

- Use feature branches: `feature/<name>`
- Open PRs to `main`
- Keep PRs focused and small
