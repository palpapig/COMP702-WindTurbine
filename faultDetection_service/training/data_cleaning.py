from pathlib import Path
import pandas as pd
import re

# Project root = parent of /training
PROJECT_ROOT = Path(__file__).resolve().parent.parent

# Default project folders
DEFAULT_INPUT_FOLDER = PROJECT_ROOT / "data" / "raw"
DEFAULT_OUTPUT_FOLDER = PROJECT_ROOT / "data" / "cleaned"

#SUMMARY_FILE = "cleaning_summary.csv"  >>>>   this inilitlise summery 

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


def clean_training_file(path: Path) -> tuple[pd.DataFrame, dict]:
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

    # Parse time
    df[TIME_COL] = pd.to_datetime(df[TIME_COL], errors="coerce")

    # Convert all non-time columns to numeric where possible
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

    # Key filtering
    before_filters = len(df)
    df = df[df[WIND_COL].notna()]
    df = df[df[POWER_COL].notna()]
    df = df[df[WIND_COL] >= MIN_WIND_SPEED]
    df = df[df[WIND_COL] <= MAX_WIND_SPEED]
    df = df[df[POWER_COL] >= MIN_POWER]
    filtered_rows_removed = before_filters - len(df)

    # Optional full dropna
    before_na = len(df)
    if DROP_ALL_NA_ROWS:
        df = df.dropna()
    na_rows_removed = before_na - len(df)

    # Final sort
    df = df.sort_values(TIME_COL).reset_index(drop=True)

    summary = {
        "file": path.name,
        "original_rows": original_rows,
        "bad_time_removed": bad_time_removed,
        "duplicate_rows_removed": duplicate_rows_removed,
        "filtered_rows_removed": filtered_rows_removed,
        "na_rows_removed": na_rows_removed,
        "final_rows": len(df),
    }

    return df, summary


def clean_all_training_files(
    input_dir: Path | None = None,
    output_dir: Path | None = None,
) -> pd.DataFrame:
    input_dir = input_dir or DEFAULT_INPUT_FOLDER
    output_dir = output_dir or DEFAULT_OUTPUT_FOLDER
    output_dir.mkdir(parents=True, exist_ok=True)

    data_files = sorted(input_dir.glob("*.csv"))

    if not data_files:
        raise FileNotFoundError(f"No CSV files found in: {input_dir.resolve()}")

    summaries = []

    for file_path in data_files:
        try:
            cleaned_df, summary = clean_training_file(file_path)

            out_file = output_dir / f"{file_path.stem}_cleaned.csv"
            cleaned_df.to_csv(out_file, index=False)

            summaries.append(summary)
            print(f"✓ Cleaned: {file_path.name} -> {out_file.name} | rows kept: {len(cleaned_df)}")

        except Exception as e:
            print(f"✗ Failed: {file_path.name} | {e}")
            summaries.append({
                "file": file_path.name,
                "original_rows": None,
                "bad_time_removed": None,
                "duplicate_rows_removed": None,
                "filtered_rows_removed": None,
                "na_rows_removed": None,
                "final_rows": None,
                "error": str(e),
            })

# summary_df = pd.DataFrame(summaries)
   # summary_df.to_csv(output_dir / SUMMARY_FILE, index=False)

   # print(f"\nDone. Cleaned files saved in: {output_dir.resolve()}") >>>> this generate summery file
   # print(f"Summary saved to: {(output_dir / SUMMARY_FILE).resolve()}") 

    return summary_df


if __name__ == "__main__":
    clean_all_training_files()