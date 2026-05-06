from fastapi import APIRouter, HTTPException

from app.models.request_models import PredictRequest
from app.models.response_models import PredictResponse
from app.services.fault_detection_service import fault_detection_service

router = APIRouter(tags=["prediction"])


@router.post("/predict", response_model=PredictResponse)

def predict(request: PredictRequest):



   turbineId = request.turbineId 
   timestamp = request.timestamp
   actualTargetValue = request.actualTargetValue
   values = request.values

   try:
        print("Before fault_detection_service.predict")
        # fault_detection_service.predict( turbineId, actualTargetValue, value, timestamp)
        
        return fault_detection_service.predict(
            turbineId,
            values,
            actualTargetValue,
            timestamp,
        )
        print("Before fault_detection_service.predict")
   except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
   except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e
