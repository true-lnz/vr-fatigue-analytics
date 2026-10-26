"""Неблокирующая консольная оболочка backend без мерцания."""

from __future__ import annotations

import asyncio
import ctypes
import json
import logging
import os
import re
import shutil
import sys
import time
from collections import deque

ESC = "\033["
RESET = ESC + "0m"
ANSI_ESCAPE = re.compile(r"\x1b\[[0-?]*[ -/]*[@-~]")
COLORS = {
    "title": ESC + "96;1m",
    "tab": ESC + "97;1m",
    "active": ESC + "30;106m",
    "ok": ESC + "92m",
    "info": ESC + "96m",
    "warning": ESC + "93m",
    "error": ESC + "91;1m",
    "debug": ESC + "95m",
    "muted": ESC + "90m",
}


def paint(text: str, color: str) -> str:
    return f"{COLORS[color]}{text}{RESET}"


class ConsoleHandler(logging.Handler):
    def __init__(self, view: "ConsoleView") -> None:
        super().__init__()
        self.view = view

    def emit(self, record: logging.LogRecord) -> None:
        self.view.logs.append((record.levelname, self.format(record)))


class ConsoleView:
    """1 Status, 2 Server Logs, 3 Cortex logs, 4 Sessions, 5 Exit.

    Стрелки и PgUp/PgDn прокручивают журналы. Если пользователь ушёл вверх,
    новые данные не сбрасывают позицию; End снова включает слежение за концом.
    """

    TABS = [
        ("1", "STATUS"),
        ("2", "SERVER LOGS"),
        ("3", "CORTEX LOGS"),
        ("4", "SESSIONS"),
        ("5", "EXIT"),
    ]
    TAB_BY_KEY = {
        "1": "status",
        "2": "server_logs",
        "3": "cortex_logs",
        "4": "sessions",
        "5": "exit",
    }

    def __init__(self, server) -> None:
        self.server = server
        self.tab = "status"
        self.logs: deque[tuple[str, str]] = deque(maxlen=500)
        self.stop_event = asyncio.Event()
        self.mouse_enabled = False
        # None означает follow-mode. Число — фиксированная первая строка окна.
        self.scroll_positions: dict[str, int | None] = {
            "server_logs": None,
            "cortex_logs": None,
        }
        self.viewport_height = 18
        self._frame_started = False
        self._screen_started = False

    @staticmethod
    def _fit_line(line: str, width: int) -> str:
        """Trim a line without cutting an ANSI escape sequence.

        Long Cortex payloads must not wrap to the next terminal row: wrapped
        rows make the next frame appear to contain stale content.
        """
        visible = ANSI_ESCAPE.sub("", line)
        if len(visible) <= width:
            return line
        # Cortex JSON is plain text after its timestamp, so truncating the
        # visible representation is preferable to allowing terminal wrapping.
        return visible[:max(0, width - 1)] + "…" if width > 1 else ""

    def enable_console_features(self) -> None:
        if os.name != "nt":
            return
        try:
            sys.stdout.reconfigure(encoding="utf-8", errors="replace")
            sys.stderr.reconfigure(encoding="utf-8", errors="replace")
            input_handle = ctypes.windll.kernel32.GetStdHandle(-10)
            input_mode = ctypes.c_ulong()
            ctypes.windll.kernel32.GetConsoleMode(input_handle, ctypes.byref(input_mode))
            ctypes.windll.kernel32.SetConsoleMode(input_handle, input_mode.value | 0x0010 | 0x0080)
            output_handle = ctypes.windll.kernel32.GetStdHandle(-11)
            output_mode = ctypes.c_ulong()
            if ctypes.windll.kernel32.GetConsoleMode(output_handle, ctypes.byref(output_mode)):
                ctypes.windll.kernel32.SetConsoleMode(output_handle, output_mode.value | 0x0004)
            self.mouse_enabled = True
        except (AttributeError, OSError):
            self.mouse_enabled = False

    @staticmethod
    def _log_line(level: str, message: str) -> str:
        color = {"ERROR": "error", "WARNING": "warning", "INFO": "info", "DEBUG": "debug"}.get(level, "muted")
        label = {"ERROR": "ОШИБКА", "WARNING": "ВНИМАНИЕ", "INFO": "ИНФО", "DEBUG": "ОТЛАДКА"}.get(level, level)
        return paint(f"[{label}]", color) + " " + message

    def _cortex_lines(self) -> list[str]:
        lines = []
        for timestamp, metrics in self.server.cortex_logs:
            stamp = time.strftime("%H:%M:%S", time.localtime(timestamp))
            payload = json.dumps(metrics, ensure_ascii=False, separators=(", ", ": "))
            lines.append(f"{paint(stamp, 'muted')} {payload}")
        return lines

    def _scroll_window(self, name: str, lines: list[str]) -> list[str]:
        height = max(1, self.viewport_height)
        max_start = max(0, len(lines) - height)
        position = self.scroll_positions[name]
        start = max_start if position is None else min(max(position, 0), max_start)
        if position is not None:
            self.scroll_positions[name] = start
        return lines[start:start + height]

    def _body_lines(self) -> list[str]:
        if self.tab == "status":
            return [
                paint("SERVER STATUS", "title"),
                f"WebSocket: {paint('ws://127.0.0.1:8765', 'ok')}   source: {paint(self.server.source, 'ok')}",
                f"Unity clients: {paint(str(len(self.server.clients)), 'ok')}   Cortex: {paint(self.server.cortex_state, 'ok' if self.server.cortex_state == 'connected' else 'warning')}",
                f"Live samples: {paint(str(len(self.server.live_metrics)), 'info')}",
                f"Completed sessions: {paint(str(self.server.completed_sessions), 'info')}   Last: {paint(self.server.last_session_id or '—', 'muted')}",
                f"Active sessions: {paint(str(len(self.server.sessions)), 'info')}",
            ]
        if self.tab == "server_logs":
            lines = [self._log_line(level, message) for level, message in self.logs]
            if not lines:
                lines = [paint("No log messages yet", "muted")]
            return [paint("SERVER LOGS", "title"), *self._scroll_window("server_logs", lines)]
        if self.tab == "cortex_logs":
            lines = self._cortex_lines()
            if not lines:
                lines = [paint("Waiting for Cortex met samples...", "warning")]
            return [paint("CORTEX LOGS / REALTIME", "title"), *self._scroll_window("cortex_logs", lines)]
        if self.tab == "sessions":
            lines = [paint("ACTIVE UNITY SESSIONS", "title")]
            lines.extend(
                f"- {paint(session.session_id, 'info')}: behavior={len(session.behavior)}, gaze={len(session.gaze)}"
                for session in self.server.sessions.values()
            )
            if not self.server.sessions:
                lines.append(paint("No active session. Start the shift in Unity.", "warning"))
            if self.server.recent_results:
                lines.append("")
                lines.append(paint("RECENTLY COMPLETED", "title"))
                lines.extend(
                    f"- {result['session_id']}: {result['score']:.3f} · {result['class']}"
                    for result in reversed(self.server.recent_results)
                )
            return lines
        return []

    def draw(self) -> None:
        terminal = shutil.get_terminal_size((110, 28))
        height = max(8, terminal.lines)
        width = max(40, min(terminal.columns, 120))
        self.viewport_height = max(5, height - 7)
        buttons = []
        for key, label in self.TABS:
            active = self.TAB_BY_KEY[key] == self.tab
            buttons.append(paint(f" {key} {label} ", "active" if active else "tab"))
        lines = [
            paint(" VR FATIGUE ANALYTICS", "title") + paint("  |  LIVE CONSOLE", "muted"),
            " ".join(buttons),
            paint("─" * width, "muted"),
            *self._body_lines(),
            "",
            paint("Keys: 1-4 tabs · ↑/↓ or PgUp/PgDn scroll · Home/End · q or 5 exit", "muted"),
        ]
        # Always write the same number of terminal rows.  Erasing each row
        # before writing it removes remnants when switching to a shorter tab.
        lines = [self._fit_line(line, width) for line in lines[:height]]
        lines.extend([""] * (height - len(lines)))
        frame = "".join(f"{line}\033[K\n" for line in lines)

        if not self._screen_started:
            # Use an alternate buffer so the live UI never contaminates the
            # shell and can be restored cleanly on exit.
            prefix = "\033[?1049h\033[2J\033[H\033[?25l"
            self._screen_started = True
        else:
            prefix = ""
        # DEC synchronized output prevents the terminal from painting a
        # partially written frame (the visible "flicker").
        sys.stdout.write(prefix + "\033[?2026h\033[H" + frame + "\033[J\033[?2026l")
        sys.stdout.flush()
        self._frame_started = True

    def _move_scroll(self, delta: int | None = None, absolute: str | None = None) -> None:
        if self.tab not in self.scroll_positions:
            return
        total = len(self.logs) if self.tab == "server_logs" else len(self.server.cortex_logs)
        max_start = max(0, total - self.viewport_height)
        current = self.scroll_positions[self.tab]
        if absolute == "top":
            self.scroll_positions[self.tab] = 0
        elif absolute == "bottom":
            self.scroll_positions[self.tab] = None
        elif delta is not None:
            start = max_start if current is None else current
            next_start = max(0, min(max_start, start + delta))
            self.scroll_positions[self.tab] = None if next_start == max_start and delta > 0 else next_start

    def command_from_mouse(self) -> str | None:
        if not self.mouse_enabled:
            return None
        try:
            class Coord(ctypes.Structure):
                _fields_ = [("X", ctypes.c_short), ("Y", ctypes.c_short)]

            class Mouse(ctypes.Structure):
                _fields_ = [("pos", Coord), ("buttons", ctypes.c_ulong), ("control", ctypes.c_ulong), ("flags", ctypes.c_ushort), ("pad", ctypes.c_ushort)]

            class Input(ctypes.Structure):
                _fields_ = [("event_type", ctypes.c_ushort), ("pad", ctypes.c_ushort), ("mouse", Mouse)]

            handle = ctypes.windll.kernel32.GetStdHandle(-10)
            available = ctypes.c_ulong()
            ctypes.windll.kernel32.GetNumberOfConsoleInputEvents(handle, ctypes.byref(available))
            if not available.value:
                return None
            event = Input()
            ctypes.windll.kernel32.PeekConsoleInputW(handle, ctypes.byref(event), 1, ctypes.byref(available))
            if event.event_type == 1:
                return None
            ctypes.windll.kernel32.ReadConsoleInputW(handle, ctypes.byref(event), 1, ctypes.byref(available))
            if event.event_type == 2 and event.mouse.buttons & 1 and event.mouse.pos.Y == 1:
                column = event.mouse.pos.X
                for left, right, command in (
                    (0, 10, "1"), (11, 28, "2"), (29, 46, "3"), (47, 60, "4"), (61, 70, "5")
                ):
                    if left <= column <= right:
                        return command
        except (AttributeError, OSError, ValueError):
            return None
        return None

    @staticmethod
    def _keyboard_command():
        import msvcrt

        if not msvcrt.kbhit():
            return None
        command = msvcrt.getwch()
        if command in {"\x00", "\xe0"}:
            command = msvcrt.getwch()
            return {
                "H": "up", "P": "down", "I": "page_up", "Q": "page_down",
                "G": "home", "O": "end",
            }.get(command, "")
        return command

    async def run(self) -> None:
        handler = ConsoleHandler(self)
        handler.setFormatter(logging.Formatter("%(asctime)s %(message)s", "%H:%M:%S"))
        logging.getLogger().addHandler(handler)
        self.enable_console_features()
        try:
            while not self.stop_event.is_set():
                self.draw()
                command = None
                redraw_at = time.monotonic() + 0.5
                while command is None and not self.stop_event.is_set():
                    command = self.command_from_mouse()
                    if command is None:
                        if os.name == "nt" and sys.stdin.isatty():
                            command = self._keyboard_command()
                        elif not sys.stdin.isatty():
                            command = await asyncio.to_thread(input)
                    if command is None:
                        await asyncio.sleep(0.05)
                        if time.monotonic() >= redraw_at:
                            self.draw()
                            redraw_at = time.monotonic() + 0.5
                command = command.strip().lower()
                if command in self.TAB_BY_KEY:
                    self.tab = self.TAB_BY_KEY[command]
                    if self.tab == "exit":
                        self.stop_event.set()
                elif command in {"up", "w"}:
                    self._move_scroll(-1)
                elif command in {"down", "s"}:
                    self._move_scroll(1)
                elif command == "page_up":
                    self._move_scroll(-self.viewport_height)
                elif command == "page_down":
                    self._move_scroll(self.viewport_height)
                elif command == "home":
                    self._move_scroll(absolute="top")
                elif command == "end":
                    self._move_scroll(absolute="bottom")
                elif command == "q":
                    self.stop_event.set()
        except (EOFError, KeyboardInterrupt):
            self.stop_event.set()
        finally:
            logging.getLogger().removeHandler(handler)
            if self._screen_started:
                sys.stdout.write("\033[?2026l\033[?25h\033[0m\033[?1049l")
            else:
                sys.stdout.write("\033[?25h\033[0m\n")
            sys.stdout.flush()
