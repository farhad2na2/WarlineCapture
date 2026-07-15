#!/usr/bin/env python3
"""Deterministic, read-only APH-507 Android texture override audit."""

from __future__ import annotations

import argparse
import binascii
import hashlib
import json
import re
import struct
import subprocess
import sys
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[2]
TASK_ID = "APH-507"
SCHEMA = "WarlineCapture.APH507AndroidTextureOverrideAudit.v1"
BUILD_REPORT_PATHS = (
    Path("Design/AgentReports/architecture_performance_android_aab_build_report.json"),
    Path("Design/AgentReports/architecture_performance_android_apk_build_report.json"),
)
CONTENT_RESIDENCY_PATH = Path(
    "Design/AgentReports/architecture_performance_content_residency_baseline.json"
)
VISUAL_EVIDENCE_PATH = Path(
    "Design/AgentReports/architecture_performance_android_texture_override_visual_evidence.json"
)
JSON_REPORT_PATH = Path(
    "Design/AgentReports/2026-07-15_aph-507_android_texture_override_audit.json"
)
MARKDOWN_REPORT_PATH = Path(
    "Design/AgentReports/2026-07-15_aph-507_android_texture_override_audit.md"
)
GENERATED_REPORT_PATHS = (JSON_REPORT_PATH, MARKDOWN_REPORT_PATH)
VISUAL_SCHEMA = "WarlineCapture.APH507AndroidTextureVisualEvidence.v1"
VISUAL_VIEWS = ("near", "medium", "far", "combat")
VISUAL_REJECTION_CHECKS = (
    "atlasBlur",
    "colorBleeding",
    "mipPop",
    "detailLoss",
    "uiContamination",
)
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
ASTC_RE = re.compile(r"ASTC_?(\d+)X(\d+)", re.IGNORECASE)
IMPORTER_FORMATS = {
    -1: "Automatic",
    48: "ASTC_4x4",
    49: "ASTC_5x5",
    50: "ASTC_6x6",
    51: "ASTC_8x8",
    52: "ASTC_10x10",
    53: "ASTC_12x12",
}
TEXTURE_COMPRESSION = {
    0: "uncompressed",
    1: "compressed",
    2: "high-quality compressed",
    3: "low-quality compressed",
}


class DuplicateJsonKeyError(ValueError):
    pass


def _reject_duplicate_json_keys(
    pairs: list[tuple[str, object]],
) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateJsonKeyError(key)
        result[key] = value
    return result


def read_json_object(path: Path) -> tuple[dict[str, object] | None, list[str]]:
    try:
        value = json.loads(
            path.read_text(encoding="utf-8"),
            object_pairs_hook=_reject_duplicate_json_keys,
        )
    except FileNotFoundError:
        return None, ["file-missing"]
    except OSError as error:
        return None, [f"file-unreadable:{type(error).__name__}"]
    except DuplicateJsonKeyError as error:
        return None, [f"duplicate-json-key:{error}"]
    except json.JSONDecodeError as error:
        return None, [f"json-invalid:{error.msg}"]
    if not isinstance(value, dict):
        return None, ["root-not-object"]
    return value, []


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def git_output(root: Path, *args: str) -> bytes:
    result = subprocess.run(
        ["git", *args],
        cwd=root,
        check=True,
        capture_output=True,
    )
    return result.stdout


def tracked_paths(root: Path = ROOT) -> list[str]:
    return sorted(
        path
        for path in git_output(root, "ls-files", "-z").decode(
            "utf-8", errors="surrogateescape"
        ).split("\0")
        if path
    )


def current_revision(
    root: Path = ROOT,
    ignored_paths: Iterable[Path] = GENERATED_REPORT_PATHS,
) -> tuple[str, list[str]]:
    head = git_output(root, "rev-parse", "HEAD").decode("ascii").strip()
    pathspecs = [".", *(f":(exclude){path.as_posix()}" for path in ignored_paths)]
    output = git_output(
        root,
        "status",
        "--porcelain=v1",
        "-z",
        "--untracked-files=no",
        "--",
        *pathspecs,
    )
    changes = sorted(
        entry
        for entry in output.decode("utf-8", errors="surrogateescape").split("\0")
        if entry
    )
    return head, changes


def evidence_path_label(path: Path, root: Path) -> str:
    try:
        return path.relative_to(root).as_posix()
    except ValueError:
        return path.as_posix()


def valid_project_path(value: object, prefix: str) -> bool:
    if not isinstance(value, str) or not value or "\\" in value:
        return False
    path = Path(value)
    return (
        not path.is_absolute()
        and ".." not in path.parts
        and value == path.as_posix()
        and value.startswith(prefix)
    )


def valid_unity_asset_path(value: object) -> bool:
    return valid_project_path(value, "Assets/") or valid_project_path(
        value, "Packages/"
    )


def scalar_int(yaml_text: str, key: str) -> int | None:
    match = re.search(
        rf"^\s+{re.escape(key)}:\s*(-?\d+)\s*$",
        yaml_text,
        re.MULTILINE,
    )
    return int(match.group(1)) if match else None


def parse_platform_settings(
    yaml_text: str,
) -> tuple[dict[str, dict[str, int | str]], list[str]]:
    blocks: list[dict[str, int | str]] = []
    current: dict[str, int | str] | None = None
    in_platform_settings = False

    for line in yaml_text.splitlines():
        if line == "  platformSettings:":
            in_platform_settings = True
            continue
        if not in_platform_settings:
            continue
        if line.startswith("  - "):
            if current is not None:
                blocks.append(current)
            current = {}
            body = line[4:]
        elif line.startswith("    ") and current is not None:
            body = line[4:]
        else:
            if current is not None:
                blocks.append(current)
            break

        match = re.fullmatch(r"([A-Za-z0-9_]+):\s*(.*)", body)
        if not match:
            continue
        key, raw_value = match.groups()
        if re.fullmatch(r"-?\d+", raw_value):
            current[key] = int(raw_value)
        else:
            current[key] = raw_value
    else:
        if current is not None:
            blocks.append(current)

    settings: dict[str, dict[str, int | str]] = {}
    errors: list[str] = []
    for index, block in enumerate(blocks):
        target = block.get("buildTarget")
        if not isinstance(target, str) or not target:
            errors.append(f"platform-block-target-invalid:{index}")
            continue
        if target in settings:
            errors.append(f"platform-block-duplicate:{target}")
            continue
        settings[target] = block
    if not blocks:
        errors.append("platform-settings-missing")
    return settings, errors


def compression_quality_label(value: int | None) -> str:
    if value is None:
        return "unknown"
    if value < 25:
        return "fast"
    if value > 75:
        return "best"
    return "normal"


def importer_format_label(value: int | None) -> str:
    if value is None:
        return "unknown"
    return IMPORTER_FORMATS.get(value, f"serialized-enum:{value}")


def parse_texture_importer(yaml_text: str) -> dict[str, object]:
    settings, errors = parse_platform_settings(yaml_text)
    default = settings.get("DefaultTexturePlatform")
    android = settings.get("Android")
    android_override_enabled = android is not None and android.get("overridden") == 1
    effective = android if android_override_enabled else default
    if effective is None:
        errors.append("effective-android-settings-missing")

    max_size = effective.get("maxTextureSize") if effective else None
    texture_format = effective.get("textureFormat") if effective else None
    texture_compression = effective.get("textureCompression") if effective else None
    compression_quality = effective.get("compressionQuality") if effective else None
    crunched = effective.get("crunchedCompression") if effective else None
    for key, value in (
        ("maxTextureSize", max_size),
        ("textureFormat", texture_format),
        ("textureCompression", texture_compression),
        ("compressionQuality", compression_quality),
        ("crunchedCompression", crunched),
    ):
        if not isinstance(value, int):
            errors.append(f"effective-setting-invalid:{key}")

    return {
        "androidBlockPresent": android is not None,
        "androidOverrideEnabled": android_override_enabled,
        "effectiveSettingsSource": (
            "Android" if android_override_enabled else "DefaultTexturePlatform"
        ),
        "androidMaxTextureSize": max_size if isinstance(max_size, int) else None,
        "androidTextureFormatValue": (
            texture_format if isinstance(texture_format, int) else None
        ),
        "androidTextureFormat": importer_format_label(
            texture_format if isinstance(texture_format, int) else None
        ),
        "androidTextureCompressionValue": (
            texture_compression if isinstance(texture_compression, int) else None
        ),
        "androidTextureCompression": TEXTURE_COMPRESSION.get(
            texture_compression,
            f"serialized-enum:{texture_compression}",
        ) if isinstance(texture_compression, int) else "unknown",
        "androidCompressionQuality": (
            compression_quality if isinstance(compression_quality, int) else None
        ),
        "androidCompressionQualityLabel": compression_quality_label(
            compression_quality if isinstance(compression_quality, int) else None
        ),
        "androidCrunchedCompression": (
            crunched == 1 if isinstance(crunched, int) else None
        ),
        "mipmapsEnabled": scalar_int(yaml_text, "enableMipMap") == 1,
        "textureType": scalar_int(yaml_text, "textureType"),
        "validationErrors": sorted(set(errors)),
    }


def image_dimensions(path: Path) -> tuple[int, int, str] | None:
    try:
        with path.open("rb") as handle:
            data = handle.read(256 * 1024)
    except OSError:
        return None

    if len(data) >= 24 and data[:8] == b"\x89PNG\r\n\x1a\n" and data[12:16] == b"IHDR":
        width, height = struct.unpack(">II", data[16:24])
        return (width, height, "PNG") if width and height else None
    if len(data) >= 10 and data[:6] in (b"GIF87a", b"GIF89a"):
        width, height = struct.unpack("<HH", data[6:10])
        return (width, height, "GIF") if width and height else None
    if len(data) >= 26 and data[:2] == b"BM":
        width, height = struct.unpack("<ii", data[18:26])
        return (abs(width), abs(height), "BMP") if width and height else None
    if len(data) >= 26 and data[:4] == b"8BPS":
        height, width = struct.unpack(">II", data[14:22])
        return (width, height, "PSD") if width and height else None
    if path.suffix.lower() == ".tga" and len(data) >= 18:
        width, height = struct.unpack("<HH", data[12:16])
        return (width, height, "TGA") if width and height else None
    if len(data) >= 4 and data[:2] == b"\xff\xd8":
        offset = 2
        while offset + 4 <= len(data):
            if data[offset] != 0xFF:
                offset += 1
                continue
            marker = data[offset + 1]
            offset += 2
            if marker in (0xD8, 0xD9) or 0xD0 <= marker <= 0xD7:
                continue
            if offset + 2 > len(data):
                break
            length = struct.unpack(">H", data[offset:offset + 2])[0]
            if length < 2 or offset + length > len(data):
                break
            if marker in {
                0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7,
                0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF,
            } and length >= 7:
                height, width = struct.unpack(">HH", data[offset + 3:offset + 7])
                return (width, height, "JPEG") if width and height else None
            offset += length
    return None


def validated_png_dimensions(path: Path) -> tuple[int, int] | None:
    try:
        data = path.read_bytes()
    except OSError:
        return None
    if not data.startswith(b"\x89PNG\r\n\x1a\n"):
        return None

    offset = 8
    dimensions: tuple[int, int] | None = None
    saw_idat = False
    saw_iend = False
    chunk_index = 0
    while offset + 12 <= len(data):
        length = struct.unpack(">I", data[offset:offset + 4])[0]
        chunk_type = data[offset + 4:offset + 8]
        chunk_end = offset + 12 + length
        if chunk_end > len(data):
            return None
        chunk_data = data[offset + 8:offset + 8 + length]
        expected_crc = struct.unpack(">I", data[offset + 8 + length:chunk_end])[0]
        actual_crc = binascii.crc32(chunk_type)
        actual_crc = binascii.crc32(chunk_data, actual_crc) & 0xFFFFFFFF
        if actual_crc != expected_crc:
            return None
        if chunk_index == 0:
            if chunk_type != b"IHDR" or length != 13:
                return None
            width, height = struct.unpack(">II", chunk_data[:8])
            if width == 0 or height == 0:
                return None
            dimensions = (width, height)
        elif chunk_type == b"IHDR":
            return None
        if chunk_type == b"IDAT":
            saw_idat = True
        if chunk_type == b"IEND":
            if length != 0 or chunk_end != len(data):
                return None
            saw_iend = True
            break
        offset = chunk_end
        chunk_index += 1
    return dimensions if dimensions is not None and saw_idat and saw_iend else None


def astc_block(format_name: object) -> tuple[int, int] | None:
    if not isinstance(format_name, str):
        return None
    match = ASTC_RE.search(format_name)
    if not match:
        return None
    width, height = int(match.group(1)), int(match.group(2))
    return (width, height) if width > 0 and height > 0 else None


def astc_quality_tier(format_name: object) -> str:
    block = astc_block(format_name)
    if block is None:
        return "unknown"
    texels = block[0] * block[1]
    if texels <= 16:
        return "very-high"
    if texels <= 25:
        return "high"
    if texels <= 36:
        return "balanced"
    if texels <= 64:
        return "compact"
    return "aggressive"


def dimensions_at_limit(width: int, height: int, limit: int) -> tuple[int, int]:
    maximum = max(width, height)
    if maximum <= limit:
        return width, height
    scale = limit / maximum
    return max(1, round(width * scale)), max(1, round(height * scale))


def astc_payload_bytes(
    width: int,
    height: int,
    format_name: object,
    mipmaps_enabled: bool,
) -> int | None:
    block = astc_block(format_name)
    if block is None:
        return None
    block_width, block_height = block
    total = 0
    while True:
        total += (
            ((width + block_width - 1) // block_width)
            * ((height + block_height - 1) // block_height)
            * 16
        )
        if not mipmaps_enabled or (width == 1 and height == 1):
            break
        width = max(1, width // 2)
        height = max(1, height // 2)
    return total


def texture_role(asset_path: str, texture_type: object) -> str:
    lower = asset_path.lower()
    if texture_type == 1 or any(
        token in lower for token in ("normal", "mask", "metallic", "roughness")
    ):
        return "world normal/mask"
    if any(token in lower for token in ("/ui/", "/gui/", "/fonts/")):
        return "UI"
    if any(token in lower for token in ("/vfx/", "/effects/", "/fx/")):
        return "VFX"
    if "atlas" in lower or "impostor" in lower:
        return "impostor/atlas"
    return "world albedo"


def load_content_residency(
    path: Path,
    root: Path,
) -> tuple[dict[str, dict[str, object]], dict[str, object]]:
    payload, read_errors = read_json_object(path)
    payload = payload or {}
    rows = payload.get("assets")
    errors = list(read_errors)
    result: dict[str, dict[str, object]] = {}
    if not isinstance(rows, list):
        errors.append("assets-not-array")
        rows = []
    ordered_paths: list[str] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict) or row.get("assetType") != "Texture2D":
            continue
        asset_path = row.get("assetPath")
        if not valid_unity_asset_path(asset_path):
            errors.append(f"texture-row-path-invalid:{index}")
            continue
        ordered_paths.append(asset_path)
        if asset_path in result:
            errors.append(f"texture-row-duplicate:{asset_path}")
            continue
        result[asset_path] = row
    if ordered_paths != sorted(ordered_paths):
        errors.append("texture-rows-not-sorted")
    summary = payload.get("summary")
    summary_count = summary.get("textureAssetCount") if isinstance(summary, dict) else None
    if not isinstance(summary_count, int):
        errors.append("summary-texture-count-invalid")
    elif summary_count != len(result):
        errors.append(f"summary-texture-count-mismatch:{summary_count}!={len(result)}")
    details = {
        "path": evidence_path_label(path, root),
        "status": payload.get("status"),
        "evidenceRevision": payload.get("baselineCommit"),
        "textureRows": len(result),
        "validationErrors": sorted(set(errors)),
        "disposition": "historical context only; never authorizes an override change",
    }
    return result, details


def _complete_build_texture_rows(
    payload: dict[str, object],
) -> tuple[dict[str, int], list[str]]:
    rows = payload.get("buildReportIncludedTextures")
    if not isinstance(rows, list):
        return {}, ["complete-texture-export-not-array"]
    result: dict[str, int] = {}
    ordered_paths: list[str] = []
    errors: list[str] = []
    for index, row in enumerate(rows):
        if not isinstance(row, dict):
            errors.append(f"complete-texture-row-not-object:{index}")
            continue
        asset_path = row.get("sourceAssetPath")
        packed_bytes = row.get("packedBytes")
        object_types = row.get("objectTypes")
        if (
            not valid_unity_asset_path(asset_path)
            or not isinstance(packed_bytes, int)
            or isinstance(packed_bytes, bool)
            or packed_bytes < 0
            or not isinstance(object_types, list)
            or "UnityEngine.Texture2D" not in object_types
        ):
            errors.append(f"complete-texture-row-invalid:{index}")
            continue
        ordered_paths.append(asset_path)
        if asset_path in result:
            errors.append(f"complete-texture-row-duplicate:{asset_path}")
            continue
        result[asset_path] = packed_bytes
    if ordered_paths != sorted(ordered_paths):
        errors.append("complete-texture-rows-not-sorted")
    if not rows:
        errors.append("complete-texture-export-empty")
    return result, errors


def _historical_top_texture_rows(payload: dict[str, object]) -> dict[str, int]:
    rows = payload.get("buildReportIncludedAssets")
    if not isinstance(rows, list):
        return {}
    result: dict[str, int] = {}
    for row in rows:
        if not isinstance(row, dict):
            continue
        asset_path = row.get("sourceAssetPath")
        packed_bytes = row.get("packedBytes")
        object_types = row.get("objectTypes")
        if (
            valid_unity_asset_path(asset_path)
            and isinstance(packed_bytes, int)
            and not isinstance(packed_bytes, bool)
            and packed_bytes >= 0
            and isinstance(object_types, list)
            and "UnityEngine.Texture2D" in object_types
        ):
            result.setdefault(asset_path, packed_bytes)
    return result


def validate_build_report(
    path: Path,
    root: Path,
    head: str,
    tracked_worktree_clean: bool,
    tracked: set[str],
    require_tracked: bool = True,
) -> tuple[dict[str, object], dict[str, int], dict[str, int]]:
    payload, read_errors = read_json_object(path)
    payload = payload or {}
    complete_rows, row_errors = _complete_build_texture_rows(payload)
    relative = evidence_path_label(path, root)
    errors = [*read_errors, *row_errors]
    expected = (
        ("schema-version-not-1", payload.get("schemaVersion") == 1),
        ("task-id-not-APH-500", payload.get("taskId") == "APH-500"),
        ("status-not-complete", payload.get("status") == "complete"),
        ("dirty-provenance-not-false", payload.get("dirty") is False),
        ("release-build-type-invalid", payload.get("releaseBuildType") == "release"),
        ("build-target-not-Android", payload.get("buildTarget") == "Android"),
        ("package-type-invalid", payload.get("packageType") in ("APK", "AAB")),
        ("detailed-build-report-not-true", payload.get("detailedBuildReport") is True),
        (
            "complete-texture-export-marker-not-true",
            payload.get("allIncludedTexturePathsExported") is True,
        ),
        (
            f"revision-mismatch:{payload.get('exactCommit')}->{head}",
            payload.get("exactCommit") == head,
        ),
        ("tracked-worktree-dirty", tracked_worktree_clean),
        ("evidence-path-not-tracked", not require_tracked or relative in tracked),
    )
    errors.extend(code for code, valid in expected if not valid)
    errors = sorted(set(errors))
    accepted = not errors
    details = {
        "path": relative,
        "packageType": payload.get("packageType"),
        "evidenceRevision": payload.get("exactCommit"),
        "dirty": payload.get("dirty"),
        "completeTextureRows": len(complete_rows),
        "topTableTextureRows": len(_historical_top_texture_rows(payload)),
        "completeTextureExport": (
            payload.get("allIncludedTexturePathsExported") is True and not row_errors
        ),
        "acceptedForCurrentRevision": accepted,
        "validationErrors": errors,
        "disposition": (
            "accepted current complete Android BuildReport"
            if accepted
            else "historical/incomplete/rejected only"
        ),
    }
    return details, complete_rows if accepted else {}, _historical_top_texture_rows(payload)


def _validate_visual_artifact(
    artifact: object,
    root: Path,
    tracked: set[str],
    require_tracked: bool,
    error_prefix: str,
) -> tuple[tuple[int, int] | None, list[str]]:
    if not isinstance(artifact, dict):
        return None, [f"{error_prefix}:artifact-not-object"]
    relative = artifact.get("path")
    expected_sha = artifact.get("sha256")
    errors: list[str] = []
    if not valid_project_path(relative, "Design/AgentReports/"):
        return None, [f"{error_prefix}:path-invalid"]
    if not isinstance(expected_sha, str) or not SHA256_RE.fullmatch(expected_sha):
        errors.append(f"{error_prefix}:sha256-invalid")
    if require_tracked and relative not in tracked:
        errors.append(f"{error_prefix}:path-not-tracked")
    absolute = root / relative
    dimensions = validated_png_dimensions(absolute)
    if dimensions is None:
        errors.append(f"{error_prefix}:png-missing-or-invalid")
    if isinstance(expected_sha, str) and SHA256_RE.fullmatch(expected_sha):
        try:
            if sha256_file(absolute) != expected_sha:
                errors.append(f"{error_prefix}:sha256-mismatch")
        except OSError:
            pass
    return (
        dimensions,
        errors,
    )


def validate_visual_evidence(
    path: Path,
    root: Path,
    head: str,
    tracked_worktree_clean: bool,
    tracked: set[str],
    require_tracked: bool = True,
) -> tuple[dict[str, object], dict[str, dict[str, object]]]:
    payload, read_errors = read_json_object(path)
    payload = payload or {}
    relative = evidence_path_label(path, root)
    if read_errors:
        return (
            {
                "path": relative,
                "evidenceRevision": None,
                "deviceModel": None,
                "graphicsApi": None,
                "candidateRows": 0,
                "acceptedForCurrentRevision": False,
                "validationErrors": sorted(set(read_errors)),
                "disposition": "missing/incomplete/rejected only",
            },
            {},
        )
    errors = list(read_errors)
    expected = (
        ("schema-invalid", payload.get("schema") == VISUAL_SCHEMA),
        ("task-id-not-APH-507", payload.get("taskId") == TASK_ID),
        ("status-not-complete", payload.get("status") == "complete"),
        ("dirty-provenance-not-false", payload.get("dirty") is False),
        ("build-target-not-Android", payload.get("buildTarget") == "Android"),
        (
            f"revision-mismatch:{payload.get('exactCommit')}->{head}",
            payload.get("exactCommit") == head,
        ),
        ("device-model-missing", isinstance(payload.get("deviceModel"), str) and bool(payload.get("deviceModel"))),
        ("graphics-api-missing", isinstance(payload.get("graphicsApi"), str) and bool(payload.get("graphicsApi"))),
        ("tracked-worktree-dirty", tracked_worktree_clean),
        ("evidence-path-not-tracked", not require_tracked or relative in tracked),
    )
    errors.extend(code for code, valid in expected if not valid)

    rows = payload.get("candidateResults")
    if not isinstance(rows, list):
        errors.append("candidate-results-not-array")
        rows = []
    elif not rows:
        errors.append("candidate-results-empty")
    ordered_paths: list[str] = []
    validated_rows: dict[str, dict[str, object]] = {}
    row_errors: list[str] = []
    for index, row in enumerate(rows):
        prefix = f"candidate:{index}"
        if not isinstance(row, dict):
            row_errors.append(f"{prefix}:not-object")
            continue
        asset_path = row.get("assetPath")
        if not valid_unity_asset_path(asset_path):
            row_errors.append(f"{prefix}:asset-path-invalid")
            continue
        ordered_paths.append(asset_path)
        if asset_path in validated_rows:
            row_errors.append(f"{prefix}:duplicate-asset-path")
            continue
        before_limit = row.get("beforeMaxTextureSize")
        after_limit = row.get("afterMaxTextureSize")
        before_format = row.get("beforeAstcFormat")
        after_format = row.get("afterAstcFormat")
        checks = row.get("rejectionChecks")
        if row.get("result") != "pass":
            row_errors.append(f"{prefix}:result-not-pass")
        if (
            not isinstance(before_limit, int)
            or isinstance(before_limit, bool)
            or before_limit < 4096
            or not isinstance(after_limit, int)
            or isinstance(after_limit, bool)
            or after_limit <= 0
            or after_limit > 2048
            or after_limit >= before_limit
        ):
            row_errors.append(f"{prefix}:limit-transition-invalid")
        if (
            astc_block(before_format) is None
            or before_format != after_format
        ):
            row_errors.append(f"{prefix}:astc-format-not-preserved")
        if (
            not isinstance(checks, dict)
            or set(checks) != set(VISUAL_REJECTION_CHECKS)
            or any(checks.get(name) is not False for name in VISUAL_REJECTION_CHECKS)
        ):
            row_errors.append(f"{prefix}:rejection-checks-not-clear")

        pairs = row.get("capturePairs")
        if not isinstance(pairs, list):
            row_errors.append(f"{prefix}:capture-pairs-not-array")
            pairs = []
        pair_views = [pair.get("view") if isinstance(pair, dict) else None for pair in pairs]
        if pair_views != list(VISUAL_VIEWS):
            row_errors.append(f"{prefix}:capture-views-invalid")
        for pair_index, pair in enumerate(pairs):
            pair_prefix = f"{prefix}:pair:{pair_index}"
            if not isinstance(pair, dict):
                row_errors.append(f"{pair_prefix}:not-object")
                continue
            camera_hash = pair.get("cameraStateSha256")
            if not isinstance(camera_hash, str) or not SHA256_RE.fullmatch(camera_hash):
                row_errors.append(f"{pair_prefix}:camera-state-sha256-invalid")
            before_dimensions, before_errors = _validate_visual_artifact(
                pair.get("before"), root, tracked, require_tracked, f"{pair_prefix}:before"
            )
            after_dimensions, after_errors = _validate_visual_artifact(
                pair.get("after"), root, tracked, require_tracked, f"{pair_prefix}:after"
            )
            row_errors.extend(before_errors)
            row_errors.extend(after_errors)
            before_artifact = pair.get("before")
            after_artifact = pair.get("after")
            if (
                isinstance(before_artifact, dict)
                and isinstance(after_artifact, dict)
                and before_artifact.get("path") == after_artifact.get("path")
            ):
                row_errors.append(f"{pair_prefix}:capture-paths-not-distinct")
            if (
                before_dimensions is not None
                and after_dimensions is not None
                and before_dimensions != after_dimensions
            ):
                row_errors.append(f"{pair_prefix}:capture-dimensions-mismatch")
        validated_rows[asset_path] = {
            "beforeMaxTextureSize": before_limit,
            "afterMaxTextureSize": after_limit,
            "astcFormat": before_format,
        }
    if ordered_paths != sorted(ordered_paths):
        row_errors.append("candidate-results-not-sorted")
    errors = sorted(set([*errors, *row_errors]))
    accepted = not errors
    details = {
        "path": relative,
        "evidenceRevision": payload.get("exactCommit"),
        "deviceModel": payload.get("deviceModel"),
        "graphicsApi": payload.get("graphicsApi"),
        "candidateRows": len(validated_rows),
        "acceptedForCurrentRevision": accepted,
        "validationErrors": errors,
        "disposition": (
            "accepted current hash-verified Android visual proof"
            if accepted
            else "missing/incomplete/rejected only"
        ),
    }
    return details, validated_rows if accepted else {}


def audit_importers(
    root: Path,
    meta_paths: Iterable[str],
    content_rows: dict[str, dict[str, object]],
    accepted_build_rows: dict[str, int],
    historical_builds: list[tuple[dict[str, object], dict[str, int]]],
    accepted_visual_rows: dict[str, dict[str, object]],
) -> tuple[list[dict[str, object]], dict[str, object]]:
    candidates: list[dict[str, object]] = []
    potential_unknown_dimensions: list[str] = []
    importer_errors: list[dict[str, object]] = []
    texture_importer_count = 0
    dimensions_read = 0
    oversized_configured_limit_count = 0

    for meta_path in sorted(meta_paths):
        absolute_meta = root / meta_path
        try:
            yaml_text = absolute_meta.read_text(encoding="utf-8")
        except OSError as error:
            importer_errors.append(
                {"metaPath": meta_path, "errors": [f"meta-unreadable:{type(error).__name__}"]}
            )
            continue
        if "\nTextureImporter:\n" not in yaml_text:
            continue
        texture_importer_count += 1
        asset_path = meta_path[:-5]
        settings = parse_texture_importer(yaml_text)
        if settings["validationErrors"]:
            importer_errors.append(
                {"metaPath": meta_path, "errors": settings["validationErrors"]}
            )
        dimensions = image_dimensions(root / asset_path)
        if dimensions is not None:
            dimensions_read += 1
        max_size = settings["androidMaxTextureSize"]
        has_oversized_limit = isinstance(max_size, int) and max_size >= 4096
        if has_oversized_limit:
            oversized_configured_limit_count += 1
        if dimensions is None:
            if has_oversized_limit:
                potential_unknown_dimensions.append(asset_path)
            continue
        source_width, source_height, source_format = dimensions
        if not has_oversized_limit or max(source_width, source_height) < 4096:
            continue

        observed = content_rows.get(asset_path, {})
        configured_format = settings["androidTextureFormat"]
        observed_format = observed.get("textureFormat")
        if astc_block(configured_format) is not None:
            astc_format = configured_format
            astc_evidence = "current configured importer"
        elif astc_block(observed_format) is not None:
            astc_format = observed_format
            astc_evidence = "historical content residency"
        else:
            astc_format = None
            astc_evidence = "not evidenced"

        limited_width, limited_height = dimensions_at_limit(
            source_width, source_height, max_size
        )
        reduced_width, reduced_height = dimensions_at_limit(
            source_width, source_height, min(2048, max_size)
        )
        current_astc_bytes = astc_payload_bytes(
            limited_width,
            limited_height,
            astc_format,
            bool(settings["mipmapsEnabled"]),
        )
        reduced_astc_bytes = astc_payload_bytes(
            reduced_width,
            reduced_height,
            astc_format,
            bool(settings["mipmapsEnabled"]),
        )

        historical_rows = []
        for report_details, top_rows in historical_builds:
            if asset_path in top_rows:
                historical_rows.append(
                    {
                        "path": report_details["path"],
                        "packageType": report_details["packageType"],
                        "evidenceRevision": report_details["evidenceRevision"],
                        "packedBytes": top_rows[asset_path],
                        "acceptedForCurrentRevision": report_details[
                            "acceptedForCurrentRevision"
                        ],
                    }
                )

        blockers: list[str] = []
        if settings["validationErrors"]:
            blockers.append("importer-settings-invalid")
        if asset_path not in accepted_build_rows:
            blockers.append("no-current-complete-BuildReport-inclusion")
        visual = accepted_visual_rows.get(asset_path)
        if visual is None:
            blockers.append("no-current-hash-verified-visual-proof")
        elif visual.get("beforeMaxTextureSize") != max_size:
            blockers.append("visual-before-limit-does-not-match-importer")
        elif (
            astc_block(configured_format) is not None
            and astc_block(visual.get("astcFormat")) != astc_block(configured_format)
        ):
            blockers.append("visual-ASTC-format-does-not-match-importer")
        authorized = not blockers

        role = texture_role(asset_path, settings["textureType"])
        candidates.append(
            {
                "assetPath": asset_path,
                "metaPath": meta_path,
                "metaSha256": sha256_file(absolute_meta),
                "sourceFormat": source_format,
                "sourceWidth": source_width,
                "sourceHeight": source_height,
                "role": role,
                "oversizedLimitClass": "8K" if max_size >= 8192 else "4K",
                **{key: value for key, value in settings.items() if key != "textureType"},
                "astcFormatForEstimate": astc_format,
                "astcFormatEvidence": astc_evidence,
                "astcQualityTier": astc_quality_tier(astc_format),
                "estimatedAstcPayloadBytesAtCurrentLimit": current_astc_bytes,
                "estimatedAstcPayloadBytesAt2048": reduced_astc_bytes,
                "estimatedAstcPayloadSavingsBytes": (
                    current_astc_bytes - reduced_astc_bytes
                    if current_astc_bytes is not None and reduced_astc_bytes is not None
                    else None
                ),
                "historicalResidency": {
                    "width": observed.get("textureWidth"),
                    "height": observed.get("textureHeight"),
                    "format": observed_format,
                    "importedSizeBytes": observed.get("importedSizeBytes"),
                } if observed else None,
                "historicalBuildReportInclusions": historical_rows,
                "acceptedCurrentBuildReportInclusion": asset_path in accepted_build_rows,
                "acceptedCurrentVisualProof": visual is not None,
                "proposedMaxTextureSize": (
                    visual.get("afterMaxTextureSize") if visual is not None else None
                ),
                "limitReductionAuthorized": authorized,
                "authorizationBlockers": sorted(blockers),
                "qualityRisk": (
                    "very-high"
                    if role == "world normal/mask" or astc_quality_tier(astc_format) == "very-high"
                    else "high"
                ),
            }
        )

    candidates.sort(key=lambda row: row["assetPath"])
    summary = {
        "trackedTextureImporterCount": texture_importer_count,
        "sourceDimensionsReadCount": dimensions_read,
        "oversizedConfiguredLimitImporterCount": oversized_configured_limit_count,
        "unknownSourceDimensionsWithOversizedLimitCount": len(potential_unknown_dimensions),
        "unknownSourceDimensionsWithOversizedLimit": sorted(potential_unknown_dimensions),
        "importerValidationErrorCount": len(importer_errors),
        "importerValidationErrors": importer_errors,
    }
    return candidates, summary


def inventory(
    root: Path = ROOT,
    *,
    head: str | None = None,
    tracked_worktree_changes: list[str] | None = None,
    all_tracked_paths: list[str] | None = None,
    meta_paths: Iterable[str] | None = None,
    content_residency_path: Path | None = None,
    build_report_paths: Iterable[Path] | None = None,
    visual_evidence_path: Path | None = None,
    require_tracked_evidence: bool = True,
) -> dict[str, object]:
    if head is None or tracked_worktree_changes is None:
        actual_head, actual_changes = current_revision(root)
        head = head or actual_head
        tracked_worktree_changes = (
            actual_changes if tracked_worktree_changes is None else tracked_worktree_changes
        )
    if all_tracked_paths is None:
        all_tracked_paths = tracked_paths(root)
    tracked = set(all_tracked_paths)
    if meta_paths is None:
        meta_paths = (
            path
            for path in all_tracked_paths
            if path.endswith(".meta") and (root / path).is_file()
        )
    clean = not tracked_worktree_changes

    content_path = content_residency_path or root / CONTENT_RESIDENCY_PATH
    content_rows, content_details = load_content_residency(content_path, root)

    accepted_build_rows: dict[str, int] = {}
    build_details: list[dict[str, object]] = []
    historical_builds: list[tuple[dict[str, object], dict[str, int]]] = []
    for report_path in build_report_paths or (root / path for path in BUILD_REPORT_PATHS):
        details, current_rows, historical_rows = validate_build_report(
            report_path,
            root,
            head,
            clean,
            tracked,
            require_tracked=require_tracked_evidence,
        )
        build_details.append(details)
        historical_builds.append((details, historical_rows))
        accepted_build_rows.update(current_rows)

    visual_path = visual_evidence_path or root / VISUAL_EVIDENCE_PATH
    visual_details, accepted_visual_rows = validate_visual_evidence(
        visual_path,
        root,
        head,
        clean,
        tracked,
        require_tracked=require_tracked_evidence,
    )
    candidates, importer_summary = audit_importers(
        root,
        meta_paths,
        content_rows,
        accepted_build_rows,
        historical_builds,
        accepted_visual_rows,
    )

    authorized = [row for row in candidates if row["limitReductionAuthorized"]]
    current_included = [
        row for row in candidates if row["acceptedCurrentBuildReportInclusion"]
    ]
    historical_included = [
        row for row in candidates if row["historicalBuildReportInclusions"]
    ]
    global_blockers: list[str] = []
    if not clean:
        global_blockers.append("tracked-worktree-dirty")
    if not any(report["acceptedForCurrentRevision"] for report in build_details):
        global_blockers.append("no-current-complete-Android-BuildReport")
    if not visual_details["acceptedForCurrentRevision"]:
        global_blockers.append("no-current-hash-verified-Android-visual-proof")
    if importer_summary["unknownSourceDimensionsWithOversizedLimitCount"]:
        global_blockers.append("oversized-limit-importers-with-unreadable-source-dimensions")

    class_counts = {
        limit_class: sum(
            row["oversizedLimitClass"] == limit_class for row in candidates
        )
        for limit_class in ("4K", "8K")
    }
    historical_aab_bytes = sum(
        inclusion["packedBytes"]
        for row in candidates
        for inclusion in row["historicalBuildReportInclusions"]
        if inclusion["packageType"] == "AAB"
    )
    return {
        "schema": SCHEMA,
        "taskId": TASK_ID,
        "status": "complete",
        "decision": (
            "ALLOW_ONLY_LISTED_BOUNDED_REDUCTIONS"
            if authorized
            else "BLOCK_ALL_LIMIT_REDUCTIONS"
        ),
        "limitReductionAuthorized": bool(authorized),
        "acceptanceContract": (
            "A candidate is authorized only when its current importer is valid, a clean same-revision "
            "complete Android BuildReport proves inclusion, and same-revision hash-verified Android "
            "near/medium/far/combat before-and-after visual proof preserves ASTC format and clears all "
            "quality rejection checks."
        ),
        "analyzedRevision": head,
        "trackedWorktreeClean": clean,
        "unityRun": False,
        "readOnlyAudit": True,
        "summary": {
            **importer_summary,
            "oversizedCandidateCount": len(candidates),
            "oversizedCandidateCountsByLimit": class_counts,
            "androidOverrideEnabledCandidateCount": sum(
                row["androidOverrideEnabled"] for row in candidates
            ),
            "historicalBuildReportIncludedCandidateCount": len(historical_included),
            "historicalAabPackedBytes": historical_aab_bytes,
            "acceptedCurrentBuildReportIncludedCandidateCount": len(current_included),
            "acceptedCurrentVisualProofCandidateCount": sum(
                row["acceptedCurrentVisualProof"] for row in candidates
            ),
            "authorizedLimitReductionCandidateCount": len(authorized),
        },
        "evidence": {
            "contentResidency": content_details,
            "buildReports": build_details,
            "visualProof": visual_details,
        },
        "globalAcceptanceBlockers": sorted(set(global_blockers)),
        "candidates": candidates,
    }


def render_json(data: dict[str, object]) -> str:
    return json.dumps(data, indent=2, sort_keys=False) + "\n"


def _bool(value: object) -> str:
    return str(bool(value)).lower()


def _mib(value: object) -> str:
    return "n/a" if not isinstance(value, int) else f"{value / (1024 * 1024):.2f} MiB"


def _markdown_cell(value: object) -> str:
    return str(value).replace("|", "\\|")


def render_markdown(data: dict[str, object]) -> str:
    summary = data["summary"]
    evidence = data["evidence"]
    lines = [
        "# APH-507 Android Texture Override Audit",
        "",
        f"- Task: `{data['taskId']}`",
        f"- Audit status: `{data['status']}`",
        f"- Decision: `{data['decision']}`",
        f"- Limit reduction authorized: `{_bool(data['limitReductionAuthorized'])}`",
        f"- Analyzed revision: `{data['analyzedRevision']}`",
        f"- Tracked worktree clean: `{_bool(data['trackedWorktreeClean'])}`",
        "- Importers/assets changed: none",
        "- Unity run: none",
        "",
        "## Decision",
        "",
        "No Android texture limit may be changed based on this report unless its candidate row says "
        "`limitReductionAuthorized=true`. The current audit blocks all limit reductions because the "
        "required current BuildReport and visual-proof gates are not both accepted.",
        "",
        data["acceptanceContract"],
        "",
        "## Current Static Audit",
        "",
        f"The deterministic scan found **{summary['oversizedCandidateCount']:,}** tracked source textures "
        "whose current effective Android max-size setting and source dimensions are at least 4096. "
        f"That includes **{summary['oversizedCandidateCountsByLimit']['4K']:,}** 4K-limit and "
        f"**{summary['oversizedCandidateCountsByLimit']['8K']:,}** 8K-limit candidates. "
        f"Explicit Android overrides are enabled on **{summary['androidOverrideEnabledCandidateCount']:,}** "
        "of those candidates.",
        "",
        f"Historical top-100 BuildReports positively include **{summary['historicalBuildReportIncludedCandidateCount']:,}** "
        f"candidates and attribute **{_mib(summary['historicalAabPackedBytes'])}** in the AAB report. "
        "That evidence is context only; it is not current authorization.",
        "",
        "| Asset | Source | Android limit/source | ASTC evidence | Quality | Est. payload | Est. 2K saving | Current build | Visual | Authorized |",
        "|---|---|---|---|---|---:|---:|---|---|---|",
    ]
    for row in data["candidates"]:
        source = f"{row['sourceWidth']}x{row['sourceHeight']} {row['sourceFormat']}"
        limit = f"{row['androidMaxTextureSize']} ({row['effectiveSettingsSource']})"
        astc = f"{row['astcFormatForEstimate'] or 'unknown'}; {row['astcFormatEvidence']}"
        lines.append(
            "| `{}` | {} | {} | {} | {} | {} | {} | `{}` | `{}` | `{}` |".format(
                _markdown_cell(row["assetPath"]),
                source,
                limit,
                astc,
                f"{row['astcQualityTier']}; {row['androidTextureCompression']}/{row['androidCompressionQualityLabel']}",
                _mib(row["estimatedAstcPayloadBytesAtCurrentLimit"]),
                _mib(row["estimatedAstcPayloadSavingsBytes"]),
                _bool(row["acceptedCurrentBuildReportInclusion"]),
                _bool(row["acceptedCurrentVisualProof"]),
                _bool(row["limitReductionAuthorized"]),
            )
        )

    lines.extend(
        [
            "",
            "ASTC payload estimates are deterministic 16-byte block calculations over the source dimensions "
            "clamped to the importer limit, including the full mip chain when enabled. They exclude container, "
            "alignment, and BuildReport overhead and are not substituted for measured build bytes.",
            "",
            "## Evidence Gates",
            "",
            "### Android BuildReports",
            "",
            "| Path | Package | Revision | Complete texture rows | Accepted |",
            "|---|---|---|---:|---|",
        ]
    )
    for report in evidence["buildReports"]:
        lines.append(
            f"| `{report['path']}` | `{report['packageType']}` | `{report['evidenceRevision']}` | "
            f"{report['completeTextureRows']:,} | `{_bool(report['acceptedForCurrentRevision'])}` |"
        )
        if report["validationErrors"]:
            lines.append(
                f"| Blockers |  |  |  | `{', '.join(report['validationErrors'])}` |"
            )
    visual = evidence["visualProof"]
    lines.extend(
        [
            "",
            "### Android Visual Proof",
            "",
            f"- Path: `{visual['path']}`",
            f"- Evidence revision: `{visual['evidenceRevision']}`",
            f"- Device / graphics API: `{visual['deviceModel']}` / `{visual['graphicsApi']}`",
            f"- Candidate rows: `{visual['candidateRows']}`",
            f"- Accepted: `{_bool(visual['acceptedForCurrentRevision'])}`",
            f"- Validation errors: `{', '.join(visual['validationErrors']) or 'none'}`",
            "",
            "The visual contract requires hash-verified PNG pairs at identical recorded camera state for near, "
            "medium, far, and combat views. It rejects atlas blur, color bleeding, mip pop, detail loss, or UI "
            "contamination and requires the same ASTC format before and after the limit change.",
            "",
            "## Fail-Closed Blockers",
            "",
        ]
    )
    blockers = data["globalAcceptanceBlockers"]
    lines.extend(f"- `{blocker}`" for blocker in blockers)
    if not blockers:
        lines.append("- None")
    lines.extend(
        [
            "",
            f"Importers with an oversized configured limit but unreadable static source dimensions: "
            f"`{summary['unknownSourceDimensionsWithOversizedLimitCount']}`. These are retained as a blind spot "
            "and cannot be treated as safe exclusions.",
            "",
            "## Reproduction",
            "",
            "```sh",
            "PYTHONPYCACHEPREFIX=/tmp/aph507-pyc python3 -m unittest \\",
            "  Tools.CI.tests.test_aph507_android_texture_override_audit -v",
            "PYTHONPYCACHEPREFIX=/tmp/aph507-pyc python3 \\",
            "  Tools/CI/aph507_android_texture_override_audit.py --write",
            "PYTHONPYCACHEPREFIX=/tmp/aph507-pyc python3 \\",
            "  Tools/CI/aph507_android_texture_override_audit.py --check",
            "```",
            "",
        ]
    )
    return "\n".join(lines)


def write_reports(root: Path, data: dict[str, object]) -> None:
    outputs = {
        JSON_REPORT_PATH: render_json(data),
        MARKDOWN_REPORT_PATH: render_markdown(data),
    }
    for relative, content in outputs.items():
        path = root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("w", encoding="utf-8", newline="\n") as handle:
            handle.write(content)


def report_check_errors(root: Path, data: dict[str, object]) -> list[str]:
    expected = {
        JSON_REPORT_PATH: render_json(data),
        MARKDOWN_REPORT_PATH: render_markdown(data),
    }
    errors: list[str] = []
    for relative, content in expected.items():
        path = root / relative
        try:
            actual = path.read_text(encoding="utf-8")
        except FileNotFoundError:
            errors.append(f"generated-report-missing:{relative.as_posix()}")
            continue
        if actual != content:
            errors.append(f"generated-report-stale:{relative.as_posix()}")
    return errors


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    action = parser.add_mutually_exclusive_group()
    action.add_argument("--write", action="store_true", help="Write deterministic reports.")
    action.add_argument("--check", action="store_true", help="Fail if reports are missing or stale.")
    parser.add_argument(
        "--require-authorization",
        action="store_true",
        help="Fail unless at least one candidate passes every reduction gate.",
    )
    args = parser.parse_args(argv)
    data = inventory()
    if args.write:
        write_reports(ROOT, data)
    elif args.check:
        errors = report_check_errors(ROOT, data)
        if errors:
            print("\n".join(errors), file=sys.stderr)
            return 1
    else:
        print(render_json(data), end="")
    if args.require_authorization and not data["limitReductionAuthorized"]:
        print("APH-507 limit reduction is not authorized", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
