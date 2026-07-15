import copy
import hashlib
import json
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib
from pathlib import Path

from Tools.CI.aph809_visual_capture_matrix import (
    CAPTURE_ROOT,
    EXPECTED_ARTIFACT_COUNT,
    EXPECTED_SLOT_COUNT,
    MatrixValidationError,
    empty_matrix,
    expected_rows,
    expected_session_paths,
    ingest_session_files,
    validate_acceptance,
    validate_inventory,
)


PROJECT_ROOT = Path(__file__).parents[3]
TRACKED_MATRIX = PROJECT_ROOT / "Design/AgentReports/2026-07-13_aph-809_visual_capture_matrix.json"
TOOL_PATH = PROJECT_ROOT / "Tools/CI/aph809_visual_capture_matrix.py"
_COMPRESSED_ROWS: dict[tuple[int, int], bytes] = {}


def _chunk(kind: bytes, payload: bytes) -> bytes:
    return (
        struct.pack(">I", len(payload))
        + kind
        + payload
        + struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF)
    )


def _png(width: int, height: int, tag: str) -> bytes:
    key = (width, height)
    if key not in _COMPRESSED_ROWS:
        scanline = b"\0" + (b"\0\x20\x40\xff" * width)
        _COMPRESSED_ROWS[key] = zlib.compress(scanline * height, level=1)
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return (
        b"\x89PNG\r\n\x1a\n"
        + _chunk(b"IHDR", ihdr)
        + _chunk(b"tEXt", f"artifact={tag}".encode("ascii"))
        + _chunk(b"IDAT", _COMPRESSED_ROWS[key])
        + _chunk(b"IEND", b"")
    )


def _complete_matrix(root: Path) -> dict:
    matrix = empty_matrix()
    matrix["revision"] = "a" * 40
    matrix["deviceProfile"] = "reference-android-24090RA29G"
    matrix["frameRateMode"] = "30fps"
    for row_index, row in enumerate(matrix["rows"]):
        row["reviewerDecision"] = "passed"
        row["reviewerNotes"] = "Reviewer inspected this exact capture state."
        width = 1920 if row["aspect"] == "16:9" else 2400
        for role in row["artifactRoles"]:
            relative = CAPTURE_ROOT / f"{row['id']}_{role}.png"
            target = root.joinpath(*relative.parts)
            target.parent.mkdir(parents=True, exist_ok=True)
            payload = _png(width, 1080, f"{row_index}-{role}")
            target.write_bytes(payload)
            row["artifacts"].append(
                {
                    "role": role,
                    "path": relative.as_posix(),
                    "sha256": hashlib.sha256(payload).hexdigest(),
                    "width": width,
                    "height": 1080,
                    "capturedAtUtc": "2026-07-15T08:30:00Z",
                    "revision": matrix["revision"],
                    "deviceProfile": matrix["deviceProfile"],
                    "frameRateMode": matrix["frameRateMode"],
                    "qualityTier": role if row["qualityTier"] == "comparison" else "current",
                    "cameraPosition": [100.0 + row_index, 34.0, 200.0],
                    "cameraRotation": [40.0, 10.0, 0.0],
                    "state": row["state"],
                }
            )
    return matrix


def _session_metadata(matrix: dict, aspect: str, profile: str) -> dict:
    artifacts = []
    for row in matrix["rows"]:
        if row["aspect"] != aspect:
            continue
        for artifact in row["artifacts"]:
            source_profile = "candidate" if artifact["role"] == "candidate" else "current"
            if source_profile == profile:
                artifacts.append({"rowId": row["id"], **copy.deepcopy(artifact)})
    return {
        "schemaVersion": 1,
        "taskId": "APH-809",
        "revision": matrix["revision"],
        "dirty": False,
        "deviceProfile": matrix["deviceProfile"],
        "frameRateMode": matrix["frameRateMode"],
        "aspect": aspect,
        "profile": profile,
        "cameraContractPath": (
            CAPTURE_ROOT / f"aph809_camera_contract_{aspect.replace(':', 'x')}.json"
        ).as_posix(),
        "artifactCount": len(artifacts),
        "artifacts": artifacts,
        "aph505EvidenceFragment": {
            "schemaVersion": 1,
            "taskId": "APH-505",
            "status": "capture-session",
            "exactCommit": matrix["revision"],
            "dirty": False,
            "candidatePaths": [],
            "capturedViews": ["near", "medium", "far"],
            "beforeAfterRole": profile,
            "beforeAfterPairsComplete": False,
            "accepted": False,
        },
    }


def _write_sessions(root: Path, matrix: dict) -> dict[tuple[str, str], Path]:
    paths = expected_session_paths(root)
    result: dict[tuple[str, str], Path] = {}
    index = 0
    for aspect in ("16:9", "20:9"):
        for profile in ("current", "candidate"):
            path = paths[index]
            index += 1
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(
                json.dumps(_session_metadata(matrix, aspect, profile), indent=2) + "\n",
                encoding="utf-8",
            )
            result[(aspect, profile)] = path
    return result


def _rewrite_json(path: Path, mutate) -> None:
    payload = json.loads(path.read_text(encoding="utf-8"))
    mutate(payload)
    path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")


class Aph809VisualCaptureMatrixTests(unittest.TestCase):
    def test_expected_inventory_has_exact_slot_and_artifact_counts(self) -> None:
        rows = expected_rows()
        self.assertEqual(EXPECTED_SLOT_COUNT, len(rows))
        self.assertEqual(
            EXPECTED_ARTIFACT_COUNT,
            sum(len(row["artifactRoles"]) for row in rows),
        )
        self.assertEqual(len(rows), len({row["id"] for row in rows}))

    def test_schema_and_tracked_pending_matrix_match_contract(self) -> None:
        schema = json.loads(
            (PROJECT_ROOT / "Tools/CI/aph809_visual_capture_matrix.schema.json").read_text(
                encoding="utf-8"
            )
        )
        self.assertEqual(1, schema["properties"]["schemaVersion"]["const"])
        self.assertEqual("APH-809", schema["properties"]["taskId"]["const"])
        self.assertFalse(schema["additionalProperties"])
        tracked = json.loads(TRACKED_MATRIX.read_text(encoding="utf-8"))
        self.assertEqual(EXPECTED_SLOT_COUNT, len(validate_inventory(tracked)["rows"]))

    def test_inventory_rejects_missing_row(self) -> None:
        matrix = empty_matrix()
        matrix["rows"].pop()
        with self.assertRaisesRegex(MatrixValidationError, "exactly 26"):
            validate_inventory(matrix)

    def test_inventory_rejects_row_identity_or_metadata_drift(self) -> None:
        for field, replacement in (("id", "invented"), ("aspect", "21:9"), ("camera", "other")):
            with self.subTest(field=field):
                matrix = empty_matrix()
                matrix["rows"][0][field] = replacement
                with self.assertRaisesRegex(MatrixValidationError, field):
                    validate_inventory(matrix)

    def test_strict_check_fails_closed_for_pending_matrix(self) -> None:
        tracked = json.loads(TRACKED_MATRIX.read_text(encoding="utf-8"))
        with self.assertRaises(MatrixValidationError):
            validate_acceptance(tracked, artifact_root=PROJECT_ROOT)
        result = subprocess.run(
            [
                sys.executable,
                str(TOOL_PATH),
                "--check",
                "--matrix",
                str(TRACKED_MATRIX),
                "--artifact-root",
                str(PROJECT_ROOT),
            ],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(1, result.returncode)
        self.assertIn("result=Failed", result.stdout)

    def test_accepts_complete_real_png_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            result = validate_acceptance(_complete_matrix(Path(temp)), artifact_root=Path(temp))
        self.assertEqual("Passed", result["result"])
        self.assertEqual(EXPECTED_SLOT_COUNT, result["slotsSatisfied"])
        self.assertEqual(EXPECTED_ARTIFACT_COUNT, result["artifactsValidated"])

    def test_ingests_exact_four_sessions_and_preserves_reviewer_fields(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            captured = _complete_matrix(root)
            sessions = _write_sessions(root, captured)
            tracked = empty_matrix()
            tracked["rows"][0]["reviewerDecision"] = "failed"
            tracked["rows"][0]["reviewerNotes"] = "Existing rejection remains authoritative."
            tracked["rows"][1]["reviewerDecision"] = "passed"
            tracked["rows"][1]["reviewerNotes"] = "Existing approval remains explicit."
            reviewer_state = [
                (row["reviewerDecision"], row["reviewerNotes"])
                for row in tracked["rows"]
            ]

            ingested = ingest_session_files(
                tracked,
                session_paths=list(reversed(list(sessions.values()))),
                artifact_root=root,
            )

        self.assertEqual(captured["revision"], ingested["revision"])
        self.assertEqual(captured["deviceProfile"], ingested["deviceProfile"])
        self.assertEqual(captured["frameRateMode"], ingested["frameRateMode"])
        self.assertEqual(
            EXPECTED_ARTIFACT_COUNT,
            sum(len(row["artifacts"]) for row in ingested["rows"]),
        )
        self.assertEqual(
            reviewer_state,
            [
                (row["reviewerDecision"], row["reviewerNotes"])
                for row in ingested["rows"]
            ],
        )

    def test_ingestion_requires_exactly_four_unique_canonical_session_paths(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sessions = _write_sessions(root, _complete_matrix(root))
            paths = list(sessions.values())
            with self.assertRaisesRegex(MatrixValidationError, "exactly four"):
                ingest_session_files(
                    empty_matrix(),
                    session_paths=paths[:3],
                    artifact_root=root,
                )
            with self.assertRaisesRegex(MatrixValidationError, "paths must be unique"):
                ingest_session_files(
                    empty_matrix(),
                    session_paths=[paths[0], paths[0], paths[2], paths[3]],
                    artifact_root=root,
                )
            with self.assertRaisesRegex(MatrixValidationError, "four canonical files"):
                ingest_session_files(
                    empty_matrix(),
                    session_paths=[root / "other.json", *paths[1:]],
                    artifact_root=root,
                )

    def test_ingestion_rejects_cross_session_revision_device_or_frame_drift(self) -> None:
        cases = (
            ("revision", "b" * 40, "same revision"),
            ("deviceProfile", "different-device", "same deviceProfile"),
            ("frameRateMode", "60fps", "same frameRateMode"),
        )
        for field, replacement, message in cases:
            with self.subTest(field=field), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                sessions = _write_sessions(root, _complete_matrix(root))
                path = sessions[("20:9", "candidate")]

                def mutate(payload: dict) -> None:
                    payload[field] = replacement
                    for artifact in payload["artifacts"]:
                        artifact[field] = replacement
                    if field == "revision":
                        payload["aph505EvidenceFragment"]["exactCommit"] = replacement

                _rewrite_json(path, mutate)
                with self.assertRaisesRegex(MatrixValidationError, message):
                    ingest_session_files(
                        empty_matrix(),
                        session_paths=list(sessions.values()),
                        artifact_root=root,
                    )

    def test_ingestion_rejects_noncanonical_or_inexact_session_artifacts(self) -> None:
        mutations = (
            (
                lambda payload: payload.__setitem__(
                    "cameraContractPath", "Design/AgentReports/other-camera.json"
                ),
                "cameraContractPath",
            ),
            (
                lambda payload: payload["artifacts"][0].__setitem__(
                    "path", (CAPTURE_ROOT / "renamed.png").as_posix()
                ),
                "path must be",
            ),
            (
                lambda payload: payload["artifacts"][1].update(
                    {
                        "rowId": payload["artifacts"][0]["rowId"],
                        "role": payload["artifacts"][0]["role"],
                    }
                ),
                "duplicate rowId/role",
            ),
        )
        for mutate, message in mutations:
            with self.subTest(message=message), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                sessions = _write_sessions(root, _complete_matrix(root))
                _rewrite_json(sessions[("16:9", "current")], mutate)
                with self.assertRaisesRegex(MatrixValidationError, message):
                    ingest_session_files(
                        empty_matrix(),
                        session_paths=list(sessions.values()),
                        artifact_root=root,
                    )

    def test_ingestion_rejects_duplicate_hashes_after_file_verification(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sessions = _write_sessions(root, _complete_matrix(root))
            session_path = sessions[("16:9", "current")]
            payload = json.loads(session_path.read_text(encoding="utf-8"))
            first, second = payload["artifacts"][:2]
            first_bytes = (root / first["path"]).read_bytes()
            (root / second["path"]).write_bytes(first_bytes)
            second["sha256"] = first["sha256"]
            session_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")

            with self.assertRaisesRegex(MatrixValidationError, "SHA-256 is reused"):
                ingest_session_files(
                    empty_matrix(),
                    session_paths=list(sessions.values()),
                    artifact_root=root,
                )

    def test_ingest_cli_is_fail_closed_and_does_not_auto_accept(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            sessions = _write_sessions(root, _complete_matrix(root))
            matrix_path = root / "matrix.json"
            tracked = empty_matrix()
            tracked["rows"][0]["reviewerDecision"] = "failed"
            tracked["rows"][0]["reviewerNotes"] = "Keep this reviewer decision."
            matrix_path.write_text(json.dumps(tracked, indent=2) + "\n", encoding="utf-8")
            command = [
                sys.executable,
                str(TOOL_PATH),
                "--ingest-sessions",
                "--matrix",
                str(matrix_path),
                "--artifact-root",
                str(root),
            ]
            result = subprocess.run(command, check=False, capture_output=True, text=True)
            self.assertEqual(0, result.returncode, result.stdout + result.stderr)
            self.assertIn("reviewerDecisions=preserved", result.stdout)
            ingested = json.loads(matrix_path.read_text(encoding="utf-8"))
            self.assertEqual("failed", ingested["rows"][0]["reviewerDecision"])
            self.assertEqual("pending", ingested["rows"][1]["reviewerDecision"])

            matrix_path.write_text(json.dumps(tracked, indent=2) + "\n", encoding="utf-8")
            original = matrix_path.read_bytes()
            _rewrite_json(
                sessions[("20:9", "candidate")],
                lambda payload: payload.__setitem__("deviceProfile", "drifted-device"),
            )
            result = subprocess.run(command, check=False, capture_output=True, text=True)
            self.assertEqual(1, result.returncode)
            self.assertIn("result=Failed", result.stdout)
            self.assertEqual(original, matrix_path.read_bytes())

    def test_rejects_missing_artifact(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            matrix = _complete_matrix(root)
            first = root / matrix["rows"][0]["artifacts"][0]["path"]
            first.unlink()
            with self.assertRaisesRegex(MatrixValidationError, "does not exist"):
                validate_acceptance(matrix, artifact_root=root)

    def test_rejects_artifact_hash_mismatch(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            matrix = _complete_matrix(root)
            matrix["rows"][0]["artifacts"][0]["sha256"] = "0" * 64
            with self.assertRaisesRegex(MatrixValidationError, "sha256 does not match"):
                validate_acceptance(matrix, artifact_root=root)

    def test_rejects_metadata_or_png_dimension_mismatch(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            matrix = _complete_matrix(root)
            matrix["rows"][0]["artifacts"][0]["width"] = 1
            with self.assertRaisesRegex(MatrixValidationError, "metadata dimensions"):
                validate_acceptance(matrix, artifact_root=root)

    def test_rejects_corrupt_png_crc_even_with_matching_hash(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            matrix = _complete_matrix(root)
            artifact = matrix["rows"][0]["artifacts"][0]
            target = root / artifact["path"]
            payload = bytearray(target.read_bytes())
            payload[40] ^= 1
            target.write_bytes(payload)
            artifact["sha256"] = hashlib.sha256(payload).hexdigest()
            with self.assertRaisesRegex(MatrixValidationError, "CRC mismatch"):
                validate_acceptance(matrix, artifact_root=root)

    def test_rejects_noncanonical_artifacts_and_comparison_camera_drift(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            base = _complete_matrix(root)
            duplicate = copy.deepcopy(base)
            duplicate["rows"][8]["artifacts"][0] = copy.deepcopy(
                duplicate["rows"][0]["artifacts"][0]
            )
            duplicate["rows"][8]["artifacts"][0]["state"] = duplicate["rows"][8]["state"]
            with self.assertRaisesRegex(MatrixValidationError, "path must be"):
                validate_acceptance(duplicate, artifact_root=root)

            drift = copy.deepcopy(base)
            comparison = next(row for row in drift["rows"] if row["qualityTier"] == "comparison")
            comparison["artifacts"][1]["cameraPosition"][0] += 0.01
            with self.assertRaisesRegex(MatrixValidationError, "exact same camera"):
                validate_acceptance(drift, artifact_root=root)


if __name__ == "__main__":
    unittest.main()
