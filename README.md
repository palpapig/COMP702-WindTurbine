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

```mermaid
flowchart TD
    A[MonitoringWorker Start] --> B[Read Config and DI<br/>IntervalSeconds Logger ScopeFactory]
    B --> C{{while not cancelled}}
    C --> D[Start Cycle<br/>startedAt UTC stopwatch]
    D --> E[Generate Mock Telemetry<br/>TurbineId WindSpeed RotorSpeed PowerOutput Vibration Temperature]

    E --> F[Upsert Turbine]
    F -->|DB write| DB1[(Turbines<br/>INSERT if missing<br/>UPDATE LastTelemetryTime Status)]

    F --> G[Insert TelemetryHistory]
    G -->|DB write| DB2[(TelemetryHistories<br/>INSERT)]

    G --> H[Alert Lifecycle in AlertManager<br/>Rule Vibration > 8]
    H -->|DB write| DB3[(Alerts<br/>INSERT Active<br/>UPDATE to Resolved<br/>UPDATE to Cleared)]

    H --> I[Update WorkerStatus<br/>worker-01 heartbeat]
    I -->|DB write| DB4[(WorkerStatuses<br/>UPSERT)]

    I --> J[Insert WorkerMetrics<br/>Signals Alarms Latency]
    J -->|DB write| DB5[(WorkerMetrics<br/>INSERT)]

    J --> K[SaveChangesAsync]
    K --> L[Structured Log<br/>Telemetry processed for turbine TurbineId]
    L --> M[Delay IntervalSeconds]
    M --> C
```

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

# training & Testing (Quick Guide)

1. Add Raw Data

Put CSV files in:

faultDetection_service/training/rawData/ 2. Clean Data

Run:

python trainingDataCleaning.py

Cleaned files will be saved in:

faultDetection_service/training/cleanData/ 3. Train Model

Run:

py -m training.Training_Model_Pipeline

Model will be saved in:

artifacts/<TURBINE_ID>/model.pkl 4. Test Prediction

Run:

python scripts/test_prediction.py

This will load the model and test it on sample data.
