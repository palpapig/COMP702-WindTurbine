from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

class PredictionInput(BaseModel):
    turbine_id: str
    vibration: float
    temperature: float

@app.post("/predict")
def predict(data: PredictionInput):
    print(">>> Python received request")
    print(data)

    is_anomaly = data.vibration > 8 or data.temperature > 80

    response = {
        "turbineId": data.turbine_id,
        "timestamp": "2026-03-18T10:00:00Z",
        "isAnomaly": is_anomaly,
        "reason": "dummy rule triggered" if is_anomaly else None
    }

    print(">>> Python returning response")
    print(response)

    return response