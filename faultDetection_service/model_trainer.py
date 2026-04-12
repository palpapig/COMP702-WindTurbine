from __future__ import annotations

from typing import Any

from training.Training_Model_Pipeline import run_training_pipeline


class ModelTrainer:
    def train(
        self,
        turbine_id: str,
        rows: list[dict[str, Any]],
        config: dict[str, Any],
    ) -> dict[str, Any]:
        target_column = config.get("target_column")
        feature_columns = config.get("feature_columns")

        return run_training_pipeline(
            turbine_id=turbine_id,
            rows=rows,
            target_column=target_column,
            feature_columns=feature_columns,
        )
