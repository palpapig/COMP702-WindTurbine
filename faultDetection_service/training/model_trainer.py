from __future__ import annotations

from datetime import datetime, timezone
import numpy as np
import pandas as pd
from sklearn.ensemble import BaggingRegressor
from sklearn.metrics import mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.neighbors import KNeighborsRegressor
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from app.config.settings import get_model_settings, get_turbines_registry, save_turbines_registry
from app.ml.model_registry import save_bundle
from app.ml.feature_config import ensure_turbine_registered


class ModelTrainer:
    def train_from_rows(
        self,
        turbine_id: str,
        rows: list[dict],
        target_column: str,
        feature_columns: list[str],
    ) -> dict:
        settings = get_model_settings()
        min_rows = int(settings["minimum_training_rows"])
        if len(rows) < min_rows:
            raise ValueError(f"Not enough rows to train turbine '{turbine_id}'. Need at least {min_rows} rows.")

        df = pd.DataFrame(rows)
        if target_column not in df.columns:
            raise ValueError(f"Target column '{target_column}' not found in training rows.")

        missing_features = [f for f in feature_columns if f not in df.columns]
        if missing_features:
            raise ValueError(f"Missing training features: {missing_features}")

        work_df = df[feature_columns + [target_column]].copy()
        for col in work_df.columns:
            work_df[col] = pd.to_numeric(work_df[col], errors="coerce")
        work_df = work_df.dropna()

        X = work_df[feature_columns]
        y = work_df[target_column]

        if len(work_df) < min_rows:
            raise ValueError(f"Not enough valid numeric rows after cleaning. Kept {len(work_df)}, need {min_rows}.")

        test_size = float(settings["test_size"])
        random_state = int(settings["random_state"])
        model_cfg = settings["knn_bagging"]

        X_train, X_test, y_train, y_test = train_test_split(
            X, y, test_size=test_size, random_state=random_state
        )

        base_knn = KNeighborsRegressor(
            n_neighbors=int(model_cfg["n_neighbors"]),
            weights=model_cfg["weights"],
            metric=model_cfg["metric"],
        )

        regressor = BaggingRegressor(
            estimator=base_knn,
            n_estimators=int(model_cfg["n_estimators"]),
            random_state=random_state,
        )

        pipeline = Pipeline([
            ("scaler", StandardScaler()),
            ("model", regressor),
        ])

        pipeline.fit(X_train, y_train)
        predictions = pipeline.predict(X_test)

        rmse = float(np.sqrt(mean_squared_error(y_test, predictions)))
        r2 = float(r2_score(y_test, predictions))
        residual_std = float(np.std(predictions - y_test, ddof=1)) if len(y_test) > 1 else 0.0

        metadata = {
            "turbine_id": turbine_id,
            "trained_at_utc": datetime.now(timezone.utc).isoformat(),
            "model_type": settings["model_type"],
            "target_column": target_column,
            "feature_columns": feature_columns,
            "rows_used": int(len(work_df)),
            "metrics": {
                "rmse": rmse,
                "r2": r2,
                "residual_std": residual_std,
            },
        }

        paths = save_bundle(turbine_id, pipeline, None, metadata)

        registry = get_turbines_registry()
        turbines = registry.setdefault("turbines", {})
        turbines[turbine_id] = {
            "target_column": target_column,
            "feature_columns": feature_columns,
            "status": "trained",
            "last_trained_at_utc": metadata["trained_at_utc"],
        }
        save_turbines_registry(registry)
        ensure_turbine_registered(turbine_id)

        return {
            "turbineId": turbine_id,
            "modelStatus": "trained",
            "rowsUsed": int(len(work_df)),
            "targetColumn": target_column,
            "featureColumns": feature_columns,
            "metrics": metadata["metrics"],
            "modelPath": paths["model"],
        }


model_trainer = ModelTrainer()
