from __future__ import annotations

from datetime import datetime, timezone
import pandas as pd

from app.ml.feature_config import ensure_turbine_registered, get_default_feature_columns, get_default_target_column
from app.ml.model_registry import load_bundle, model_exists, load_model
from app.models.response_models import PredictResponse
from app.services.alarm_service import alarm_service
from app.Loggers import get_logger,ensure_required_features

logger = get_logger(__name__)

model, metadata = load_model()

class FaultDetectionService:
    def predict(
        self,
        turbineId: str,
        values: dict,
        actualTargetValue: float,
        timestamp: datetime | None = None,
    ) -> PredictResponse:
     
           # get feature order used during training
        # IMPORTANT: model expects SAME order during prediction
        feature_columns = metadata["feature_columns"]

        # check if any required feature is missing from input
        missing = ensure_required_features(feature_columns, values)
        if missing:
            raise ValueError(f"Missing required prediction features: {missing}")

        # build a row dictionary in EXACT same feature order
        row = {
            feature: values.get(feature)
            for feature in feature_columns
        }

        # convert to pandas DataFrame (model expects 2D input)
        # also explicitly enforce column order
        frame = pd.DataFrame([row], columns=feature_columns)

        # convert all values to numeric (handles strings from JSON)
        for col in frame.columns:
            frame[col] = pd.to_numeric(frame[col], errors="coerce")

        # check if any value became NaN (invalid input)
        if frame.isna().any().any():
            bad_cols = frame.columns[frame.isna().any()].tolist()
            raise ValueError(f"Non-numeric or null feature values for: {bad_cols}")

        # perform prediction using trained model
        predicted_value = float(model.predict(frame)[0])
       

   # default values for alarm logic
        alarm = None
        is_anomaly = False
        reason = None

        # only compute anomaly if actual value is provided
        if actualTargetValue is not None:

            # residual = difference between predicted and actual
            # NOTE: this is the key signal for fault detection
            residual = predicted_value - actualTargetValue

            # standard deviation of residuals from training (used for control limits)
            residual_std = metadata.get("metrics", {}).get("residual_std")

            # evaluate alarm using EWMA logic
            alarm = alarm_service.evaluate(
                turbineId,
                residual=residual,
                residual_std=residual_std
            )

            # anomaly if either A1 or A2 triggered
            is_anomaly = bool(alarm.a1_triggered or alarm.a2_triggered)

            # explain why anomaly happened
            if alarm.a2_triggered:
                reason = "A2 alarm triggered: repeated abnormal residual pattern"
            elif alarm.a1_triggered:
                reason = "A1 alarm triggered: residual moved outside control limits"

        # return structured API response
        return PredictResponse(
            turbineId=turbineId,
            timestamp=timestamp or datetime.now(timezone.utc),
            isAnomaly=is_anomaly,
            reason=reason,
            predictedValue=predicted_value,
            actualValue=actualTargetValue,
            modelStatus="trained",   # since we always use one global model
            alarm=alarm,
        )
        


fault_detection_service = FaultDetectionService()
