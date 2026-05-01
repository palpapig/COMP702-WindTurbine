from pathlib import Path
import pandas as pd

BASE_DIR = Path(__file__).resolve().parent.parent
FOLDER_PATH = BASE_DIR / "data" / "cleaned"

TARGET_COLUMN = "Gear oil temperature (°C)"


def main():
    csv_files = list(FOLDER_PATH.glob("*.csv"))

    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in {FOLDER_PATH}")

    dfs = []

    for file in csv_files:
        print(f"Loading: {file.name}")
        df = pd.read_csv(file)
        df.columns = df.columns.str.strip()
        dfs.append(df)

    df = pd.concat(dfs, ignore_index=True)

    if TARGET_COLUMN not in df.columns:
        raise ValueError(f"Target column '{TARGET_COLUMN}' not found")

    # Convert all columns to numeric (ignore non-numeric like timestamp)
    for col in df.columns:
        df[col] = pd.to_numeric(df[col], errors="coerce")

    # Drop rows where target is missing
    df = df.dropna(subset=[TARGET_COLUMN])

    # Keep only numeric columns
    df = df.select_dtypes(include="number")

    # Remove target from feature list
    feature_columns = [col for col in df.columns if col != TARGET_COLUMN]

    correlations = []

    for feature in feature_columns:
        corr = df[feature].corr(df[TARGET_COLUMN])

        # skip if correlation is NaN (e.g., constant column)
        if pd.isna(corr):
            continue

        correlations.append({
            "feature": feature,
            "correlation_with_target": corr,
            "abs_correlation": abs(corr),
        })

    result = pd.DataFrame(correlations)
    result = result.sort_values("abs_correlation", ascending=False)

    print("\nPCC results (all features):")
    print(result.to_string(index=False))

    output_path = FOLDER_PATH / "pcc_all_features.csv"
    result.to_csv(output_path, index=False)

    print(f"\nSaved to: {output_path}")


if __name__ == "__main__":
    main()