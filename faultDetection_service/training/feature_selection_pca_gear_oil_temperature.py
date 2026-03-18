from pathlib import Path
import pandas as pd
import numpy as np
import re
import json
import matplotlib.pyplot as plt

from sklearn.preprocessing import StandardScaler
from sklearn.decomposition import PCA

BASE_DIR = Path(__file__).parent
INPUT_FOLDER = BASE_DIR / "cleanData"
OUTPUT_FOLDER = BASE_DIR / "pcaResults_gear_oil_temperature"
SUMMARY_FILE = "pca_summary_gear_oil_temperature.csv"

TIME_COL_OPTIONS = ["# Date and time", "Date and time", "time", "Time", "Timestamp"]

TARGET_COL_OPTIONS = [
    "Gear oil temperature (°C)",
    "Gear oil temperature",
    "Gearbox oil sump temperature (°C)",
    "Gearbox oil sump temperature",
    "Gearbox oil temperature (°C)",
    "Gearbox oil temperature",
]

EXPLAINED_VARIANCE_THRESHOLD = 0.95
TOP_FEATURES_PER_PC = 5

# Exact columns to always exclude if present
EXACT_EXCLUDE_COLUMNS = {
    "# Date and time",
    "Date and time",
    "time",
    "Time",
    "Timestamp",
}

# Columns too close to the target / likely leakage
LEAKAGE_COLUMNS = {
    "Gear oil temperature, Max (°C)",
    "Gear oil temperature, Min (°C)",
    "Gear oil temperature, Standard deviation (°C)",
    "Gear oil temperature, StdDev (°C)",
    "Gear oil temperature, Std (°C)",
    "Gear oil inlet temperature (°C)",
    "Gear oil inlet temperature, Max (°C)",
    "Gear oil inlet temperature, Min (°C)",
    "Gear oil inlet temperature, StdDev (°C)",
    "Gear oil inlet temperature, Standard deviation (°C)",
}

# Pattern-based exclusion for non-predictor / accounting / KPI / counter fields
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

# Optional: remove min/max/std variants globally to reduce feature explosion
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
            f"Could not find target gear oil temperature column in {path.name}. "
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

    # keep numeric predictors only
    X = X.select_dtypes(include=[np.number])

    # remove columns that became empty
    X = X.dropna(axis=1, how="all")

    # fill missing values
    X = X.fillna(X.median(numeric_only=True))

    # remove constant columns
    nunique = X.nunique(dropna=True)
    constant_cols = nunique[nunique <= 1].index.tolist()
    if constant_cols:
        excluded_cols.extend(constant_cols)
        X = X.loc[:, nunique > 1]

    if X.empty:
        raise ValueError("No usable numeric predictor columns remained after exclusions.")

    return X, y, sorted(set(excluded_cols))


def run_pca(X: pd.DataFrame):
    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)

    pca_full = PCA()
    pca_full.fit(X_scaled)

    cum_var = np.cumsum(pca_full.explained_variance_ratio_)
    n_components = np.searchsorted(cum_var, EXPLAINED_VARIANCE_THRESHOLD) + 1

    pca = PCA(n_components=n_components)
    X_pca = pca.fit_transform(X_scaled)

    pc_names = [f"PC{i+1}" for i in range(n_components)]

    loadings = pd.DataFrame(
        pca.components_.T,
        index=X.columns,
        columns=pc_names
    )

    explained = pd.DataFrame({
        "component": pc_names,
        "explained_variance_ratio": pca.explained_variance_ratio_,
        "cumulative_explained_variance": np.cumsum(pca.explained_variance_ratio_)
    })

    scores = pd.DataFrame(X_pca, columns=pc_names)

    return scaler, pca, explained, loadings, scores, pca_full


def get_top_features_from_loadings(loadings: pd.DataFrame, top_n: int = 5) -> dict:
    result = {}
    for pc in loadings.columns:
        top = loadings[pc].abs().sort_values(ascending=False).head(top_n)
        result[pc] = top.index.tolist()
    return result


def plot_scree(pca_full: PCA, out_path: Path, title: str):
    explained = pca_full.explained_variance_ratio_
    cumulative = np.cumsum(explained)
    x = np.arange(1, len(explained) + 1)

    plt.figure(figsize=(10, 5))
    plt.plot(x, explained, marker="o", label="Explained variance ratio")
    plt.plot(x, cumulative, marker="o", label="Cumulative explained variance")
    plt.axhline(EXPLAINED_VARIANCE_THRESHOLD, linestyle="--", label=f"{EXPLAINED_VARIANCE_THRESHOLD:.0%} threshold")
    plt.xlabel("Principal Component")
    plt.ylabel("Variance Explained")
    plt.title(f"{title} - PCA Scree Plot (Gear Oil Temperature)")
    plt.legend()
    plt.tight_layout()
    plt.savefig(out_path)
    plt.close()


def process_file(path: Path, output_dir: Path) -> dict:
    df, time_col, target_col = load_cleaned_file(path)
    X, y, excluded_cols = build_feature_matrix(df, time_col, target_col)

    scaler, pca, explained, loadings, scores, pca_full = run_pca(X)
    top_features = get_top_features_from_loadings(loadings, TOP_FEATURES_PER_PC)

    stem = path.stem

    explained.to_csv(
        output_dir / f"{stem}_gear_oil_temperature_explained_variance.csv",
        index=False
    )

    loadings.to_csv(
        output_dir / f"{stem}_gear_oil_temperature_pca_loadings.csv"
    )

    transformed = scores.copy()
    transformed[target_col] = y.reset_index(drop=True)
    transformed.to_csv(
        output_dir / f"{stem}_gear_oil_temperature_pca_scores.csv",
        index=False
    )

    pd.DataFrame({"excluded_columns": excluded_cols}).to_csv(
        output_dir / f"{stem}_gear_oil_temperature_excluded_columns.csv",
        index=False
    )

    plot_scree(
        pca_full,
        output_dir / f"{stem}_gear_oil_temperature_scree_plot.png",
        stem
    )

    summary_json = {
        "file": path.name,
        "target_column": target_col,
        "input_feature_count_after_exclusion": int(X.shape[1]),
        "rows_used": int(X.shape[0]),
        "selected_component_count": int(len(explained)),
        "explained_variance_threshold": EXPLAINED_VARIANCE_THRESHOLD,
        "excluded_columns_count": len(excluded_cols),
        "excluded_columns": excluded_cols,
        "top_features_per_pc": top_features,
    }

    with open(
        output_dir / f"{stem}_gear_oil_temperature_pca_summary.json",
        "w",
        encoding="utf-8"
    ) as f:
        json.dump(summary_json, f, indent=2)

    return {
        "file": path.name,
        "target_column": target_col,
        "rows_used": int(X.shape[0]),
        "input_feature_count_after_exclusion": int(X.shape[1]),
        "selected_component_count": int(len(explained)),
        "excluded_columns_count": len(excluded_cols),
        "final_cumulative_explained_variance": float(explained["cumulative_explained_variance"].iloc[-1]),
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
                f"✓ PCA done: {file_path.name} | "
                f"target={result['target_column']} | "
                f"features={result['input_feature_count_after_exclusion']} | "
                f"excluded={result['excluded_columns_count']} | "
                f"components={result['selected_component_count']} | "
                f"cum_var={result['final_cumulative_explained_variance']:.4f}"
            )
        except Exception as e:
            print(f"✗ Failed: {file_path.name} | {e}")
            summaries.append({
                "file": file_path.name,
                "target_column": None,
                "rows_used": None,
                "input_feature_count_after_exclusion": None,
                "selected_component_count": None,
                "excluded_columns_count": None,
                "final_cumulative_explained_variance": None,
                "error": str(e),
            })

    pd.DataFrame(summaries).to_csv(output_dir / SUMMARY_FILE, index=False)

    print(f"\nDone. PCA results saved in: {output_dir.resolve()}")
    print(f"Summary saved to: {(output_dir / SUMMARY_FILE).resolve()}")


if __name__ == "__main__":
    main()