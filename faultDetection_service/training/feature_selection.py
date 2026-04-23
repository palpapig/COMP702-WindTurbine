import os
import pandas as pd
import numpy as np

# =========================
# CONFIG
# =========================
BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FOLDER_PATH = os.path.join(BASE_DIR, "data", "raw")

# Preferred target name
TARGET_COLUMN = "Gear oil temperature (°C)"

# Keep features whose absolute PCC with target is above this threshold
CORR_THRESHOLD = 0.5

# Remove one of two features if their correlation with each other is above this threshold
MULTICOL_THRESHOLD = 0.95


# =========================
# HELPERS
# =========================
def clean_column_names(df: pd.DataFrame) -> pd.DataFrame:
    """
    Clean weird encoding issues and trim spaces from column names.
    This helps fix cases like:
    - Â°C -> °C
    - Â°  -> °
    - BOM characters
    """
    df.columns = (
        df.columns.astype(str)
        .str.replace("Â°C", "°C", regex=False)
        .str.replace("Â°", "°", regex=False)
        .str.replace("\ufeff", "", regex=False)
        .str.strip()
    )
    return df


def normalize_name(name: str) -> str:
    """
    Normalize a column name so matching works even if encoding/spacing differs.
    Example:
    'Gear oil temperature (Â°C)' and 'Gear oil temperature (°C)'
    both normalize to something similar.
    """
    return (
        str(name).lower()
        .replace("â°c", "c")
        .replace("â°", "")
        .replace("°c", "c")
        .replace("°", "")
        .replace("(", "")
        .replace(")", "")
        .replace('"', "")
        .replace("'", "")
        .replace(",", "")
        .replace("-", "")
        .replace("_", "")
        .replace("/", "")
        .replace("\\", "")
        .replace(" ", "")
        .strip()
    )


def find_target_column(df: pd.DataFrame, preferred_name: str) -> str:
    """
    Find the real target column even if the encoding is slightly broken.
    Priority:
    1. Exact match
    2. Normalized exact match
    3. Contains 'gear oil temperature'
    """
    # Exact match first
    if preferred_name in df.columns:
        return preferred_name

    # Normalized lookup
    normalized_lookup = {}
    for col in df.columns:
        normalized_lookup[normalize_name(col)] = col

    preferred_key = normalize_name(preferred_name)
    if preferred_key in normalized_lookup:
        return normalized_lookup[preferred_key]

    # Loose fallback search
    for col in df.columns:
        col_norm = normalize_name(col)
        if "gearoiltemperature" in col_norm:
            return col

    raise ValueError(f"Target column '{preferred_name}' not found.")


def convert_series_to_numeric(series: pd.Series) -> pd.Series:
    """
    Convert a pandas Series to numeric safely.
    Handles:
    - spaces
    - quotes
    - commas used as decimal separators
    - common missing value strings
    """
    cleaned = (
        series.astype(str)
        .str.strip()
        .str.replace('"', "", regex=False)
        .str.replace(",", ".", regex=False)
    )

    cleaned = cleaned.replace(
        {
            "": np.nan,
            "nan": np.nan,
            "NaN": np.nan,
            "None": np.nan,
            "null": np.nan,
            "-": np.nan,
        }
    )

    return pd.to_numeric(cleaned, errors="coerce")


def looks_like_bad_single_column_parse(df: pd.DataFrame) -> bool:
    """
    Detect cases where the CSV was read incorrectly as a single giant column.
    This usually happens when the wrong separator was used.
    """
    if df.shape[1] != 1:
        return False

    only_col = str(df.columns[0]).lower()

    suspicious_tokens = [
        "date and time",
        "wind speed",
        "gear oil temperature",
        "power",
        "temperature",
        "rotor speed",
    ]

    comma_count = only_col.count(",")

    if comma_count >= 5:
        return True

    if sum(token in only_col for token in suspicious_tokens) >= 2:
        return True

    return False


# =========================
# READ ONE CSV ROBUSTLY
# =========================
def read_csv_robust(file_path: str) -> pd.DataFrame:
    """
    Read a CSV file robustly by:
    - finding the real header row first
    - trying different encodings and separators
    - rejecting fake successful reads where the whole row becomes one column
    """
    last_error = None

    header_row = None
    with open(file_path, "r", encoding="utf-8", errors="replace") as f:
        for i, line in enumerate(f):
            lowered = line.lower()
            if "# date and time" in lowered or "date and time" in lowered:
                header_row = i
                break

    if header_row is None:
        raise ValueError(f"Could not find the real header row in: {file_path}")

    print(f"Detected header row at line: {header_row}")

    # Try comma first because your file clearly behaves like a comma-separated file
    attempts = [
        {"encoding": "utf-8", "sep": ","},
        {"encoding": "latin1", "sep": ","},
        {"encoding": "utf-8", "sep": ";"},
        {"encoding": "latin1", "sep": ";"},
        {"encoding": "utf-8", "sep": "\t"},
        {"encoding": "latin1", "sep": "\t"},
    ]

    for attempt in attempts:
        try:
            df = pd.read_csv(
                file_path,
                encoding=attempt["encoding"],
                sep=attempt["sep"],
                skiprows=header_row,
                low_memory=False,
                on_bad_lines="skip",
            )

            df = clean_column_names(df)

            # Reject fake success where the file becomes one giant column
            if looks_like_bad_single_column_parse(df):
                print(
                    f"Bad parse with encoding={attempt['encoding']} sep={repr(attempt['sep'])} "
                    f"-> only one giant column detected, retrying..."
                )
                continue

            # Also reject extremely small column count for SCADA-like data
            if df.shape[1] < 5:
                print(
                    f"Bad parse with encoding={attempt['encoding']} sep={repr(attempt['sep'])} "
                    f"-> only {df.shape[1]} columns detected, retrying..."
                )
                continue

            print(f"Read success with encoding={attempt['encoding']} sep={repr(attempt['sep'])}")
            print(f"Detected {df.shape[1]} columns.")
            return df

        except Exception as e:
            last_error = e

    raise ValueError(f"Could not read file correctly: {file_path}\nLast error: {last_error}")


# =========================
# LOAD CSV FILES
# =========================
def load_data(folder_path: str) -> pd.DataFrame:
    """
    Load all CSV files from the folder, clean headers, and combine them into one DataFrame.
    """
    dfs = []

    print("SCRIPT FILE:", __file__)
    print("BASE_DIR:", BASE_DIR)
    print("FOLDER_PATH:", folder_path)
    print("FOLDER EXISTS:", os.path.isdir(folder_path))

    if not os.path.isdir(folder_path):
        raise FileNotFoundError(f"Folder not found: {folder_path}")

    files = os.listdir(folder_path)
    print("ALL FILES IN FOLDER:", files)

    csv_files = [f for f in files if f.lower().endswith(".csv")]
    print("CSV FILES FOUND:", csv_files)

    if not csv_files:
        raise ValueError(f"No .csv files found in: {folder_path}")

    for file_name in csv_files:
        file_path = os.path.join(folder_path, file_name)
        print(f"\nLoading: {file_path}")

        try:
            df = read_csv_robust(file_path)
        except Exception as e:
            print(f"Skipping {file_name} due to read error: {e}")
            continue

        df = clean_column_names(df)

        print("Loaded shape:", df.shape)
        print("First 10 columns:", df.columns[:10].tolist())

        dfs.append(df)

    if not dfs:
        raise ValueError(f"No CSV files were successfully loaded from: {folder_path}")

    combined_df = pd.concat(dfs, ignore_index=True)
    combined_df = clean_column_names(combined_df)

    print("\nCOMBINED SHAPE:", combined_df.shape)
    return combined_df


# =========================
# FEATURE SELECTION
# =========================
def select_features(df: pd.DataFrame) -> dict:
    """
    Perform feature selection in 3 stages:
    1. Keep features correlated with target
    2. Remove highly inter-correlated features (multicollinearity)
    3. Remove leakage columns related to target
    """
    df = clean_column_names(df)

    print("\nCOLUMNS CONTAINING 'gear oil':")
    for col in df.columns:
        if "gear oil" in col.lower():
            print(repr(col))

    actual_target = find_target_column(df, TARGET_COLUMN)
    print("\nResolved target column:", repr(actual_target))

    # Keep timestamp columns untouched
    timestamp_cols = []
    for col in df.columns:
        if "date and time" in col.lower():
            timestamp_cols.append(col)

    # Convert all non-timestamp columns to numeric
    for col in df.columns:
        if col not in timestamp_cols:
            df[col] = convert_series_to_numeric(df[col])

    print("Target dtype after conversion:", df[actual_target].dtype)
    print("Non-null target values:", df[actual_target].notna().sum())
    print("Sample target values:")
    print(df[actual_target].dropna().head(10))

    # Keep numeric columns only
    df = df.select_dtypes(include=[np.number]).copy()

    if actual_target not in df.columns:
        raise ValueError(f"Target column '{actual_target}' is not numeric after conversion.")

    # Remove rows where target is missing
    df = df.dropna(subset=[actual_target]).copy()
    print("Shape after dropping missing target rows:", df.shape)

    print("\nCalculating correlations...")
    corr = df.corr(numeric_only=True)
    target_corr = corr[actual_target].drop(actual_target)

    # Sort by absolute correlation descending
    target_corr = target_corr.reindex(
        target_corr.abs().sort_values(ascending=False).index
    )

    print("\nTop correlations with target:")
    print(target_corr.head(15))

    # Step 1: select features correlated with target
    selected = target_corr[target_corr.abs() > CORR_THRESHOLD].index.tolist()
    print(f"\nSelected features (|corr| > {CORR_THRESHOLD}): {len(selected)}")

    if not selected:
        raise ValueError(
            f"No features passed the correlation threshold of {CORR_THRESHOLD}."
        )

    # Step 2: remove multicollinearity
    corr_matrix = df[selected].corr().abs()
    upper = corr_matrix.where(
        np.triu(np.ones(corr_matrix.shape), k=1).astype(bool)
    )

    to_drop = [col for col in upper.columns if any(upper[col] > MULTICOL_THRESHOLD)]
    reduced = [col for col in selected if col not in to_drop]

    print(f"Removed due to multicollinearity (>{MULTICOL_THRESHOLD}): {len(to_drop)}")

    # Step 3: remove leakage columns related to target
    leakage_removed = []
    final_features = []

    target_norm = normalize_name(actual_target)

    for col in reduced:
        col_norm = normalize_name(col)

        # Remove the target itself or close variants of it
        if "gearoiltemperature" in col_norm:
            leakage_removed.append(col)
        elif col_norm == target_norm:
            leakage_removed.append(col)
        else:
            final_features.append(col)

    print(f"Removed due to leakage: {len(leakage_removed)}")

    print("\nFinal features:")
    for f in final_features:
        print(f"- {f}")

    final_df = df[final_features + [actual_target]].copy()

    return {
        "actual_target": actual_target,
        "final_df": final_df,
        "target_corr": target_corr,
        "selected": selected,
        "to_drop": to_drop,
        "leakage_removed": leakage_removed,
        "final_features": final_features,
    }


# =========================
# SAVE OUTPUTS
# =========================
def save_outputs(folder_path: str, results: dict) -> None:
    """
    Save:
    - filtered dataset as CSV
    - text report summarizing selection steps
    """
    output_csv = os.path.join(folder_path, "selected_features.csv")
    output_report = os.path.join(folder_path, "feature_selection_report.txt")

    results["final_df"].to_csv(output_csv, index=False)

    with open(output_report, "w", encoding="utf-8") as f:
        f.write("FEATURE SELECTION REPORT\n")
        f.write("========================\n\n")

        f.write(f"Preferred Target Variable:\n{TARGET_COLUMN}\n\n")
        f.write(f"Resolved Target Variable:\n{results['actual_target']}\n\n")
        f.write(f"Correlation Threshold Used: {CORR_THRESHOLD}\n")
        f.write(f"Multicollinearity Threshold Used: {MULTICOL_THRESHOLD}\n\n")

        f.write("Top 15 Correlations With Target:\n")
        f.write(results["target_corr"].head(15).to_string())
        f.write("\n\n")

        f.write(f"Features selected by PCC (|r| > {CORR_THRESHOLD}):\n")
        for col in results["selected"]:
            f.write(f"- {col}\n")
        f.write("\n")

        f.write(f"Features removed for multicollinearity (>{MULTICOL_THRESHOLD}):\n")
        if results["to_drop"]:
            for col in results["to_drop"]:
                f.write(f"- {col}\n")
        else:
            f.write("None\n")
        f.write("\n")

        f.write("Features removed due to target leakage:\n")
        if results["leakage_removed"]:
            for col in results["leakage_removed"]:
                f.write(f"- {col}\n")
        else:
            f.write("None\n")
        f.write("\n")

        f.write("Final Features Used for Training:\n")
        for col in results["final_features"]:
            f.write(f"- {col}\n")
        f.write("\n")

        f.write(f"Final dataset shape: {results['final_df'].shape}\n")

    print(f"\nSaved filtered dataset to: {output_csv}")
    print(f"Saved report to: {output_report}")


# =========================
# MAIN
# =========================
def main():
    df = load_data(FOLDER_PATH)
    results = select_features(df)
    save_outputs(FOLDER_PATH, results)


if __name__ == "__main__":
    main()