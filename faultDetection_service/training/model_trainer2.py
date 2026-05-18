from __future__ import annotations

from pathlib import Path
import json
import joblib
import numpy as np
import pandas as pd

from sklearn.ensemble import BaggingRegressor
from sklearn.metrics import mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.neighbors import KNeighborsRegressor
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler
from app.config.settings import get_model_settings


# ===== CONFIG =====


BASE_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = BASE_DIR / "data" / "cleaned"
csv_files = list(DATA_DIR.glob("*.csv"))

if not csv_files:
    raise FileNotFoundError("No CSV files found in data/cleaned")
CSV_PATH = csv_files[0]


MODEL_OUTPUT_DIR = BASE_DIR / "artifacts" / "global_model"
MODEL_OUTPUT_DIR.mkdir(parents=True, exist_ok=True)

MODEL_PATH = MODEL_OUTPUT_DIR / "model.pkl"
METADATA_PATH = MODEL_OUTPUT_DIR / "metadata.json"

settings = get_model_settings()
FEATURE_COLUMNS = settings["default_feature_columns"]
TARGET_COLUMN = settings["default_target_column"]
WEIGHTS = settings["knn_bagging"]["weights"]
METRIC = settings["knn_bagging"]["metric"]
N_NEIGHBORS = settings["knn_bagging"]["n_neighbors"]
N_ESTIMATORS = settings["baggingRegressor"]["n_estimators"]
RANDOM_STATE = settings["baggingRegressor"]["random_state"]
TEST_SIZE = 0.2




def train_global_model() -> dict:
    # Load cleaned CSV
    df = pd.read_csv(CSV_PATH)
    print("Rows loaded:", len(df))
    print(df.head())

    # Check target exists
    if TARGET_COLUMN not in df.columns:
        raise ValueError(f"Target column '{TARGET_COLUMN}' not found in CSV.")

    # Check features exist
    missing_features = [col for col in FEATURE_COLUMNS if col not in df.columns]
    if missing_features:
        raise ValueError(f"Missing feature columns: {missing_features}")

    # Keep only needed columns

    work_df = df[FEATURE_COLUMNS + [TARGET_COLUMN]].copy()

    # Convert values to numeric
    for col in work_df.columns:
        work_df[col] = pd.to_numeric(work_df[col], errors="coerce")

    # Drop invalid rows
    ## df = df.dropna()

    if work_df.empty:
        raise ValueError("No valid rows left after cleaning.")

    # Split input and target
    X = work_df[FEATURE_COLUMNS]
    y = work_df[TARGET_COLUMN]

    # Train/test split: 80% train, 20% test
    X_train, X_test, y_train, y_test = train_test_split(
        X,
        y,
        test_size=TEST_SIZE,
        random_state=RANDOM_STATE,
    )

    # Base KNN model
    knn = KNeighborsRegressor(
        n_neighbors=N_NEIGHBORS,
        weights=WEIGHTS,
        metric=METRIC,
    )

    # Bagging around KNN
    regressor = BaggingRegressor(
        estimator=knn,
        n_estimators=N_ESTIMATORS,
        random_state=RANDOM_STATE,
    )

    # Full pipeline: scale data first, then train model
    model = Pipeline([
        ("scaler", StandardScaler()),
        ("model", regressor),
    ])

    # Train model
    model.fit(X_train, y_train)

    # Test model
    predictions = model.predict(X_test)

    rmse = float(np.sqrt(mean_squared_error(y_test, predictions)))
    r2 = float(r2_score(y_test, predictions))
    residual_std = float(np.std(y_test - predictions, ddof=1))

    # Save one global model.pkl
    joblib.dump(model, MODEL_PATH)

    metadata = {
        "model_type": "global_knn_bagging",
        "target_column": TARGET_COLUMN,
        "feature_columns": FEATURE_COLUMNS,
        "rows_used": int(len(work_df)),
        "test_size": TEST_SIZE,
        "metrics": {
            "rmse": rmse,
            "r2": r2,
            "residual_std": residual_std,
        },
        "model_path": str(MODEL_PATH),
    }

    with open(METADATA_PATH, "w", encoding="utf-8") as f:
        json.dump(metadata, f, indent=2)

    return metadata


if __name__ == "__main__":
    result = train_global_model()
    print("Global model trained successfully")
    print(json.dumps(result, indent=2))