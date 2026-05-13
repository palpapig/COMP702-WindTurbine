from pathlib import Path
from tests.ewmaTest import EwmaChartGenerator


BASE_DIR = Path(__file__).resolve().parent
MODEL_DIR = BASE_DIR / "model"

generator = EwmaChartGenerator(
    model_path=MODEL_DIR / "model.pkl",
    metadata_path=MODEL_DIR / "metadata.json",
)

results = generator.generate_from_csv(
    csv_path=BASE_DIR / "data" / "raw" / "data.csv",
    output_graph_path=MODEL_DIR / "ewma_chart.png",
    output_results_path=MODEL_DIR / "ewma_results.csv",
    output_prediction_graph_path=MODEL_DIR / "actual_vs_predicted_chart.png",
)

print(results.head())
print("Done")