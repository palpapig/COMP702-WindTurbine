from __future__ import annotations

from training.train_model import train_turbine_from_rows


def run_training_pipeline(turbine_id: str, rows: list[dict], target_column: str | None = None, feature_columns: list[str] | None = None) -> dict:
    return train_turbine_from_rows(
        turbine_id=turbine_id,
        rows=rows,
        target_column=target_column,
        feature_columns=feature_columns,
    )
