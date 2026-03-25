from __future__ import annotations

from datetime import datetime
from typing import Any

from pydantic import BaseModel, Field


class PredictRequest(BaseModel):
    turbine_id: str = Field(..., description="Wind turbine identifier")
    timestamp: datetime | None = None
    values: dict[str, Any]
    actual_target_value: float | None = Field(
        default=None,
        description="Optional actual observed target for residual/alarm evaluation"
    )


class TrainRow(BaseModel):
    timestamp: datetime | None = None
    values: dict[str, Any]


class TrainRequest(BaseModel):
    turbine_id: str
    rows: list[TrainRow]
    target_column: str | None = None
    feature_columns: list[str] | None = None
    force_retrain: bool = False
