from __future__ import annotations

from datetime import datetime
from pydantic import BaseModel, Field, ConfigDict


class AlarmInfo(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    residual: float | None = None
    ewma: float | None = None
    ucl: float | None = None
    lcl: float | None = None
    a1_triggered: bool = Field(default=False, alias="a1Triggered")
    a2_triggered: bool = Field(default=False, alias="a2Triggered")
    consecutive_a1_count: int = Field(default=0, alias="consecutiveA1Count")


class Metrics(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    rmse: float | None = None
    r2: float | None = None


class PredictResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)
    turbine_id: str = Field(..., alias="turbineId")
    timestamp: datetime | None = None
    residual: float | None = None
    is_faulty: bool = Field(..., alias="isFaulty")   #  match .NET
    alarm_lvl: int = Field(..., alias="alarmLvl")    #  added correctly
    reason: str | None = None
    predicted_value: float | None = Field(default=None, alias="predictedValue")
    actual_value: float | None = Field(default=None, alias="actualValue")
    alarm: AlarmInfo | None = None

