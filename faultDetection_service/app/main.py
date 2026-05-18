from fastapi import FastAPI

from app.api.predict import router as predict_router
# from app.api.train import router as train_router training through api is no more available
from app.config.settings import get_turbines_registry, reload_model_settings

app = FastAPI(title="Wind Fault Detection Service")
app.include_router(predict_router)

#app.include_router(train_router) training through api is no more available


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/turbines")
def turbines():
    return get_turbines_registry()

