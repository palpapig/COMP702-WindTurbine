from __future__ import annotations

import json
from pathlib import Path
from functools import lru_cache
from typing import Any

BASE_DIR = Path(__file__).resolve().parents[2]
ARTIFACTS_DIR = BASE_DIR / "artifacts"
DATA_DIR = BASE_DIR / "data"
CONFIG_DIR = BASE_DIR / "app" / "config"
MODEL_SETTINGS_PATH = CONFIG_DIR / "model_settings.json"
TURBINES_REGISTRY_PATH = CONFIG_DIR / "turbines.json"


def load_json(path: Path) -> dict[str, Any]:
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def save_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(payload, f, indent=2, ensure_ascii=False)


@lru_cache(maxsize=1)
def get_model_settings() -> dict[str, Any]:
    return load_json(MODEL_SETTINGS_PATH)


def reload_model_settings() -> dict[str, Any]:
    get_model_settings.cache_clear()
    return get_model_settings()


def get_turbines_registry() -> dict[str, Any]:
    if not TURBINES_REGISTRY_PATH.exists():
        save_json(TURBINES_REGISTRY_PATH, {"turbines": {}})
    return load_json(TURBINES_REGISTRY_PATH)


def save_turbines_registry(payload: dict[str, Any]) -> None:
    save_json(TURBINES_REGISTRY_PATH, payload)
