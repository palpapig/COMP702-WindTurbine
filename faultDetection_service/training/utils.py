# training/utils.py
# Helper functions for the SVM pitch prediction project
# References:
# - Pandit & Infield 2019 (SVR with RBF kernel)
# - Zhang et al. 2024 (residual-based anomaly detection)

import numpy as np
import matplotlib.pyplot as plt
import joblib
from sklearn.metrics import r2_score, mean_absolute_error, mean_squared_error

# Pandit & Infield 2019 use epsilon = IQR(pitch) / 13.349 for blade pitch curve modeling
# we followed the paper exactly because this heuristic is specific to wind turbine pitch control,
# & the paper demonstrated good results with this value.
def calculate_epsilon_from_iqr(y, factor=13.349):
    """
    Compute epsilon for SVR using the heuristic from Pandit & Infield 2019.
    They used epsilon = IQR(y) / 13.349.
    y: array-like of target values (pitch angles)
    """
    q75, q25 = np.percentile(y, [75, 25])
    iqr = q75 - q25
    return iqr / factor

def evaluate_model(y_true, y_pred):
    """
    Calculate and print regression metrics.
    Returns a dict with R2, RMSE, MAE.
    """
    r2 = r2_score(y_true, y_pred)
    rmse = np.sqrt(mean_squared_error(y_true, y_pred))
    mae = mean_absolute_error(y_true, y_pred)
    print(f"Model evaluation (test set):")
    print(f"  R²  = {r2:.4f}")
    print(f"  RMSE= {rmse:.4f}°")
    print(f"  MAE = {mae:.4f}°")
    return {"R2": r2, "RMSE": rmse, "MAE": mae}

def save_model_with_scaler(model, scaler, filepath):
    """Save both the trained SVR and the StandardScaler together."""
    joblib.dump({"model": model, "scaler": scaler}, filepath)
    print(f"Model and scaler saved to {filepath}")

def load_model_with_scaler(filepath):
    """Load the saved model and scaler."""
    data = joblib.load(filepath)
    return data["model"], data["scaler"]

def plot_pitch_time_series(df_original, df_filtered, predictions, output_path="pitch_time_series.png"):
    """
    Acceptance test graph 1:
    Actual vs predicted pitch over the first N rows (time series).
    Uses index as time order (since CSV does not have timestamp column).
    """
    #we only plot the first 500 rows to keep the graph readable, using row index as time order
    n_plot = min(500, len(df_original))
    
    plt.figure(figsize=(12, 5))
    plt.plot(range(n_plot), df_original["pitchAngle"].iloc[:n_plot], 
             'b-', label="Actual pitch", linewidth=1.5, alpha=0.8)
    plt.plot(range(n_plot), predictions[:n_plot], 
             'r--', label="SVM predicted pitch", linewidth=1.5, alpha=0.8)
    plt.xlabel("Sample index (time order)")
    plt.ylabel("Blade pitch angle (degrees)")
    plt.title("Blade Pitch: Actual vs SVM-Predicted (Time Series)")
    plt.legend()
    plt.grid(True, alpha=0.3)
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close()
    print(f"Time series graph saved to {output_path}")

def plot_pitch_curve(df_original, model, scaler, output_path="pitch_curve.png"):
    """
    Acceptance test graph 2:
    Scatter plot of actual pitch vs wind speed, with SVR prediction curve.
    """
    # Create dense wind speed range for smooth prediction curve
    # Using 200 points gives a smooth curve without excessive computation.
    wind_range = np.linspace(0, 25, 200).reshape(-1, 1)
    # Scale using the same scaler (fitted on training data)
    wind_scaled = scaler.transform(wind_range)
    pitch_pred = model.predict(wind_scaled)
    
    plt.figure(figsize=(10, 6))
    # Actual data as faint scatter points
    plt.scatter(df_original["windSpeed"], df_original["pitchAngle"], 
                c="gray", s=1, alpha=0.4, label="Actual data points")
    # SVR prediction curve
    plt.plot(wind_range, pitch_pred, 'r-', linewidth=2.5, label="SVM predicted pitch curve")
    plt.xlabel("Wind speed (m/s)")
    plt.ylabel("Blade pitch angle (degrees)")
    plt.title("Blade Pitch Curve: Actual Data and SVR Fit")
    plt.legend()
    plt.grid(True, alpha=0.3)
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close()
    print(f"Pitch curve graph saved to {output_path}")