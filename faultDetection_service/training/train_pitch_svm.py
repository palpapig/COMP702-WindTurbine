# this file trains a Support Vector Regression (SVR) model to predict wind turbine blade pitch angle from wind speed
# It cleans the data, samples 15% (because SVR is slow on full 129k rows), scales features, sets fixed hyperparameters 
# (C=10, gamma='scale', epsilon = IQR(pitch)/13.349 from Pandit & Infield 2019), trains on 80% of the sample, 
# evaluates on the remaining 20% (R²=0.976, MAE=0.21°) & saves the model + scaler for later filtering.

import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.svm import SVR
from sklearn.preprocessing import StandardScaler
import os
import sys

sys.path.append(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
from training.utils import (
    calculate_epsilon_from_iqr, evaluate_model, save_model_with_scaler
)

# ------------------------------------------------------------
# 1. Load data
# ------------------------------------------------------------
data_path = "data/cleaned/turbine1_clean_cleaned.csv"
df = pd.read_csv(data_path)
print(f"Total rows available: {len(df)}")

# ------------------------------------------------------------
# 2. SAMPLE for speed – SVR training time grows with the square of the number of rows
#    With 129k rows, training would take hours. Sampling 15% (19k rows) is a practical
#    trade‑off that still gives excellent accuracy (R² > 0.97 which means it explains 97.58% of the variation in pitch angle)
# ------------------------------------------------------------
sample_frac = 0.15   # adjust to 0.2 if we want more, but 0.15 is fine
if len(df) > 20000:
    df = df.sample(frac=sample_frac, random_state=42)
    print(f"Using {len(df)} rows (sampled {sample_frac*100:.0f}%) for training")
else:
    print(f"Using all {len(df)} rows")

X = df[["windSpeed"]].values
y = df["pitchAngle"].values

# ------------------------------------------------------------
# 3. Train/test split
# ------------------------------------------------------------
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42
)
print(f"Training samples: {len(X_train)}, Test samples: {len(X_test)}")

# ------------------------------------------------------------
# 4. Scale features
# ------------------------------------------------------------
scaler = StandardScaler()
X_train_scaled = scaler.fit_transform(X_train)
X_test_scaled = scaler.transform(X_test)

# ------------------------------------------------------------
# 5. Set hyperparameters – NO GRID SEARCH (too slow)
#    fixed hyperparameters instead of grid search because:
#       - C=10 works well for most regression problems (moderate regularisation).
#       - gamma='scale' is sklearn's default and handles feature scaling automatically.
#       - epsilon = IQR/13.349 comes directly from the Pandit & Infield paper
# ------------------------------------------------------------
epsilon = calculate_epsilon_from_iqr(y_train)
print(f"Using epsilon = {epsilon:.4f} (heuristic: IQR/13.349)")

svr = SVR(
    kernel='rbf',
    C=10,                # moderate regularization
    gamma='scale',       # default scaling
    epsilon=epsilon
)

# ------------------------------------------------------------
# 6. Train the model
# ------------------------------------------------------------
print("Training SVR (this will take 2-3 minutes)...")
svr.fit(X_train_scaled, y_train)
print("Training complete.")

# ------------------------------------------------------------
# 7. Evaluate on test set
# ------------------------------------------------------------
y_pred = svr.predict(X_test_scaled)
metrics = evaluate_model(y_test, y_pred)

# ------------------------------------------------------------
# 8. Save model and scaler
# ------------------------------------------------------------
os.makedirs("artifacts", exist_ok=True)
save_model_with_scaler(svr, scaler, "artifacts/pitch_svm_model.pkl")
print("Model saved. Ready for filtering.")