from __future__ import annotations

from pathlib import Path
import pandas as pd

from training.train_model import train_turbine_from_rows
from app.ml.feature_config import (
    get_default_feature_columns,
    get_default_target_column,
)

PROJECT_ROOT = Path(__file__).resolve().parent.parent
CLEANED_DATA_DIR = PROJECT_ROOT / "data" / "cleaned"


def extract_turbine_id(file_path: Path) -> str:
    name = file_path.stem
    if name.endswith("_cleaned"):
        name = name[:-8]
    return name


def run_training_pipeline(
    turbine_id: str,
    rows: list[dict],
    target_column: str | None = None,
    feature_columns: list[str] | None = None,
) -> dict:
    return train_turbine_from_rows(
        turbine_id=turbine_id,
        rows=rows,
        target_column=target_column,
        feature_columns=feature_columns,
    )


def run_training_from_cleaned_csvs() -> list[dict]:
    files = sorted(CLEANED_DATA_DIR.glob("*.csv"))

    if not files:
        raise FileNotFoundError(f"No cleaned CSV files found in: {CLEANED_DATA_DIR}")

    target_column = get_default_target_column()
    feature_columns = get_default_feature_columns()

    results: list[dict] = []

    for file_path in files:
        turbine_id = extract_turbine_id(file_path)
        df = pd.read_csv(file_path)
        rows = df.to_dict(orient="records")

        result = run_training_pipeline(
            turbine_id=turbine_id,
            rows=rows,
            target_column=target_column,
            feature_columns=feature_columns,
        )

        results.append({
            "file": file_path.name,
            "turbine_id": turbine_id,
            "result": result,
        })

        print(f"✓ Trained {turbine_id} from {file_path.name}")

    return results


if __name__ == "__main__":
    run_training_from_cleaned_csvs()