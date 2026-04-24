# Wind Fault Detection Service

Hybrid-ready Python ML service for C# integration.

## Endpoints
- `POST /predict` -> live prediction and alarm evaluation
- `POST /train` -> train or retrain one turbine model from batch rows sent by C#
- `GET /health` -> health check
- `GET /turbines` -> registered turbines

## Run
```bash
uvicorn app.main:app --reload
```

## Notes
- Target column, feature columns, model type, and training options are controlled in JSON.
- Models are stored per turbine in `artifacts/<turbine_id>/`.
- Unknown turbines are registered automatically, but safe training is done through `/train`.
- This avoids blocking live prediction with expensive training.
