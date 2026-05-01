from fastapi import APIRouter, HTTPException

from app.models.request_models import PredictRequest
from app.models.response_models import PredictResponse
from app.services.fault_detection_service import fault_detection_service

router = APIRouter(tags=["prediction"])


@router.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest):
    try:
        return fault_detection_service.predict(
            turbine_id=request.turbine_id,
            values=request.values,
            timestamp=request.timestamp,
            actual_target_value=request.actual_target_value,
        )
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e
