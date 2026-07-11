import copy
import json
import tempfile
import unittest
from pathlib import Path

from Tools.CI.aph810_visual_evidence_contract import (
    EvidenceValidationError,
    validate_evidence,
)


def connected_report() -> dict:
    operation = {"tool": "mcp.unity.inspect", "target": "Match/Canvas", "result": "present"}
    return {
        "schemaVersion": 1,
        "taskId": "APH-810",
        "subject": "Match HUD visual acceptance",
        "revision": "abc123",
        "mcp": {"status": "connected", "probe": "mcp.unity.ping passed", "unavailableReason": None},
        "mcpEvidence": {
            "hierarchy": copy.deepcopy(operation),
            "console": {"tool": "mcp.unity.console", "target": "Console", "result": "zero errors"},
            "playMode": {"tool": "mcp.unity.play", "target": "Play Mode", "result": "running"},
            "screenshots": [
                {
                    "tool": "mcp.unity.screenshot",
                    "path": "captures/match.png",
                    "view": "gameplay-camera",
                    "description": "HUD is aligned",
                }
            ],
        },
        "fallback": None,
        "conclusion": "Accepted",
    }


def fallback_report() -> dict:
    return {
        "schemaVersion": 1,
        "taskId": "APH-810",
        "subject": "Match HUD visual acceptance",
        "revision": "abc123",
        "mcp": {
            "status": "unavailable",
            "probe": "mcp.unity.ping timed out",
            "unavailableReason": "Unity MCP endpoint unavailable",
        },
        "mcpEvidence": None,
        "fallback": {
            "runnerCommand": "Tools/CI/invoke_unity_macos.sh --log /tmp/run.log -- -executeMethod Game.Editor.Run",
            "logPath": "logs/run.log",
            "resultMarker": "[Runner] result=Passed",
            "screenshots": [
                {
                    "path": "captures/match.png",
                    "view": "gameplay-camera",
                    "description": "HUD is aligned",
                }
            ],
        },
        "conclusion": "Accepted with MCP limitation recorded",
    }


class Aph810VisualEvidenceContractTests(unittest.TestCase):
    def test_accepts_complete_mcp_evidence(self) -> None:
        result = validate_evidence(connected_report())
        self.assertEqual("Passed", result["result"])
        self.assertEqual("mcp", result["evidencePath"])

    def test_requires_every_mcp_evidence_category(self) -> None:
        report = connected_report()
        del report["mcpEvidence"]["console"]
        with self.assertRaisesRegex(EvidenceValidationError, "console"):
            validate_evidence(report)

    def test_rejects_fallback_mixed_with_connected_mcp(self) -> None:
        report = connected_report()
        report["fallback"] = fallback_report()["fallback"]
        with self.assertRaisesRegex(EvidenceValidationError, "fallback must be null"):
            validate_evidence(report)

    def test_accepts_complete_fallback_evidence(self) -> None:
        result = validate_evidence(fallback_report())
        self.assertEqual("fallback", result["evidencePath"])
        self.assertIn("invoke_unity_macos.sh", result["evidence"]["runnerCommand"])

    def test_fallback_requires_exact_unavailable_reason(self) -> None:
        report = fallback_report()
        report["mcp"]["unavailableReason"] = " "
        with self.assertRaisesRegex(EvidenceValidationError, "unavailableReason"):
            validate_evidence(report)

    def test_fallback_requires_runner_log_marker_and_screenshots(self) -> None:
        for missing in ("runnerCommand", "logPath", "resultMarker", "screenshots"):
            with self.subTest(missing=missing):
                report = fallback_report()
                del report["fallback"][missing]
                with self.assertRaisesRegex(EvidenceValidationError, missing):
                    validate_evidence(report)

    def test_rejects_multiline_runner_command(self) -> None:
        report = fallback_report()
        report["fallback"]["runnerCommand"] = "first command\nsecond command"
        with self.assertRaisesRegex(EvidenceValidationError, "one exact shell command"):
            validate_evidence(report)

    def test_artifact_check_requires_recorded_files(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            report = fallback_report()
            with self.assertRaisesRegex(EvidenceValidationError, "logPath does not exist"):
                validate_evidence(report, artifact_root=root)

            (root / "logs").mkdir()
            (root / "logs/run.log").write_text("[Runner] result=Passed\n", encoding="utf-8")
            with self.assertRaisesRegex(EvidenceValidationError, "screenshots.*does not exist"):
                validate_evidence(report, artifact_root=root)

            (root / "captures").mkdir()
            (root / "captures/match.png").write_bytes(b"png")
            self.assertEqual(
                "Passed",
                validate_evidence(report, artifact_root=root)["result"],
            )

    def test_artifact_check_requires_result_marker_in_log(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            report = fallback_report()
            (root / "logs").mkdir()
            (root / "captures").mkdir()
            (root / "logs/run.log").write_text("runner completed without marker\n", encoding="utf-8")
            (root / "captures/match.png").write_bytes(b"png")
            with self.assertRaisesRegex(EvidenceValidationError, "resultMarker is absent"):
                validate_evidence(report, artifact_root=root)

    def test_artifact_check_rejects_empty_screenshot(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            report = fallback_report()
            (root / "logs").mkdir()
            (root / "captures").mkdir()
            (root / "logs/run.log").write_text("[Runner] result=Passed\n", encoding="utf-8")
            (root / "captures/match.png").touch()
            with self.assertRaisesRegex(EvidenceValidationError, "path is empty"):
                validate_evidence(report, artifact_root=root)

    def test_schema_is_valid_json_and_matches_contract_version(self) -> None:
        schema_path = Path(__file__).parents[1] / "aph810_visual_evidence.schema.json"
        schema = json.loads(schema_path.read_text(encoding="utf-8"))
        self.assertEqual(1, schema["properties"]["schemaVersion"]["const"])
        self.assertEqual("APH-810", schema["properties"]["taskId"]["const"])
        self.assertEqual(False, schema["additionalProperties"])

    def test_rejects_unknown_fields(self) -> None:
        report = connected_report()
        report["manualNote"] = "not in schema"
        with self.assertRaisesRegex(EvidenceValidationError, "unknown fields"):
            validate_evidence(report)


if __name__ == "__main__":
    unittest.main()
