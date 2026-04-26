from pathlib import Path
import joblib
import pandas as pd

from app.ml.feature_config import get_default_feature_columns, get_default_target_column

PROJECT_ROOT = Path(__file__).resolve().parent.parent

TURBINE_ID = "T1"

MODEL_PATH = PROJECT_ROOT / "artifacts" / TURBINE_ID / "model.pkl"
CSV_PATH = PROJECT_ROOT / "data" / "cleaned" / f"{TURBINE_ID}_cleaned.csv"


def main():
    if not MODEL_PATH.exists():
        raise FileNotFoundError(f"Model not found: {MODEL_PATH}")

    if not CSV_PATH.exists():
        raise FileNotFoundError(f"CSV not found: {CSV_PATH}")

    model = joblib.load(MODEL_PATH)
    df = pd.read_csv(CSV_PATH)

    feature_columns = get_default_feature_columns()
    target_column = get_default_target_column()

    missing_features = [col for col in feature_columns if col not in df.columns]
    if missing_features:
        raise ValueError(f"Missing feature columns in CSV: {missing_features}")

    if target_column not in df.columns:
        raise ValueError(f"Missing target column in CSV: {target_column}")

    # ✅ MULTIPLE ROW TEST
    sample_df = df.sample(n=10)

    X = sample_df[feature_columns]
    actual_values = sample_df[target_column].values

    predictions = model.predict(X)

    for i in range(len(sample_df)):
        actual = actual_values[i]
        pred = predictions[i]

        print(f"Row {i}: predicted={pred}, actual={actual}, residual={actual - pred}")


if __name__ == "__main__":
    main()