from pathlib import Path
import pandas as pd

BASE_DIR = Path(__file__).resolve().parent.parent

RAW_DIR = BASE_DIR / "data" / "raw"
CLEANED_DIR = BASE_DIR / "data" / "cleaned"
CLEANED_DIR.mkdir(parents=True, exist_ok=True)

WIND_COL = "Wind speed (m/s)"
POWER_COL = "Power (kW)"


def clean_file(file_path: Path) -> None:
    print(f"Reading: {file_path.name}")

    df = pd.read_csv(file_path)
    df.columns = df.columns.str.strip()

    if WIND_COL not in df.columns:
        raise ValueError(f"Missing column: {WIND_COL}")

    if POWER_COL not in df.columns:
        raise ValueError(f"Missing column: {POWER_COL}")

    before = len(df)

    # Convert wind speed and power to numeric
    df[WIND_COL] = pd.to_numeric(df[WIND_COL], errors="coerce")
    df[POWER_COL] = pd.to_numeric(df[POWER_COL], errors="coerce")

    # Remove rows where wind speed or power is missing
    df = df.dropna(subset=[WIND_COL, POWER_COL])

    # Basic physical cleaning
    df = df[df[WIND_COL] >= 0]
    df = df[df[POWER_COL] >= 0]

    # Remove impossible / extreme wind speed values
    df = df[df[WIND_COL] <= 30]

    # Remove very low wind with high power
    df = df[~((df[WIND_COL] < 3) & (df[POWER_COL] > 50))]

    # Remove high wind with almost zero power
    df = df[~((df[WIND_COL] > 5) & (df[POWER_COL] < 10))]

    # Power curve based outlier removal using binning
    df["wind_bin"] = pd.cut(df[WIND_COL], bins=60)

    cleaned_parts = []

    for _, group in df.groupby("wind_bin", observed=False):
        if len(group) < 20:
            cleaned_parts.append(group)
            continue

        q1 = group[POWER_COL].quantile(0.25)
        q3 = group[POWER_COL].quantile(0.75)
        iqr = q3 - q1

        lower = q1 - 1.5 * iqr
        upper = q3 + 1.5 * iqr

        cleaned_group = group[
            (group[POWER_COL] >= lower) &
            (group[POWER_COL] <= upper)
        ]

        cleaned_parts.append(cleaned_group)

    cleaned_df = pd.concat(cleaned_parts, ignore_index=True)

    cleaned_df = cleaned_df.drop(columns=["wind_bin"])

    after = len(cleaned_df)
    removed = before - after

    output_path = CLEANED_DIR / f"{file_path.stem}_cleaned.csv"
    cleaned_df.to_csv(output_path, index=False)

    print(f"Saved: {output_path.name}")
    print(f"Before: {before}")
    print(f"After: {after}")
    print(f"Removed: {removed}")
    print("-" * 40)


def main() -> None:
    csv_files = list(RAW_DIR.glob("*.csv"))

    if not csv_files:
        raise FileNotFoundError(f"No CSV files found in {RAW_DIR}")

    for file_path in csv_files:
        clean_file(file_path)


if __name__ == "__main__":
    main()