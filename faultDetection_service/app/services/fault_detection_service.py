from __future__ import annotations

from datetime import datetime, timezone
import pandas as pd

from app.ml.feature_config import ensure_turbine_registered, get_default_feature_columns, get_default_target_column
from app.ml.model_registry import load_bundle, model_exists
from app.models.response_models import PredictResponse
from app.services.alarm_service import alarm_service
from app.utils.logger import get_logger
from app.utils.validators import ensure_required_features

logger = get_logger(__name__)


class FaultDetectionService:
    def predict(
        self,
        turbine_id: str,
        values: dict,
        timestamp: datetime | None = None,
        actual_target_value: float | None = None,
    ) -> PredictResponse:
        turbine_cfg = ensure_turbine_registered(turbine_id)

        if not model_exists(turbine_id):
            logger.warning("No model found for turbine %s", turbine_id)
            return PredictResponse(
                turbineId=turbine_id,
                timestamp=timestamp or datetime.now(timezone.utc),
                isAnomaly=False,
                reason="No trained model found for this turbine. Send batch training data to /train first.",
                predictedValue=None,
                actualValue=actual_target_value,
                modelStatus="model_not_found",
                alarm=None,
            )

        bundle = load_bundle(turbine_id)
        if bundle is None:
            raise ValueError(f"Failed to load model bundle for turbine '{turbine_id}'.")

        metadata = bundle["metadata"]
        feature_columns = metadata.get("feature_columns") or turbine_cfg.get("feature_columns") or get_default_feature_columns()
        missing = ensure_required_features(feature_columns, values)
        if missing:
            raise ValueError(f"Missing required prediction features: {missing}")

        frame = pd.DataFrame([{feature: values.get(feature) for feature in feature_columns}])
        for col in frame.columns:
            frame[col] = pd.to_numeric(frame[col], errors="coerce")
        if frame.isna().any().any():
            bad_cols = frame.columns[frame.isna().any()].tolist()
            raise ValueError(f"Non-numeric or null feature values for: {bad_cols}")

        predicted_value = float(bundle["model"].predict(frame)[0])

        alarm = None
        is_anomaly = False
        reason = None
        if actual_target_value is not None:
            residual = predicted_value - actual_target_value
            residual_std = metadata.get("metrics", {}).get("residual_std")
            alarm = alarm_service.evaluate(turbine_id, residual=residual, residual_std=residual_std)
            is_anomaly = bool(alarm.a1_triggered or alarm.a2_triggered)
            if alarm.a2_triggered:
                reason = "A2 alarm triggered: repeated abnormal residual pattern"
            elif alarm.a1_triggered:
                reason = "A1 alarm triggered: residual moved outside control limits"

        return PredictResponse(
            turbineId=turbine_id,
            timestamp=timestamp or datetime.now(timezone.utc),
            isAnomaly=is_anomaly,
            reason=reason,
            predictedValue=predicted_value,
            actualValue=actual_target_value,
            modelStatus="trained",
            alarm=alarm,
        )


fault_detection_service = FaultDetectionService()
