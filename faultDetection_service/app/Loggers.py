import logging
from __future__ import annotations
from typing import Iterable


def get_logger(name: str) -> logging.Logger:
    logger = logging.getLogger(name)
    if not logger.handlers:
        logging.basicConfig(
            level=logging.INFO,
            format="%(asctime)s | %(levelname)s | %(name)s | %(message)s"
        )
    return logger




def ensure_required_features(feature_names: Iterable[str], row: dict) -> list[str]:
    missing = []
    for feature in feature_names:
        if feature not in row or row[feature] is None:
            missing.append(feature)
    return missing
