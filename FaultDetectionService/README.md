# Wind Fault Detection Service

## Overview

Hybrid-ready Python ML service for integration with a .NET system.

---

## Setup the virtual envirnoment / run in terminal one by one

# 1. Create virtual environment

1:
cd faultDetection_service
2:
python -m venv .venv
"or"
py -m venv .venv

# 2. Activate it

1:
.venv\Scripts\Activate

# 3. Install dependencies

2:
pip install -r requirements.txt

# 4. Run the server

1:
python -m uvicorn app.main:app --reload
"or"
py -m uvicorn app.main:app --reload

---

## Run - You need to do this every time you open VS

1:
cd faultDetection_service
2:
.venv\Scripts\Activate
3:
python -m uvicorn app.main:app --reload
"or"
py -m uvicorn app.main:app --reload

---

## Endpoints

- POST /train → train or retrain a turbine model from batch rows sent by C#
- POST /predict → perform prediction and evaluate fault/alarm state

---

## Training

- .NET sends batch turbine data to /train
- Each turbine is trained independently
- Models are stored per turbine in:

artifacts/<turbine_id>/

Supports:

- custom targetColumn
- custom featureColumns
- forced retraining (forceRetrain)

---

## Prediction

- Loads model based on turbineId
- Predicts expected value
- Computes residual:

residual = actual − predicted

- Applies EWMA for anomaly detection
- Generates alarm levels:
  - A1 → threshold exceeded
  - A2 → repeated A1 triggers

---

## Configuration

Controlled via JSON settings:

- target column
- feature columns
- model type (kNN + Bagging)
- training parameters

---

## Integration with .NET

- C# service collects turbine data from database
- Uses RunTrainingForTurbineAsync to send data
- Data is serialized as JSON and sent to /train
- Response includes model status and metrics

---

## Notes

- Models are stored per turbine (artifacts/<turbine_id>/)
- Unknown turbines are registered automatically
- Training is separated from prediction to avoid blocking real-time operations
- Minimum row requirement applies for training
- Training and prediction must use consistent feature columns

---

## Summary

- API-based ML service for wind turbine monitoring
- Supports multi-turbine model training
- Uses residual-based fault detection (Fluids 2022 approach)
- Designed for real-time integration with .NET
