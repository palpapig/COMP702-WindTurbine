"""
Global Wind Turbine Model Training Script

Purpose:
    This script trains the machine learning model used for wind turbine
    failure prediction.

What it does:
    1. Loads a cleaned training CSV file from the trainingReady folder.
    2. Reads the target column, feature columns, and model settings from config.
    3. Trains a KNN regression model inside a pipeline with StandardScaler.
    4. Tests the model using a train/test split.
    5. Calculates evaluation metrics such as RMSE, R², and residual standard deviation.
    6. Saves the trained model as a .pkl and onnx files.
    7. Converts the trained model to ONNX format so it can be used in the .NET project.
    8. Compares ONNX predictions with sklearn predictions to make sure conversion worked correctly.
    9. Saves metadata such as feature order, target column, metrics, and model path.

Important:
    The feature column order saved in metadata must match the feature order used
    later in the .NET prediction service. If the order changes, predictions can be wrong.

Note:
    Python is used here for training and model conversion only.
    Live prediction, residual calculation, EWMA, and alarm evaluation are handled in .NET.
"""

from __future__ import annotations

from pathlib import Path
import json
import joblib
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import onnxruntime as ort
import onnxruntime as ort

from sklearn.ensemble import BaggingRegressor
from sklearn.metrics import mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.neighbors import KNeighborsRegressor
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler
from app.config.settings import get_model_settings
from skl2onnx import to_onnx
from skl2onnx.common.data_types import FloatTensorType


# ===== CONFIG =====


BASE_DIR = Path(__file__).resolve().parent.parent
DATA_DIR = BASE_DIR / "data" / "trainingReady"
csv_files = list(DATA_DIR.glob("*.csv"))

if not csv_files:
    raise FileNotFoundError("No CSV files found in data/trainingReady")
CSV_PATH = csv_files[0]


MODEL_OUTPUT_DIR = BASE_DIR / "artifacts" / "final_Model_converted"
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




def train_model() -> dict:
    # Load cleaned CSV
    df = pd.read_csv(CSV_PATH)
    print("Rows loaded:", len(df))
    print(df.head())
    print(f"Target: {TARGET_COLUMN}")
    print(f"Feature Columns: {FEATURE_COLUMNS}")

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
    work_df = work_df.dropna()

    print(len(work_df))

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

    """ removed tempoorly cuz it can not be converted to onnx
    # Bagging around KNN
    regressor = BaggingRegressor(
        estimator=knn,
        n_estimators=N_ESTIMATORS,
        random_state=RANDOM_STATE,
    )
    """
    # Full pipeline: scale data first, then train model
    model = Pipeline([
        ("scaler", StandardScaler()),
        ("model", knn),
    ])

    # Train model
    model.fit(X_train, y_train)

    # Test model
    predictions = model.predict(X_test)

    plt.figure(figsize=(10, 6))
    plt.scatter(y_test, predictions, alpha=0.5)

    min_val = min(y_test.min(), predictions.min())
    max_val = max(y_test.max(), predictions.max())

    plt.plot([min_val, max_val], [min_val, max_val], linestyle="--")

    plt.xlabel("Actual " + TARGET_COLUMN)
    plt.ylabel("Predicted " + TARGET_COLUMN)
    plt.title("Predicted vs Actual " + TARGET_COLUMN)
    plt.grid(True)

    GRAPH_PATH = MODEL_OUTPUT_DIR / "predicted_vs_actual.png"

    plt.savefig(GRAPH_PATH, dpi=300, bbox_inches="tight")
    plt.close()

    rmse = float(np.sqrt(mean_squared_error(y_test, predictions)))
    r2 = float(r2_score(y_test, predictions))
    residual_std = float(np.std(y_test - predictions, ddof=1))

    # Save one global model.pkl
    joblib.dump(model, MODEL_PATH)


    # Convert model to ONNX
    onnx_path = MODEL_OUTPUT_DIR / "model.onnx"

    # define input shape
    input_type = [
       ("float_input", FloatTensorType([None, len(FEATURE_COLUMNS)]))
    ]

    # convert
    onnx_model = to_onnx( model,initial_types= input_type)

    # save ONNX file
   #with open(onnx_path, "wb") as f:
    # f.write(onnx_model.SerializeToString())

    print("ONNX model saved:", onnx_path)



        # testing the convert onnx model
    # testing the converted ONNX model
    session = ort.InferenceSession(str(onnx_path))

    input_name = session.get_inputs()[0].name

    # use exact same features and order
    X_sample = X_test[FEATURE_COLUMNS].iloc[:100]

    # ONNX needs numpy float32
    sample = X_sample.to_numpy().astype(np.float32)

    # ONNX prediction
    onnx_predictions = session.run(None, {input_name: sample})[0]

    # sklearn prediction using same columns/order
    sklearn_predictions = model.predict(X_sample)

    print("Sklearn predictions:")
    print(sklearn_predictions)

    diff = sklearn_predictions.ravel() - onnx_predictions.ravel()

    print("Mean difference:", np.mean(diff))
    print("Mean absolute difference:", np.mean(np.abs(diff)))
    print("Max absolute difference:", np.max(np.abs(diff)))
     # saving the metadata
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
    result = train_model() 
    print("Global model trained successfully")
    print(json.dumps(result, indent=2))