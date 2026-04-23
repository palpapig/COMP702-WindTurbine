# Wind Fault Detection Service

## Overview

Hybrid-ready Python ML service for integration with a .NET system.

---

## Setup

### Python Environment

Create virtual environment:
cd faultDetection_service
python -m venv .venv

Activate environment (Windows):
.venv\Scripts\activate

Install dependencies:
pip install -r requirements.txt

---

## Endpoints

- POST /train → train or retrain a turbine model from batch rows sent by C#
- POST /predict → perform prediction and evaluate fault/alarm state

---

## Run

Start the API:
uvicorn app.main:app --reload

Open in browser:
http://127.0.0.1:8000/docs

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
