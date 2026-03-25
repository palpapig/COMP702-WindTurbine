from __future__ import annotations

from datetime import datetime
from pydantic import BaseModel


class AlarmInfo(BaseModel):
    residual: float | None = None
    ewma: float | None = None
    ucl: float | None = None
    lcl: float | None = None
    a1_triggered: bool = False
    a2_triggered: bool = False
    consecutive_a1_count: int = 0


class PredictResponse(BaseModel):
    turbineId: str
    timestamp: datetime | None = None
    isAnomaly: bool
    reason: str | None = None
    predictedValue: float | None = None
    actualValue: float | None = None
    modelStatus: str
    alarm: AlarmInfo | None = None


class TrainResponse(BaseModel):
    turbineId: str
    modelStatus: str
    rowsUsed: int
    targetColumn: str
    featureColumns: list[str]
    metrics: dict
    modelPath: str
