from __future__ import annotations

from app.ml.feature_config import get_default_feature_columns, get_default_target_column
from training.model_trainer import model_trainer


def train_turbine_from_rows(turbine_id: str, rows: list[dict], target_column: str | None = None, feature_columns: list[str] | None = None) -> dict:
    return model_trainer.train_from_rows(
        turbine_id=turbine_id,
        rows=rows,
        target_column=target_column or get_default_target_column(),
        feature_columns=feature_columns or get_default_feature_columns(),
    )
