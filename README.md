# User Manual / README

**For Dimension Software**

This document explains how to set up, configure, and run the complete Wind Turbine Monitoring System.

**Last Updated:** May 2026 (based on code in the `sprint-3-merged` branch)

---

# Table of Contents

1. [System Overview](#system-overview)
2. [Repository Structure](#repository-structure)
3. [Prerequisites](#prerequisites)
4. [Database Setup (Supabase PostgreSQL)](#database-setup-supabase-postgresql)
5. [Environment Variables & Configuration](#environment-variables--configuration)
6. [Running the Simulator (OPC UA Data Source)](#running-the-simulator-opc-ua-data-source)
7. [Running the .NET Worker (Core Processing)](#running-the-net-worker-core-processing)
8. [Running the Python Training Environment (Failure Detection & Degradation)](#running-the-python-training-environment-failure-detection--degradation)
9. [Running the React Frontend Dashboard](#running-the-react-frontend-dashboard)
10. [Retraining the Failure Detection Model (kNN Regressor)](#retraining-the-failure-detection-model-knn-regressor)
11. [Troubleshooting](#troubleshooting)
12. [Appendix: File Paths Reference](#appendix-file-paths-reference)

---

# System Overview

The Wind Turbine Monitoring System monitors wind turbine performance, detects faults, and benchmarks efficiency over time.

The system consists of four main components:

| Component                   | Technology                | Role                                                                                              |
| --------------------------- | ------------------------- | ------------------------------------------------------------------------------------------------- |
| OPC UA Simulator            | .NET 8 Console            | Replays historical CSV data as live OPC UA tags                                                   |
| .NET Worker                 | .NET 8 Background Service | Fetches live OPC UA data, runs ONNX models, computes alarms, and stores results in Supabase       |
| Python Training Environment | Python 3.10+              | Trains failure detection (kNN) and degradation (SVR) models; exports ONNX models                  |
| React Frontend              | React 19 + Vite           | Displays telemetry, benchmarking, degradation, failure detection, and alert management dashboards |

## Architecture Notes

- The worker uses an ONNX model (kNN regressor) exported from Python for failure prediction.
- Degradation analysis uses separate SVR models trained via a Python script that is called directly from the worker (system Python, not a virtual environment).
- Data is persisted in a Supabase PostgreSQL database.
- The frontend connects directly to Supabase and does not require a backend API.

## Architecture (Brief)

### How the Windows Service Connects to Database and Interface

- The Windows/.NET Worker reads the PostgreSQL connection string from `ConnectionStrings__MonitoringDb` (preferred) or `Worker/appsettings.json`.
- On startup, the Worker initializes EF Core and applies migrations automatically.
- During runtime, the Worker ingests telemetry, computes analytics, and writes results to Supabase tables.
- The frontend connects to Supabase directly and reads those tables for dashboards, tables, exports, and alerts.

```mermaid
flowchart LR
    A[OPC UA Simulator] -->|Live telemetry| B[Windows Worker Service (.NET)]
    B -->|ConnectionStrings__MonitoringDb + EF Core| C[(Supabase PostgreSQL)]
    C -->|Read queries| D[Frontend Interface (React)]
```

### Feature Descriptions

- **Failure Detection:** Uses an ONNX kNN model to predict gearbox oil temperature, then applies residual/EWMA logic to raise A1/A2 alarms.
- **Benchmarking:** Compares actual turbine power output against expected baseline behavior to quantify performance deviation.
- **Degradation Analysis:** Uses SVR-based models to evaluate degradation trends (for example Region 2 / 2.5 behavior) over time.

---

# Repository Structure

```text
COMP702-WindTurbine/
├── COMP702-WindTurbine.Api/          # Unused WebAPI stub
├── Simulator/
│   ├── config/simulator.settings.json
│   ├── data/turbine4_2017-2022.csv
│   └── server/Simulator.Server/      # OPC UA server console project
├── Worker/                            # Main .NET worker service
│   ├── appsettings.json
│   ├── data/turbine1_clean.csv
│   ├── TrainedModel/                  # ONNX model & metadata (kNN failure detection)
│   ├── PythonDegradationTraining/     # SVR training script & output models
│   ├── services/                      # Benchmarker, FailureDetection, DegradationAnalyser, etc.
│   ├── Workers/MonitoringWorker.cs
│   ├── Migrations/                    # EF Core migrations
│   └── Worker.csproj                  # Single project file (after cleanup)
├── faultDetection_service/            # Python training for failure detection
│   ├── app/
│   │   ├── config/model_settings.json
│   │   └── ml/                        # Feature config, model registry (unused in production)
│   ├── training/model_trainer.py      # Trains kNN and exports ONNX
│   ├── requirements.txt
│   └── artifacts/final_Model_converted/
├── frontend/
│   ├── src/
│   │   ├── components/                # BenchmarkGraphs, FailureDetectionGraph, Tables, etc.
│   │   ├── pages/                     # DashboardPage, ExportPage, AlertsPage
│   │   └── utils/supabase.js
│   ├── package.json
│   └── vite.config.js
├── test/HistoryReadClient/            # Utility for testing OPC UA history reads
└── .gitignore
```

# Prerequisites

Before running the system, ensure the following software is installed:

- .NET 8 SDK
- Python 3.10 or 3.11 (Python 3.12 is not fully tested)
- Node.js 20+ and npm
- PostgreSQL database (Supabase is used; free tier sufficient)
- Git

> **Note:** Required database tables are created automatically through Entity Framework Core migrations.

---

# Database Setup (Supabase PostgreSQL)

The worker stores:

- Telemetry data
- Benchmarking results
- Failure detection results
- Alarm records

All data is stored in a PostgreSQL database hosted on Supabase.

## Steps

### 1. Create a Supabase Project

Create a new Supabase project or use an existing one.

### 2. Obtain the Database Connection String

Navigate to:

```text
Supabase Dashboard
→ Project Settings
→ Database
→ Connection String
→ URI
```

Example:

```text
Host=aws-0-...;
Port=5432;
Database=postgres;
Username=...;
Password=...;
SSL Mode=Require;
Trust Server Certificate=true
```

### 3. Configure the Connection String

#### Option A (Recommended)

Set an environment variable:

```text
ConnectionStrings__MonitoringDb
```

#### Option B

Update the connection string directly in:

```text
Worker/appsettings.json
```

Under:

```json
{
  "ConnectionStrings": {
    "MonitoringDb": "..."
  }
}
```

> The connection string committed in the repository is intended for development only. Replace it with your own database credentials.

---

# Environment Variables & Configuration

The worker loads configuration from:

```text
Worker/appsettings.json
```

## Key Configuration Sections

| Section                    | Purpose                                                             |
| -------------------------- | ------------------------------------------------------------------- |
| `DataSource:Type`          | Selects data source (`OpcUaSimulator` or `Simulated`)               |
| `OpcUaDataSource`          | OPC UA endpoint, turbine ID, and node mappings                      |
| `SimulatedDataSource`      | Test mode (`Replay` or `Generative`)                                |
| `FailureDetectionSettings` | ONNX model path, EWMA settings, control limits, and residual biases |

## Environment Variables

No additional environment variables are required unless overriding the database connection string.

---

# Running the Simulator (OPC UA Data Source)

The simulator replays historical CSV data as live OPC UA telemetry. It also provides a historical data interface used by the worker’s `PlaceholderHistoricalDataSource`.

## Start the Simulator

```bash
cd Simulator/server/Simulator.Server
dotnet run
```

## What the Simulator Does

- Starts an OPC UA endpoint (default `opc.tcp://localhost:4840`)
- Publishes live values every 2 seconds (configurable in `simulator.settings.json`)
- Automatically maps CSV columns to canonical OPC node names (e.g., `WindSpeed`, `Power`) for consistency with the worker’s expectations

Configuration file:

```text
Simulator/config/simulator.settings.json
```

## Expected Output

```text
[OPC] Endpoint started: opc.tcp://localhost:4840
[Heartbeat] ... live=srcTs=..., points=...
[LiveValues] WindSpeed=... Power=... RotorSpeed=...
```

## Important

Keep the simulator terminal running while the worker is running.

---

# Running the .NET Worker (Core Processing)

The worker continuously:

- Reads live turbine data (from OPC UA or simulated source)
- Runs failure detection inference using an ONNX model (kNN regressor)
- Calculates residuals
- Triggers A1 and A2 alarms (EWMA-based)
- Stores results in Supabase
- Executes benchmarking and degradation analysis on a schedule

## Build and Run

```bash
cd Worker
dotnet build
dotnet run --project Worker.csproj
```

## Database Migrations

Database migrations are applied automatically during startup (see `Program.cs`).

## Monitoring Interval

The worker processes data every 30 seconds (hardcoded in `MonitoringWorker.cs`).

## Verify Successful Operation

Watch the console for messages such as:

```text
Processing new data started
Pipeline complete. id: ...
```

Verify records are being created in the following tables:

- `TurbineData`
- `FailureDetectionResult`
- `BenchmarkResult`
- `DegradationResult`

## Common Startup Error

If the worker reports:

```text
Missing database connection string
```

Configure `ConnectionStrings__MonitoringDb` as described above.

---

# Running the Python Training Environment (Failure Detection & Degradation)

The project includes two separate training systems.

## Failure Detection Training

**Location:** `faultDetection_service/`

**Model:** kNN regressor (the `BaggingRegressor` is not used because it cannot be converted to ONNX). The pipeline consists of a `StandardScaler` followed by a `KNeighborsRegressor`.

**Purpose:** Predict Gearbox Oil Temperature and export an ONNX model.

---

## Degradation Training

**Location:** `Worker/PythonDegradationTraining/`

**Model:** Support Vector Regression (SVR)

**Purpose:** Train degradation models for:

- Region 2 (`GeneratorSpeed` vs `Power`)
- Region 2.5 (`PitchAngle` vs `Power`)

---

## Create a Python Virtual Environment

```bash
cd faultDetection_service
python -m venv .venv
.venv\Scripts\activate
```

## Install Dependencies

```bash
pip install -r requirements.txt
```

Key dependencies include:

- pandas
- scikit-learn
- onnx
- onnxruntime
- skl2onnx
- matplotlib
- joblib
- fastapi
- uvicorn

> **Note:** The FastAPI server (formerly used for prediction) is no longer active. Only the training scripts are used.

## Python Executable Paths

`PythonProcessService.cs` (starts the optional FastAPI service) uses a hardcoded path to the virtual environment’s `python.exe`. This service is not used in the current production pipeline.

`DegradationAnalyser.cs` (which trains the SVR models) calls `python` directly and relies on the system PATH.

Therefore:

- Ensure Python is available in your PATH.
- Ensure the required packages are installed in that Python environment.

If you need to run the optional FastAPI service, adjust the path in `PythonProcessService.cs` to match your virtual environment location.

---

## Testing Failure Detection Training

```bash
cd faultDetection_service
.venv\Scripts\activate
py -m training.model_trainer
```

### Output

The trainer:

1. Reads CSV files from `data/trainingReady/`
2. Trains a kNN model (scaled data)
3. Generates:

```text
artifacts/final_Model_converted/model.onnx
artifacts/final_Model_converted/metadata.json
```

### Copy to Worker

```bash
cp artifacts/final_Model_converted/model.onnx ../Worker/TrainedModel/model.onnx
cp artifacts/final_Model_converted/metadata.json ../Worker/TrainedModel/metadata.json
```

---

## Testing Degradation Training

```bash
cd Worker
python PythonDegradationTraining/DegradationTraining.py <data_csv> <output_model.onnx> <model_name>
```

The script also produces CSV files (actual vs predicted) and charts inside:

```text
PythonDegradationTraining/outputs/
```

for manual validation.

---

# Running the React Frontend Dashboard

The frontend displays:

- Telemetry
- Benchmarking results
- Failure detection charts
- Degradation analysis
- Alarm management

## Start the Frontend

```bash
cd frontend
npm install
npm run dev
```

Open:

```text
http://localhost:5173
```

## Key Features

### Dashboard

- Power difference vs wind speed
- Actual vs predicted temperature
- Turbine selector

### Tables

- Current turbine status (most recent telemetry per turbine)
- Complete telemetry history

### Export

Download CSV exports for:

- Telemetry
- Benchmark results
- Fault detection alerts (attempts table `Alerts`; if that fails, falls back to `Alert`)

### Alerts

- View A1/A2 alarms
- Acknowledge alarms

## Supabase Configuration

Frontend configuration is located in:

```text
frontend/src/utils/supabase.js
```

If the Supabase URL or anonymous key changes, update this file accordingly.

---

# Retraining the Failure Detection Model (kNN Regressor)

Retrain the model whenever new sensor data becomes available.

## Step 1: Prepare Training Data

Place CSV files in:

```text
faultDetection_service/data/trainingReady/
```

Required columns (as defined in `model_settings.json`):

- RotorSpeed
- GearOilInletTemp
- GeneratorBearingFrontTemp
- RearBearingTemp
- GearOilInletPressure
- NacelleTemp
- GearboxOilTemp (target)

The feature order in the CSV does not matter, but the order used for prediction in .NET must match the order used during training.

See warning below.

## Step 2: Run Training

```bash
cd faultDetection_service
.venv\Scripts\activate
py -m training.model_trainer
```

### Output Files

```text
artifacts/final_Model_converted/model.onnx
artifacts/final_Model_converted/metadata.json
```

## Step 3: Copy Model Files

```bash
cp artifacts/final_Model_converted/model.onnx ../Worker/TrainedModel/model.onnx
cp artifacts/final_Model_converted/metadata.json ../Worker/TrainedModel/metadata.json
```

## Step 4: Restart the Worker

Restart the worker to load the newly trained ONNX model.

## Important Warning

The feature order in the ONNX model must match the order expected by `FailureDetection.cs`.

In `Worker/services/FailureDetection.cs` (lines 35–43), the feature array is hardcoded:

```csharp
private readonly string[] featureColumns =
{
    "RotorSpeed",
    "GearOilInletTemp",
    "GeneratorBearingFrontTemp",
    "RearBearingTemp",
    "GearOilInletPressure",
    "NacelleTemp"
};
```

If you modify the feature columns in `model_settings.json`, you must update this array in the C# code exactly in the same order and rebuild the worker.

---

# Troubleshooting

| Problem                                       | Likely Cause                               | Solution                                                                 |
| --------------------------------------------- | ------------------------------------------ | ------------------------------------------------------------------------ |
| Worker fails to start with database error     | Missing or invalid connection string       | Configure `ConnectionStrings__MonitoringDb` or update `appsettings.json` |
| ONNX model not found                          | Missing `model.onnx` file                  | Run training and copy model into `Worker/TrainedModel/`                  |
| OPC UA connection refused                     | Simulator not running                      | Start `Simulator.Server` first                                           |
| Python subprocess fails (DegradationAnalyser) | Python not in PATH or missing dependencies | Ensure system Python has scikit-learn, pandas, etc. installed            |
| Frontend shows no data                        | Supabase permissions or RLS issue          | Configure development permissions or Row Level Security policies         |
| ModuleNotFoundError during training           | Missing dependencies                       | Activate virtual environment and run `pip install -r requirements.txt`   |

---

# Appendix: File Paths Reference

| Component                  | Critical Files                                                                               |
| -------------------------- | -------------------------------------------------------------------------------------------- |
| Simulator                  | `Simulator/server/Simulator.Server/Program.cs`<br>`Simulator/config/simulator.settings.json` |
| Worker Configuration       | `Worker/appsettings.json`                                                                    |
| Worker ONNX Model          | `Worker/TrainedModel/model.onnx`                                                             |
| Failure Detection Training | `faultDetection_service/training/model_trainer.py`                                           |
| Degradation Training       | `Worker/PythonDegradationTraining/DegradationTraining.py`                                    |
| Frontend Supabase Client   | `frontend/src/utils/supabase.js`                                                             |
| Database Migrations        | `Worker/Migrations/*.cs`                                                                     |

---

# Additional Notes for Handover

- Start the Simulator before starting the Worker.
- Ensure Supabase is configured and accessible.
- The frontend depends entirely on data being written by the worker.
- The ONNX model must exist in `Worker/TrainedModel/` before failure detection can function.
- Python environment:
  - The optional FastAPI service (not used) expects a virtual environment at `faultDetection_service/.venv`.
  - Degradation training (SVR) uses the system Python. Ensure it has the required packages installed.
- Code cleanup:
  - Before final build, ensure that duplicate project files (`Worker.csproj` and `COMP702-WindTurbine.csproj`) have been resolved.
  - Keep only `Worker.csproj`.
  - Remove any merge conflict markers.
- Database migrations are applied automatically during worker startup.
