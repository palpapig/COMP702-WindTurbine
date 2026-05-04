from __future__ import annotations

from datetime import datetime
from typing import Any

from pydantic import BaseModel, Field, ConfigDict, model_validator



class PredictRequest(BaseModel):
    turbineId: str
    timestamp: datetime | None = None
    actualTargetValue: float
    values: dict[str, float] = Field(default_factory=dict)




############# this was used for training but is no more used ##########
    """
class TrainRow(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    timestamp: datetime | None = None
    values: dict[str, float] = Field(default_factory=dict)

    @model_validator(mode="before")
    @classmethod
    def normalize_row(cls, data: Any) -> Any:
       
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

    turbineId: str = Field(..., alias="turbineId")
    rows: list[TrainRow]
    target_column: str | None = Field(default=None, alias="targetColumn")
    feature_columns: list[str] | None = Field(default=None, alias="featureColumns")
    force_retrain: bool = Field(default=False, alias="forceRetrain")
    """