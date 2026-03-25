from fastapi import FastAPI

from app.api.predict import router as predict_router
from app.api.train import router as train_router
from app.config.settings import get_turbines_registry, reload_model_settings

app = FastAPI(title="Wind Fault Detection Service")
app.include_router(predict_router)
app.include_router(train_router)


@app.get("/health")
def health():
    settings = reload_model_settings()
    return {
        "status": "ok",
        "model_type": settings["model_type"],
        "default_target_column": settings["default_target_column"],
        "default_feature_count": len(settings["default_feature_columns"]),
    }


@app.get("/turbines")
def turbines():
    return get_turbines_registry()
