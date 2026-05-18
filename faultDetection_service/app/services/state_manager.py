from collections import defaultdict


def default_turbine_state():
    return {
        "last_ewma": 0.0,
        "consecutive_a1_count": 0,
        "residual_std": None,
    }


class StateManager:
    def __init__(self):
        self.state = defaultdict(default_turbine_state)

    def get(self, turbine_id):
        return self.state[turbine_id]

    def update(self, turbine_id, last_ewma=None, consecutive_a1_count=None, residual_std=None):
        turbine_state = self.state[turbine_id]

        if last_ewma is not None:
            turbine_state["last_ewma"] = last_ewma

        if consecutive_a1_count is not None:
            turbine_state["consecutive_a1_count"] = consecutive_a1_count

        if residual_std is not None:
            turbine_state["residual_std"] = residual_std

        return turbine_state


state_manager = StateManager()