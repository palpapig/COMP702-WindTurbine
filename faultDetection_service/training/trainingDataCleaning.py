from pathlib import Path
import pandas as pd
import re

BASE_DIR = Path(__file__).parent
INPUT_FOLDER = BASE_DIR / "rawData"
OUTPUT_FOLDER = BASE_DIR / "cleanData"
SUMMARY_FILE = "cleaning_summary.csv"

# Kelmarsh / Greenbyte format
CSV_SKIPROWS = 9

TIME_COL = "# Date and time"
WIND_COL = "Wind speed (m/s)"
POWER_COL = "Power (kW)"
WIND_DIR_COL = "Wind direction (°)"
NACELLE_POS_COL = "Nacelle position (°)"

# Basic training-data filters
MIN_WIND_SPEED = 3.0
MAX_WIND_SPEED = 40.0
MIN_POWER = 0.0

# Drop rows with missing values in all columns after key cleaning
DROP_ALL_NA_ROWS = False

# ---------------------------
# Outlier handling settings
# ---------------------------
ENABLE_OUTLIER_REMOVAL = True

# IQR multiplier: 1.5 is standard, 3.0 is less aggressive
IQR_MULTIPLIER = 1.5

# Only use columns that matter for training / power behavior
OUTLIER_COLUMNS = [
    "Wind speed (m/s)",
    "Power (kW)",
    "Rotor speed (RPM)",
    "Blade angle (pitch position) A (°)",
    "Nacelle position (°)",
    "Wind direction (°)",
    "Nacelle ambient temperature (°C)",
    "Yaw error (°)",
]


def normalize_text(s: str) -> str:
    s = str(s).replace("Â°", "°")
    s = re.sub(r"\s+", " ", s).strip()
    return s


def normalize_columns(df: pd.DataFrame) -> pd.DataFrame:
    df = df.copy()
    df.columns = [normalize_text(c) for c in df.columns]
    return df


def ensure_yaw_error(df: pd.DataFrame) -> pd.DataFrame:
    df = df.copy()

    if "Yaw error (°)" not in df.columns and WIND_DIR_COL in df.columns and NACELLE_POS_COL in df.columns:
        wind_dir = pd.to_numeric(df[WIND_DIR_COL], errors="coerce")
        nacelle_pos = pd.to_numeric(df[NACELLE_POS_COL], errors="coerce")
        df["Yaw error (°)"] = ((wind_dir - nacelle_pos + 180) % 360) - 180

    return df


def drop_duplicate_timestamps(df: pd.DataFrame, time_col: str) -> pd.DataFrame:
    return df.drop_duplicates(subset=[time_col], keep="first").copy()


def get_existing_outlier_columns(df: pd.DataFrame) -> list[str]:
    return [col for col in OUTLIER_COLUMNS if col in df.columns]


def remove_iqr_outliers(df: pd.DataFrame, cols: list[str], multiplier: float = 1.5) -> tuple[pd.DataFrame, int]:
    """
    Removes rows where any selected column falls outside:
    [Q1 - multiplier*IQR, Q3 + multiplier*IQR]
    """
    if not cols:
        return df.copy(), 0

    cleaned = df.copy()
    original_len = len(cleaned)

    keep_mask = pd.Series(True, index=cleaned.index)

    for col in cols:
        series = pd.to_numeric(cleaned[col], errors="coerce")

        # Ignore columns with too few usable values
        valid = series.dropna()
        if len(valid) < 4:
            continue

        q1 = valid.quantile(0.25)
        q3 = valid.quantile(0.75)
        iqr = q3 - q1

        # If column is constant or nearly constant, skip it
        if pd.isna(iqr) or iqr == 0:
            continue

        lower = q1 - multiplier * iqr
        upper = q3 + multiplier * iqr

        col_mask = series.isna() | ((series >= lower) & (series <= upper))
        keep_mask &= col_mask

    cleaned = cleaned[keep_mask].copy()
    removed_count = original_len - len(cleaned)

    return cleaned, removed_count


def clean_training_file(path: Path) -> tuple[pd.DataFrame, dict]:
    # Read Greenbyte CSV: skip metadata lines, keep actual header row
    df = pd.read_csv(path, skiprows=CSV_SKIPROWS, engine="python")
    df = normalize_columns(df)
    df = ensure_yaw_error(df)

    original_rows = len(df)

    if TIME_COL not in df.columns:
        raise ValueError(f"Missing time column '{TIME_COL}' in {path.name}")
    if WIND_COL not in df.columns:
        raise ValueError(f"Missing wind column '{WIND_COL}' in {path.name}")
    if POWER_COL not in df.columns:
        raise ValueError(f"Missing power column '{POWER_COL}' in {path.name}")

    # Parse timestamp
    df[TIME_COL] = pd.to_datetime(df[TIME_COL], errors="coerce")

    # Convert all columns except time to numeric where possible
    for col in df.columns:
        if col != TIME_COL:
            df[col] = pd.to_numeric(df[col], errors="coerce")

    # Remove bad timestamps
    before_bad_time = len(df)
    df = df.dropna(subset=[TIME_COL])
    bad_time_removed = before_bad_time - len(df)

    # Remove duplicate timestamps
    before_dupes = len(df)
    df = drop_duplicate_timestamps(df, TIME_COL)
    duplicate_rows_removed = before_dupes - len(df)

    # Key-column filtering
    before_filters = len(df)
    df = df[df[WIND_COL].notna()]
    df = df[df[POWER_COL].notna()]
    df = df[df[WIND_COL] >= MIN_WIND_SPEED]
    df = df[df[WIND_COL] <= MAX_WIND_SPEED]
    df = df[df[POWER_COL] >= MIN_POWER]
    filtered_rows_removed = before_filters - len(df)

    # Optional full dropna after basic cleaning
    before_na = len(df)
    if DROP_ALL_NA_ROWS:
        df = df.dropna()
    na_rows_removed = before_na - len(df)

    # Outlier removal
    outlier_rows_removed = 0
    outlier_cols_used = []

    if ENABLE_OUTLIER_REMOVAL:
        outlier_cols_used = get_existing_outlier_columns(df)
        df, outlier_rows_removed = remove_iqr_outliers(
            df,
            cols=outlier_cols_used,
            multiplier=IQR_MULTIPLIER
        )

    # Sort by time
    df = df.sort_values(TIME_COL).reset_index(drop=True)

    summary = {
        "file": path.name,
        "original_rows": original_rows,
        "bad_time_removed": bad_time_removed,
        "duplicate_rows_removed": duplicate_rows_removed,
        "filtered_rows_removed": filtered_rows_removed,
        "na_rows_removed": na_rows_removed,
        "outlier_rows_removed": outlier_rows_removed,
        "outlier_columns_used": ", ".join(outlier_cols_used) if outlier_cols_used else "",
        "final_rows": len(df),
    }

    return df, summary


def main():
    input_dir = INPUT_FOLDER
    output_dir = OUTPUT_FOLDER
    output_dir.mkdir(parents=True, exist_ok=True)

    data_files = sorted(input_dir.glob("*.csv"))

    if not data_files:
        print(f"No CSV files found in: {input_dir.resolve()}")
        return

    summaries = []

    for file_path in data_files:
        try:
            cleaned_df, summary = clean_training_file(file_path)

            out_file = output_dir / f"{file_path.stem}_cleaned.csv"
            cleaned_df.to_csv(out_file, index=False)

            summaries.append(summary)
            print(
                f"✓ Cleaned: {file_path.name} -> {out_file.name} | "
                f"rows kept: {len(cleaned_df)} | "
                f"outliers removed: {summary['outlier_rows_removed']}"
            )

        except Exception as e:
            print(f"✗ Failed: {file_path.name} | {e}")
            summaries.append({
                "file": file_path.name,
                "original_rows": None,
                "bad_time_removed": None,
                "duplicate_rows_removed": None,
                "filtered_rows_removed": None,
                "na_rows_removed": None,
                "outlier_rows_removed": None,
                "outlier_columns_used": None,
                "final_rows": None,
                "error": str(e),
            })

    pd.DataFrame(summaries).to_csv(output_dir / SUMMARY_FILE, index=False)

    print(f"\nDone. Cleaned files saved in: {output_dir.resolve()}")
    print(f"Summary saved to: {(output_dir / SUMMARY_FILE).resolve()}")


if __name__ == "__main__":
    main()