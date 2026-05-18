from __future__ import annotations

from datetime import datetime, timezone

import pandas as pd
from sklearn.ensemble import BaggingRegressor
from sklearn.impute import SimpleImputer
from sklearn.metrics import mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.neighbors import KNeighborsRegressor
from sklearn.pipeline import Pipeline

from app.ml.model_registry import model_exists, load_bundle, save_bundle
from app.models.request_models import TrainRequest
from app.models.response_models import Metrics, TrainResponse


class ModelTrainingService:
    def train_one_turbine(self, request: TrainRequest) -> TrainResponse:
        if not request.rows:
            raise ValueError("Training request contains no rows.")

        if model_exists(request.turbine_id) and not request.force_retrain:
            existing = load_bundle(request.turbine_id)
            if existing is None:
                raise ValueError("Model exists check passed, but saved bundle could not be loaded.")

            metadata = existing["metadata"]

            return TrainResponse(
                turbine_id=request.turbine_id,
                model_status="already_trained",
                rows_used=metadata.get("rowsUsed", 0),
                target_column=metadata.get("targetColumn", ""),
                feature_columns=metadata.get("featureColumns", []),
                metrics=Metrics(
                    rmse=metadata.get("metrics", {}).get("rmse"),
                    r2=metadata.get("metrics", {}).get("r2"),
                ),
                model_path=str(existing["paths"]["model"]),
            )

        df = self._rows_to_dataframe(request)

        target_column = request.target_column or self._infer_target_column(df)
        if target_column not in df.columns:
            raise ValueError(f"Target column '{target_column}' not found in training data.")

        feature_columns = request.feature_columns or self._infer_feature_columns(df, target_column)

        missing_features = [col for col in feature_columns if col not in df.columns]
        if missing_features:
            raise ValueError(f"Missing feature columns in training data: {missing_features}")

        working_df = df[feature_columns + [target_column]].copy()
        working_df = working_df.dropna(subset=[target_column])

        if working_df.empty:
            raise ValueError("No usable rows remain after dropping rows with missing target values.")

        if len(working_df) < 5:
            raise ValueError("Not enough usable rows to train a model. Need at least 5.")

        x = working_df[feature_columns]
        y = working_df[target_column]

        x_train, x_test, y_train, y_test = train_test_split(
            x, y, test_size=0.2, random_state=42
        )

        model = Pipeline(
            steps=[
                ("imputer", SimpleImputer(strategy="mean")),
                (
                    "regressor",
                    BaggingRegressor(
                        estimator=KNeighborsRegressor(n_neighbors=5),
                        n_estimators=10,
                        random_state=42,
                    ),
                ),
            ]
        )

        model.fit(x_train, y_train)
        y_pred = model.predict(x_test)

        rmse = float(mean_squared_error(y_test, y_pred) ** 0.5)
        r2 = float(r2_score(y_test, y_pred))

        metadata = {
            "turbineId": request.turbine_id,
            "trainedAt": datetime.now(timezone.utc).isoformat(),
            "rowsUsed": int(len(working_df)),
            "targetColumn": target_column,
            "featureColumns": feature_columns,
            "metrics": {
                "rmse": rmse,
                "r2": r2,
            },
        }

        saved_paths = save_bundle(
            turbine_id=request.turbine_id,
            model=model,
            scaler=None,
            metadata=metadata,
        )

        return TrainResponse(
            turbine_id=request.turbine_id,
            model_status="trained",
            rows_used=int(len(working_df)),
            target_column=target_column,
            feature_columns=feature_columns,
            metrics=Metrics(rmse=rmse, r2=r2),
            model_path=saved_paths["model"],
        )

    def _rows_to_dataframe(self, request: TrainRequest) -> pd.DataFrame:
        records: list[dict] = []

        for row in request.rows:
            record: dict = {}

            if row.timestamp is not None:
                record["timestamp"] = row.timestamp

            for key, value in row.values.items():
                record[key] = value

            records.append(record)

        return pd.DataFrame(records)

    def _infer_target_column(self, df: pd.DataFrame) -> str:
        candidate_names = [
            "Gost",
            "gost",
            "gearOilTemp",
            "gearboxOilSumpTemperature",
            "target",
            "power",
            "Power",
        ]

        for name in candidate_names:
            if name in df.columns:
                return name

        raise ValueError(
            "No target column was provided and no default target column could be inferred."
        )

    def _infer_feature_columns(self, df: pd.DataFrame, target_column: str) -> list[str]:
        excluded = {"timestamp", target_column}
        numeric_columns: list[str] = []

        for col in df.columns:
            if col in excluded:
                continue
            if pd.api.types.is_numeric_dtype(df[col]):
                numeric_columns.append(col)

        if not numeric_columns:
            raise ValueError("No numeric feature columns found for training.")

        return numeric_columns