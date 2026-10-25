"""Normalize the confirmed Cortex ``met`` stream without inventing fields."""

from __future__ import annotations

MET_COLUMNS = [
    "attention.isActive", "attention", "eng.isActive", "eng", "exc.isActive", "exc",
    "lex", "str.isActive", "str", "rel.isActive", "rel", "int.isActive", "int",
]


def normalize_met_sample(sample: list[object], timestamp: float) -> dict:
    """Convert a Cortex met array to a named internal message.

    Only columns observed from the user's EPOC X session are accepted.
    """
    values = dict(zip(MET_COLUMNS, sample))
    metrics = {
        key: values[key]
        for key in ("attention", "eng", "exc", "lex", "str", "rel", "int")
        if key in values and isinstance(values[key], (int, float))
    }
    active = {
        key.removesuffix(".isActive"): values[key]
        for key in values
        if key.endswith(".isActive")
    }
    return {"type": "emotiv_metrics", "stream": "met", "t_s": timestamp, "metrics": metrics, "active": active}
