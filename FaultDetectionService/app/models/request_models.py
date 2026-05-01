from __future__ import annotations

from datetime import datetime
from typing import Any

from pydantic import BaseModel, Field, ConfigDict, model_validator


class PredictRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    turbine_id: str = Field(..., alias="turbineId", description="Wind turbine identifier")
    timestamp: datetime | None = None
    values: dict[str, float] = Field(default_factory=dict)
    actual_target_value: float | None = Field(
        default=None,
        alias="actualTargetValue",
        description="Optional actual observed target for residual/alarm evaluation",
    )


class TrainRow(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    timestamp: datetime | None = None
    values: dict[str, float] = Field(default_factory=dict)

    @model_validator(mode="before")
    @classmethod
    def normalize_row(cls, data: Any) -> Any:
        """
        Supports both shapes:

        1) Nested shape:
           {
             "timestamp": "...",
             "values": {
               "windSpeed": 8.2,
               "power": 1200
             }
           }

        2) Flat .NET shape:
           {
             "timestamp": "...",
             "windSpeed": 8.2,
             "power": 1200
           }

        For shape (2), everything except timestamp is moved into values.
        """
        if not isinstance(data, dict):
            return data

        if "values" in data:
            return data

        timestamp = data.get("timestamp")
        values = {k: v for k, v in data.items() if k != "timestamp"}

        return {
            "timestamp": timestamp,
            "values": values,
        }


class TrainRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    turbine_id: str = Field(..., alias="turbineId")
    rows: list[TrainRow]
    target_column: str | None = Field(default=None, alias="targetColumn")
    feature_columns: list[str] | None = Field(default=None, alias="featureColumns")
    force_retrain: bool = Field(default=False, alias="forceRetrain")