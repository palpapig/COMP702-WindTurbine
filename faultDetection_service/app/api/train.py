from fastapi import APIRouter, HTTPException

from app.ml.feature_config import get_default_feature_columns, get_default_target_column
from app.models.request_models import TrainRequest
from app.models.response_models import TrainResponse
from training.train_model import train_turbine_from_rows

router = APIRouter(tags=["training"])


@router.post("/train", response_model=TrainResponse)
def train(request: TrainRequest):
    try:
        rows = [row.values for row in request.rows]
        result = train_turbine_from_rows(
            turbine_id=request.turbine_id,
            rows=rows,
            target_column=request.target_column or get_default_target_column(),
            feature_columns=request.feature_columns or get_default_feature_columns(),
        )
        return result
    except ValueError as e:
        raise HTTPException(status_code=400, detail=str(e)) from e
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e)) from e
