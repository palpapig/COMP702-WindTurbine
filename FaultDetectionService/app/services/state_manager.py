from __future__ import annotations

from collections import defaultdict


class StateManager:
    def __init__(self) -> None:
        self._state = defaultdict(lambda: {
            "last_ewma": 0.0,
            "consecutive_a1_count": 0,
            "residual_std": None,
        })

    def get(self, turbine_id: str) -> dict:
        return self._state[turbine_id]

    def update(self, turbine_id: str, **kwargs) -> dict:
        self._state[turbine_id].update(kwargs)
        return self._state[turbine_id]


state_manager = StateManager()
