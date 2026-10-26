"""Small, explainable Live WebSocket backend for the coursework."""

from __future__ import annotations

import asyncio
import json
import logging
import math
import html
import ssl
import time
from collections import deque
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

import websockets
from websockets.exceptions import ConnectionClosed
from .cortex_metrics import normalize_met_sample
from .live_config import CLIENT_ID, CLIENT_SECRET
from .console import ConsoleView

SCHEMA = "fatigue.v1"
HOST = "127.0.0.1"
PORT = 8765


@dataclass
class Session:
    session_id: str
    duration_s: float = 60.0
    behavior: list[dict[str, Any]] = field(default_factory=list)
    gaze: list[dict[str, Any]] = field(default_factory=list)
    emotiv: list[dict[str, Any]] = field(default_factory=list)
    started_at: float = field(default_factory=time.time)


def _median(values: list[float]) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    middle = len(ordered) // 2
    return ordered[middle] if len(ordered) % 2 else (ordered[middle - 1] + ordered[middle]) / 2


def _clamp(value: float, low: float = 0.0, high: float = 1.0) -> float:
    return max(low, min(high, value))


def _number(message: dict[str, Any], name: str) -> float:
    value = message.get(name)
    if value is None or isinstance(value, bool):
        raise ValueError(f"missing numeric field: {name}")
    result = float(value)
    if not math.isfinite(result):
        raise ValueError(f"non-finite numeric field: {name}")
    return result


def validate_event(message: dict[str, Any], kind: str) -> None:
    if not message.get("session_id"):
        raise ValueError("session_id is required")
    _number(message, "t_s")
    if kind == "behavior_event":
        if message.get("event") == "target_response":
            _number(message, "reaction_time_s")
            if not isinstance(message.get("correct"), bool):
                raise ValueError("correct must be boolean")
        elif message.get("event") == "target_missed":
            if not message.get("target_id"):
                raise ValueError("target_id is required")
        else:
            raise ValueError(f"unsupported behavior event: {message.get('event')}")
    elif kind == "gaze_event":
        if message.get("event") != "fixation_end":
            raise ValueError("gaze event must be fixation_end")
        _number(message, "duration_s")
        _number(message, "dispersion")


def score_session(session: Session, source: str = "live") -> dict[str, Any]:
    responses = [e for e in session.behavior if e.get("event") == "target_response"]
    reactions = [float(e["reaction_time_s"]) for e in responses if "reaction_time_s" in e and float(e["reaction_time_s"]) >= 0]
    errors = sum(1 for e in responses if e.get("correct") is False)
    missed = sum(1 for e in session.behavior if e.get("event") == "target_missed")
    fixations = [float(e["duration_s"]) for e in session.gaze if "duration_s" in e and float(e["duration_s"]) >= 0]
    dispersions = [float(e["dispersion"]) for e in session.gaze if "dispersion" in e and float(e["dispersion"]) >= 0]
    distractions = sum(1 for e in session.gaze if e.get("distracted"))
    emotiv_mean = {}
    if session.emotiv:
        keys = set().union(*(item.get("metrics", {}).keys() for item in session.emotiv))
        emotiv_mean = {key: round(sum(float(item["metrics"][key]) for item in session.emotiv if key in item.get("metrics", {})) / max(1, sum(1 for item in session.emotiv if key in item.get("metrics", {}))), 3) for key in sorted(keys)}

    mean_reaction = sum(reactions) / len(reactions) if reactions else 0.0
    mean_fixation = sum(fixations) / len(fixations) if fixations else 0.0
    mean_dispersion = sum(dispersions) / len(dispersions) if dispersions else 0.0
    def time_limit(event: dict[str, Any]) -> float:
        at = float(event.get("t_s", 0))
        return 8.0 if at < 16 else 6.0 if at < 34 else 4.0
    late = sum(1 for e in responses if float(e.get("reaction_time_s", 0)) > time_limit(e) * 0.5)
    behavior_component = _clamp(mean_reaction / 3.0 * 0.55 + errors / 10.0 * 0.30 + missed / 10.0 * 0.15 + late / 10.0 * 0.10)
    gaze_component = 0.0 if not session.gaze else _clamp(
        mean_dispersion * 0.60 + distractions / 10.0 * 0.25 + _clamp((0.35 - mean_fixation) / 0.35) * 0.15
    )
    emotiv_component = 0.0
    if emotiv_mean:
        # A transparent prototype feature: lower engagement and relaxation raise the index.
        engagement = float(emotiv_mean.get("eng", 0.5))
        relaxation = float(emotiv_mean.get("rel", 0.5))
        emotiv_component = _clamp((1.0 - engagement) * 0.6 + (1.0 - relaxation) * 0.4)
    components = [(behavior_component, 0.55)]
    if session.gaze:
        components.append((gaze_component, 0.25))
    if emotiv_mean:
        components.append((emotiv_component, 0.20))
    weight_total = sum(weight for _, weight in components)
    score = round(_clamp(sum(value * weight for value, weight in components) / weight_total), 3)
    phase_stats = {}
    for phase, (start, end) in enumerate(((0, 16), (16, 34), (34, 60)), 1):
        phase_events = [e for e in responses if start <= float(e.get("t_s", 0)) < end]
        phase_missed = sum(1 for e in session.behavior if e.get("event") == "target_missed" and start <= float(e.get("t_s", 0)) < end)
        phase_errors = sum(1 for e in phase_events if not e.get("correct"))
        phase_trials = len(phase_events) + phase_missed
        phase_stats[f"phase_{phase}"] = {
            "responses": len(phase_events),
            "errors": phase_errors,
            "missed": phase_missed,
            "accuracy": round((phase_trials - phase_errors - phase_missed) / phase_trials, 3) if phase_trials else 0.0,
            "mean_reaction_s": round(sum(float(e.get("reaction_time_s", 0)) for e in phase_events) / len(phase_events), 3) if phase_events else 0.0,
        }
    category = "low" if score < 0.34 else "medium" if score < 0.67 else "high"
    return {
        "fatigue_score": score,
        "class": category,
        "features": {
            "reaction_time_mean_s": round(mean_reaction, 3),
            "reaction_time_median_s": round(_median(reactions), 3),
            "duration_s": round(session.duration_s, 3),
            "errors": errors,
            "missed_targets": missed,
            "late_responses": late,
            "fixation_duration_mean_s": round(mean_fixation, 3),
            "gaze_dispersion_mean": round(mean_dispersion, 3),
            "distractions": distractions,
            "emotiv_samples": len(session.emotiv),
            "emotiv_met_mean": emotiv_mean,
            "emotiv_component": round(emotiv_component, 3),
            "phase_stats": phase_stats,
        },
        "source": source,
    }


class FatigueServer:
    def __init__(self, log_path: str = "logs/backend.jsonl", source: str = "live") -> None:
        self.sessions: dict[str, Session] = {}
        self.log_path = Path(log_path)
        self.log_path.parent.mkdir(parents=True, exist_ok=True)
        self.source = source
        self.live_metrics: list[dict[str, Any]] = []
        # Последние подтверждённые met-сообщения для realtime-вкладки консоли.
        self.cortex_logs: deque[tuple[float, dict[str, Any]]] = deque(maxlen=500)
        self.cortex_state = "connecting"
        self.clients: set[Any] = set()
        self.completed_sessions = 0
        self.last_session_id = ""
        self.recent_results: deque[dict[str, Any]] = deque(maxlen=5)

    async def broadcast_service_status(self) -> None:
        message = json.dumps({"schema": SCHEMA, "type": "service_status", "source": self.source, "state": "ready", "emotiv_state": self.cortex_state})
        for client in tuple(self.clients):
            try:
                await client.send(message)
            except ConnectionClosed:
                self.clients.discard(client)

    async def start_cortex(self) -> asyncio.Task | None:
        if self.source != "live":
            return None
        return asyncio.create_task(self._cortex_loop(CLIENT_ID, CLIENT_SECRET))

    async def _cortex_call(self, socket, request_id: int, method: str, params: dict) -> dict:
        await socket.send(json.dumps({"jsonrpc": "2.0", "id": request_id, "method": method, "params": params}))
        while True:
            response = json.loads(await socket.recv())
            if response.get("id") == request_id:
                return response

    async def _cortex_loop(self, client_id: str, client_secret: str) -> None:
        context = ssl._create_unverified_context()
        token = None
        cortex_session = None
        socket = None
        try:
            async with websockets.connect("wss://localhost:6868", ssl=context) as socket:
                auth = await self._cortex_call(socket, 1, "authorize", {"clientId": client_id, "clientSecret": client_secret, "license": "", "debit": 10})
                token = auth.get("result", {}).get("cortexToken")
                if not token:
                    self.cortex_state = "authorization_failed"
                    logging.warning("Live Cortex authorization failed")
                    return
                headsets = await self._cortex_call(socket, 2, "queryHeadsets", {})
                connected = next((item for item in headsets.get("result", []) if item.get("status") == "connected"), None)
                if not connected:
                    self.cortex_state = "headset_not_connected"
                    logging.warning("Live Cortex has no connected headset")
                    return
                session = await self._cortex_call(socket, 3, "createSession", {"cortexToken": token, "headset": connected["id"], "status": "active"})
                cortex_session = session.get("result", {}).get("id")
                if not cortex_session:
                    self.cortex_state = "session_failed"
                    logging.warning("Live Cortex session creation failed")
                    return
                subscription = await self._cortex_call(socket, 4, "subscribe", {"cortexToken": token, "session": cortex_session, "streams": ["met"]})
                self.cortex_state = "connected"
                await self.broadcast_service_status()
                logging.info("Live Cortex subscribed: %s", subscription.get("result", {}).get("success", []))
                columns = None
                while True:
                    item = json.loads(await socket.recv())
                    if item.get("met") is None:
                        continue
                    if columns is None:
                        columns = ["attention.isActive", "attention", "eng.isActive", "eng", "exc.isActive", "exc", "lex", "str.isActive", "str", "rel.isActive", "rel", "int.isActive", "int"]
                    normalized = normalize_met_sample(item["met"], float(item.get("time", time.time())))
                    self.live_metrics.append(normalized)
                    del self.live_metrics[:-300]
                    self.cortex_logs.append((time.time(), normalized["metrics"]))
                    if len(self.live_metrics) == 1:
                        await self.broadcast_service_status()
                    for active_session in self.sessions.values():
                        active_session.emotiv.append(normalized)
        except asyncio.CancelledError:
            raise
        except Exception as error:
            self.cortex_state = "unavailable"
            await self.broadcast_service_status()
            logging.warning("Live Cortex unavailable: %s", error)
        finally:
            if socket is not None and token and cortex_session:
                try:
                    await self._cortex_call(socket, 90, "updateSession", {"cortexToken": token, "session": cortex_session, "status": "close"})
                except Exception:
                    pass

    def _log(self, message: dict[str, Any]) -> None:
        with self.log_path.open("a", encoding="utf-8") as stream:
            stream.write(json.dumps(message, ensure_ascii=False) + "\n")

    def _write_report(self, session_id: str, result: dict[str, Any]) -> Path:
        report_dir = Path("reports")
        report_dir.mkdir(exist_ok=True)
        safe_id = "".join(char for char in session_id if char.isalnum() or char in "-_")
        path = report_dir / f"session_{safe_id}.html"
        features = result["features"]
        category_names = {"low": "Низкая", "medium": "Средняя", "high": "Высокая"}
        labels = {
            "reaction_time_mean_s": "Среднее время реакции",
            "reaction_time_median_s": "Типичное время реакции",
            "duration_s": "Продолжительность теста",
            "errors": "Ошибочные ответы",
            "missed_targets": "Пропущенные задания",
            "late_responses": "Замедленные ответы",
            "fixation_duration_mean_s": "Средняя длительность фиксации взгляда",
            "gaze_dispersion_mean": "Разброс направления взгляда",
            "distractions": "Отвлечения взгляда",
            "emotiv_samples": "Получено показаний Emotiv",
            "emotiv_component": "Вклад показаний Emotiv",
        }
        explanations = {
            "reaction_time_mean_s": "Среднее время между появлением задания и ответом. Большее значение означает более медленную реакцию в этом тесте.",
            "reaction_time_median_s": "Время реакции посередине ряда ответов: половина ответов была быстрее, половина — медленнее. Меньше зависит от единичных необычно долгих ответов.",
            "duration_s": "Фактическая продолжительность теста в секундах.",
            "errors": "Количество неверных ответов на задания.",
            "missed_targets": "Количество заданий, на которые пользователь не успел ответить.",
            "late_responses": "Количество ответов, занявших больше половины отведённого на задание времени.",
            "fixation_duration_mean_s": "Среднее время, в течение которого виртуальный взгляд оставался направленным на одну область.",
            "gaze_dispersion_mean": "Средняя изменчивость направления виртуального взгляда. Это условный показатель модели, а не измерение настоящего взгляда.",
            "distractions": "Количество эпизодов, отмеченных моделью как отвлечение взгляда.",
            "emotiv_samples": "Сколько показаний Emotiv было получено за время теста.",
            "emotiv_component": "Условный вклад показателей вовлечённости и расслабленности Emotiv в общий индекс прототипа.",
        }
        emotiv_descriptions = {
            "attention": "Внимание — оценка уровня сосредоточенности по данным Emotiv.",
            "eng": "Вовлечённость — оценка активности и включённости пользователя.",
            "exc": "Возбуждение — оценка эмоциональной и физиологической активации.",
            "lex": "Долговременное возбуждение — сглаженная оценка активации за более продолжительный период.",
            "str": "Стресс — оценка напряжения по модели Emotiv.",
            "rel": "Расслабленность — оценка спокойного состояния.",
            "int": "Интерес — оценка заинтересованности и вовлечённости.",
        }

        def value_text(key: str, value: Any) -> str:
            if key.endswith("_s"):
                return f"{float(value):.2f} с"
            if key == "gaze_dispersion_mean":
                return f"{float(value):.2f}"
            if key == "emotiv_component":
                return f"{float(value):.3f}"
            return html.escape(str(value))

        rows = "".join(
            f'<tr><th title="{html.escape(explanations.get(key, ""), quote=True)}">{html.escape(labels.get(key, key))}<span class="help" aria-hidden="true">?</span></th><td>{value_text(key, value)}</td></tr>'
            for key, value in features.items()
            if key not in {"emotiv_met_mean", "phase_stats"}
        )
        emotiv_rows = "".join(
            f'<li title="{html.escape(emotiv_descriptions.get(key, "Показатель Emotiv; его значение приведено в исходном масштабе потока."), quote=True)}"><span>{html.escape(emotiv_descriptions.get(key, key.upper()).split(" — ")[0])}<small>{html.escape(key.upper())}</small></span><strong>{float(value):.3f}</strong></li>'
            for key, value in features["emotiv_met_mean"].items()
        ) or "<li>Показания не получены</li>"
        phase_names = {"phase_1": "Начало теста", "phase_2": "Середина теста", "phase_3": "Конец теста"}
        phase_cards = "".join(
            f"""<article class="phase"><h3>{phase_names[key]}</h3>
            <p>Точность: <b>{value['accuracy'] * 100:.1f}%</b></p>
            <p>Ответов: {value['responses']} · Ошибок: {value['errors']} · Пропусков: {value['missed']}</p>
            <p>Среднее время реакции: {value['mean_reaction_s']:.2f} с</p></article>"""
            for key, value in features["phase_stats"].items()
        )
        category = category_names.get(result["class"], result["class"])
        path.write_text(f"""<!doctype html><html lang="ru"><meta charset="utf-8"><title>Отчёт {html.escape(safe_id)}</title>
<style>
*{{box-sizing:border-box}}body{{margin:0;font:16px/1.55 Arial,sans-serif;color:#203047;background:#f5f7fb}}
main{{max-width:980px;margin:0 auto;padding:34px 20px 48px}}h1{{margin:0 0 6px;color:#173f62;font-size:32px}}
.muted{{color:#65758a}}.score{{display:flex;justify-content:space-between;gap:20px;align-items:center;margin:26px 0;padding:25px 28px;background:#174a70;color:white;border-radius:16px;box-shadow:0 8px 24px #174a7030}}
.score strong{{display:block;font-size:34px;margin-top:4px}}.score-value{{cursor:help;border-bottom:1px dotted #ffffffaa}}.badge{{padding:9px 14px;border-radius:999px;background:#ffffff22;font-weight:bold;white-space:nowrap}}
section{{margin-top:24px;padding:24px;background:white;border:1px solid #e1e8f0;border-radius:16px;box-shadow:0 3px 12px #2030470b}}h2{{margin:0 0 16px;color:#214f73;font-size:21px}}
table{{width:100%;border-collapse:collapse}}td,th{{padding:12px 8px;text-align:left;border-bottom:1px solid #edf1f5}}th{{width:62%;font-weight:normal;color:#52657a}}tr:last-child td,tr:last-child th{{border-bottom:0}}
.metrics li,th[title]{{cursor:help}}.metrics li:hover,th[title]:hover{{background:#edf5fb}}th[title]{{position:relative}}.help{{display:inline-flex;align-items:center;justify-content:center;margin-left:7px;width:16px;height:16px;border:1px solid #91a5b9;border-radius:50%;font-size:11px;color:#637a91;vertical-align:middle}}.metrics small{{display:block;font-size:10px;color:#8a99a8}}
.phases{{display:grid;grid-template-columns:repeat(3,1fr);gap:12px}}.phase{{padding:15px;background:#f7f9fc;border:1px solid #e3eaf2;border-radius:12px}}.phase h3{{margin:0 0 8px;font-size:16px;color:#214f73}}.phase p{{margin:5px 0;font-size:14px}}
.metrics{{display:flex;flex-wrap:wrap;gap:10px;padding:0;margin:0;list-style:none}}.metrics li{{display:flex;gap:10px;justify-content:space-between;min-width:180px;padding:10px 12px;background:#f7f9fc;border-radius:9px}}.metrics span{{color:#52657a}}
.note{{font-size:14px;color:#65758a;margin-bottom:0}}@media(max-width:700px){{.score,.phases{{display:block}}.badge{{display:inline-block;margin-top:14px}}.phase{{margin-top:10px}}}}
</style><body><main>
<h1>Результаты теста</h1><p class="muted">Сессия {html.escape(safe_id)} · источник данных: {html.escape(result['source'])}</p>
<div class="score"><div>Индекс признаков утомления<strong class="score-value" title="Итоговый условный показатель прототипа. Он объединяет поведенческие данные, особенности виртуального взгляда и доступные показатели Emotiv. Чем выше значение, тем больше признаков утомления зафиксировано в этом тесте.">{result['fatigue_score']:.3f}</strong></div><div class="badge">Оценка усталости: {html.escape(category)}</div></div>
<section><h2>Основные показатели</h2><table>{rows}</table></section>
<section><h2>Как менялся результат по ходу теста</h2><div class="phases">{phase_cards}</div></section>
<section><h2>Показания Emotiv</h2><ul class="metrics">{emotiv_rows}</ul></section>
<p class="note">Это учебная эвристическая оценка, а не медицинское заключение. Результат нельзя использовать для постановки диагноза.</p>
</main></body></html>""", encoding="utf-8")
        return path

    async def handle(self, websocket) -> None:
        self.clients.add(websocket)
        await websocket.send(json.dumps({"schema": SCHEMA, "type": "service_status", "source": self.source, "state": "ready", "emotiv_state": self.cortex_state}))
        try:
            async for raw in websocket:
                try:
                    message = json.loads(raw)
                    self._log(message)
                    response = await self.process(message)
                    if response is not None:
                        await websocket.send(json.dumps(response, ensure_ascii=False))
                except (json.JSONDecodeError, TypeError, ValueError) as error:
                    await websocket.send(json.dumps({"schema": SCHEMA, "type": "error", "code": "invalid_message", "recoverable": True, "message": str(error)}))
        except ConnectionClosed:
            logging.info("client disconnected")
        finally:
            self.clients.discard(websocket)

    async def process(self, message: dict[str, Any]) -> dict[str, Any] | None:
        if not isinstance(message, dict):
            raise ValueError("message must be an object")
        if message.get("schema") != SCHEMA:
            raise ValueError(f"unsupported schema: {message.get('schema')}")
        kind = message.get("type")
        session_id = str(message.get("session_id", ""))
        if kind == "session_start":
            duration = _number(message, "duration_s")
            if not session_id or duration <= 0:
                raise ValueError("session_id and positive duration_s are required")
            self.sessions[session_id] = Session(session_id, duration)
            logging.info("SESSION START session_id=%s duration=%ss active=%s", session_id, duration, len(self.sessions))
            return {"schema": SCHEMA, "type": "analysis_progress", "session_id": session_id, "state": "running", "progress": 0.0}
        if kind == "behavior_event" and session_id in self.sessions:
            validate_event(message, kind)
            self.sessions[session_id].behavior.append(message)
            return None
        if kind == "gaze_event" and session_id in self.sessions:
            validate_event(message, kind)
            self.sessions[session_id].gaze.append(message)
            return None
        if kind == "session_finish" and session_id in self.sessions:
            validate_event(message, kind)
            session = self.sessions.pop(session_id)
            result = score_session(session, self.source)
            self.completed_sessions += 1
            self.last_session_id = session_id
            self.recent_results.append({"session_id": session_id, "score": result["fatigue_score"], "class": result["class"]})
            logging.info("SESSION FINISH session_id=%s score=%.3f active=%s", session_id, result["fatigue_score"], len(self.sessions))
            logging.info("session_result session_id=%s score=%.3f class=%s features=%s", session_id, result["fatigue_score"], result["class"], result["features"])
            self._log({"schema": SCHEMA, "type": "session_result", "session_id": session_id, **result})
            report_path = self._write_report(session_id, result)
            logging.info("HTML report saved: %s", report_path)
            return {"schema": SCHEMA, "type": "session_result", "session_id": session_id, **result}
        raise ValueError(f"unknown or out-of-order message: {kind}")


async def main(host: str = HOST, port: int = PORT) -> None:
    logging.getLogger().setLevel(logging.INFO)
    logging.getLogger().addHandler(logging.NullHandler())
    server = FatigueServer(source="live")
    cortex_task = await server.start_cortex()
    async with websockets.serve(server.handle, host, port):
        logging.info("fatigue backend listening on ws://%s:%s", host, port)
        console = ConsoleView(server)
        try:
            await console.run()
        finally:
            if cortex_task:
                cortex_task.cancel()


if __name__ == "__main__":
    asyncio.run(main())
