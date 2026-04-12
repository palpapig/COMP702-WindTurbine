from __future__ import annotations

from pathlib import Path
import json
from typing import Any


class TrainingConfigError(Exception):
    """Raised when training config is missing or invalid."""


class TrainingConfigService:
    def __init__(self, config_dir: str | Path):
        self.config_dir = Path(config_dir)

    def load_config(self, turbine_id: str) -> dict[str, Any]:
        turbine_file = self.config_dir / f"{turbine_id}.json"
        default_file = self.config_dir / "default.json"

        if turbine_file.exists():
            config_file = turbine_file
        elif default_file.exists():
            config_file = default_file
        else:
            raise TrainingConfigError(
                f"No config found. Expected either {turbine_file} or {default_file}"
            )

        try:
            with open(config_file, "r", encoding="utf-8") as f:
                config = json.load(f)
        except json.JSONDecodeError as ex:
            raise TrainingConfigError(f"Invalid JSON in config file: {config_file}") from ex

        self._validate_config(config, config_file)
        return config

    def build_required_columns(self, config: dict[str, Any]) -> list[str]:
        columns: list[str] = []

        time_column = config.get("time_column")
        target_column = config.get("target_column")
        feature_columns = config.get("feature_columns", [])

        if time_column:
            columns.append(time_column)

        if target_column:
            columns.append(target_column)

        for col in feature_columns:
            if col:
                columns.append(col)

        seen: set[str] = set()
        unique_columns: list[str] = []
        for col in columns:
            if col not in seen:
                seen.add(col)
                unique_columns.append(col)

        return unique_columns

    def get_months_back(self, config: dict[str, Any]) -> int:
        months_back = config.get("months_back", 3)
        if not isinstance(months_back, int) or months_back <= 0:
            raise TrainingConfigError("months_back must be a positive integer")
        return months_back

    def is_enabled(self, config: dict[str, Any]) -> bool:
        return bool(config.get("enabled", True))

    def _validate_config(self, config: dict[str, Any], config_file: Path) -> None:
        required_fields = ["target_column", "feature_columns"]
        for field in required_fields:
            if field not in config:
                raise TrainingConfigError(
                    f"Missing required field '{field}' in config file: {config_file}"
                )

        if not isinstance(config["feature_columns"], list) or not config["feature_columns"]:
            raise TrainingConfigError(
                f"'feature_columns' must be a non-empty list in config file: {config_file}"
            )

        if not isinstance(config["target_column"], str) or not config["target_column"].strip():
            raise TrainingConfigError(
                f"'target_column' must be a non-empty string in config file: {config_file}"
            )
