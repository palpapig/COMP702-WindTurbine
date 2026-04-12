from __future__ import annotations

import requests
from typing import Any


class DotNetDataClientError(Exception):
    """Raised when .NET data request fails."""


class DotNetDataClient:
    def __init__(self, base_url: str, api_key: str | None = None, timeout_seconds: int = 120):
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
        self.timeout_seconds = timeout_seconds

    def fetch_training_data(
        self,
        turbine_id: str,
        months_back: int,
        columns: list[str],
    ) -> dict[str, Any]:
        if not turbine_id.strip():
            raise DotNetDataClientError("turbine_id cannot be empty")

        if months_back <= 0:
            raise DotNetDataClientError("months_back must be > 0")

        if not columns:
            raise DotNetDataClientError("columns list cannot be empty")

        url = f"{self.base_url}/api/training-data"

        headers = {
            "Content-Type": "application/json",
        }

        if self.api_key:
            headers["X-API-Key"] = self.api_key

        payload = {
            "turbineId": turbine_id,
            "monthsBack": months_back,
            "columns": columns,
        }

        try:
            response = requests.post(
                url,
                json=payload,
                headers=headers,
                timeout=self.timeout_seconds,
            )
        except requests.RequestException as ex:
            raise DotNetDataClientError(
                f"Failed to connect to .NET API at {url}: {ex}"
            ) from ex

        if not response.ok:
            raise DotNetDataClientError(
                f".NET API returned {response.status_code}: {response.text}"
            )

        try:
            data = response.json()
        except ValueError as ex:
            raise DotNetDataClientError("Invalid JSON returned by .NET API") from ex

        if "rows" not in data:
            raise DotNetDataClientError("Response JSON does not contain 'rows'")

        return data
