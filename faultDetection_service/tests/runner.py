from pathlib import Path
from app.services.ewma_chart_generator import EwmaChartGenerator


BASE_DIR = Path(__file__).resolve().parent

MODEL_DIR = BASE_DIR / "artifacts" / "final_Model_converted"

generator = EwmaChartGenerator(model_path=MODEL_DIR / "model.pkl", etadata_path=MODEL_DIR / "metadata.json",)

results = generator.generate_from_csv(csv_path=BASE_DIR / "data" / "testEwma" / "your_test_file.csv",output_graph_path=MODEL_DIR / "ewma_chart.png", output_results_path=MODEL_DIR / "ewma_results.csv",
)

print(results.head())
print("Done")