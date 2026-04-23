import os
import pandas as pd
import numpy as np

# =========================
# CONFIG
# =========================
FOLDER_PATH = "../data/raw/Turbine_Data_Kelmarsh_1_2019-01-01_-_2020-01-01_228.csv"
TARGET_COLUMN = "Gear oil temperature (°C)"
CORR_THRESHOLD = 0.5
MULTICOL_THRESHOLD = 0.95


# =========================
# LOAD CSV FILES
# =========================
def load_data(folder_path: str) -> pd.DataFrame:
    dfs = []

    if not os.path.isdir(folder_path):
        raise FileNotFoundError(f"Folder not found: {folder_path}")

    for file_name in os.listdir(folder_path):
        if file_name.lower().endswith(".csv"):
            file_path = os.path.join(folder_path, file_name)
            print(f"Loading: {file_name}")

            try:
                # Try utf-8 first
                df = pd.read_csv(file_path, encoding="utf-8")
            except:
                try:
                    # Fallback encoding
                    df = pd.read_csv(file_path, encoding="latin1")
                except Exception as e:
                    print(f"Skipping {file_name} due to read error: {e}")
                    continue

            # Fix encoding issues in headers
            df.columns = (
                df.columns
                .str.replace("Â°C", "°C", regex=False)
                .str.replace("Â°", "°", regex=False)
                .str.strip()
            )

            dfs.append(df)

    if not dfs:
        raise ValueError("No CSV files loaded.")

    combined_df = pd.concat(dfs, ignore_index=True)
    print(f"\nTotal rows: {combined_df.shape[0]}")
    print(f"Total columns: {combined_df.shape[1]}")
    return combined_df


# =========================
# FEATURE SELECTION
# =========================
def select_features(df: pd.DataFrame):

    if TARGET_COLUMN not in df.columns:
        raise ValueError(f"Target column '{TARGET_COLUMN}' not found.")

    # Keep numeric only
    df = df.select_dtypes(include=[np.number])

    # Drop missing target rows
    df = df.dropna(subset=[TARGET_COLUMN])

    print("\nCalculating correlations...")

    corr = df.corr()
    target_corr = corr[TARGET_COLUMN].drop(TARGET_COLUMN)

    # Sort by absolute correlation
    target_corr = target_corr.reindex(
        target_corr.abs().sort_values(ascending=False).index
    )

    print("\nTop correlations:")
    print(target_corr.head(15))

    # STEP 1 — Select strong features
    selected = target_corr[abs(target_corr) > CORR_THRESHOLD].index.tolist()

    print(f"\nSelected features (> {CORR_THRESHOLD}): {len(selected)}")

    # STEP 2 — Remove multicollinearity
    corr_matrix = df[selected].corr().abs()

    upper = corr_matrix.where(
        np.triu(np.ones(corr_matrix.shape), k=1).astype(bool)
    )

    to_drop = [col for col in upper.columns if any(upper[col] > MULTICOL_THRESHOLD)]

    reduced = [col for col in selected if col not in to_drop]

    print(f"Removed due to multicollinearity (> {MULTICOL_THRESHOLD}): {len(to_drop)}")

    # STEP 3 — Remove leakage
    final_features = []
    leakage_removed = []

    for col in reduced:
        if "gear oil temperature" in col.lower():
            leakage_removed.append(col)
        else:
            final_features.append(col)

    print(f"Removed due to leakage: {len(leakage_removed)}")

    print("\nFinal features:")
    for f in final_features:
        print(f"- {f}")

    # Final dataset
    final_df = df[final_features + [TARGET_COLUMN]]

    return {
        "final_df": final_df,
        "target_corr": target_corr,
        "selected": selected,
        "to_drop": to_drop,
        "leakage_removed": leakage_removed,
        "final_features": final_features
    }


# =========================
# SAVE OUTPUT
# =========================
def save_outputs(folder_path, results):

    data_path = os.path.join(folder_path, "selected_features.csv")
    report_path = os.path.join(folder_path, "feature_selection_report.txt")

    # Save dataset
    results["final_df"].to_csv(data_path, index=False)

    # Save report
    with open(report_path, "w", encoding="utf-8") as f:
        f.write("FEATURE SELECTION REPORT\n")
        f.write("========================\n\n")

        f.write(f"Target: {TARGET_COLUMN}\n\n")
        f.write(f"Correlation threshold: {CORR_THRESHOLD}\n")
        f.write(f"Multicollinearity threshold: {MULTICOL_THRESHOLD}\n\n")

        f.write("Top correlations:\n")
        f.write(results["target_corr"].head(15).to_string())
        f.write("\n\n")

        f.write("Selected features:\n")
        for col in results["selected"]:
            f.write(f"- {col}\n")

        f.write("\nRemoved (multicollinearity):\n")
        for col in results["to_drop"]:
            f.write(f"- {col}\n")

        f.write("\nRemoved (leakage):\n")
        for col in results["leakage_removed"]:
            f.write(f"- {col}\n")

        f.write("\nFinal features:\n")
        for col in results["final_features"]:
            f.write(f"- {col}\n")

        f.write(f"\nFinal dataset shape: {results['final_df'].shape}\n")

    print(f"\nSaved dataset: {data_path}")
    print(f"Saved report: {report_path}")


# =========================
# MAIN
# =========================
def main():
    df = load_data(FOLDER_PATH)
    results = select_features(df)
    save_outputs(FOLDER_PATH, results)


if __name__ == "__main__":
    main()