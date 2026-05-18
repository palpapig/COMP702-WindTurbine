from __future__ import annotations

import json
from pathlib import Path
import joblib

from app.config.settings import ARTIFACTS_DIR
from app.ml.feature_config import ensure_turbine_registered






def load_model(model_dir):
    model_dir = Path(model_dir)
    model_dir = Path 
    model_path = model_dir / "model.pkl"
    metadata_path = model_dir / "metadata.json"

    if not model_path.exists():
        raise ValueError("Model does not exist")

    if not metadata_path.exists():
        raise ValueError("Metadata does not exist")

    model = joblib.load(model_path)

    with open(metadata_path, "r") as f:
        metadata = json.load(f)

    return model, metadata



