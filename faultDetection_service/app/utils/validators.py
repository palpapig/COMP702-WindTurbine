from __future__ import annotations

from typing import Iterable


def ensure_required_features(feature_names: Iterable[str], row: dict) -> list[str]:
    missing = []
    for feature in feature_names:
        if feature not in row or row[feature] is None:
            missing.append(feature)
    return missing
