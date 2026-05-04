from __future__ import annotations

import json
from pathlib import Path
import joblib

from app.config.settings import ARTIFACTS_DIR
from app.ml.feature_config import ensure_turbine_registered




MODEL_DIR = Path("artifacts/global_model")

def load_model():
    model_path = MODEL_DIR / "model.pkl"
    metadata_path = MODEL_DIR / "metadata.json"

    if not model_path.exists():
        raise ValueError("Model does not exist")

    if not metadata_path.exists():
        raise ValueError("Metadata does not exist")

    model = joblib.load(model_path)

    with open(metadata_path, "r") as f:
        metadata = json.load(f)

    return model, metadata




 ########### needs to be removed ###############


def get_turbine_dir(turbine_id: str) -> Path:
    turbine_dir = ARTIFACTS_DIR / turbine_id
    turbine_dir.mkdir(parents=True, exist_ok=True)
    return turbine_dir


def get_model_paths(turbine_id: str) -> dict[str, Path]:
    base = get_turbine_dir(turbine_id)
    return {
        "model": base / "model.pkl",
        "scaler": base / "scaler.pkl",
        "metadata": base / "metadata.json"
    }


def model_exists(turbine_id: str) -> bool:
    ensure_turbine_registered(turbine_id)
    return get_model_paths(turbine_id)["model"].exists()


def load_bundle(turbine_id: str) -> dict | None:
    ensure_turbine_registered(turbine_id)
    paths = get_model_paths(turbine_id)
    if not paths["model"].exists() or not paths["metadata"].exists():
        return None

    model = joblib.load(paths["model"])
    scaler = joblib.load(paths["scaler"]) if paths["scaler"].exists() else None
    with open(paths["metadata"], "r", encoding="utf-8") as f:
        metadata = json.load(f)

    return {"model": model, "scaler": scaler, "metadata": metadata, "paths": paths}


def save_bundle(turbine_id: str, model, scaler, metadata: dict) -> dict[str, str]:
    paths = get_model_paths(turbine_id)
    joblib.dump(model, paths["model"])
    if scaler is not None:
        joblib.dump(scaler, paths["scaler"])
    with open(paths["metadata"], "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2, ensure_ascii=False)
    return {k: str(v) for k, v in paths.items()}


if __name__ == "__main__":
    try:
        model, metadata = load_model()
        print("Model loaded successfully")
        print("Model type:", type(model))
        print("Metadata:", metadata)
    except Exception as e:
        print("Error:", e)