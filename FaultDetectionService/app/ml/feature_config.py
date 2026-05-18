from __future__ import annotations

from app.config.settings import get_model_settings, get_turbines_registry, save_turbines_registry


def get_default_target_column() -> str:
    return get_model_settings()["default_target_column"]


def get_default_feature_columns() -> list[str]:
    return list(get_model_settings()["default_feature_columns"])


def get_turbine_config(turbine_id: str) -> dict:
    registry = get_turbines_registry()
    return registry.get("turbines", {}).get(turbine_id, {})


def ensure_turbine_registered(turbine_id: str) -> dict:
    registry = get_turbines_registry()
    turbines = registry.setdefault("turbines", {})
    if turbine_id not in turbines:
        turbines[turbine_id] = {
            "target_column": get_default_target_column(),
            "feature_columns": get_default_feature_columns(),
            "status": "registered_no_model"
        }
        save_turbines_registry(registry)
    return turbines[turbine_id]
