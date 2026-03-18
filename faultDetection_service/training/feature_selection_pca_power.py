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
OUTPUT_FOLDER = BASE_DIR / "pcaResults"
SUMMARY_FILE = "pca_summary.csv"

TIME_COL_OPTIONS = ["# Date and time", "Date and time", "time", "Time", "Timestamp"]
TARGET_COL_OPTIONS = ["Power (kW)", "Active Power (kW)", "ActivePower", "Power"]

# Keep components until this much variance is explained
EXPLAINED_VARIANCE_THRESHOLD = 0.95

# Number of strongest original variables to report per principal component
TOP_FEATURES_PER_PC = 5


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
        raise ValueError(f"Could not find target power column in {path.name}")

    if time_col and time_col in df.columns:
        df[time_col] = pd.to_datetime(df[time_col], errors="coerce")

    for col in df.columns:
        if col != time_col:
            df[col] = pd.to_numeric(df[col], errors="coerce")

    return df, time_col, target_col


def build_feature_matrix(df: pd.DataFrame, time_col: str | None, target_col: str) -> tuple[pd.DataFrame, pd.Series]:
    drop_cols = [target_col]
    if time_col and time_col in df.columns:
        drop_cols.append(time_col)

    X = df.drop(columns=drop_cols, errors="ignore").copy()
    y = df[target_col].copy()

    # numeric predictors only
    X = X.select_dtypes(include=[np.number])

    # drop columns with all missing values
    X = X.dropna(axis=1, how="all")

    # fill remaining missing values with column median
    X = X.fillna(X.median(numeric_only=True))

    # remove constant columns
    nunique = X.nunique(dropna=True)
    X = X.loc[:, nunique > 1]

    if X.empty:
        raise ValueError("No usable numeric predictor columns remained after preprocessing.")

    return X, y


def run_pca(X: pd.DataFrame):
    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(X)

    # fit full PCA first
    pca_full = PCA()
    X_pca_full = pca_full.fit_transform(X_scaled)

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
    plt.title(f"{title} - PCA Scree Plot")
    plt.legend()
    plt.tight_layout()
    plt.savefig(out_path)
    plt.close()


def process_file(path: Path, output_dir: Path) -> dict:
    df, time_col, target_col = load_cleaned_file(path)
    X, y = build_feature_matrix(df, time_col, target_col)

    scaler, pca, explained, loadings, scores, pca_full = run_pca(X)
    top_features = get_top_features_from_loadings(loadings, TOP_FEATURES_PER_PC)

    stem = path.stem

    explained.to_csv(output_dir / f"{stem}_explained_variance.csv", index=False)
    loadings.to_csv(output_dir / f"{stem}_pca_loadings.csv")

    transformed = scores.copy()
    transformed[target_col] = y.reset_index(drop=True)
    transformed.to_csv(output_dir / f"{stem}_pca_scores.csv", index=False)

    plot_scree(pca_full, output_dir / f"{stem}_scree_plot.png", stem)

    summary_json = {
        "file": path.name,
        "target_column": target_col,
        "input_feature_count": int(X.shape[1]),
        "rows_used": int(X.shape[0]),
        "selected_component_count": int(len(explained)),
        "explained_variance_threshold": EXPLAINED_VARIANCE_THRESHOLD,
        "top_features_per_pc": top_features,
    }

    with open(output_dir / f"{stem}_pca_summary.json", "w", encoding="utf-8") as f:
        json.dump(summary_json, f, indent=2)

    return {
        "file": path.name,
        "target_column": target_col,
        "rows_used": int(X.shape[0]),
        "input_feature_count": int(X.shape[1]),
        "selected_component_count": int(len(explained)),
        "final_cumulative_explained_variance": float(explained["cumulative_explained_variance"].iloc[-1]),
    }


def main():
    input_dir = INPUT_FOLDER
    output_dir = OUTPUT_FOLDER
    output_dir.mkdir(parents=True, exist_ok=True)

    files = sorted(input_dir.glob("*.csv"))
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
                f"features={result['input_feature_count']} | "
                f"components={result['selected_component_count']} | "
                f"cum_var={result['final_cumulative_explained_variance']:.4f}"
            )
        except Exception as e:
            print(f"✗ Failed: {file_path.name} | {e}")
            summaries.append({
                "file": file_path.name,
                "target_column": None,
                "rows_used": None,
                "input_feature_count": None,
                "selected_component_count": None,
                "final_cumulative_explained_variance": None,
                "error": str(e),
            })

    pd.DataFrame(summaries).to_csv(output_dir / SUMMARY_FILE, index=False)
    print(f"\nDone. PCA results saved in: {output_dir.resolve()}")
    print(f"Summary saved to: {(output_dir / SUMMARY_FILE).resolve()}")


if __name__ == "__main__":
    main()