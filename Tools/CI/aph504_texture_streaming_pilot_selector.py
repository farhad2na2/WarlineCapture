#!/usr/bin/env python3
"""Deterministic APH-504 texture-streaming pilot evidence selector.

Collection is read-only. The optional --write action writes only the two tracked
APH-504 evidence reports owned by this slice.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import struct
import subprocess
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[2]
TASK_ID = "APH-504"
REPORT_DATE = "2026-07-15"
PILOT_LIMIT = 2
EXPECTED_MOBILE_BUDGET_MIB = 256
AAB_REPORT_PATH = Path("Design/AgentReports/architecture_performance_android_aab_build_report.json")
CONTENT_RESIDENCY_PATH = Path("Design/AgentReports/architecture_performance_content_residency_baseline.json")
QUALITY_SETTINGS_PATH = Path("ProjectSettings/QualitySettings.asset")
PACKAGE_MANIFEST_PATH = Path("Packages/manifest.json")
PACKAGE_LOCK_PATH = Path("Packages/packages-lock.json")
APH505_EVIDENCE_PATH = Path(
    "Design/AgentReports/architecture_performance_texture_streaming_visual_evidence.json"
)
APH506_EVIDENCE_PATH = Path(
    "Design/AgentReports/architecture_performance_texture_streaming_performance_evidence.json"
)
JSON_REPORT_PATH = Path("Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.json")
MARKDOWN_REPORT_PATH = Path("Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.md")

COMMIT_RE = re.compile(r"^[0-9a-f]{40}$")
FAMILY_RE = re.compile(r"_Texture_(\d{2})_")
INTEGER_RE = re.compile(r"^-?\d+$")
PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


class ValidationError(ValueError):
    def __init__(self, code: str, detail: str) -> None:
        super().__init__(detail)
        self.code = code


class DuplicateJsonKeyError(ValueError):
    pass


@dataclass(frozen=True)
class TextureMeta:
    serialized_version: int
    enable_mip_map: int
    is_readable: int
    streaming_mipmaps: int | None
    ignore_mipmap_limit: int | None
    sprite_mode: int
    texture_type: int
    texture_shape: int
    default_max_texture_size: int
    default_overridden: int


@dataclass(frozen=True)
class BuildCandidate:
    packed_bytes: int | None
    errors: tuple[str, ...]


@dataclass(frozen=True)
class BuildEvidence:
    exact_commit: str | None
    dirty: bool | None
    reported_count: int | None
    total_count: int | None
    candidates: dict[str, BuildCandidate]
    errors: tuple[str, ...]

    @property
    def export_complete(self) -> bool:
        return (
            self.reported_count is not None
            and self.total_count is not None
            and self.reported_count == self.total_count
        )


@dataclass(frozen=True)
class MobileQuality:
    global_texture_mipmap_limit: int
    streaming_active: int
    add_all_cameras: int
    memory_budget_mib: int
    max_level_reduction: int
    max_file_io_requests: int


@dataclass(frozen=True)
class RepositoryInventory:
    importer_meta_paths: tuple[str, ...]
    importer_hash: str
    manifest_package_count: int
    locked_package_count: int
    package_hash: str
    errors: tuple[str, ...]


@dataclass(frozen=True)
class EvidenceGate:
    accepted: bool
    errors: tuple[str, ...]


def _run_git(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        ["git", *args],
        cwd=root,
        check=check,
        capture_output=True,
    )


def _git_text(root: Path, *args: str) -> str:
    return _run_git(root, *args).stdout.decode("utf-8").strip()


def _git_blob_id(root: Path, revision: str, relative_path: str) -> str | None:
    result = _run_git(root, "rev-parse", f"{revision}:{relative_path}", check=False)
    if result.returncode != 0:
        return None
    value = result.stdout.decode("ascii").strip()
    return value if re.fullmatch(r"[0-9a-f]{40,64}", value) else None


def _tracked_changes(root: Path, paths: Iterable[str] | None = None) -> list[str]:
    args = ["status", "--porcelain=v1", "-z", "--untracked-files=no"]
    if paths is not None:
        args.extend(["--", *paths])
    result = _run_git(root, *args)
    return sorted(entry for entry in result.stdout.decode("utf-8").split("\0") if entry)


def _read_optional(path: Path) -> bytes | None:
    try:
        return path.read_bytes()
    except OSError:
        return None


def _sha256(value: bytes | None) -> str | None:
    return hashlib.sha256(value).hexdigest() if value is not None else None


def _tracked_texture_importer_meta_paths(root: Path) -> tuple[str, ...]:
    result = _run_git(root, "ls-files", "-z", "--", "*.meta")
    paths = result.stdout.decode("utf-8").split("\0")
    return tuple(
        sorted(
            path
            for path in paths
            if path
            and (root / path).is_file()
            and b"\nTextureImporter:\n" in (root / path).read_bytes()
        )
    )


def _path_content_hash(root: Path, paths: Iterable[str]) -> str:
    digest = hashlib.sha256()
    for relative_path in sorted(paths):
        content = _read_optional(root / relative_path)
        digest.update(relative_path.encode("utf-8"))
        digest.update(b"\0")
        digest.update(hashlib.sha256(content).digest() if content is not None else b"missing")
        digest.update(b"\0")
    return digest.hexdigest()


def _one_integer(text: str, indentation: int, key: str, required: bool = True) -> int | None:
    prefix = " " * indentation
    values = re.findall(rf"^{re.escape(prefix + key)}:\s*(.*?)\s*$", text, re.MULTILINE)
    if not values and not required:
        return None
    if len(values) != 1:
        raise ValidationError(
            f"{key}-count-invalid",
            f"{key} must occur exactly once at indentation {indentation}; found {len(values)}",
        )
    if not INTEGER_RE.fullmatch(values[0]):
        raise ValidationError(f"{key}-not-integer", f"{key} is not an integer: {values[0]!r}")
    return int(values[0])


def _default_platform_settings(text: str) -> tuple[int, int]:
    lines = text.splitlines()
    headers = [index for index, line in enumerate(lines) if line == "  platformSettings:"]
    if len(headers) != 1:
        raise ValidationError(
            "platform-settings-count-invalid",
            f"platformSettings must occur exactly once; found {len(headers)}",
        )

    items: list[dict[str, list[str]]] = []
    current: dict[str, list[str]] | None = None
    for line in lines[headers[0] + 1 :]:
        if line.startswith("  - "):
            current = {}
            items.append(current)
            key, separator, value = line[4:].partition(":")
            if not separator:
                raise ValidationError("platform-item-malformed", f"malformed platform item: {line!r}")
            current.setdefault(key, []).append(value.strip())
            continue
        if line.startswith("    ") and current is not None:
            key, separator, value = line[4:].partition(":")
            if not separator:
                raise ValidationError("platform-field-malformed", f"malformed platform field: {line!r}")
            current.setdefault(key, []).append(value.strip())
            continue
        if line.startswith("  ") and line.strip():
            break
        if line.strip():
            raise ValidationError("platform-block-malformed", f"unexpected platform line: {line!r}")

    defaults = [item for item in items if item.get("buildTarget") == ["DefaultTexturePlatform"]]
    if len(defaults) != 1:
        raise ValidationError(
            "default-platform-count-invalid",
            f"DefaultTexturePlatform must occur exactly once; found {len(defaults)}",
        )
    values: list[int] = []
    for key in ("maxTextureSize", "overridden"):
        raw = defaults[0].get(key, [])
        if len(raw) != 1 or not INTEGER_RE.fullmatch(raw[0]):
            raise ValidationError(
                f"default-{key}-invalid",
                f"DefaultTexturePlatform {key} must be one integer; found {raw!r}",
            )
        values.append(int(raw[0]))
    return values[0], values[1]


def parse_texture_meta(text: str) -> TextureMeta:
    if text.splitlines().count("TextureImporter:") != 1:
        raise ValidationError("texture-importer-count-invalid", "TextureImporter must occur exactly once")
    default_max, default_overridden = _default_platform_settings(text)
    return TextureMeta(
        serialized_version=int(_one_integer(text, 2, "serializedVersion")),
        enable_mip_map=int(_one_integer(text, 4, "enableMipMap")),
        is_readable=int(_one_integer(text, 2, "isReadable")),
        streaming_mipmaps=_one_integer(text, 2, "streamingMipmaps", required=False),
        ignore_mipmap_limit=_one_integer(text, 2, "ignoreMipmapLimit", required=False),
        sprite_mode=int(_one_integer(text, 2, "spriteMode")),
        texture_type=int(_one_integer(text, 2, "textureType")),
        texture_shape=int(_one_integer(text, 2, "textureShape")),
        default_max_texture_size=default_max,
        default_overridden=default_overridden,
    )


def classify_world_texture(asset_path: str, meta: TextureMeta) -> tuple[str | None, tuple[str, ...]]:
    lower = asset_path.lower()
    tokens = set(filter(None, re.split(r"[^a-z0-9]+", Path(lower).stem)))
    exclusions = []
    if any(
        marker in lower
        for marker in ("/ui/", "/gui/", "/interface", "/fonts/", "/generated/", "/effects/", "/vfx/", "/atlases/")
    ):
        exclusions.append("protected-path-class")
    if meta.sprite_mode != 0:
        exclusions.append("sprite-importer")
    if meta.texture_shape != 1:
        exclusions.append(f"texture-shape-not-2d:{meta.texture_shape}")
    if exclusions:
        return None, tuple(exclusions)
    if meta.texture_type == 1 or tokens.intersection(
        {"normal", "normals", "mask", "masks", "metallic", "roughness", "occlusion", "specular"}
    ):
        return "world normal/mask", ()
    if meta.texture_type == 0:
        return "world albedo", ()
    return None, (f"texture-type-not-world:{meta.texture_type}",)


def classify_inventory_texture(asset_path: str, yaml_text: str) -> str:
    lower = asset_path.lower()
    tokens = set(filter(None, re.split(r"[^a-z0-9]+", Path(lower).stem)))

    def scalar(key: str) -> int | None:
        match = re.search(rf"^  {re.escape(key)}:\s*(-?\d+)\s*$", yaml_text, re.MULTILINE)
        return int(match.group(1)) if match else None

    generated_reference = "/generated/" in lower and bool(
        re.search(r"/(?:references?|sources?)/", lower)
        or re.search(r"(?:^|[_-])(?:reference|source)(?:[_-]|\.)", Path(lower).name)
    )
    if generated_reference:
        return "generated source/reference"
    if "impostor" in lower or "atlas" in tokens or "/atlases/" in lower:
        return "impostor/atlas"
    if (
        "/effects/" in lower
        or "/fx/" in lower
        or "/vfx/" in lower
        or tokens.intersection({"vfx", "particle", "particles", "muzzleflash", "smoke", "glow"})
    ):
        return "VFX"
    if scalar("spriteMode") not in (None, 0) or scalar("textureType") == 8 or any(
        marker in lower for marker in ("/ui/", "/gui/", "/interface", "/fonts/")
    ):
        return "UI"
    if scalar("textureType") == 1 or tokens.intersection(
        {"normal", "normals", "mask", "masks", "metallic", "roughness", "occlusion", "specular"}
    ):
        return "world normal/mask"
    return "world albedo"


def parse_png_dimensions(header: bytes) -> tuple[int, int]:
    if len(header) < 24 or header[:8] != PNG_SIGNATURE or header[12:16] != b"IHDR":
        raise ValidationError("png-header-invalid", "source is not a readable PNG IHDR header")
    width, height = struct.unpack(">II", header[16:24])
    if width <= 0 or height <= 0:
        raise ValidationError("png-dimensions-invalid", f"invalid PNG dimensions {width}x{height}")
    return width, height


def _strict_json_object(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise DuplicateJsonKeyError(f"duplicate JSON key: {key}")
        result[key] = value
    return result


def _parse_json_object(data: bytes | None, unavailable_code: str) -> tuple[dict[str, object] | None, list[str]]:
    if data is None:
        return None, [unavailable_code]
    try:
        payload = json.loads(data.decode("utf-8"), object_pairs_hook=_strict_json_object)
    except (UnicodeError, json.JSONDecodeError, DuplicateJsonKeyError) as error:
        return None, [f"malformed:{type(error).__name__}:{error}"]
    if not isinstance(payload, dict):
        return None, ["not-object"]
    return payload, []


def collect_repository_inventory(root: Path) -> RepositoryInventory:
    importer_meta_paths = _tracked_texture_importer_meta_paths(root)
    manifest_bytes = _read_optional(root / PACKAGE_MANIFEST_PATH)
    lock_bytes = _read_optional(root / PACKAGE_LOCK_PATH)
    errors: list[str] = []
    manifest_count = 0
    locked_count = 0

    manifest, manifest_errors = _parse_json_object(manifest_bytes, "package-manifest-unavailable")
    lock, lock_errors = _parse_json_object(lock_bytes, "package-lock-unavailable")
    errors.extend(f"package-manifest-{error}" for error in manifest_errors)
    errors.extend(f"package-lock-{error}" for error in lock_errors)
    if manifest is not None:
        dependencies = manifest.get("dependencies")
        if not isinstance(dependencies, dict) or not all(
            isinstance(key, str) and isinstance(value, str) for key, value in dependencies.items()
        ):
            errors.append("package-manifest-dependencies-invalid")
        else:
            manifest_count = len(dependencies)
    if lock is not None:
        dependencies = lock.get("dependencies")
        if not isinstance(dependencies, dict) or not all(isinstance(key, str) for key in dependencies):
            errors.append("package-lock-dependencies-invalid")
        else:
            locked_count = len(dependencies)

    return RepositoryInventory(
        importer_meta_paths=importer_meta_paths,
        importer_hash=_path_content_hash(root, importer_meta_paths),
        manifest_package_count=manifest_count,
        locked_package_count=locked_count,
        package_hash=_path_content_hash(root, (PACKAGE_MANIFEST_PATH.as_posix(), PACKAGE_LOCK_PATH.as_posix())),
        errors=tuple(sorted(set(errors))),
    )


def parse_current_build_gate(
    data: bytes | None,
    head: str,
    required_paths: Iterable[str] = (),
) -> EvidenceGate:
    payload, errors = _parse_json_object(data, "aab-report-unavailable")
    if payload is None:
        return EvidenceGate(False, tuple(f"current-build-{error}" for error in errors))
    checks = (
        (payload.get("schemaVersion") == 1, "schema-version-not-1"),
        (payload.get("taskId") == "APH-500", "task-id-not-APH-500"),
        (payload.get("status") == "complete", "status-not-complete"),
        (payload.get("exactCommit") == head, f"revision-mismatch:{payload.get('exactCommit')}->{head}"),
        (payload.get("dirty") is False, "dirty-provenance-not-false"),
        (payload.get("releaseBuildType") == "release", "release-build-type-invalid"),
        (payload.get("buildTarget") == "Android", "build-target-not-Android"),
        (payload.get("detailedBuildReport") is True, "detailed-build-report-not-true"),
        (payload.get("allIncludedTexturePathsExported") is True, "complete-texture-export-marker-not-true"),
    )
    errors.extend(code for passed, code in checks if not passed)
    rows = payload.get("buildReportIncludedTextures")
    paths: list[str] = []
    if not isinstance(rows, list):
        errors.append("complete-texture-export-not-array")
    else:
        for row in rows:
            if not isinstance(row, dict):
                errors.append("complete-texture-row-not-object")
                continue
            path = row.get("sourceAssetPath")
            object_types = row.get("objectTypes")
            if not isinstance(path, str) or not path or path != path.strip():
                errors.append("complete-texture-row-path-invalid")
                continue
            if not isinstance(object_types, list) or "UnityEngine.Texture2D" not in object_types:
                errors.append(f"complete-texture-row-type-invalid:{path}")
                continue
            paths.append(path)
        if paths != sorted(paths) or len(paths) != len(set(paths)):
            errors.append("complete-texture-export-order-or-uniqueness-invalid")
    for path in sorted(set(required_paths) - set(paths)):
        errors.append(f"selected-texture-absent-from-complete-build-export:{path}")
    return EvidenceGate(not errors, tuple(sorted(set(errors))))


def parse_current_residency_gate(
    data: bytes | None,
    head: str,
    required_paths: Iterable[str] = (),
) -> EvidenceGate:
    payload, errors = _parse_json_object(data, "content-residency-unavailable")
    if payload is None:
        return EvidenceGate(False, tuple(f"current-residency-{error}" for error in errors))
    checks = (
        (payload.get("status") == "complete", "status-not-complete"),
        (payload.get("baselineCommit") == head, f"revision-mismatch:{payload.get('baselineCommit')}->{head}"),
    )
    errors.extend(code for passed, code in checks if not passed)
    assets = payload.get("assets")
    texture_paths: set[str] = set()
    if not isinstance(assets, list):
        errors.append("assets-not-array")
    else:
        texture_paths = {
            row["assetPath"]
            for row in assets
            if isinstance(row, dict)
            and row.get("assetType") == "Texture2D"
            and isinstance(row.get("assetPath"), str)
        }
        if not texture_paths:
            errors.append("texture-rows-absent")
    for path in sorted(set(required_paths) - texture_paths):
        errors.append(f"selected-texture-absent-from-residency:{path}")
    return EvidenceGate(not errors, tuple(sorted(set(errors))))


def parse_visual_evidence_gate(
    data: bytes | None,
    head: str,
    candidate_paths: list[str],
) -> EvidenceGate:
    payload, errors = _parse_json_object(data, "evidence-unavailable")
    if payload is None:
        return EvidenceGate(False, tuple(f"aph505-{error}" for error in errors))
    checks = (
        (payload.get("schemaVersion") == 1, "schema-version-not-1"),
        (payload.get("taskId") == "APH-505", "task-id-not-APH-505"),
        (payload.get("status") == "complete", "status-not-complete"),
        (payload.get("exactCommit") == head, f"revision-mismatch:{payload.get('exactCommit')}->{head}"),
        (payload.get("dirty") is False, "dirty-provenance-not-false"),
        (payload.get("candidatePaths") == candidate_paths, "candidate-paths-mismatch"),
        (payload.get("capturedViews") == ["near", "medium", "far"], "captured-views-incomplete"),
        (payload.get("beforeAfterPairsComplete") is True, "before-after-pairs-incomplete"),
        (payload.get("accepted") is True, "accepted-not-true"),
    )
    errors.extend(code for passed, code in checks if not passed)
    regressions = payload.get("visualRegressions")
    expected_regressions = ("blur", "latePop", "terrainSeams", "missingVegetationDetail")
    if not isinstance(regressions, dict):
        errors.append("visual-regressions-not-object")
    else:
        for key in expected_regressions:
            if regressions.get(key) is not False:
                errors.append(f"visual-regression-not-rejected:{key}")
    return EvidenceGate(not errors, tuple(sorted(set(f"aph505-{error}" for error in errors))))


def parse_performance_evidence_gate(
    data: bytes | None,
    head: str,
    candidate_paths: list[str],
) -> EvidenceGate:
    payload, errors = _parse_json_object(data, "evidence-unavailable")
    if payload is None:
        return EvidenceGate(False, tuple(f"aph506-{error}" for error in errors))
    duration = payload.get("durationSeconds")
    checks = (
        (payload.get("schemaVersion") == 1, "schema-version-not-1"),
        (payload.get("taskId") == "APH-506", "task-id-not-APH-506"),
        (payload.get("status") == "complete", "status-not-complete"),
        (payload.get("exactCommit") == head, f"revision-mismatch:{payload.get('exactCommit')}->{head}"),
        (payload.get("dirty") is False, "dirty-provenance-not-false"),
        (payload.get("candidatePaths") == candidate_paths, "candidate-paths-mismatch"),
        (_is_int(duration) and duration >= 600, "duration-below-600-seconds"),
        (payload.get("memoryMeasured") is True, "memory-not-measured"),
        (payload.get("ioMeasured") is True, "io-not-measured"),
        (payload.get("memoryRegressionAccepted") is True, "memory-regression-not-accepted"),
        (payload.get("ioRegressionAccepted") is True, "io-regression-not-accepted"),
        (payload.get("accepted") is True, "accepted-not-true"),
    )
    errors.extend(code for passed, code in checks if not passed)
    return EvidenceGate(not errors, tuple(sorted(set(f"aph506-{error}" for error in errors))))


def _is_int(value: object) -> bool:
    return isinstance(value, int) and not isinstance(value, bool)


def parse_build_evidence(data: bytes | None, expected_paths: Iterable[str]) -> BuildEvidence:
    expected = tuple(expected_paths)
    unavailable = {path: BuildCandidate(None, ("aab-report-unavailable",)) for path in expected}
    if data is None:
        return BuildEvidence(None, None, None, None, unavailable, ("aab-report-unavailable",))
    try:
        payload = json.loads(data.decode("utf-8"), object_pairs_hook=_strict_json_object)
    except (UnicodeError, json.JSONDecodeError, DuplicateJsonKeyError) as error:
        code = f"aab-report-malformed:{type(error).__name__}:{error}"
        return BuildEvidence(None, None, None, None, unavailable, (code,))
    if not isinstance(payload, dict):
        return BuildEvidence(None, None, None, None, unavailable, ("aab-report-not-object",))

    errors: list[str] = []
    exact_commit = payload.get("exactCommit")
    if not isinstance(exact_commit, str) or not COMMIT_RE.fullmatch(exact_commit):
        errors.append("aab-exact-commit-invalid")
        exact_commit = None
    dirty = payload.get("dirty")
    if not isinstance(dirty, bool):
        errors.append("aab-dirty-provenance-invalid")
        dirty = None
    elif dirty:
        errors.append("aab-dirty-provenance-true")
    if payload.get("status") != "complete":
        errors.append("aab-status-not-complete")

    reported_count = payload.get("reportedIncludedAssetCount")
    total_count = payload.get("totalIncludedAssetCount")
    if not _is_int(reported_count) or reported_count < 0:
        errors.append("aab-reported-count-invalid")
        reported_count = None
    if not _is_int(total_count) or total_count < 0:
        errors.append("aab-total-count-invalid")
        total_count = None

    rows = payload.get("buildReportIncludedAssets")
    if not isinstance(rows, list):
        errors.append("aab-included-assets-not-array")
        rows = []
    if reported_count is not None and reported_count != len(rows):
        errors.append("aab-reported-count-mismatch")
    if reported_count is not None and total_count is not None and reported_count > total_count:
        errors.append("aab-reported-count-exceeds-total")

    candidates: dict[str, BuildCandidate] = {}
    for asset_path in expected:
        matches = [row for row in rows if isinstance(row, dict) and row.get("sourceAssetPath") == asset_path]
        candidate_errors: list[str] = []
        packed_bytes: int | None = None
        if len(matches) != 1:
            candidate_errors.append(f"aab-included-row-count:{len(matches)}")
        else:
            row = matches[0]
            object_types = row.get("objectTypes")
            if not isinstance(object_types, list) or "UnityEngine.Texture2D" not in object_types:
                candidate_errors.append("aab-object-type-not-texture2d")
            raw_packed_bytes = row.get("packedBytes")
            if not _is_int(raw_packed_bytes) or raw_packed_bytes <= 0:
                candidate_errors.append("aab-packed-bytes-invalid")
            else:
                packed_bytes = raw_packed_bytes
        candidates[asset_path] = BuildCandidate(packed_bytes, tuple(candidate_errors))
    return BuildEvidence(exact_commit, dirty, reported_count, total_count, candidates, tuple(errors))


def parse_mobile_quality(text: str) -> MobileQuality:
    lines = text.splitlines()
    starts = [index for index, line in enumerate(lines) if line.startswith("  - serializedVersion:")]
    blocks = []
    for position, start in enumerate(starts):
        end = starts[position + 1] if position + 1 < len(starts) else len(lines)
        block = "\n".join(lines[start:end])
        if re.findall(r"^    name:\s*(.*?)\s*$", block, re.MULTILINE) == ["Mobile"]:
            blocks.append(block)
    if len(blocks) != 1:
        raise ValidationError("mobile-quality-count-invalid", f"Mobile tier count is {len(blocks)}")
    block = blocks[0]
    return MobileQuality(
        global_texture_mipmap_limit=int(_one_integer(block, 4, "globalTextureMipmapLimit")),
        streaming_active=int(_one_integer(block, 4, "streamingMipmapsActive")),
        add_all_cameras=int(_one_integer(block, 4, "streamingMipmapsAddAllCameras")),
        memory_budget_mib=int(_one_integer(block, 4, "streamingMipmapsMemoryBudget")),
        max_level_reduction=int(_one_integer(block, 4, "streamingMipmapsMaxLevelReduction")),
        max_file_io_requests=int(_one_integer(block, 4, "streamingMipmapsMaxFileIORequests")),
    )


def validate_mobile_quality(quality: MobileQuality) -> list[str]:
    errors: list[str] = []
    if quality.streaming_active != 1:
        errors.append(f"mobile-streaming-not-active:{quality.streaming_active}")
    if quality.add_all_cameras != 1:
        errors.append(f"mobile-streaming-camera-coverage-not-all:{quality.add_all_cameras}")
    if quality.memory_budget_mib != EXPECTED_MOBILE_BUDGET_MIB:
        errors.append(f"mobile-memory-budget-not-{EXPECTED_MOBILE_BUDGET_MIB}:{quality.memory_budget_mib}")
    if quality.max_level_reduction < 0 or quality.max_level_reduction > 2:
        errors.append(f"mobile-max-level-reduction-out-of-range:{quality.max_level_reduction}")
    if quality.max_file_io_requests <= 0:
        errors.append(f"mobile-max-file-io-requests-invalid:{quality.max_file_io_requests}")
    return errors


def _family(asset_path: str) -> str:
    match = FAMILY_RE.search(asset_path)
    if match:
        return f"numbered-texture:{match.group(1)}"
    stem = Path(asset_path).stem.lower()
    stem = re.sub(r"(?:[_-](?:a|b|c|normal|normals|mask|masks))+$", "", stem)
    return f"{Path(asset_path).parent.as_posix().lower()}:{stem}"


def select_pilot(rows: list[dict[str, object]], selector_errors: list[str]) -> list[str]:
    selected: list[dict[str, object]] = []
    if not selector_errors:
        eligible = [row for row in rows if not row["exclusionReasons"]]
        eligible.sort(key=lambda row: (-int(row["historicalAabPackedBytes"]), str(row["assetPath"])))
        selected_families: set[str] = set()
        for row in eligible:
            family = str(row["textureFamily"])
            if family in selected_families:
                continue
            row["proposedForPilot"] = True
            row["selectionReasons"] = [
                f"pilot-rank:{len(selected) + 1}",
                f"texture-family-representative:{family}",
                str(row["currentCategory"]).replace(" ", "-").replace("/", "-"),
                "current-tracked-importer-inventory",
                "historical-aab-positive-inclusion",
                "asset-and-meta-unchanged-since-historical-aab",
                "mipmaps-enabled",
                "explicit-streaming-baseline-disabled",
            ]
            selected.append(row)
            selected_families.add(family)
            if len(selected) == PILOT_LIMIT:
                break

    selected_families = {str(row["textureFamily"]) for row in selected}
    for row in rows:
        if row["proposedForPilot"] or row["exclusionReasons"]:
            continue
        if selector_errors:
            row["exclusionReasons"].append("selector-global-gate-failed")
        elif str(row["textureFamily"]) in selected_families:
            row["exclusionReasons"].append(f"texture-family-quota-filled:{row['textureFamily']}")
        else:
            row["exclusionReasons"].append(f"pilot-cap-reached:{PILOT_LIMIT}")
    return [str(row["assetPath"]) for row in selected]


def collect(root: Path = ROOT) -> dict[str, object]:
    head = _git_text(root, "rev-parse", "HEAD")
    inventory_at_start = collect_repository_inventory(root)
    importer_asset_paths = tuple(path[:-5] for path in inventory_at_start.importer_meta_paths)
    meta_path_by_asset = dict(zip(importer_asset_paths, inventory_at_start.importer_meta_paths))

    aab_bytes = _read_optional(root / AAB_REPORT_PATH)
    build = parse_build_evidence(aab_bytes, importer_asset_paths)
    historical_candidate_paths = tuple(
        sorted(
            path
            for path, candidate in build.candidates.items()
            if candidate.packed_bytes is not None or candidate.errors != ("aab-included-row-count:0",)
        )
    )
    control_paths = [
        AAB_REPORT_PATH.as_posix(),
        CONTENT_RESIDENCY_PATH.as_posix(),
        QUALITY_SETTINGS_PATH.as_posix(),
        PACKAGE_MANIFEST_PATH.as_posix(),
        PACKAGE_LOCK_PATH.as_posix(),
        APH505_EVIDENCE_PATH.as_posix(),
        APH506_EVIDENCE_PATH.as_posix(),
        *historical_candidate_paths,
        *(meta_path_by_asset[path] for path in historical_candidate_paths),
    ]
    scoped_changes_at_start = _tracked_changes(root, control_paths)
    before = {path: _read_optional(root / path) for path in control_paths}

    quality_errors: list[str] = []
    quality: MobileQuality | None = None
    quality_bytes = before[QUALITY_SETTINGS_PATH.as_posix()]
    if quality_bytes is None:
        quality_errors.append("mobile-quality-settings-unavailable")
    else:
        try:
            quality = parse_mobile_quality(quality_bytes.decode("utf-8"))
            quality_errors.extend(validate_mobile_quality(quality))
        except (UnicodeError, ValidationError) as error:
            code = error.code if isinstance(error, ValidationError) else type(error).__name__
            quality_errors.append(f"mobile-quality-settings-malformed:{code}:{error}")

    semantic_counts = {
        "UI": 0,
        "world albedo": 0,
        "world normal/mask": 0,
        "VFX": 0,
        "impostor/atlas": 0,
        "generated source/reference": 0,
    }
    parsed_current_metas: dict[str, TextureMeta] = {}
    current_categories: dict[str, str | None] = {}
    importer_parse_errors: dict[str, str] = {}
    for asset_path, meta_path in zip(importer_asset_paths, inventory_at_start.importer_meta_paths):
        meta_bytes = _read_optional(root / meta_path)
        if meta_bytes is None:
            importer_parse_errors[asset_path] = "current-meta-unavailable"
            continue
        try:
            yaml_text = meta_bytes.decode("utf-8")
        except UnicodeError as error:
            importer_parse_errors[asset_path] = f"current-meta-malformed:{type(error).__name__}"
            continue
        category = classify_inventory_texture(asset_path, yaml_text)
        current_categories[asset_path] = category
        semantic_counts[category] += 1
        try:
            meta = parse_texture_meta(yaml_text)
            parsed_current_metas[asset_path] = meta
        except ValidationError as error:
            code = error.code
            importer_parse_errors[asset_path] = f"current-meta-malformed:{code}"

    rows: list[dict[str, object]] = []
    for asset_path in historical_candidate_paths:
        meta_path = meta_path_by_asset[asset_path]
        exclusions: list[str] = []
        current_meta = parsed_current_metas.get(asset_path)
        current_category = current_categories.get(asset_path)
        dimensions: tuple[int, int] | None = None

        if asset_path in importer_parse_errors:
            exclusions.append(importer_parse_errors[asset_path])
        if current_category not in ("world albedo", "world normal/mask"):
            exclusions.append("current-semantic-category-not-world")

        if current_meta is not None:
            if current_meta.enable_mip_map != 1:
                exclusions.append(f"mipmaps-not-enabled:{current_meta.enable_mip_map}")
            if current_meta.streaming_mipmaps is None:
                exclusions.append("streamingMipmaps-field-absent")
            elif current_meta.streaming_mipmaps != 0:
                exclusions.append(f"streaming-baseline-not-disabled:{current_meta.streaming_mipmaps}")
            if current_meta.ignore_mipmap_limit is None:
                exclusions.append("ignoreMipmapLimit-field-absent")
            elif current_meta.ignore_mipmap_limit != 0:
                exclusions.append(f"ignore-mipmap-limit-not-disabled:{current_meta.ignore_mipmap_limit}")
            if current_meta.default_overridden != 0:
                exclusions.append(f"default-platform-overridden:{current_meta.default_overridden}")

        source_bytes = before.get(asset_path)
        if Path(asset_path).suffix.lower() != ".png":
            exclusions.append(f"source-format-not-png:{Path(asset_path).suffix.lower() or 'none'}")
        elif source_bytes is None:
            exclusions.append("source-header-unavailable")
        else:
            try:
                dimensions = parse_png_dimensions(source_bytes[:24])
            except ValidationError as error:
                exclusions.append(f"source-header-malformed:{error.code}")
        if current_meta is not None and dimensions is not None:
            if current_meta.default_max_texture_size < max(dimensions):
                exclusions.append(
                    f"default-max-size-below-source:{current_meta.default_max_texture_size}<{max(dimensions)}"
                )

        build_candidate = build.candidates[asset_path]
        exclusions.extend(build_candidate.errors)
        aab_asset_blob = _git_blob_id(root, build.exact_commit, asset_path) if build.exact_commit else None
        aab_meta_blob = _git_blob_id(root, build.exact_commit, meta_path) if build.exact_commit else None
        head_asset_blob = _git_blob_id(root, head, asset_path)
        head_meta_blob = _git_blob_id(root, head, meta_path)
        unchanged_since_aab = (
            aab_asset_blob is not None
            and aab_meta_blob is not None
            and aab_asset_blob == head_asset_blob
            and aab_meta_blob == head_meta_blob
        )
        if not unchanged_since_aab:
            exclusions.append("asset-or-meta-changed-since-historical-aab")

        rows.append(
            {
                "assetPath": asset_path,
                "metaPath": meta_path,
                "textureFamily": _family(asset_path),
                "sourceDimensions": list(dimensions) if dimensions else None,
                "currentCategory": current_category,
                "historicalAabPackedBytes": build_candidate.packed_bytes,
                "unchangedSinceHistoricalAab": unchanged_since_aab,
                "currentImporter": (
                    {
                        "serializedVersion": current_meta.serialized_version,
                        "enableMipMap": current_meta.enable_mip_map,
                        "isReadable": current_meta.is_readable,
                        "streamingMipmaps": current_meta.streaming_mipmaps,
                        "ignoreMipmapLimit": current_meta.ignore_mipmap_limit,
                        "defaultMaxTextureSize": current_meta.default_max_texture_size,
                    }
                    if current_meta
                    else None
                ),
                "proposedForPilot": False,
                "selectionReasons": [],
                "exclusionReasons": sorted(set(exclusions)),
            }
        )

    after = {path: _read_optional(root / path) for path in control_paths}
    input_hashes_unchanged = all(_sha256(before[path]) == _sha256(after[path]) for path in control_paths)
    inventory_at_end = collect_repository_inventory(root)
    importer_inventory_unchanged = (
        inventory_at_start.importer_meta_paths == inventory_at_end.importer_meta_paths
        and inventory_at_start.importer_hash == inventory_at_end.importer_hash
    )
    package_inventory_unchanged = inventory_at_start.package_hash == inventory_at_end.package_hash
    scoped_changes_at_end = _tracked_changes(root, control_paths)
    tracked_changes_at_end = _tracked_changes(root)

    selector_errors = list(build.errors) + quality_errors + list(inventory_at_start.errors)
    if scoped_changes_at_start != scoped_changes_at_end:
        selector_errors.append("scoped-tracked-input-status-changed-during-read")
    if not input_hashes_unchanged:
        selector_errors.append("control-input-hash-changed-during-read")
    if not importer_inventory_unchanged:
        selector_errors.append("texture-importer-inventory-changed-during-read")
    if not package_inventory_unchanged:
        selector_errors.append("package-inventory-changed-during-read")
    selected_paths = select_pilot(rows, selector_errors)
    selector_valid = not selector_errors and len(selected_paths) == PILOT_LIMIT
    current_build_gate = parse_current_build_gate(
        before[AAB_REPORT_PATH.as_posix()], head, selected_paths
    )
    current_residency_gate = parse_current_residency_gate(
        before[CONTENT_RESIDENCY_PATH.as_posix()], head, selected_paths
    )
    visual_gate = parse_visual_evidence_gate(before[APH505_EVIDENCE_PATH.as_posix()], head, selected_paths)
    performance_gate = parse_performance_evidence_gate(
        before[APH506_EVIDENCE_PATH.as_posix()], head, selected_paths
    )
    mutation_preconditions = {
        "selectorValid": selector_valid,
        "trackedWorktreeClean": not tracked_changes_at_end,
        "scopedTrackedInputsClean": not scoped_changes_at_end,
        "controlInputsStable": input_hashes_unchanged,
        "currentRevisionCompleteTextureBuildEvidence": current_build_gate.accepted,
        "currentRevisionContentResidencyEvidence": current_residency_gate.accepted,
        "aph502FinalBucketsAccepted": current_build_gate.accepted and current_residency_gate.accepted,
        "mobileStreamingConfigurationValid": quality is not None and not quality_errors,
        "fullResolutionNearbyTexturesPreserved": (
            quality is not None and quality.global_texture_mipmap_limit == 0
        ),
        "aph505VisualEvidenceAccepted": visual_gate.accepted,
        "aph506PerformanceEvidenceAccepted": performance_gate.accepted,
        "textureImporterInventoryStable": importer_inventory_unchanged,
        "packageInventoryStable": package_inventory_unchanged and not inventory_at_start.errors,
    }
    mutation_authorized = all(mutation_preconditions.values())
    unresolved = [
        f"precondition-failed:{name}"
        for name, accepted in mutation_preconditions.items()
        if not accepted
    ]
    unresolved.extend(current_build_gate.errors)
    unresolved.extend(current_residency_gate.errors)
    unresolved.extend(visual_gate.errors)
    unresolved.extend(performance_gate.errors)
    if build.exact_commit != head:
        unresolved.append(f"historical-aab-revision-mismatch:{build.exact_commit}->{head}")
    if not build.export_complete:
        unresolved.append(f"historical-aab-top-table-incomplete:{build.reported_count}/{build.total_count}")
    if tracked_changes_at_end:
        unresolved.append("tracked-worktree-dirty")
    unresolved = sorted(set(unresolved))

    return {
        "schemaVersion": 2,
        "taskId": TASK_ID,
        "reportDate": REPORT_DATE,
        "status": (
            "pilot-mutation-authorized"
            if mutation_authorized
            else "candidate-plan-valid-rollout-blocked"
            if selector_valid
            else "candidate-plan-invalid"
        ),
        "selectorValid": selector_valid,
        "pilotReadyForMutation": mutation_authorized,
        "readOnlyCollection": True,
        "mutationAuthorized": mutation_authorized,
        "expansionAuthorized": False,
        "mutationPreconditions": mutation_preconditions,
        "pilotLimit": PILOT_LIMIT,
        "proposedCandidatePaths": selected_paths,
        "proposedCandidateCount": len(selected_paths),
        "selectorErrors": sorted(set(selector_errors)),
        "unresolvedEvidence": unresolved,
        "analyzedRevision": head,
        "trackedWorktreeClean": not tracked_changes_at_end,
        "scopedTrackedInputsClean": not scoped_changes_at_end,
        "scopedTrackedInputChanges": scoped_changes_at_end,
        "controlInputHashesUnchangedDuringRead": input_hashes_unchanged,
        "currentRepositoryEvidence": {
            "trackedTextureImporterCount": len(inventory_at_start.importer_meta_paths),
            "textureImporterInventorySha256": inventory_at_start.importer_hash,
            "textureImporterInventoryUnchangedDuringRead": importer_inventory_unchanged,
            "semanticCounts": semantic_counts,
            "strictImporterParseErrorCount": len(importer_parse_errors),
            "historicalBuildCandidateCount": len(historical_candidate_paths),
            "manifestPackageCount": inventory_at_start.manifest_package_count,
            "lockedPackageCount": inventory_at_start.locked_package_count,
            "packageInventorySha256": inventory_at_start.package_hash,
            "packageInventoryUnchangedDuringRead": package_inventory_unchanged,
            "errors": list(inventory_at_start.errors),
        },
        "historicalAabEvidence": {
            "path": AAB_REPORT_PATH.as_posix(),
            "exactCommit": build.exact_commit,
            "dirty": build.dirty,
            "reportedIncludedAssetCount": build.reported_count,
            "totalIncludedAssetCount": build.total_count,
            "exportComplete": build.export_complete,
            "errors": list(build.errors),
        },
        "currentBuildEvidence": {
            "path": AAB_REPORT_PATH.as_posix(),
            "accepted": current_build_gate.accepted,
            "errors": list(current_build_gate.errors),
        },
        "currentResidencyEvidence": {
            "path": CONTENT_RESIDENCY_PATH.as_posix(),
            "accepted": current_residency_gate.accepted,
            "errors": list(current_residency_gate.errors),
        },
        "aph505VisualEvidence": {
            "path": APH505_EVIDENCE_PATH.as_posix(),
            "accepted": visual_gate.accepted,
            "errors": list(visual_gate.errors),
        },
        "aph506PerformanceEvidence": {
            "path": APH506_EVIDENCE_PATH.as_posix(),
            "accepted": performance_gate.accepted,
            "errors": list(performance_gate.errors),
        },
        "mobileQuality": (
            {
                "path": QUALITY_SETTINGS_PATH.as_posix(),
                "globalTextureMipmapLimit": quality.global_texture_mipmap_limit,
                "streamingMipmapsActive": quality.streaming_active,
                "streamingMipmapsAddAllCameras": quality.add_all_cameras,
                "streamingMipmapsMemoryBudgetMiB": quality.memory_budget_mib,
                "streamingMipmapsMaxLevelReduction": quality.max_level_reduction,
                "streamingMipmapsMaxFileIORequests": quality.max_file_io_requests,
                "errors": quality_errors,
            }
            if quality
            else {"path": QUALITY_SETTINGS_PATH.as_posix(), "errors": quality_errors}
        ),
        "candidates": rows,
    }


def render_check(data: dict[str, object]) -> str:
    result = "Passed" if data["selectorValid"] else "Failed"
    return (
        f"[APH-504] result={result} selector_valid={str(data['selectorValid']).lower()} "
        f"pilot_ready={str(data['pilotReadyForMutation']).lower()} "
        f"proposed_count={data['proposedCandidateCount']} "
        f"mutation_authorized={str(data['mutationAuthorized']).lower()} "
        f"expansion_authorized=false unresolved_count={len(data['unresolvedEvidence'])}"
    )


def render_json(data: dict[str, object]) -> str:
    return json.dumps(data, indent=2, sort_keys=False) + "\n"


def render_markdown(data: dict[str, object]) -> str:
    quality = data["mobileQuality"]
    evidence = data["historicalAabEvidence"]
    repository = data["currentRepositoryEvidence"]
    build = data["currentBuildEvidence"]
    residency = data["currentResidencyEvidence"]
    visual = data["aph505VisualEvidence"]
    performance = data["aph506PerformanceEvidence"]
    lines = [
        "# APH-504 Texture Streaming Pilot Candidate Plan",
        "",
        f"- Evidence date: `{data['reportDate']}`",
        f"- Status: `{data['status']}`",
        f"- Analyzed revision: `{data['analyzedRevision']}`",
        f"- Selector valid: `{str(data['selectorValid']).lower()}`",
        f"- Pilot ready for importer mutation: `{str(data['pilotReadyForMutation']).lower()}`",
        f"- Importer mutation authorized: `{str(data['mutationAuthorized']).lower()}`",
        "- Pilot expansion authorized: `false`",
        "- Unity and Android runs: `none`",
        "",
        "## Decision",
        "",
        f"The read-only selector derives up to {data['pilotLimit']} candidates from the current tracked "
        "TextureImporter inventory intersected with positive historical Android BuildReport rows. "
        "No asset path or APH-502 revision is embedded in the selector. Importer mutation is authorized "
        "only when every precondition below is true; expansion remains a separate rejected decision.",
        "",
        "## Current Repository Evidence",
        "",
        f"- Tracked TextureImporter count: `{repository['trackedTextureImporterCount']}`",
        f"- Importer inventory SHA-256: `{repository['textureImporterInventorySha256']}`",
        f"- Historical BuildReport candidate intersection: `{repository['historicalBuildCandidateCount']}`",
        f"- Manifest packages: `{repository['manifestPackageCount']}`",
        f"- Locked packages: `{repository['lockedPackageCount']}`",
        f"- Package inventory SHA-256: `{repository['packageInventorySha256']}`",
        f"- Strict importer parse errors: `{repository['strictImporterParseErrorCount']}`",
        "",
        "## Proposed Candidate Set",
        "",
        "| Texture | Decision | Category | Historical AAB bytes | Reasons |",
        "|---|---|---|---:|---|",
    ]
    for row in data["candidates"]:
        decision = "proposed" if row["proposedForPilot"] else "excluded"
        reasons = row["selectionReasons"] if row["proposedForPilot"] else row["exclusionReasons"]
        lines.append(
            f"| `{row['assetPath']}` | {decision} | {row['currentCategory']} | "
            f"{row['historicalAabPackedBytes'] or '-'} | {', '.join(reasons)} |"
        )
    lines.extend(
        [
            "",
            "## Evidence Disposition",
            "",
            f"- Historical AAB revision: `{evidence['exactCommit']}`; dirty=`{str(evidence['dirty']).lower()}`; "
            f"exported assets=`{evidence['reportedIncludedAssetCount']}/{evidence['totalIncludedAssetCount']}`.",
            "- Historical positive rows prove prior inclusion only; they do not prove current-revision inclusion or absence.",
            f"- Scoped tracked inputs clean: `{str(data['scopedTrackedInputsClean']).lower()}`.",
            f"- Control-input hashes unchanged during collection: "
            f"`{str(data['controlInputHashesUnchangedDuringRead']).lower()}`.",
            f"- Current complete texture BuildReport accepted: `{str(build['accepted']).lower()}`; "
            f"errors=`{', '.join(build['errors']) or 'none'}`.",
            f"- Current content-residency inventory accepted: `{str(residency['accepted']).lower()}`; "
            f"errors=`{', '.join(residency['errors']) or 'none'}`.",
            f"- APH-505 visual evidence accepted: `{str(visual['accepted']).lower()}`; "
            f"path=`{visual['path']}`; errors=`{', '.join(visual['errors']) or 'none'}`.",
            f"- APH-506 performance evidence accepted: `{str(performance['accepted']).lower()}`; "
            f"path=`{performance['path']}`; errors=`{', '.join(performance['errors']) or 'none'}`.",
            "",
            "## Mobile Configuration",
            "",
            f"- Streaming active: `{quality.get('streamingMipmapsActive')}`",
            f"- Add all cameras: `{quality.get('streamingMipmapsAddAllCameras')}`",
            f"- Streaming memory budget: `{quality.get('streamingMipmapsMemoryBudgetMiB')} MiB`",
            f"- Global texture mip limit: `{quality.get('globalTextureMipmapLimit')}`",
            f"- Maximum streaming level reduction: `{quality.get('streamingMipmapsMaxLevelReduction')}`",
            f"- Maximum file I/O requests: `{quality.get('streamingMipmapsMaxFileIORequests')}`",
            "",
            "The 256 MiB value is an observed bounded configuration, not an accepted product budget. "
            "The global mip limit of 1 prevents full source mip preservation for nearby views while "
            "the proposed importers keep `ignoreMipmapLimit: 0`.",
            "",
            "## Unresolved Evidence",
            "",
        ]
    )
    lines.extend(f"- `{reason}`" for reason in data["unresolvedEvidence"])
    lines.extend(
        [
            "",
            "## Mutation Preconditions",
            "",
        ]
    )
    for name, accepted in data["mutationPreconditions"].items():
        lines.append(f"- `{name}`: `{str(accepted).lower()}`")
    lines.extend(
        [
            "",
            "## Acceptance Boundary",
            "",
            "The selector contract is accepted when candidate discovery is deterministic and read-only. Importer "
            "mutation remains fail-closed until a clean same-revision complete texture BuildReport and residency "
            "inventory accept APH-502, the Mobile tier preserves full-resolution nearby mips, APH-505 supplies "
            "accepted near/medium/far before-and-after visual pairs, and APH-506 supplies an accepted 600-second "
            "memory and I/O measurement for the exact candidate paths and revision. The APH-505 and APH-506 JSON "
            "contracts are validated at the evidence paths listed above. No APH-504 report can authorize expansion.",
            "",
            "## Reproduction",
            "",
            "```sh",
            "PYTHONPYCACHEPREFIX=/tmp/aph504-pyc python3 -m unittest \\",
            "  Tools.CI.tests.test_aph504_texture_streaming_pilot_selector -v",
            "PYTHONPYCACHEPREFIX=/tmp/aph504-pyc python3 \\",
            "  Tools/CI/aph504_texture_streaming_pilot_selector.py --check",
            "```",
            "",
        ]
    )
    return "\n".join(lines)


def write_reports(root: Path, data: dict[str, object]) -> None:
    (root / JSON_REPORT_PATH).write_text(render_json(data), encoding="utf-8")
    (root / MARKDOWN_REPORT_PATH).write_text(render_markdown(data), encoding="utf-8")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT, help="repository root")
    parser.add_argument("--json", action="store_true", help="emit deterministic JSON")
    parser.add_argument("--check", action="store_true", help="validate the bounded selector contract")
    parser.add_argument("--write", action="store_true", help="write the two APH-504 evidence reports")
    args = parser.parse_args()
    root = args.root.resolve()
    data = collect(root)
    if args.write:
        write_reports(root, data)
    if args.json:
        print(render_json(data), end="")
    elif args.check:
        print(render_check(data))
    elif not args.write:
        print(render_markdown(data), end="")
    return 0 if not args.check or data["selectorValid"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
