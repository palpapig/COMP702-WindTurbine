from __future__ import annotations

from typing import Any


class RetrainService:
    def __init__(
        self,
        config_service,
        data_client,
        trainer,
    ):
        self.config_service = config_service
        self.data_client = data_client
        self.trainer = trainer

    def retrain(self, turbine_id: str) -> dict[str, Any]:
        config = self.config_service.load_config(turbine_id)

        if not self.config_service.is_enabled(config):
            return {
                "turbineId": turbine_id,
                "status": "skipped",
                "reason": "training disabled in config",
            }

        columns = self.config_service.build_required_columns(config)
        months_back = self.config_service.get_months_back(config)

        data = self.data_client.fetch_training_data(
            turbine_id=turbine_id,
            months_back=months_back,
            columns=columns,
        )

        rows = data.get("rows", [])
        if not rows:
            return {
                "turbineId": turbine_id,
                "status": "failed",
                "reason": "no rows returned from .NET API",
            }

        result = self.trainer.train(
            turbine_id=turbine_id,
            rows=rows,
            config=config,
        )

        return {
            "turbineId": turbine_id,
            "status": "success",
            "rowsReceived": len(rows),
            "requiredColumns": columns,
            "trainingResult": result,
        }
