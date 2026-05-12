# Wind Fault Detection Service

## Overview

This is a failure prediction machine learning service for the wind turbine fault detection project.

The Python service is now used for **training only**. It trains a kNN + Bagging Regressor model to predict the target variable, such as gearbox oil temperature.

Live prediction, residual calculation, EWMA chart logic, A1/A2 alarm evaluation, and saving failure results are now handled in the **.NET project**.

---

## Setup the virtual environment / run in terminal one by one

# 1. Create virtual environment

1:
cd faultDetection_service

2:
python -m venv .venv

"or"

py -m venv .venv

Activate it

3:
.venv\Scripts\Activate

"or for mac"

source .venv/bin/activate

Install dependencies

4:
pip install -r requirements.txt

"or for mac"

python -m pip install -r requirements.txt

Run the server

5:
python -m uvicorn app.main:app --reload

"or"

py -m uvicorn app.main:app --reload

---

## Run - You need to do this every time you open VS

1:
cd faultDetection_service

2:
.venv\Scripts\Activate

"or for mac"

source .venv/bin/activate

3:
python -m uvicorn app.main:app --reload

"or"

py -m uvicorn app.main:app --reload

---

## Endpoints

- POST /train → trains or retrains the machine learning model using batch turbine data from .NET

---

## Training

The Python service is responsible for training the model.

Training process:

- Receives batch turbine data from .NET
- Loads the selected feature columns and target column
- Trains the kNN + Bagging Regressor model
- Evaluates the model using metrics such as RMSE and R²
- Saves the trained model and metadata
- Returns training status and metrics to .NET

---

## Prediction

Prediction is **not handled by Python anymore**.

Prediction is now handled in the .NET project.

The .NET project:

- Loads the trained/exported model
- Predicts the expected target value
- Computes residual:

residual = actual − predicted

- Applies EWMA for anomaly detection
- Generates alarm levels:
  - A1 → threshold exceeded
  - A2 → repeated A1 triggers
- Saves failure detection results to the database

---

## Configuration

Controlled via JSON settings:

- target column
- feature columns
- model type, such as kNN + Bagging
- training parameters
- minimum training rows
- test size
- random state

Important:

- Training and prediction must use the same feature columns.
- Feature order must stay consistent between Python training and .NET prediction.

---

## Integration with .NET

- C# service collects turbine data from the database
- C# sends batch training data to Python
- Python trains the model using the /train endpoint
- Python returns model status and training metrics
- .NET uses the trained/exported model for prediction
- .NET calculates residual, EWMA, alarm level, and failure status
- .NET saves the result data to the database

---

## Notes

- Python is used for training only.
- .NET is used for prediction and alarm evaluation.
- Training and prediction must use consistent feature columns.
- The trained model metadata should be saved and used to confirm the feature order.
