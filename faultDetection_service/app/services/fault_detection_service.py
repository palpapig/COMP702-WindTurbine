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
print("Model loaded")

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
        row = {}

        for feature in feature_columns:
            value = values.get(feature)
            row[feature] = value
  
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
        print("Started Prediction \n\n")
        predicted_value = float(model.predict(frame)[0])
        print("Prediction finished \n\n")
       

   # default values for alarm logic
        alarm = None
        is_faulty = False
        reason = None
        alarm_lvl = 0

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
            is_faulty = bool(alarm.a1_triggered or alarm.a2_triggered)

            # determine alarm level
            if alarm and alarm.a2_triggered:
               alarm_lvl = 2
            elif alarm and alarm.a1_triggered:
               alarm_lvl = 1
            else:
               alarm_lvl = 0

            # explain why anomaly happened
            if alarm.a2_triggered:
                reason = "A2 alarm triggered: repeated abnormal residual pattern"
            elif alarm.a1_triggered:
                reason = "A1 alarm triggered: residual moved outside control limits"

        # return structured API response
        print("turbineId:", turbineId,)
        print("Predicted:", predicted_value,)
        print("Actual:", actualTargetValue,)
        print("residual:", residual,)
        print("alarm_lvl:", alarm_lvl,)
        print("Failuar Status:", is_faulty,"\n")
        return PredictResponse(
             turbine_id=turbineId,
             timestamp=timestamp or datetime.now(timezone.utc),
             is_faulty=is_faulty,
             alarm_lvl=alarm_lvl,
             predicted_value=predicted_value,
             actual_value=actualTargetValue,
             residual=residual,
           
)
        


fault_detection_service = FaultDetectionService()
