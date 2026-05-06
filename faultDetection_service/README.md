# Wind Fault Detection Service

## Overview

this is a failure pediction machine that use knn for predicteing target variable and EWMA chart to evaluate failure existance

---

## Setup the virtual envirnoment / run in terminal one by one

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
.venv/bin/python

Install dependencies

4:
pip install -r requirements.txt

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
.venv/bin/python

3:
python -m uvicorn app.main:app --reload
"or"
py -m uvicorn app.main:app --reload

---

## Endpoints

- POST /predict → perform prediction and evaluate fault/alarm state

---

## Prediction

- Loads model
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
- Response includes residual, alarm lvl, failure status, predicted value

---

## Notes

- Training and prediction must use consistent feature columns
