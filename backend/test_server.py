import asyncio
import tempfile
import unittest

from backend.server import FatigueServer, SCHEMA, Session, score_session


class ScoringTests(unittest.TestCase):
    def test_empty_session_is_low_and_deterministic(self):
        expected = score_session(Session("s"))
        self.assertEqual(expected, score_session(Session("s")))
        self.assertEqual((expected["fatigue_score"], expected["class"]), (0.0, "low"))

    def test_high_behavior_and_gaze_is_high(self):
        session = Session("s")
        session.behavior = [
            {"event": "target_response", "reaction_time_s": 3.0, "correct": False}
            for _ in range(10)
        ]
        session.behavior += [{"event": "target_missed"} for _ in range(10)]
        session.gaze = [
            {"duration_s": 0.0, "dispersion": 1.0, "distracted": True}
            for _ in range(10)
        ]
        result = score_session(session)
        self.assertEqual(result["class"], "high")
        self.assertEqual(result["fatigue_score"], 1.0)


class ProtocolTests(unittest.TestCase):
    def test_invalid_event_is_rejected_without_losing_session(self):
        with tempfile.TemporaryDirectory() as directory:
            server = FatigueServer(f"{directory}/events.jsonl")
            start = {"schema": SCHEMA, "type": "session_start", "session_id": "s", "duration_s": 60}
            asyncio.run(server.process(start))
            with self.assertRaises(ValueError):
                asyncio.run(server.process({"schema": SCHEMA, "type": "behavior_event", "session_id": "s", "t_s": 1, "event": "target_response"}))
            self.assertIn("s", server.sessions)

    def test_finish_returns_result_and_removes_session(self):
        with tempfile.TemporaryDirectory() as directory:
            server = FatigueServer(f"{directory}/events.jsonl")
            asyncio.run(server.process({"schema": SCHEMA, "type": "session_start", "session_id": "s", "duration_s": 60}))
            result = asyncio.run(server.process({"schema": SCHEMA, "type": "session_finish", "session_id": "s", "t_s": 60}))
            self.assertEqual(result["type"], "session_result")
            self.assertNotIn("s", server.sessions)


if __name__ == "__main__":
    unittest.main()
