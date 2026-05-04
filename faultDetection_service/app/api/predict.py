from fastapi import APIRouter, HTTPException

from app.models.request_models import PredictRequest
from app.models.response_models import PredictResponse
from app.services.fault_detection_service import fault_detection_service

router = APIRouter(tags=["prediction"])


@router.post("/predict", response_model=PredictResponse)

def predict(request: PredictRequest):



   turbineId = request.TurbineId 
   timestamp = request.Timestamp
   actualTargetValue = request.ActualTargetValue
   values = request.Values

   try:
        # fault_detection_service.predict( turbineId, actualTargetValue, value, timestamp)
        return fault_detection_service.predict(
            turbineId,
            actualTargetValue,
            values,
            timestamp,
        )
   except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
   except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e
