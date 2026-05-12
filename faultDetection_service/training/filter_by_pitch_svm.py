# Use the trained SVR model to detect curtailment:
# Remove rows where actual pitch deviates more than 2 degrees from predicted pitch
# Also produces the two graphs

import pandas as pd
import numpy as np
import os
import sys
sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from training.utils import load_model_with_scaler, plot_pitch_time_series, plot_pitch_curve

# ----------------------------------------------------------------------
# Configuration – we can change these parameters
# ----------------------------------------------------------------------
INPUT_CSV = "data/cleaned/turbine1_clean_cleaned.csv"
OUTPUT_CSV = "data/filtered/turbine1_clean_curtailment_removed.csv"
MODEL_PATH = "artifacts/pitch_svm_model.pkl"
RESIDUAL_THRESHOLD = 2.0   # degrees – any row where blade pitch differs by more than 2° should be removed

# ----------------------------------------------------------------------
# 1. load the saved model & scaler
# ----------------------------------------------------------------------
if not os.path.exists(MODEL_PATH):
    raise FileNotFoundError(f"Model not found at {MODEL_PATH}. Run train_pitch_svm.py first.")
# the model and scaler were saved together using joblib. This way we don't have to retrain; we just load & apply to any new data
model, scaler = load_model_with_scaler(MODEL_PATH)
print(f"Model loaded from {MODEL_PATH}")

# ----------------------------------------------------------------------
# 2. load input data
# ----------------------------------------------------------------------
if not os.path.exists(INPUT_CSV):
    raise FileNotFoundError(f"Input CSV not found: {INPUT_CSV}")

df = pd.read_csv(INPUT_CSV)
original_count = len(df)
print(f"Loaded {original_count} rows from {INPUT_CSV}")

# validate required columns
if "windSpeed" not in df.columns or "pitchAngle" not in df.columns:
    raise ValueError("Input CSV must contain 'windSpeed' and 'pitchAngle' columns.")

# ----------------------------------------------------------------------
# 3. predict pitch for all rows
#    we predict on the full cleaned dataset (129,557 rows so far) because
#    prediction is fast (linear complexity) even though training was slow
# ----------------------------------------------------------------------
X = df[["windSpeed"]].values
X_scaled = scaler.transform(X)
predicted_pitch = model.predict(X_scaled)

# compute absolute residual
residual_abs = np.abs(df["pitchAngle"].values - predicted_pitch)

# ----------------------------------------------------------------------
# 4. apply curtailment filter: keep rows where residual ≤ threshold
# ----------------------------------------------------------------------
mask_keep = residual_abs <= RESIDUAL_THRESHOLD
filtered_df = df[mask_keep].copy()
filtered_count = len(filtered_df)

print(f"\nFiltering with threshold = {RESIDUAL_THRESHOLD}°")
print(f"Rows removed: {original_count - filtered_count} ({(1 - filtered_count/original_count)*100:.1f}%)")
print(f"Rows kept: {filtered_count}")

# add predicted pitch & residual as new columns for debugging 
filtered_df["predicted_pitch"] = predicted_pitch[mask_keep]
filtered_df["residual_abs"] = residual_abs[mask_keep]

# ----------------------------------------------------------------------
# 5. save filtered CSV – preserve all original columns
# ----------------------------------------------------------------------
os.makedirs(os.path.dirname(OUTPUT_CSV), exist_ok=True)
# save with extra columns (for verification)
filtered_df.to_csv(OUTPUT_CSV, index=False)
print(f"Filtered data saved to {OUTPUT_CSV}")

# ----------------------------------------------------------------------
# 6. generate graphs (using original unfiltered data)
#    so we can visually confirm that the model fits well even on rows that might be removed
# ----------------------------------------------------------------------
print("\nGenerating graphs...")
plot_pitch_time_series(df, filtered_df, predicted_pitch, 
                       output_path="pitch_time_series.png")
plot_pitch_curve(df, model, scaler, output_path="pitch_curve.png")

print("\nAll done. Check the images and the filtered CSV.")