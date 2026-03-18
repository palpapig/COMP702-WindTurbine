from pathlib import Path
import pandas as pd
import numpy as np
import re
import json

BASE_DIR = Path(__file__).parent
INPUT_FOLDER = BASE_DIR / "cleanData"
OUTPUT_FOLDER = BASE_DIR / "featureSelectionResults_power"

SUMMARY_FILE = "feature_selection_summary_power.csv"

TIME_COL_OPTIONS = ["# Date and time", "Date and time", "time", "Time", "Timestamp"]
TARGET_COL_OPTIONS = ["Power (kW)", "Active Power (kW)", "ActivePower", "Power"]

# Paper-inspired threshold for multicollinearity pruning
FEATURE_CORR_THRESHOLD = 0.85

# Optional minimum absolute correlation with target to keep as "best"
TARGET_CORR_THRESHOLD = 0.10

# Exact columns to always exclude
EXACT_EXCLUDE_COLUMNS = {
    "# Date and time",
    "Date and time",
    "time",
    "Time",
    "Timestamp",
}

# Columns too close to target / leakage / target variants
LEAKAGE_COLUMNS = {
    "Power, Standard deviation (kW)",
    "Power, Minimum (kW)",
    "Power, Maximum (kW)",
    "Potential power default PC (kW)",
    "Potential power learned PC (kW)",
    "Potential power reference turbines (kW)",
    "Cascading potential power (kW)",
    "Cascading potential power for performance (kW)",
    "Potential power met mast anemometer (kW)",
    "Potential power primary reference turbines (kW)",
    "Potential power secondary reference turbines (kW)",
    "Turbine Power setpoint (kW)",
    "Potential power estimated (kW)",
    "Potential power MPC (kW)",
    "Potential power met mast anemometer MPC (kW)",
    "Turbine Power setpoint, Max (kW)",
    "Turbine Power setpoint, Min (kW)",
    "Turbine Power setpoint, StdDev (kW)",
    "Available Capacity for Production (kW)",
    "Available Capacity for Production (Planned) (kW)",
    "Energy Export (kWh)",
    "Energy Export counter (kWh)",
    "Energy Import (kWh)",
    "Energy Import counter (kWh)",
    "Virtual Production (kWh)",
    "Energy Theoretical (kWh)",
}

# Non-predictor / KPI / accounting style fields
EXCLUDE_PATTERNS = [
    r"lost production",
    r"energy export",
    r"energy import",
    r"energy budget",
    r"energy theoretical",
    r"virtual production",
    r"reactive energy",
    r"counter",
    r"availability",
    r"avail\.",
    r"capacity factor",
    r"production factor",
    r"performance index",
    r"equivalent full load hours",
    r"users view",
    r"manufacturers view",
    r"contractual",
    r"planned",
    r"system avail",
    r"curtailment",
]

# Optional: remove all min/max/std style columns globally
DROP_STAT_VARIANTS = False
STAT_VARIANT_PATTERNS = [
    r",\s*max\b",
    r",\s*min\b",
    r",\s*standard deviation\b",
    r",\s*stddev\b",
    r",\s*std\b",
]


def normalize_text(s: str) -> str:
    s = str(s).replace("Â°", "°")
    s = re.sub(r"\s+", " ", s).strip()
    return s


def normalize_columns(df: pd.DataFrame) -> pd.DataFrame:
    df = df.copy()
    df.columns = [normalize_text(c) for c in df.columns]
    return df


def find_col(df: pd.DataFrame, options: list[str]) -> str | None:
    for o in options:
        if o in df.columns:
            return o

    lower_map = {c.lower(): c for c in df.columns}
    for o in options:
        o_low = o.lower()
        for low_name, original_name in lower_map.items():
            if o_low in low_name:
                return original_name
    return None


def load_cleaned_file(path: Path) -> tuple[pd.DataFrame, str | None, str]:
    df = pd.read_csv(path)
    df = normalize_columns(df)

    time_col = find_col(df, TIME_COL_OPTIONS)
    target_col = find_col(df, TARGET_COL_OPTIONS)

    if target_col is None:
        raise ValueError(
            f"Could not find target power column in {path.name}. "
            f"Checked: {TARGET_COL_OPTIONS}"
        )

    if time_col and time_col in df.columns:
        df[time_col] = pd.to_datetime(df[time_col], errors="coerce")

    for col in df.columns:
        if col != time_col:
            df[col] = pd.to_numeric(df[col], errors="coerce")

    return df, time_col, target_col


def should_exclude_column(col: str, target_col: str, time_col: str | None) -> bool:
    col_norm = normalize_text(col)
    col_low = col_norm.lower()

    if col_norm == target_col:
        return True

    if time_col and col_norm == time_col:
        return True

    if col_norm in EXACT_EXCLUDE_COLUMNS:
        return True

    if col_norm in LEAKAGE_COLUMNS:
        return True

    for pattern in EXCLUDE_PATTERNS:
        if re.search(pattern, col_low):
            return True

    if DROP_STAT_VARIANTS:
        for pattern in STAT_VARIANT_PATTERNS:
            if re.search(pattern, col_low):
                return True

    return False


def build_feature_matrix(
    df: pd.DataFrame,
    time_col: str | None,
    target_col: str
) -> tuple[pd.DataFrame, pd.Series, list[str]]:
    y = df[target_col].copy()

    candidate_cols = []
    excluded_cols = []

    for col in df.columns:
        if should_exclude_column(col, target_col, time_col):
            excluded_cols.append(col)
        else:
            candidate_cols.append(col)

    X = df[candidate_cols].copy()
    X = X.select_dtypes(include=[np.number])
    X = X.dropna(axis=1, how="all")

    # fill missing values for correlation computation
    X = X.fillna(X.median(numeric_only=True))

    # remove constant columns
    nunique = X.nunique(dropna=True)
    constant_cols = nunique[nunique <= 1].index.tolist()
    if constant_cols:
        excluded_cols.extend(constant_cols)
        X = X.loc[:, nunique > 1]

    # align target and drop missing target rows
    valid_target_mask = y.notna()
    X = X.loc[valid_target_mask].reset_index(drop=True)
    y = y.loc[valid_target_mask].reset_index(drop=True)

    if X.empty:
        raise ValueError("No usable numeric predictor columns remained after exclusions.")

    return X, y, sorted(set(excluded_cols))


def remove_highly_correlated_features(
    X: pd.DataFrame,
    threshold: float = FEATURE_CORR_THRESHOLD
) -> tuple[pd.DataFrame, list[str], pd.DataFrame]:
    corr_matrix = X.corr().abs()
    upper = corr_matrix.where(np.triu(np.ones(corr_matrix.shape), k=1).astype(bool))

    to_drop = [column for column in upper.columns if any(upper[column] > threshold)]
    X_pruned = X.drop(columns=to_drop, errors="ignore")

    pairs = []
    for col in upper.columns:
        high_corr_rows = upper.index[upper[col] > threshold].tolist()
        for row_name in high_corr_rows:
            pairs.append({
                "feature_1": row_name,
                "feature_2": col,
                "abs_correlation": float(upper.loc[row_name, col])
            })

    pairs_df = pd.DataFrame(pairs).sort_values(
        by="abs_correlation",
        ascending=False
    ) if pairs else pd.DataFrame(columns=["feature_1", "feature_2", "abs_correlation"])

    return X_pruned, to_drop, pairs_df


def rank_features_against_target(X: pd.DataFrame, y: pd.Series) -> pd.DataFrame:
    rows = []

    for col in X.columns:
        temp = pd.DataFrame({
            "feature": X[col],
            "target": y
        }).dropna()

        if len(temp) < 2:
            continue

        corr = temp["feature"].corr(temp["target"])
        if pd.isna(corr):
            continue

        rows.append({
            "feature": col,
            "pearson_corr_with_target": float(corr),
            "abs_corr_with_target": float(abs(corr))
        })

    ranked = pd.DataFrame(rows).sort_values(
        by="abs_corr_with_target",
        ascending=False
    ).reset_index(drop=True)

    return ranked


def process_file(path: Path, output_dir: Path) -> dict:
    df, time_col, target_col = load_cleaned_file(path)
    X, y, excluded_cols = build_feature_matrix(df, time_col, target_col)

    input_feature_count_before_pruning = X.shape[1]

    X_pruned, corr_dropped_cols, high_corr_pairs_df = remove_highly_correlated_features(
        X,
        threshold=FEATURE_CORR_THRESHOLD
    )

    ranked_df = rank_features_against_target(X_pruned, y)

    selected_df = ranked_df[
        ranked_df["abs_corr_with_target"] >= TARGET_CORR_THRESHOLD
    ].copy()

    selected_features = selected_df["feature"].tolist()

    stem = path.stem

    ranked_df.to_csv(
        output_dir / f"{stem}_power_feature_ranking.csv",
        index=False
    )

    selected_df.to_csv(
        output_dir / f"{stem}_power_selected_features.csv",
        index=False
    )

    high_corr_pairs_df.to_csv(
        output_dir / f"{stem}_power_high_corr_pairs_removed.csv",
        index=False
    )

    pd.DataFrame({"excluded_columns": excluded_cols}).to_csv(
        output_dir / f"{stem}_power_excluded_columns.csv",
        index=False
    )

    pd.DataFrame({"dropped_due_to_high_feature_correlation": corr_dropped_cols}).to_csv(
        output_dir / f"{stem}_power_multicollinearity_dropped_columns.csv",
        index=False
    )

    summary_json = {
        "file": path.name,
        "target_column": target_col,
        "feature_corr_threshold": FEATURE_CORR_THRESHOLD,
        "target_corr_threshold": TARGET_CORR_THRESHOLD,
        "input_feature_count_before_pruning": int(input_feature_count_before_pruning),
        "feature_count_after_multicollinearity_pruning": int(X_pruned.shape[1]),
        "excluded_columns_count": len(excluded_cols),
        "multicollinearity_dropped_columns_count": len(corr_dropped_cols),
        "selected_features_count": len(selected_features),
        "selected_features": selected_features
    }

    with open(
        output_dir / f"{stem}_power_feature_selection_summary.json",
        "w",
        encoding="utf-8"
    ) as f:
        json.dump(summary_json, f, indent=2)

    return {
        "file": path.name,
        "target_column": target_col,
        "input_feature_count_before_pruning": int(input_feature_count_before_pruning),
        "feature_count_after_multicollinearity_pruning": int(X_pruned.shape[1]),
        "excluded_columns_count": len(excluded_cols),
        "multicollinearity_dropped_columns_count": len(corr_dropped_cols),
        "selected_features_count": len(selected_features),
        "top_feature": ranked_df.iloc[0]["feature"] if not ranked_df.empty else None,
        "top_feature_abs_corr": float(ranked_df.iloc[0]["abs_corr_with_target"]) if not ranked_df.empty else None
    }


def main():
    input_dir = INPUT_FOLDER
    output_dir = OUTPUT_FOLDER
    output_dir.mkdir(parents=True, exist_ok=True)

    files = [
        f for f in sorted(input_dir.glob("*_cleaned.csv"))
        if "summary" not in f.name.lower()
    ]

    if not files:
        print(f"No cleaned CSV files found in: {input_dir.resolve()}")
        return

    summaries = []

    for file_path in files:
        try:
            result = process_file(file_path, output_dir)
            summaries.append(result)

            print(
                f"✓ Feature selection done: {file_path.name} | "
                f"before={result['input_feature_count_before_pruning']} | "
                f"after_corr_prune={result['feature_count_after_multicollinearity_pruning']} | "
                f"selected={result['selected_features_count']} | "
                f"top={result['top_feature']} ({result['top_feature_abs_corr']:.3f})"
                if result["top_feature_abs_corr"] is not None
                else f"✓ Feature selection done: {file_path.name}"
            )

        except Exception as e:
            print(f"✗ Failed: {file_path.name} | {e}")
            summaries.append({
                "file": file_path.name,
                "target_column": None,
                "input_feature_count_before_pruning": None,
                "feature_count_after_multicollinearity_pruning": None,
                "excluded_columns_count": None,
                "multicollinearity_dropped_columns_count": None,
                "selected_features_count": None,
                "top_feature": None,
                "top_feature_abs_corr": None,
                "error": str(e),
            })

    pd.DataFrame(summaries).to_csv(output_dir / SUMMARY_FILE, index=False)

    print(f"\nDone. Feature-selection results saved in: {output_dir.resolve()}")
    print(f"Summary saved to: {(output_dir / SUMMARY_FILE).resolve()}")


if __name__ == "__main__":
    main()