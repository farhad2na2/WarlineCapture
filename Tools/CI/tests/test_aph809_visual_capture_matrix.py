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

    def test_rejects_reused_artifacts_and_comparison_camera_drift(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            base = _complete_matrix(root)
            duplicate = copy.deepcopy(base)
            duplicate["rows"][8]["artifacts"][0] = copy.deepcopy(
                duplicate["rows"][0]["artifacts"][0]
            )
            duplicate["rows"][8]["artifacts"][0]["state"] = duplicate["rows"][8]["state"]
            with self.assertRaisesRegex(MatrixValidationError, "artifact path is reused"):
                validate_acceptance(duplicate, artifact_root=root)

            drift = copy.deepcopy(base)
            comparison = next(row for row in drift["rows"] if row["qualityTier"] == "comparison")
            comparison["artifacts"][1]["cameraPosition"][0] += 0.01
            with self.assertRaisesRegex(MatrixValidationError, "exact same camera"):
                validate_acceptance(drift, artifact_root=root)


if __name__ == "__main__":
    unittest.main()
