from __future__ import annotations

import os

from training_config_service import TrainingConfigService
from dotnet_data_client import DotNetDataClient
from retrain_service import RetrainService
from model_trainer import ModelTrainer


CONFIG_DIR = "./config/training"
DOTNET_BASE_URL = os.getenv("DOTNET_BASE_URL", "http://127.0.0.1:5000")
DOTNET_API_KEY = os.getenv("DOTNET_API_KEY")  # optional
DEFAULT_TURBINE_ID = os.getenv("TURBINE_ID", "T1")


if __name__ == "__main__":
    config_service = TrainingConfigService(CONFIG_DIR)
    data_client = DotNetDataClient(
        base_url=DOTNET_BASE_URL,
        api_key=DOTNET_API_KEY,
        timeout_seconds=120,
    )
    trainer = ModelTrainer()

    service = RetrainService(
        config_service=config_service,
        data_client=data_client,
        trainer=trainer,
    )

    result = service.retrain(DEFAULT_TURBINE_ID)
    print(result)
