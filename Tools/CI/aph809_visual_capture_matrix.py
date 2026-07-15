#!/usr/bin/env python3
"""Validate the deterministic, fail-closed APH-809 visual-capture matrix."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import struct
import tempfile
import zlib
from datetime import datetime
from pathlib import Path, PurePosixPath
from typing import Any


SCHEMA_VERSION = 1
TASK_ID = "APH-809"
EXPECTED_SLOT_COUNT = 26
EXPECTED_ARTIFACT_COUNT = 32
CAPTURE_ROOT = PurePosixPath(
    "Design/AgentReports/Captures/ArchitecturePerformanceHardening/APH-809"
)
DEFAULT_MATRIX_PATH = Path(
    "Design/AgentReports/2026-07-13_aph-809_visual_capture_matrix.json"
)
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"
SHA256_PATTERN = re.compile(r"^[0-9a-f]{64}$")
REVISION_PATTERN = re.compile(r"^[0-9a-f]{40}$")
UTC_PATTERN = re.compile(r"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$")
ASPECT_DIMENSIONS = {"16:9": (1920, 1080), "20:9": (2400, 1080)}
SESSION_PROFILES = ("current", "candidate")
SESSION_IDENTITIES = tuple(
    (aspect, profile)
    for aspect in ASPECT_DIMENSIONS
    for profile in SESSION_PROFILES
)
ARTIFACT_FIELDS = (
    "role",
    "path",
    "sha256",
    "width",
    "height",
    "capturedAtUtc",
    "revision",
    "deviceProfile",
    "frameRateMode",
    "qualityTier",
    "cameraPosition",
    "cameraRotation",
    "state",
)
ARTIFACT_KEYS = set(ARTIFACT_FIELDS)


class MatrixValidationError(RuntimeError):
    pass


def _row(
    row_id: str,
    surface: str,
    category: str,
    aspect: str,
    camera: str,
    state: str,
    quality_tier: str,
    artifact_roles: tuple[str, ...],
) -> dict[str, Any]:
    return {
        "id": row_id,
        "surface": surface,
        "category": category,
        "aspect": aspect,
        "camera": camera,
        "state": state,
        "qualityTier": quality_tier,
        "artifactRoles": list(artifact_roles),
    }


def expected_rows() -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for aspect in ASPECT_DIMENSIONS:
        slug = aspect.replace(":", "x")
        rows.append(
            _row(
                f"menu-main-{slug}",
                "menu",
                "menu",
                aspect,
                "main-menu",
                "main-menu-idle",
                "current",
                ("capture",),
            )
        )

    graphics_views = (
        ("gameplay-zoom", "gameplay-zoom"),
        ("max-zoom-out", "max-zoom-out"),
        ("night", "gameplay-zoom"),
    )
    for aspect in ASPECT_DIMENSIONS:
        slug = aspect.replace(":", "x")
        for view, camera in graphics_views:
            rows.append(
                _row(
                    f"graphics-tier-{view}-{slug}",
                    "match",
                    "graphics-tier",
                    aspect,
                    camera,
                    f"{view}-current-vs-candidate",
                    "comparison",
                    ("current", "candidate"),
                )
            )

    day_night_states = (
        ("day", "day-12-00"),
        ("dusk", "dusk-21-00"),
        ("night", "night-23-00"),
    )
    for aspect in ASPECT_DIMENSIONS:
        slug = aspect.replace(":", "x")
        for phase, state in day_night_states:
            rows.append(
                _row(
                    f"day-night-{phase}-{slug}",
                    "match",
                    "day-night",
                    aspect,
                    "gameplay-zoom",
                    state,
                    "current",
                    ("capture",),
                )
            )

    for aspect in ASPECT_DIMENSIONS:
        slug = aspect.replace(":", "x")
        for distance in ("near", "medium", "far"):
            rows.append(
                _row(
                    f"static-map-{distance}-{slug}",
                    "match",
                    "static-map-chunks",
                    aspect,
                    f"static-map-{distance}",
                    f"{distance}-chunk-readability",
                    "current",
                    ("capture",),
                )
            )

    for aspect in ASPECT_DIMENSIONS:
        slug = aspect.replace(":", "x")
        for distance in ("near", "medium", "far"):
            rows.append(
                _row(
                    f"mip-streaming-{distance}-{slug}",
                    "match",
                    "mip-streaming",
                    aspect,
                    f"mip-streaming-{distance}",
                    f"{distance}-settled",
                    "current",
                    ("capture",),
                )
            )

    if len(rows) != EXPECTED_SLOT_COUNT:
        raise AssertionError(f"expected {EXPECTED_SLOT_COUNT} rows, built {len(rows)}")
    artifact_count = sum(len(row["artifactRoles"]) for row in rows)
    if artifact_count != EXPECTED_ARTIFACT_COUNT:
        raise AssertionError(
            f"expected {EXPECTED_ARTIFACT_COUNT} artifacts, built {artifact_count}"
        )
    return rows


def empty_matrix() -> dict[str, Any]:
    return {
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "revision": None,
        "deviceProfile": None,
        "frameRateMode": None,
        "rows": [
            {
                **row,
                "artifacts": [],
                "reviewerDecision": "pending",
                "reviewerNotes": None,
            }
            for row in expected_rows()
        ],
    }


def expected_session_paths(artifact_root: Path) -> list[Path]:
    return [
        artifact_root.joinpath(
            *CAPTURE_ROOT.parts,
            f"aph809_{aspect.replace(':', 'x')}_{profile}_capture_session.json",
        )
        for aspect, profile in SESSION_IDENTITIES
    ]


def _object(value: Any, path: str) -> dict[str, Any]:
    if not isinstance(value, dict):
        raise MatrixValidationError(f"{path} must be an object")
    return value


def _only_keys(value: dict[str, Any], allowed: set[str], path: str) -> None:
    unknown = sorted(set(value) - allowed)
    missing = sorted(allowed - set(value))
    if unknown:
        raise MatrixValidationError(f"{path} has unknown fields: {', '.join(unknown)}")
    if missing:
        raise MatrixValidationError(f"{path} is missing fields: {', '.join(missing)}")


def _non_empty(value: Any, path: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise MatrixValidationError(f"{path} must be a non-empty string")
    return value.strip()


def validate_inventory(data: Any) -> dict[str, Any]:
    matrix = _object(data, "matrix")
    top_keys = {
        "schemaVersion",
        "taskId",
        "revision",
        "deviceProfile",
        "frameRateMode",
        "rows",
    }
    _only_keys(matrix, top_keys, "matrix")
    if matrix["schemaVersion"] != SCHEMA_VERSION:
        raise MatrixValidationError(f"schemaVersion must be {SCHEMA_VERSION}")
    if matrix["taskId"] != TASK_ID:
        raise MatrixValidationError(f"taskId must be {TASK_ID}")
    rows = matrix["rows"]
    if not isinstance(rows, list):
        raise MatrixValidationError("rows must be an array")
    if len(rows) != EXPECTED_SLOT_COUNT:
        raise MatrixValidationError(
            f"rows must contain exactly {EXPECTED_SLOT_COUNT} entries; found {len(rows)}"
        )

    expected = expected_rows()
    row_keys = set(expected[0]) | {"artifacts", "reviewerDecision", "reviewerNotes"}
    seen_ids: set[str] = set()
    for index, (actual_value, expected_row) in enumerate(zip(rows, expected)):
        path = f"rows[{index}]"
        actual = _object(actual_value, path)
        _only_keys(actual, row_keys, path)
        for key, expected_value in expected_row.items():
            if actual[key] != expected_value:
                raise MatrixValidationError(
                    f"{path}.{key} must be {expected_value!r}; found {actual[key]!r}"
                )
        if actual["id"] in seen_ids:
            raise MatrixValidationError(f"duplicate row id: {actual['id']}")
        seen_ids.add(actual["id"])
        if not isinstance(actual["artifacts"], list):
            raise MatrixValidationError(f"{path}.artifacts must be an array")
        if actual["reviewerDecision"] not in {"pending", "passed", "failed"}:
            raise MatrixValidationError(
                f"{path}.reviewerDecision must be pending, passed, or failed"
            )
        notes = actual["reviewerNotes"]
        if notes is not None and (not isinstance(notes, str) or not notes.strip()):
            raise MatrixValidationError(f"{path}.reviewerNotes must be null or non-empty")

    return matrix


def _validate_vector(value: Any, path: str) -> tuple[float, float, float]:
    if not isinstance(value, list) or len(value) != 3:
        raise MatrixValidationError(f"{path} must contain exactly three numbers")
    result: list[float] = []
    for index, component in enumerate(value):
        if isinstance(component, bool) or not isinstance(component, (int, float)):
            raise MatrixValidationError(f"{path}[{index}] must be a number")
        number = float(component)
        if not math.isfinite(number):
            raise MatrixValidationError(f"{path}[{index}] must be finite")
        result.append(number)
    return result[0], result[1], result[2]


def _resolve_artifact_path(path_text: str, artifact_root: Path) -> Path:
    posix = PurePosixPath(path_text)
    if posix.is_absolute() or ".." in posix.parts:
        raise MatrixValidationError("artifact.path must be a project-relative path without '..'")
    if posix.suffix.lower() != ".png":
        raise MatrixValidationError("artifact.path must name a PNG")
    try:
        posix.relative_to(CAPTURE_ROOT)
    except ValueError as exc:
        raise MatrixValidationError(
            f"artifact.path must be under {CAPTURE_ROOT.as_posix()}"
        ) from exc
    return artifact_root.joinpath(*posix.parts)


def _expected_artifact_path(row_id: str, role: str) -> str:
    return (CAPTURE_ROOT / f"{row_id}_{role}.png").as_posix()


def _parse_png(path: Path) -> tuple[int, int]:
    try:
        payload = path.read_bytes()
    except OSError as exc:
        raise MatrixValidationError(f"artifact does not exist or is unreadable: {path}") from exc
    if not payload.startswith(PNG_SIGNATURE):
        raise MatrixValidationError(f"artifact is not a PNG: {path}")

    offset = len(PNG_SIGNATURE)
    ihdr: bytes | None = None
    idat = bytearray()
    saw_iend = False
    while offset < len(payload):
        if offset + 12 > len(payload):
            raise MatrixValidationError(f"PNG chunk header is truncated: {path}")
        length = struct.unpack(">I", payload[offset : offset + 4])[0]
        chunk_type = payload[offset + 4 : offset + 8]
        data_start = offset + 8
        data_end = data_start + length
        crc_end = data_end + 4
        if crc_end > len(payload):
            raise MatrixValidationError(f"PNG chunk is truncated: {path}")
        chunk_data = payload[data_start:data_end]
        expected_crc = struct.unpack(">I", payload[data_end:crc_end])[0]
        actual_crc = zlib.crc32(chunk_type + chunk_data) & 0xFFFFFFFF
        if actual_crc != expected_crc:
            raise MatrixValidationError(f"PNG CRC mismatch: {path}")
        if ihdr is None:
            if chunk_type != b"IHDR" or length != 13:
                raise MatrixValidationError(f"PNG must begin with a 13-byte IHDR: {path}")
            ihdr = chunk_data
        elif chunk_type == b"IHDR":
            raise MatrixValidationError(f"PNG contains multiple IHDR chunks: {path}")
        if chunk_type == b"IDAT":
            idat.extend(chunk_data)
        if chunk_type == b"IEND":
            if length != 0 or crc_end != len(payload):
                raise MatrixValidationError(f"PNG IEND is malformed or not final: {path}")
            saw_iend = True
            break
        offset = crc_end

    if ihdr is None or not idat or not saw_iend:
        raise MatrixValidationError(f"PNG is missing IHDR, IDAT, or IEND: {path}")
    width, height, bit_depth, color_type, compression, filtering, interlace = struct.unpack(
        ">IIBBBBB", ihdr
    )
    if width <= 0 or height <= 0:
        raise MatrixValidationError(f"PNG dimensions must be positive: {path}")
    if bit_depth != 8 or color_type not in {2, 6} or compression != 0 or filtering != 0 or interlace != 0:
        raise MatrixValidationError(
            f"PNG must be non-interlaced 8-bit RGB or RGBA: {path}"
        )
    channels = 3 if color_type == 2 else 4
    expected_raw_length = (1 + width * channels) * height
    try:
        raw = zlib.decompress(bytes(idat))
    except zlib.error as exc:
        raise MatrixValidationError(f"PNG IDAT stream is invalid: {path}") from exc
    if len(raw) != expected_raw_length:
        raise MatrixValidationError(
            f"PNG decompressed length mismatch: {path}; expected {expected_raw_length}, found {len(raw)}"
        )
    stride = 1 + width * channels
    if any(raw[row * stride] > 4 for row in range(height)):
        raise MatrixValidationError(f"PNG contains an invalid scanline filter: {path}")
    return width, height


def _validate_artifact(
    value: Any,
    path: str,
    *,
    row: dict[str, Any],
    role: str,
    matrix: dict[str, Any],
    artifact_root: Path,
) -> tuple[str, str, tuple[float, float, float], tuple[float, float, float]]:
    artifact = _object(value, path)
    _only_keys(artifact, ARTIFACT_KEYS, path)
    if artifact["role"] != role:
        raise MatrixValidationError(f"{path}.role must be {role!r}")
    path_text = _non_empty(artifact["path"], f"{path}.path")
    expected_path = _expected_artifact_path(row["id"], role)
    if path_text != expected_path:
        raise MatrixValidationError(f"{path}.path must be {expected_path!r}")
    resolved = _resolve_artifact_path(path_text, artifact_root)
    digest = _non_empty(artifact["sha256"], f"{path}.sha256")
    if not SHA256_PATTERN.fullmatch(digest):
        raise MatrixValidationError(f"{path}.sha256 must be lowercase SHA-256")
    try:
        actual_digest = hashlib.sha256(resolved.read_bytes()).hexdigest()
    except OSError as exc:
        raise MatrixValidationError(f"artifact does not exist or is unreadable: {resolved}") from exc
    if digest != actual_digest:
        raise MatrixValidationError(f"{path}.sha256 does not match {resolved}")

    expected_width, expected_height = ASPECT_DIMENSIONS[row["aspect"]]
    if artifact["width"] != expected_width or artifact["height"] != expected_height:
        raise MatrixValidationError(
            f"{path} metadata dimensions must be {expected_width}x{expected_height}"
        )
    png_width, png_height = _parse_png(resolved)
    if (png_width, png_height) != (expected_width, expected_height):
        raise MatrixValidationError(
            f"{path} PNG dimensions must be {expected_width}x{expected_height}; "
            f"found {png_width}x{png_height}"
        )

    captured_at = _non_empty(artifact["capturedAtUtc"], f"{path}.capturedAtUtc")
    if not UTC_PATTERN.fullmatch(captured_at):
        raise MatrixValidationError(f"{path}.capturedAtUtc must be UTC ISO-8601")
    try:
        datetime.fromisoformat(captured_at[:-1] + "+00:00")
    except ValueError as exc:
        raise MatrixValidationError(f"{path}.capturedAtUtc is invalid") from exc

    for key in ("revision", "deviceProfile", "frameRateMode"):
        if artifact[key] != matrix[key]:
            raise MatrixValidationError(f"{path}.{key} must match matrix.{key}")
    expected_tier = role if row["qualityTier"] == "comparison" else row["qualityTier"]
    if artifact["qualityTier"] != expected_tier:
        raise MatrixValidationError(f"{path}.qualityTier must be {expected_tier!r}")
    if artifact["state"] != row["state"]:
        raise MatrixValidationError(f"{path}.state must match row state")

    position = _validate_vector(artifact["cameraPosition"], f"{path}.cameraPosition")
    rotation = _validate_vector(artifact["cameraRotation"], f"{path}.cameraRotation")
    return path_text, digest, position, rotation


def _validate_submitted_evidence(
    data: Any,
    *,
    artifact_root: Path,
    require_reviewer_pass: bool,
) -> tuple[dict[str, Any], str]:
    matrix = validate_inventory(data)
    revision = _non_empty(matrix["revision"], "matrix.revision")
    if not REVISION_PATTERN.fullmatch(revision):
        raise MatrixValidationError("matrix.revision must be an exact 40-character lowercase commit")
    _non_empty(matrix["deviceProfile"], "matrix.deviceProfile")
    if matrix["frameRateMode"] not in {"30fps", "60fps"}:
        raise MatrixValidationError("matrix.frameRateMode must be 30fps or 60fps")

    seen_paths: set[str] = set()
    seen_hashes: set[str] = set()
    for index, row in enumerate(matrix["rows"]):
        path = f"rows[{index}]"
        if require_reviewer_pass:
            if row["reviewerDecision"] != "passed":
                raise MatrixValidationError(f"{path}.reviewerDecision must be passed")
            _non_empty(row["reviewerNotes"], f"{path}.reviewerNotes")
        roles = row["artifactRoles"]
        artifacts = row["artifacts"]
        if len(artifacts) != len(roles):
            raise MatrixValidationError(
                f"{path}.artifacts must contain exactly {len(roles)} entries"
            )
        camera_pairs: list[tuple[tuple[float, float, float], tuple[float, float, float]]] = []
        for artifact_index, (artifact, role) in enumerate(zip(artifacts, roles)):
            artifact_path, digest, position, rotation = _validate_artifact(
                artifact,
                f"{path}.artifacts[{artifact_index}]",
                row=row,
                role=role,
                matrix=matrix,
                artifact_root=artifact_root,
            )
            if artifact_path in seen_paths:
                raise MatrixValidationError(f"artifact path is reused: {artifact_path}")
            if digest in seen_hashes:
                raise MatrixValidationError(f"artifact SHA-256 is reused: {digest}")
            seen_paths.add(artifact_path)
            seen_hashes.add(digest)
            camera_pairs.append((position, rotation))
        if row["qualityTier"] == "comparison" and len(set(camera_pairs)) != 1:
            raise MatrixValidationError(
                f"{path} current/candidate artifacts must use the exact same camera transform"
            )

    if len(seen_paths) != EXPECTED_ARTIFACT_COUNT:
        raise MatrixValidationError(
            f"matrix must provide exactly {EXPECTED_ARTIFACT_COUNT} unique PNG artifacts"
        )
    return matrix, revision


def validate_acceptance(data: Any, *, artifact_root: Path) -> dict[str, Any]:
    _, revision = _validate_submitted_evidence(
        data,
        artifact_root=artifact_root,
        require_reviewer_pass=True,
    )
    return {
        "result": "Passed",
        "taskId": TASK_ID,
        "slotsSatisfied": EXPECTED_SLOT_COUNT,
        "slotsRequired": EXPECTED_SLOT_COUNT,
        "artifactsValidated": EXPECTED_ARTIFACT_COUNT,
        "artifactsRequired": EXPECTED_ARTIFACT_COUNT,
        "revision": revision,
    }


def _expected_session_artifacts(aspect: str, profile: str) -> set[tuple[str, str]]:
    expected: set[tuple[str, str]] = set()
    for row in expected_rows():
        if row["aspect"] != aspect:
            continue
        for role in row["artifactRoles"]:
            source_profile = "candidate" if role == "candidate" else "current"
            if source_profile == profile:
                expected.add((row["id"], role))
    expected_count = 13 if profile == "current" else 3
    if len(expected) != expected_count:
        raise AssertionError(
            f"expected {expected_count} artifacts for {aspect}/{profile}, built {len(expected)}"
        )
    return expected


def _validate_aph505_fragment(
    value: Any,
    path: str,
    *,
    revision: str,
    profile: str,
) -> None:
    fragment = _object(value, path)
    keys = {
        "schemaVersion",
        "taskId",
        "status",
        "exactCommit",
        "dirty",
        "candidatePaths",
        "capturedViews",
        "beforeAfterRole",
        "beforeAfterPairsComplete",
        "accepted",
    }
    _only_keys(fragment, keys, path)
    if type(fragment["schemaVersion"]) is not int or fragment["schemaVersion"] != 1:
        raise MatrixValidationError(f"{path}.schemaVersion must be 1")
    if fragment["taskId"] != "APH-505":
        raise MatrixValidationError(f"{path}.taskId must be APH-505")
    if fragment["status"] != "capture-session":
        raise MatrixValidationError(f"{path}.status must be capture-session")
    if fragment["exactCommit"] != revision:
        raise MatrixValidationError(f"{path}.exactCommit must match session.revision")
    if fragment["dirty"] is not False:
        raise MatrixValidationError(f"{path}.dirty must be false")
    candidate_paths = fragment["candidatePaths"]
    if not isinstance(candidate_paths, list):
        raise MatrixValidationError(f"{path}.candidatePaths must be an array")
    for index, candidate_path in enumerate(candidate_paths):
        _non_empty(candidate_path, f"{path}.candidatePaths[{index}]")
    if fragment["capturedViews"] != ["near", "medium", "far"]:
        raise MatrixValidationError(f"{path}.capturedViews must be near, medium, far")
    if fragment["beforeAfterRole"] != profile:
        raise MatrixValidationError(f"{path}.beforeAfterRole must match session.profile")
    if fragment["beforeAfterPairsComplete"] is not False:
        raise MatrixValidationError(f"{path}.beforeAfterPairsComplete must be false")
    if fragment["accepted"] is not False:
        raise MatrixValidationError(f"{path}.accepted must be false")


def _validate_session(
    value: Any,
    path: str,
    *,
    expected_aspect: str,
    expected_profile: str,
) -> dict[str, Any]:
    session = _object(value, path)
    keys = {
        "schemaVersion",
        "taskId",
        "revision",
        "dirty",
        "deviceProfile",
        "frameRateMode",
        "aspect",
        "profile",
        "cameraContractPath",
        "artifactCount",
        "artifacts",
        "aph505EvidenceFragment",
    }
    _only_keys(session, keys, path)
    if type(session["schemaVersion"]) is not int or session["schemaVersion"] != SCHEMA_VERSION:
        raise MatrixValidationError(f"{path}.schemaVersion must be {SCHEMA_VERSION}")
    if session["taskId"] != TASK_ID:
        raise MatrixValidationError(f"{path}.taskId must be {TASK_ID}")
    revision = _non_empty(session["revision"], f"{path}.revision")
    if not REVISION_PATTERN.fullmatch(revision):
        raise MatrixValidationError(
            f"{path}.revision must be an exact 40-character lowercase commit"
        )
    if session["dirty"] is not False:
        raise MatrixValidationError(f"{path}.dirty must be false")
    device_profile = _non_empty(session["deviceProfile"], f"{path}.deviceProfile")
    frame_rate_mode = session["frameRateMode"]
    if frame_rate_mode not in {"30fps", "60fps"}:
        raise MatrixValidationError(f"{path}.frameRateMode must be 30fps or 60fps")
    if session["aspect"] != expected_aspect:
        raise MatrixValidationError(f"{path}.aspect must be {expected_aspect!r}")
    if session["profile"] != expected_profile:
        raise MatrixValidationError(f"{path}.profile must be {expected_profile!r}")

    aspect_token = expected_aspect.replace(":", "x")
    expected_contract = (
        CAPTURE_ROOT / f"aph809_camera_contract_{aspect_token}.json"
    ).as_posix()
    if session["cameraContractPath"] != expected_contract:
        raise MatrixValidationError(
            f"{path}.cameraContractPath must be {expected_contract!r}"
        )

    expected_pairs = _expected_session_artifacts(expected_aspect, expected_profile)
    expected_count = len(expected_pairs)
    if type(session["artifactCount"]) is not int or session["artifactCount"] != expected_count:
        raise MatrixValidationError(f"{path}.artifactCount must be {expected_count}")
    artifacts = session["artifacts"]
    if not isinstance(artifacts, list) or len(artifacts) != expected_count:
        raise MatrixValidationError(
            f"{path}.artifacts must contain exactly {expected_count} entries"
        )

    rows_by_id = {row["id"]: row for row in expected_rows()}
    validated_artifacts: dict[tuple[str, str], dict[str, Any]] = {}
    for index, artifact_value in enumerate(artifacts):
        artifact_path = f"{path}.artifacts[{index}]"
        artifact = _object(artifact_value, artifact_path)
        _only_keys(artifact, ARTIFACT_KEYS | {"rowId"}, artifact_path)
        row_id = _non_empty(artifact["rowId"], f"{artifact_path}.rowId")
        role = _non_empty(artifact["role"], f"{artifact_path}.role")
        pair = (row_id, role)
        if pair not in expected_pairs:
            raise MatrixValidationError(
                f"{artifact_path} is not expected for {expected_aspect}/{expected_profile}: "
                f"rowId={row_id!r} role={role!r}"
            )
        if pair in validated_artifacts:
            raise MatrixValidationError(
                f"{path}.artifacts contains duplicate rowId/role: {row_id}/{role}"
            )
        row = rows_by_id[row_id]
        if artifact["revision"] != revision:
            raise MatrixValidationError(f"{artifact_path}.revision must match session.revision")
        if artifact["deviceProfile"] != device_profile:
            raise MatrixValidationError(
                f"{artifact_path}.deviceProfile must match session.deviceProfile"
            )
        if artifact["frameRateMode"] != frame_rate_mode:
            raise MatrixValidationError(
                f"{artifact_path}.frameRateMode must match session.frameRateMode"
            )
        if artifact["qualityTier"] != expected_profile:
            raise MatrixValidationError(
                f"{artifact_path}.qualityTier must be {expected_profile!r}"
            )
        if artifact["state"] != row["state"]:
            raise MatrixValidationError(f"{artifact_path}.state must match row state")
        expected_artifact_path = _expected_artifact_path(row_id, role)
        if artifact["path"] != expected_artifact_path:
            raise MatrixValidationError(
                f"{artifact_path}.path must be {expected_artifact_path!r}"
            )
        validated_artifacts[pair] = {
            key: artifact[key]
            for key in ARTIFACT_FIELDS
        }

    missing = sorted(expected_pairs - set(validated_artifacts))
    if missing:
        formatted = ", ".join(f"{row_id}/{role}" for row_id, role in missing)
        raise MatrixValidationError(f"{path}.artifacts is missing expected entries: {formatted}")
    _validate_aph505_fragment(
        session["aph505EvidenceFragment"],
        f"{path}.aph505EvidenceFragment",
        revision=revision,
        profile=expected_profile,
    )
    return {
        "revision": revision,
        "deviceProfile": device_profile,
        "frameRateMode": frame_rate_mode,
        "artifacts": validated_artifacts,
    }


def ingest_session_files(
    data: Any,
    *,
    session_paths: list[Path],
    artifact_root: Path,
) -> dict[str, Any]:
    matrix = validate_inventory(data)
    if len(session_paths) != 4:
        raise MatrixValidationError(
            f"session ingestion requires exactly four metadata files; found {len(session_paths)}"
        )

    root = artifact_root.resolve()
    expected_files = expected_session_paths(root)
    provided_files = [path.resolve() for path in session_paths]
    if len(set(provided_files)) != 4:
        raise MatrixValidationError("session metadata paths must be unique")
    if set(provided_files) != set(expected_files):
        raise MatrixValidationError(
            f"session metadata files must be the four canonical files under {CAPTURE_ROOT.as_posix()}"
        )

    sessions: list[dict[str, Any]] = []
    artifacts_by_pair: dict[tuple[str, str], dict[str, Any]] = {}
    for (aspect, profile), session_path in zip(SESSION_IDENTITIES, expected_files):
        try:
            payload = json.loads(session_path.read_text(encoding="utf-8-sig"))
        except OSError as exc:
            raise MatrixValidationError(
                f"session metadata does not exist or is unreadable: {session_path}"
            ) from exc
        except json.JSONDecodeError as exc:
            raise MatrixValidationError(
                f"session metadata is not valid JSON: {session_path}: {exc}"
            ) from exc
        session = _validate_session(
            payload,
            f"session[{aspect}/{profile}]",
            expected_aspect=aspect,
            expected_profile=profile,
        )
        sessions.append(session)
        overlap = set(artifacts_by_pair) & set(session["artifacts"])
        if overlap:
            row_id, role = sorted(overlap)[0]
            raise MatrixValidationError(
                f"session metadata reuses rowId/role across sessions: {row_id}/{role}"
            )
        artifacts_by_pair.update(session["artifacts"])

    revisions = {session["revision"] for session in sessions}
    device_profiles = {session["deviceProfile"] for session in sessions}
    frame_rate_modes = {session["frameRateMode"] for session in sessions}
    if len(revisions) != 1:
        raise MatrixValidationError("all four sessions must use the same revision")
    if len(device_profiles) != 1:
        raise MatrixValidationError("all four sessions must use the same deviceProfile")
    if len(frame_rate_modes) != 1:
        raise MatrixValidationError("all four sessions must use the same frameRateMode")
    if len(artifacts_by_pair) != EXPECTED_ARTIFACT_COUNT:
        raise MatrixValidationError(
            f"sessions must provide exactly {EXPECTED_ARTIFACT_COUNT} artifacts"
        )

    ingested_rows: list[dict[str, Any]] = []
    for expected_row, existing_row in zip(expected_rows(), matrix["rows"]):
        artifacts = [
            artifacts_by_pair[(expected_row["id"], role)]
            for role in expected_row["artifactRoles"]
        ]
        ingested_rows.append(
            {
                **expected_row,
                "artifacts": artifacts,
                "reviewerDecision": existing_row["reviewerDecision"],
                "reviewerNotes": existing_row["reviewerNotes"],
            }
        )

    ingested = {
        "schemaVersion": SCHEMA_VERSION,
        "taskId": TASK_ID,
        "revision": next(iter(revisions)),
        "deviceProfile": next(iter(device_profiles)),
        "frameRateMode": next(iter(frame_rate_modes)),
        "rows": ingested_rows,
    }
    _validate_submitted_evidence(
        ingested,
        artifact_root=root,
        require_reviewer_pass=False,
    )
    return ingested


def _atomic_write_json(path: Path, value: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    output_mode = path.stat().st_mode & 0o777 if path.exists() else 0o644
    temporary_path: Path | None = None
    try:
        with tempfile.NamedTemporaryFile(
            mode="w",
            encoding="utf-8",
            dir=path.parent,
            prefix=f".{path.name}.",
            suffix=".tmp",
            delete=False,
        ) as temporary:
            temporary_path = Path(temporary.name)
            temporary.write(json.dumps(value, indent=2) + "\n")
            temporary.flush()
            os.fsync(temporary.fileno())
        temporary_path.chmod(output_mode)
        temporary_path.replace(path)
    finally:
        if temporary_path is not None and temporary_path.exists():
            temporary_path.unlink()


def render_report(matrix: dict[str, Any]) -> str:
    satisfied = sum(
        1
        for row in matrix["rows"]
        if row["reviewerDecision"] == "passed"
        and len(row["artifacts"]) == len(row["artifactRoles"])
    )
    artifacts = sum(len(row["artifacts"]) for row in matrix["rows"])
    lines = [
        "# APH-809 Visual Capture Matrix",
        "",
        "This report inventories the required evidence contract. It does not claim visual acceptance.",
        "",
        f"- Contract slots: `{EXPECTED_SLOT_COUNT}`",
        f"- Required PNG artifacts: `{EXPECTED_ARTIFACT_COUNT}`",
        f"- Slots with submitted artifacts and reviewer pass: `{satisfied} / {EXPECTED_SLOT_COUNT}`",
        f"- Submitted artifacts: `{artifacts} / {EXPECTED_ARTIFACT_COUNT}`",
        "- Acceptance status: `Incomplete`" if satisfied != EXPECTED_SLOT_COUNT else "- Acceptance status: `Ready for strict validation`",
        "- Strict command: `python3 Tools/CI/aph809_visual_capture_matrix.py --check`",
        "",
        "| Row | Surface | Category | Aspect | Camera | State | Required roles | Decision |",
        "|---|---|---|---|---|---|---|---|",
    ]
    for row in matrix["rows"]:
        lines.append(
            f"| `{row['id']}` | {row['surface']} | {row['category']} | {row['aspect']} | "
            f"{row['camera']} | {row['state']} | {', '.join(row['artifactRoles'])} | "
            f"{row['reviewerDecision']} |"
        )
    lines.extend(
        [
            "",
            "## Remaining Evidence",
            "",
            "Every pending row still needs a real PNG captured from one exact revision and device profile, "
            "complete capture metadata, SHA-256 verification, and an explicit reviewer decision. "
            "Current-versus-candidate rows additionally require identical camera transforms. Logs alone do not satisfy this contract.",
            "",
        ]
    )
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--check", action="store_true", help="Require complete accepted evidence.")
    mode.add_argument(
        "--inventory-only",
        action="store_true",
        help="Validate only the deterministic row inventory; never implies acceptance.",
    )
    mode.add_argument(
        "--ingest-sessions",
        action="store_true",
        help="Ingest the four canonical Unity capture-session metadata files.",
    )
    parser.add_argument("--matrix", type=Path, default=DEFAULT_MATRIX_PATH)
    parser.add_argument("--artifact-root", type=Path, default=Path("."))
    parser.add_argument("--report", type=Path)
    args = parser.parse_args()

    try:
        payload = json.loads(args.matrix.read_text(encoding="utf-8-sig"))
        matrix = validate_inventory(payload)
        if args.ingest_sessions:
            matrix = ingest_session_files(
                matrix,
                session_paths=expected_session_paths(args.artifact_root.resolve()),
                artifact_root=args.artifact_root,
            )
            _atomic_write_json(args.matrix, matrix)
        if args.report is not None:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            args.report.write_text(render_report(matrix), encoding="utf-8")
        if args.check:
            result = validate_acceptance(matrix, artifact_root=args.artifact_root)
            print(
                "[APH-809 VisualCaptureMatrix] result=Passed "
                f"slots={result['slotsSatisfied']}/{result['slotsRequired']} "
                f"artifacts={result['artifactsValidated']}/{result['artifactsRequired']} "
                f"revision={result['revision']}"
            )
        elif args.ingest_sessions:
            print(
                "[APH-809 VisualCaptureMatrix] result=Passed mode=ingest-sessions "
                f"sessions=4 slots={EXPECTED_SLOT_COUNT} artifacts={EXPECTED_ARTIFACT_COUNT} "
                f"revision={matrix['revision']} reviewerDecisions=preserved"
            )
        else:
            print(
                "[APH-809 VisualCaptureMatrix] result=Passed mode=inventory-only "
                f"slots={EXPECTED_SLOT_COUNT} artifacts={EXPECTED_ARTIFACT_COUNT} "
                "acceptanceReady=false"
            )
    except (OSError, json.JSONDecodeError, MatrixValidationError) as exc:
        print(f"[APH-809 VisualCaptureMatrix] result=Failed reason={exc}")
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
