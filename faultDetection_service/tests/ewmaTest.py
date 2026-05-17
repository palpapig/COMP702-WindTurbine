from pathlib import Path
import json
import joblib
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt


class EwmaChartGenerator:
    """
    Reads a CSV file, predicts target values using the saved model,
    calculates residuals, applies EWMA alarm logic, saves an EWMA chart,
    saves an actual-vs-predicted chart, and optionally exports results to CSV.
    """

    def __init__(
        self,
        model_path: str | Path,
        metadata_path: str | Path,
        ewma_lambda: float = 0.2,
        control_limit_k: float = 4.0, # 3 originaly
        a2_consecutive_count: int = 3,
    ):
        self.model_path = Path(model_path)
        self.metadata_path = Path(metadata_path)

        self.ewma_lambda = ewma_lambda
        self.control_limit_k = control_limit_k
        self.a2_consecutive_count = a2_consecutive_count

        self.model = None
        self.metadata = None
        self.feature_columns = []
        self.target_column = ""
        self.residual_std = 0.0

        self.previous_ewma = 0.0
        self.consecutive_a1_count = 0

        self._load_model_and_metadata()

    def _load_model_and_metadata(self):
        if not self.model_path.exists():
            raise FileNotFoundError(f"Model file not found: {self.model_path}")

        if not self.metadata_path.exists():
            raise FileNotFoundError(f"Metadata file not found: {self.metadata_path}")

        self.model = joblib.load(self.model_path)

        with open(self.metadata_path, "r", encoding="utf-8") as file:
            self.metadata = json.load(file)

        self.feature_columns = self.metadata["feature_columns"]
        self.target_column = self.metadata["target_column"]
        self.residual_std = self.metadata["metrics"]["residual_std"]

    def _evaluate_alarm(self, residual: float) -> dict:
        ewma = (
            self.ewma_lambda * residual
            + (1 - self.ewma_lambda) * self.previous_ewma
        )

        self.previous_ewma = ewma

        upper_limit = self.control_limit_k * self.residual_std
        lower_limit = -self.control_limit_k * self.residual_std

        is_a1 = ewma > upper_limit or ewma < lower_limit

        if is_a1:
            self.consecutive_a1_count += 1
        else:
            self.consecutive_a1_count = 0

        is_a2 = self.consecutive_a1_count >= self.a2_consecutive_count

        return {
            "ewma": ewma,
            "upper_limit": upper_limit,
            "lower_limit": lower_limit,
            "is_a1": is_a1,
            "is_a2": is_a2,
        }

    def generate_from_csv(
        self,
        csv_path: str | Path,
        output_graph_path: str | Path,
        output_results_path: str | Path | None = None,
        output_prediction_graph_path: str | Path | None = None,
    ) -> pd.DataFrame:
        csv_path = Path(csv_path)
        output_graph_path = Path(output_graph_path)

        if not csv_path.exists():
            raise FileNotFoundError(f"CSV file not found: {csv_path}")

        df = pd.read_csv(csv_path)
        df["Timestamp"] = pd.to_datetime(df["Timestamp"])
        df = df.sort_values(by="Timestamp").reset_index(drop=True)

        missing_features = [
            col for col in self.feature_columns
            if col not in df.columns
        ]

        if missing_features:
            raise ValueError(f"Missing feature columns: {missing_features}")

        if self.target_column not in df.columns:
            raise ValueError(
                f"Target column '{self.target_column}' not found in CSV."
            )

        work_df = df[self.feature_columns + [self.target_column]].copy()

        for col in work_df.columns:
            work_df[col] = pd.to_numeric(work_df[col], errors="coerce")

        work_df = work_df.dropna()

        if work_df.empty:
            raise ValueError("No valid rows left after cleaning.")

        x = work_df[self.feature_columns]
        actual_values = work_df[self.target_column].to_numpy()

        predicted_values = self.model.predict(x)
        residuals = actual_values - predicted_values

        ewma_values = []
        upper_limits = []
        lower_limits = []
        a1_values = []
        a2_values = []

        self.previous_ewma = 0.0
        self.consecutive_a1_count = 0

        for residual in residuals:
            alarm_result = self._evaluate_alarm(float(residual))

            ewma_values.append(alarm_result["ewma"])
            upper_limits.append(alarm_result["upper_limit"])
            lower_limits.append(alarm_result["lower_limit"])
            a1_values.append(1 if alarm_result["is_a1"] else 0)
            a2_values.append(1 if alarm_result["is_a2"] else 0)

        result_df = pd.DataFrame()

        # Timestamp
        if "Timestamp" in df.columns:
            result_df["Timestamp"] = df.loc[work_df.index, "Timestamp"].values
        elif "timestamp" in df.columns:
            result_df["Timestamp"] = df.loc[work_df.index, "timestamp"].values
        else:
            result_df["Timestamp"] = None

        # TurbineId
        if "TurbineId" in df.columns:
            result_df["TurbineId"] = df.loc[work_df.index, "TurbineId"].values
        elif "turbineId" in df.columns:
            result_df["TurbineId"] = df.loc[work_df.index, "turbineId"].values
        elif "turbine_id" in df.columns:
            result_df["TurbineId"] = df.loc[work_df.index, "turbine_id"].values
        else:
            result_df["TurbineId"] = "BK-TEST-4"

        # Prediction/result values matching C# FailureDetectionResult
        result_df["Residual"] = residuals
        result_df["IsAbnormal"] = False
        result_df["AlarmLvl"] = 0
        result_df["PredictedValue"] = predicted_values
        result_df["ActualValue"] = actual_values
        result_df["LCL"] = lower_limits
        result_df["UCL"] = upper_limits
        result_df["EWMA"] = ewma_values 
        result_df["A1Triggered"] = [value == 1 for value in a1_values]
        result_df["A2Triggered"] = [value == 1 for value in a2_values]

        result_df["IsAbnormal"] = (
            result_df["A1Triggered"] | result_df["A2Triggered"]
        )

        result_df["AlarmLvl"] = np.where(
            result_df["A2Triggered"],
            2,
            np.where(result_df["A1Triggered"], 1, 0)
        )

        # Reorder columns to match C# model order, except Id is excluded
        # because Supabase/database should generate Id automatically.
        result_df = result_df[
            [
                "TurbineId",
                "Timestamp",
                "Residual",
                "IsAbnormal",
                "AlarmLvl",
                "PredictedValue",
                "ActualValue",
                "LCL",
                "UCL",
                "EWMA",
                "A1Triggered",
                "A2Triggered",
            ]
        ]

        print("Mean actual:", np.mean(actual_values))
        print("Mean predicted:", np.mean(predicted_values))
        print("Mean residual:", np.mean(residuals))
        print("Min residual:", np.min(residuals))
        print("Max residual:", np.max(residuals))

        print("Residual std from metadata:", self.residual_std)
        print("Upper limit:", self.control_limit_k * self.residual_std)
        print("Lower limit:", -self.control_limit_k * self.residual_std)

        self._draw_chart(result_df, output_graph_path)

        if output_prediction_graph_path is not None:
            self._draw_prediction_chart(
                result_df,
                Path(output_prediction_graph_path)
            )

        if output_results_path is not None:
            output_results_path = Path(output_results_path)
            output_results_path.parent.mkdir(parents=True, exist_ok=True)
            result_df.to_csv(output_results_path, index=False)

        return result_df

    def _draw_chart(self, result_df: pd.DataFrame, output_graph_path: Path):
        output_graph_path.parent.mkdir(parents=True, exist_ok=True)

        x_axis = np.arange(len(result_df))

        plt.figure(figsize=(14, 7))

        plt.plot(
            x_axis,
            result_df["EWMA"],
            label="EWMA Residual",
            linewidth=1.5,
        )

        plt.plot(
            x_axis,
            result_df["UCL"],
            linestyle="--",
            label="Upper Control Limit",
        )

        plt.plot(
            x_axis,
            result_df["LCL"],
            linestyle="--",
            label="Lower Control Limit",
        )

        a1_points = result_df[result_df["A1Triggered"] == True]
        a2_points = result_df[result_df["A2Triggered"] == True]

        plt.scatter(
            a1_points.index,
            a1_points["EWMA"],
            label="A1 Alarm",
            marker="o",
        )

        plt.scatter(
            a2_points.index,
            a2_points["EWMA"],
            label="A2 Alarm",
            marker="x",
        )

        plt.axhline(0, linewidth=1)

        plt.title("EWMA Residual Control Chart")
        plt.xlabel("Row Number")
        plt.ylabel("EWMA Residual")
        plt.legend()
        plt.grid(True)

        plt.savefig(output_graph_path, dpi=300, bbox_inches="tight")
        plt.close()

        print(f"EWMA chart saved to: {output_graph_path}")

    def _draw_prediction_chart(
    self,
    result_df: pd.DataFrame,
    output_graph_path: Path,
    ):
        output_graph_path.parent.mkdir(parents=True, exist_ok=True)

        plot_df = result_df.copy()

        if "Timestamp" in plot_df.columns and plot_df["Timestamp"].notna().any():
            plot_df["Timestamp"] = pd.to_datetime(plot_df["Timestamp"], errors="coerce")
            plot_df = plot_df.dropna(subset=["Timestamp"])
            plot_df = plot_df.sort_values(by="Timestamp").reset_index(drop=True)
            x_axis = plot_df["Timestamp"]
            x_label = "Timestamp"
        else:
            x_axis = np.arange(len(plot_df))
            x_label = "Row Number"

        # Smooth only for visualization
        rolling_window = 24  # 24 rows = 4 hours if data is every 10 minutes

        plot_df["ActualSmooth"] = (
            plot_df["ActualValue"]
            .rolling(window=rolling_window, min_periods=1)
            .mean()
        )

        plot_df["PredictedSmooth"] = (
            plot_df["PredictedValue"]
            .rolling(window=rolling_window, min_periods=1)
            .mean()
        )

        plt.figure(figsize=(16, 8))

        # Raw lines, light/thin

        '''
        plt.plot(
            x_axis,
            plot_df["ActualValue"],
            label="Actual Raw",
            linewidth=0.6,
            alpha=0.25,
        )

        plt.plot(
            x_axis,
            plot_df["PredictedValue"],
            label="Predicted Raw",
            linewidth=0.6,
            alpha=0.25,
        )
         '''

        # Smoothed lines, clearer
        plt.plot(
            x_axis,
            plot_df["ActualSmooth"],
            label="Actual Smoothed",
            linewidth=2.0,
        )

        plt.plot(
            x_axis,
            plot_df["PredictedSmooth"],
            label="Predicted Smoothed",
            linewidth=2.0,
        )

        a1_points = plot_df[plot_df["A1Triggered"] == True]
        a2_points = plot_df[plot_df["A2Triggered"] == True]

        if "Timestamp" in plot_df.columns and plot_df["Timestamp"].notna().any():
            a1_x = a1_points["Timestamp"]
            a2_x = a2_points["Timestamp"]
        else:
            a1_x = a1_points.index
            a2_x = a2_points.index

        # Alarm dots on actual value line
        plt.scatter(
            a1_x,
            a1_points["ActualValue"],
            label="A1 Alarm",
            marker="o",
            s=35,
            zorder=5,
        )

        plt.scatter(
            a2_x,
            a2_points["ActualValue"],
            label="A2 Alarm",
            marker="x",
            s=60,
            zorder=6,
        )

        plt.title("Actual vs Predicted Values with Alarm Points")
        plt.xlabel(x_label)
        plt.ylabel(self.target_column)
        plt.legend()
        plt.grid(True, alpha=0.3)

        plt.tight_layout()
        plt.savefig(output_graph_path, dpi=300, bbox_inches="tight")
        plt.close()

        print(f"Prediction chart saved to: {output_graph_path}")