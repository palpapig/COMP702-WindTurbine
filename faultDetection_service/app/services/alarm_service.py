from __future__ import annotations

from app.config.settings import get_model_settings
from app.models.response_models import AlarmInfo
from app.services.state_manager import state_manager


class AlarmService:
    def evaluate(self, turbine_id: str, residual: float, residual_std: float | None) -> AlarmInfo:
        settings = get_model_settings()["alarm"]
        ewma_lambda = float(settings["ewma_lambda"])
        k = float(settings["control_limit_k"])
        a2_count_required = int(settings["a2_consecutive_a1_count"])

        state = state_manager.get(turbine_id)
        prev_ewma = float(state.get("last_ewma", 0.0))
        ewma = ewma_lambda * residual + (1 - ewma_lambda) * prev_ewma

        residual_std = float(residual_std or 0.0)
        ucl = k * residual_std
        lcl = -ucl
        a1 = False
        if residual_std > 0:
            a1 = ewma > ucl or ewma < lcl

        consecutive = state.get("consecutive_a1_count", 0) + 1 if a1 else 0
        a2 = consecutive >= a2_count_required

        state_manager.update(
            turbine_id,
            last_ewma=ewma,
            consecutive_a1_count=consecutive,
            residual_std=residual_std,
        )

        return AlarmInfo(
            residual=residual,
            ewma=ewma,
            ucl=ucl,
            lcl=lcl,
            a1_triggered=a1,
            a2_triggered=a2,
            consecutive_a1_count=consecutive,
        )


alarm_service = AlarmService()
