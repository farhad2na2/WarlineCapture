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
REPORT_DATE = "2026-07-14"
APH502_REVISION = "bc0287616ac225de524d836cd8409c4fd0d49eb0"
PILOT_LIMIT = 2
EXPECTED_MOBILE_BUDGET_MIB = 256
AAB_REPORT_PATH = Path("Design/AgentReports/architecture_performance_android_aab_build_report.json")
QUALITY_SETTINGS_PATH = Path("ProjectSettings/QualitySettings.asset")
JSON_REPORT_PATH = Path("Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.json")
MARKDOWN_REPORT_PATH = Path("Design/AgentReports/2026-07-14_aph-504_texture_streaming_pilot_plan.md")
CANDIDATE_ASSET_PATHS = (
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png",
    "Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png",
)

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


def _run_git(root: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess[bytes]:
    return subprocess.run(
        ["git", *args],
        cwd=root,
        check=check,
        capture_output=True,
    )


def _git_text(root: Path, *args: str) -> str:
    return _run_git(root, *args).stdout.decode("utf-8").strip()


def _git_blob(root: Path, revision: str, relative_path: str) -> bytes | None:
    result = _run_git(root, "show", f"{revision}:{relative_path}", check=False)
    return result.stdout if result.returncode == 0 else None


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
    if not match:
        raise ValidationError("candidate-family-unresolved", f"no texture family in {asset_path}")
    return match.group(1)


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
                str(row["aph502Category"]).replace(" ", "-").replace("/", "-"),
                "clean-historical-aab-positive-inclusion",
                "asset-and-meta-unchanged-since-aph502-and-aab",
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
    control_paths = [
        AAB_REPORT_PATH.as_posix(),
        QUALITY_SETTINGS_PATH.as_posix(),
        *CANDIDATE_ASSET_PATHS,
        *(f"{path}.meta" for path in CANDIDATE_ASSET_PATHS),
    ]
    tracked_changes_at_start = _tracked_changes(root)
    scoped_changes_at_start = _tracked_changes(root, control_paths)
    before = {path: _read_optional(root / path) for path in control_paths}

    build = parse_build_evidence(before[AAB_REPORT_PATH.as_posix()], CANDIDATE_ASSET_PATHS)
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

    rows: list[dict[str, object]] = []
    for asset_path in CANDIDATE_ASSET_PATHS:
        meta_path = f"{asset_path}.meta"
        exclusions: list[str] = []
        current_meta: TextureMeta | None = None
        aph502_meta: TextureMeta | None = None
        current_category: str | None = None
        aph502_category: str | None = None
        dimensions: tuple[int, int] | None = None

        meta_bytes = before.get(meta_path)
        if meta_bytes is None:
            exclusions.append("current-meta-unavailable")
        else:
            try:
                current_meta = parse_texture_meta(meta_bytes.decode("utf-8"))
                current_category, category_exclusions = classify_world_texture(asset_path, current_meta)
                exclusions.extend(category_exclusions)
            except (UnicodeError, ValidationError) as error:
                code = error.code if isinstance(error, ValidationError) else type(error).__name__
                exclusions.append(f"current-meta-malformed:{code}")

        aph502_bytes = _git_blob(root, APH502_REVISION, meta_path)
        if aph502_bytes is None:
            exclusions.append("aph502-meta-unavailable")
        else:
            try:
                aph502_meta = parse_texture_meta(aph502_bytes.decode("utf-8"))
                aph502_category, category_exclusions = classify_world_texture(asset_path, aph502_meta)
                exclusions.extend(f"aph502-{reason}" for reason in category_exclusions)
            except (UnicodeError, ValidationError) as error:
                code = error.code if isinstance(error, ValidationError) else type(error).__name__
                exclusions.append(f"aph502-meta-malformed:{code}")

        if current_category != aph502_category:
            exclusions.append(f"semantic-category-drift:{aph502_category}->{current_category}")
        if current_category not in ("world albedo", "world normal/mask"):
            exclusions.append("current-semantic-category-not-world")
        if aph502_category not in ("world albedo", "world normal/mask"):
            exclusions.append("aph502-semantic-category-not-world")

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
        if source_bytes is None:
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
        unchanged_since_aph502 = (
            _git_blob_id(root, APH502_REVISION, asset_path) == head_asset_blob
            and _git_blob_id(root, APH502_REVISION, meta_path) == head_meta_blob
        )
        if not unchanged_since_aab:
            exclusions.append("asset-or-meta-changed-since-historical-aab")
        if not unchanged_since_aph502:
            exclusions.append("asset-or-meta-changed-since-aph502")

        rows.append(
            {
                "assetPath": asset_path,
                "metaPath": meta_path,
                "textureFamily": _family(asset_path),
                "sourceDimensions": list(dimensions) if dimensions else None,
                "aph502Category": aph502_category,
                "currentCategory": current_category,
                "historicalAabPackedBytes": build_candidate.packed_bytes,
                "unchangedSinceHistoricalAab": unchanged_since_aab,
                "unchangedSinceAph502": unchanged_since_aph502,
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
    scoped_changes_at_end = _tracked_changes(root, control_paths)
    tracked_changes_at_end = _tracked_changes(root)

    selector_errors = list(build.errors) + quality_errors
    if scoped_changes_at_start or scoped_changes_at_end:
        selector_errors.append("scoped-tracked-input-dirty")
    if scoped_changes_at_start != scoped_changes_at_end:
        selector_errors.append("scoped-tracked-input-status-changed-during-read")
    if not input_hashes_unchanged:
        selector_errors.append("control-input-hash-changed-during-read")
    if tracked_changes_at_start != tracked_changes_at_end:
        selector_errors.append("tracked-worktree-changed-during-read")

    selected_paths = select_pilot(rows, selector_errors)
    unresolved = [
        "aph502-final-buckets-unaccepted",
        "current-revision-clean-complete-texture-build-report-absent",
        "current-revision-clean-residency-inventory-absent",
        "candidate-material-renderer-camera-coverage-unresolved",
        "aph505-near-medium-far-before-after-visual-evidence-absent",
        "aph506-ten-minute-memory-io-evidence-absent",
        "pilot-importer-settings-not-applied",
    ]
    if build.exact_commit != head:
        unresolved.append(f"historical-aab-revision-mismatch:{build.exact_commit}->{head}")
    if not build.export_complete:
        unresolved.append(f"historical-aab-export-incomplete:{build.reported_count}/{build.total_count}")
    if tracked_changes_at_end:
        unresolved.append(f"tracked-worktree-dirty:{len(tracked_changes_at_end)}")
    if quality is not None and quality.global_texture_mipmap_limit != 0:
        unresolved.append(
            f"full-source-near-mips-not-preserved:globalTextureMipmapLimit={quality.global_texture_mipmap_limit}"
        )
    selected_rows = [row for row in rows if row["proposedForPilot"]]
    if any(row["currentImporter"] and row["currentImporter"]["isReadable"] == 1 for row in selected_rows):
        unresolved.append("selected-readable-texture-cpu-copy-memory-unmeasured")
    if not any(row["proposedForPilot"] and row["currentCategory"] == "world normal/mask" for row in rows):
        unresolved.append("normal-mask-representative-not-selected:explicit-streaming-fields-absent")
    unresolved = sorted(set(unresolved))

    selector_valid = not selector_errors and len(selected_paths) == PILOT_LIMIT
    pilot_ready = selector_valid and not unresolved
    return {
        "schemaVersion": 1,
        "taskId": TASK_ID,
        "reportDate": REPORT_DATE,
        "status": "candidate-plan-valid-rollout-blocked" if selector_valid else "candidate-plan-invalid",
        "selectorValid": selector_valid,
        "pilotReadyForMutation": pilot_ready,
        "readOnlyCollection": True,
        "mutationAuthorized": False,
        "expansionAuthorized": False,
        "pilotLimit": PILOT_LIMIT,
        "proposedCandidatePaths": selected_paths,
        "proposedCandidateCount": len(selected_paths),
        "selectorErrors": sorted(set(selector_errors)),
        "unresolvedEvidence": unresolved,
        "aph502Revision": APH502_REVISION,
        "analyzedRevision": head,
        "trackedWorktreeClean": not tracked_changes_at_end,
        "trackedWorktreeChangeCount": len(tracked_changes_at_end),
        "scopedTrackedInputsClean": not scoped_changes_at_end,
        "scopedTrackedInputChanges": scoped_changes_at_end,
        "controlInputHashesUnchangedDuringRead": input_hashes_unchanged,
        "historicalAabEvidence": {
            "path": AAB_REPORT_PATH.as_posix(),
            "exactCommit": build.exact_commit,
            "dirty": build.dirty,
            "reportedIncludedAssetCount": build.reported_count,
            "totalIncludedAssetCount": build.total_count,
            "exportComplete": build.export_complete,
            "errors": list(build.errors),
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
        f"proposed_count={data['proposedCandidateCount']} mutation_authorized=false "
        f"expansion_authorized=false unresolved_count={len(data['unresolvedEvidence'])}"
    )


def render_json(data: dict[str, object]) -> str:
    return json.dumps(data, indent=2, sort_keys=False) + "\n"


def render_markdown(data: dict[str, object]) -> str:
    quality = data["mobileQuality"]
    evidence = data["historicalAabEvidence"]
    lines = [
        "# APH-504 Texture Streaming Pilot Candidate Plan",
        "",
        f"- Evidence date: `{data['reportDate']}`",
        f"- Status: `{data['status']}`",
        f"- Analyzed revision: `{data['analyzedRevision']}`",
        f"- Selector valid: `{str(data['selectorValid']).lower()}`",
        f"- Pilot ready for importer mutation: `{str(data['pilotReadyForMutation']).lower()}`",
        "- Importer mutation authorized: `false`",
        "- Pilot expansion authorized: `false`",
        "- Unity and Android runs: `none`",
        "",
        "## Decision",
        "",
        "The read-only selector proposes two world-albedo textures as a bounded future pilot. "
        "It does not authorize either importer change or a wider streaming rollout.",
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
            f"| `{row['assetPath']}` | {decision} | {row['aph502Category']} | "
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
            "## Acceptance Boundary",
            "",
            "The selector contract is accepted when its inputs parse deterministically, scoped inputs stay clean, "
            "the exact two candidates are proposed, and both mutation flags remain false. APH-504 itself remains "
            "incomplete until current-revision build/residency evidence, APH-505 visual captures, and APH-506 "
            "ten-minute memory/I/O measurements pass.",
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
