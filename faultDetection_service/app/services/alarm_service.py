from __future__ import annotations

from app.config.settings import get_model_settings
from app.models.response_models import AlarmInfo
from app.services.state_manager import state_manager


class AlarmService:
    def evaluate(self, turbineId: str, residual: float, residual_std: float | None) -> AlarmInfo:
        # Load alarm settings
        settings = get_model_settings()["alarm"]
        ewma_lambda = float(settings["ewma_lambda"])
        k = float(settings["control_limit_k"])
        a2_required = int(settings["a2_consecutive_a1_count"])

        # Get saved state for this turbine
        state = state_manager.get(turbineId)

        # Previous EWMA (already exists because of default state)
        prev_ewma = float(state["last_ewma"])

        # Calculate EWMA (smoothed residual)
        ewma = ewma_lambda * residual + (1 - ewma_lambda) * prev_ewma

        # Handle None → convert to 0
        residual_std = float(residual_std or 0.0)

        # Control limits
        ucl = k * residual_std
        lcl = -ucl

        # A1 alarm → if EWMA outside limits
        a1 = residual_std > 0 and (ewma > ucl or ewma < lcl)

        # Count consecutive A1 alarms
        if a1:
            consecutive = state["consecutive_a1_count"] + 1
        else:
            consecutive = 0

        # A2 alarm → triggered after N consecutive A1
        a2 = consecutive >= a2_required

        # Save updated values for next call
        state_manager.update(
            turbineId,
            last_ewma=ewma,
            consecutive_a1_count=consecutive,
            residual_std=residual_std,
        )

        # Return results
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