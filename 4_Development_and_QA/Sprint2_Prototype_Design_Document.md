# Sprint 2 Prototype Design Document

## 1. Purpose
- Sprint 2 prototype integrates monitoring pipeline, dashboard visualization, benchmarking persistence, and fault-detection service connection.
- Prototype focus is end-to-end demonstrability across ingestion, analysis, storage, and presentation.

## 2. Scope of the Prototype
- Multi-page frontend monitoring interface is implemented in `frontend/wind-dashboard`.
- Backend benchmarking-related data structures and persistence paths are expanded.
- Python fault-detection service is added under `faultDetection_service` and connected to C# prediction flow.
- Model training/retraining-related configuration and scheduling components are present in C# training modules.

## 3. Components and Interactions

### 3.1 Frontend Monitoring Interface
- UI includes page-based structure (Dashboard, Tables, Config, Export).
- Dashboard-level visualization components are added for turbine monitoring and analysis views.
- Frontend reads data from configured backend/Supabase-connected paths used in the project.

### 3.2 Benchmarking and Data Persistence
- Backend contains additional benchmarking entities and related schema updates.
- Data persistence covers telemetry history plus benchmarking-oriented outputs.
- Benchmarking-related service logic is included in backend service layer.

### 3.3 Fault Detection Integration
- Python service includes prediction/training API modules and service-layer logic.
- C# includes Python prediction integration through dedicated prediction engine code.
- Fault-detection project includes configuration, training scripts, and test files.

### 3.4 Training Workflow Components
- C# training module includes training config, request/response models, and scheduling service files.
- Training-related settings file is present for runtime configuration.
- Branch history shows retraining automation work included in Sprint 2 development stream.

## 4. Data Flow
- Telemetry data is ingested by monitoring backend pipeline.
- Backend processing writes telemetry and benchmarking-related results to persistence layer.
- Prediction/training interactions are routed to Python fault-detection service.
- Service outputs are available to backend and frontend monitoring views.

## 5. User Experience (Current Prototype)
- Users can navigate multiple monitoring pages rather than a single view.
- Users can inspect latest and historical turbine-related information through UI components.
- Prototype supports demonstration of monitoring + analysis + fault-detection integration in one flow.

## 6. Sprint 2 Branch Evidence (Objective Summary)
- `dashboard-sprint2`: dashboard structure and monitoring UI expansion.
- `benchmarkingGraphs-sprint2`: includes dashboard branch work plus benchmarking graph additions.
- `benchmarking`: benchmarking entities/migrations/service-level persistence work.
- `faultDetection-trainingAutomation`: Python fault-detection service and .NET integration baseline.
- `faultDetection-prt-3`: additional feature-selection/training-scheduling related development.

## 7. Observed Repository Notes
- `dashboard-sprint2` is an ancestor of `benchmarkingGraphs-sprint2`.
- `faultDetection-prt-2` and `faultDetection-trainingAutomation` are ancestors of `faultDetection-prt-3`.
- Some branches include non-functional/noisy commit content (for example dependency/vendor files), which is separate from functional prototype scope.