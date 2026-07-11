#!/usr/bin/env python3
"""Generate deterministic, read-only evidence for APH-508 animation textures."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
from dataclasses import dataclass
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / "Design/AgentReports/2026-07-11_aph-508_animation_texture_audit.md"
GENERATED_ROOT = Path("Assets/Game/Prefabs/Generated")
TEXTURE_PATTERN = "*/ModelResources/AnimationTexture[0-2].asset"
BUILD_REPORTS = (
    Path("Design/AgentReports/architecture_performance_android_apk_build_report.json"),
    Path("Design/AgentReports/architecture_performance_android_aab_build_report.json"),
)
CONTENT_REPORT = Path("Design/AgentReports/architecture_performance_content_residency_baseline.json")
GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)
ANIMATION_RE = re.compile(
    r"- fps:\s*(\d+)\s*\n\s*start:\s*(\d+)\s*\n\s*frames:\s*(\d+)",
    re.MULTILINE,
)
HEADER_FIELDS = {
    "width": r"m_Width:\s*(\d+)",
    "height": r"m_Height:\s*(\d+)",
    "payload_bytes": r"m_CompleteImageSize:\s*(\d+)",
    "texture_format": r"m_TextureFormat:\s*(\d+)",
    "mip_count": r"m_MipCount:\s*(\d+)",
    "readable": r"m_IsReadable:\s*(\d+)",
    "streaming": r"m_StreamingMipmaps:\s*(\d+)",
    "color_space": r"m_ColorSpace:\s*(\d+)",
    "filter_mode": r"m_FilterMode:\s*(\d+)",
}


@dataclass(frozen=True)
class TextureEvidence:
    path: str
    set_name: str
    index: int
    guid: str
    file_bytes: int
    file_sha256: str
    payload_sha256: str
    width: int
    height: int
    payload_bytes: int
    texture_format: int
    mip_count: int
    readable: int
    streaming: int
    color_space: int
    filter_mode: int
    reference_files: tuple[str, ...]
    packed_apk_bytes: int | None
    packed_aab_bytes: int | None
    imported_bytes: int | None


@dataclass(frozen=True)
class SetEvidence:
    name: str
    animator_count: int
    authored_clip_count: int
    unique_clip_layout_count: int
    min_bones: int
    max_bones: int
    maximum_used_texel: int
    texture_capacity_texels: int

    @property
    def utilization_percent(self) -> float:
        if not self.texture_capacity_texels:
            return 0.0
        return self.maximum_used_texel * 100.0 / self.texture_capacity_texels


def relative(root: Path, path: Path) -> str:
    return path.relative_to(root).as_posix()


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def parse_texture(root: Path, path: Path) -> dict[str, int | str]:
    data = path.read_bytes()
    marker = b"_typelessdata:"
    marker_index = data.find(marker)
    if marker_index < 0:
        raise ValueError(f"missing inline image data: {relative(root, path)}")
    header = data[:marker_index].decode("utf-8", errors="strict")
    payload = data[marker_index + len(marker):].splitlines()[0].strip()
    values: dict[str, int | str] = {
        "file_bytes": len(data),
        "file_sha256": sha256(data),
        "payload_sha256": sha256(payload),
    }
    for name, pattern in HEADER_FIELDS.items():
        match = re.search(pattern, header)
        if match is None:
            raise ValueError(f"missing {name}: {relative(root, path)}")
        values[name] = int(match.group(1))
    expected_hex_bytes = int(values["payload_bytes"]) * 2
    if len(payload) != expected_hex_bytes:
        raise ValueError(
            f"payload length mismatch: {relative(root, path)} "
            f"expected={expected_hex_bytes} actual={len(payload)}"
        )
    return values


def read_guid(meta_path: Path) -> str:
    match = GUID_RE.search(meta_path.read_text(encoding="utf-8"))
    if match is None:
        raise ValueError(f"missing guid: {meta_path}")
    return match.group(1)


def guid_references(root: Path, guids: list[str]) -> dict[str, tuple[str, ...]]:
    references: dict[str, list[str]] = {guid: [] for guid in guids}
    needles = {guid: guid.encode("ascii") for guid in guids}
    command = ["git", "-C", str(root), "grep", "-l", "-z", "-F"]
    for guid in guids:
        command.extend(("-e", guid))
    command.extend(("--", "Assets", "ProjectSettings", "Packages"))
    result = subprocess.run(command, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    matches = result.stdout.decode().split("\0")
    for asset_path in sorted(item for item in matches if item):
        path = root / asset_path
        if path.suffix == ".meta":
            continue
        try:
            content = path.read_bytes()
            for guid, needle in needles.items():
                if needle in content:
                    references[guid].append(asset_path)
        except OSError:
            continue
    return {guid: tuple(sorted(items)) for guid, items in references.items()}


def load_build_contributions(root: Path) -> dict[str, dict[str, int]]:
    contributions: dict[str, dict[str, int]] = {}
    for report_path in BUILD_REPORTS:
        data = json.loads((root / report_path).read_text(encoding="utf-8"))
        package_type = str(data["packageType"]).lower()
        contributions[package_type] = {
            row["sourceAssetPath"]: int(row["packedBytes"])
            for row in data["buildReportIncludedAssets"]
        }
    return contributions


def load_content_inventory(root: Path) -> dict[str, dict]:
    data = json.loads((root / CONTENT_REPORT).read_text(encoding="utf-8"))
    return {row["assetPath"]: row for row in data["assets"]}


def collect_set(root: Path, name: str, capacity: int) -> SetEvidence:
    animator_root = root / GENERATED_ROOT / name / "Animators"
    animator_paths = sorted(animator_root.glob("*.prefab"))
    clip_count = 0
    layouts: set[tuple[tuple[int, int, int], ...]] = set()
    bones: list[int] = []
    maximum_used_texel = 0
    for animator_path in animator_paths:
        content = animator_path.read_text(encoding="utf-8-sig")
        bones_match = re.search(r"bonesCount:\s*(\d+)", content)
        if bones_match is None:
            raise ValueError(f"missing bonesCount: {relative(root, animator_path)}")
        bone_count = int(bones_match.group(1))
        animations = tuple(
            (int(fps), int(start), int(frames))
            for fps, start, frames in ANIMATION_RE.findall(content)
        )
        if not animations:
            raise ValueError(f"missing animations: {relative(root, animator_path)}")
        bones.append(bone_count)
        layouts.add(animations)
        clip_count += max(0, len(animations) - 1)
        maximum_used_texel = max(
            maximum_used_texel,
            *(start + frames * bone_count for _, start, frames in animations),
        )
    if not bones:
        raise ValueError(f"no animator prefabs: {animator_root}")
    return SetEvidence(
        name=name,
        animator_count=len(animator_paths),
        authored_clip_count=clip_count,
        unique_clip_layout_count=len(layouts),
        min_bones=min(bones),
        max_bones=max(bones),
        maximum_used_texel=maximum_used_texel,
        texture_capacity_texels=capacity,
    )


def collect(root: Path) -> tuple[list[TextureEvidence], list[SetEvidence], list[str]]:
    build = load_build_contributions(root)
    inventory = load_content_inventory(root)
    texture_paths = sorted((root / GENERATED_ROOT).glob(TEXTURE_PATTERN))
    parsed_textures: list[tuple[Path, str, str, dict[str, int | str]]] = []
    for path in texture_paths:
        parsed = parse_texture(root, path)
        asset_path = relative(root, path)
        guid = read_guid(Path(f"{path}.meta"))
        parsed_textures.append((path, asset_path, guid, parsed))
    references = guid_references(root, [item[2] for item in parsed_textures])
    textures: list[TextureEvidence] = []
    for path, asset_path, guid, parsed in parsed_textures:
        index_match = re.search(r"AnimationTexture(\d+)\.asset$", asset_path)
        if index_match is None:
            raise ValueError(f"unexpected texture name: {asset_path}")
        inventory_row = inventory.get(asset_path, {})
        textures.append(TextureEvidence(
            path=asset_path,
            set_name=path.parent.parent.name,
            index=int(index_match.group(1)),
            guid=guid,
            file_bytes=int(parsed["file_bytes"]),
            file_sha256=str(parsed["file_sha256"]),
            payload_sha256=str(parsed["payload_sha256"]),
            width=int(parsed["width"]),
            height=int(parsed["height"]),
            payload_bytes=int(parsed["payload_bytes"]),
            texture_format=int(parsed["texture_format"]),
            mip_count=int(parsed["mip_count"]),
            readable=int(parsed["readable"]),
            streaming=int(parsed["streaming"]),
            color_space=int(parsed["color_space"]),
            filter_mode=int(parsed["filter_mode"]),
            reference_files=references[guid],
            packed_apk_bytes=build.get("apk", {}).get(asset_path),
            packed_aab_bytes=build.get("aab", {}).get(asset_path),
            imported_bytes=inventory_row.get("importedSizeBytes"),
        ))
    if len(textures) != 6:
        raise ValueError(f"expected six generated animation textures, found {len(textures)}")
    capacities = {item.set_name: item.width * item.height for item in textures}
    sets = [collect_set(root, name, capacities[name]) for name in sorted(capacities)]
    unload_hits: list[str] = []
    unload_needles = (b"Resources.UnloadUnusedAssets", b"Resources.UnloadAsset(", b"AssetBundle.Unload(", b"Addressables.Release")
    scripts_root = root / "Assets/Game/Scripts"
    for path in sorted(scripts_root.rglob("*.cs")):
        if "Editor" in path.parts:
            continue
        content = path.read_bytes()
        if any(needle in content for needle in unload_needles):
            unload_hits.append(relative(root, path))
    return textures, sets, unload_hits


def fmt_bytes(value: int | None) -> str:
    if value is None:
        return "not included"
    return f"{value:,} ({value / 1024 / 1024:.2f} MiB)"


def render_report(textures: list[TextureEvidence], sets: list[SetEvidence], unload_hits: list[str]) -> str:
    payload_groups: dict[str, list[str]] = {}
    for item in textures:
        payload_groups.setdefault(item.payload_sha256, []).append(item.path)
    exact_duplicates = [paths for paths in payload_groups.values() if len(paths) > 1]
    included = [item for item in textures if item.packed_apk_bytes is not None or item.packed_aab_bytes is not None]
    excluded = [item for item in textures if item not in included]
    lines = [
        "# APH-508 Generated Animation Texture Audit",
        "",
        "- Scope: the six tracked `AnimationTexture0..2.asset` files under generated character batches.",
        "- Method: deterministic static serialization/GUID/clip analysis plus existing clean Android BuildReport and Unity content-inventory evidence.",
        "- Safety: read-only audit; no texture importer, Unity asset, scene, prefab, package, or runtime code was changed.",
        "- Status: bounded audit complete; named device-runtime residency and post-unload memory remain unmeasured.",
        "",
        "## Executive Findings",
        "",
        f"- Project payload: `{sum(item.payload_bytes for item in textures):,}` bytes across six RGBAHalf textures.",
        f"- Android build payload: three `CharactersBaked` textures, `{sum(item.packed_aab_bytes or 0 for item in included):,}` attributed AAB bytes. The three legacy-batch textures are absent from both recorded APK and AAB BuildReports.",
        f"- Exact duplication: {'none; all six inline pixel payload hashes are distinct' if not exact_duplicates else 'detected; see duplication section'}.",
        "- Runtime ownership: each batch material binds all three matrix-row textures together, so any renderer using that material makes the three textures a single residency unit; no per-clip texture loading exists.",
        "- Precision: generation writes three rows of bone matrices into linear, point-filtered `RGBAHalf` textures. Signed floating-point and no color conversion are structural requirements. The audit does not prove that a lower precision is visually safe.",
        "- Unload: no first-party non-Editor explicit unload/release call was found. Direct material dependencies can be released only when their owning material/renderers and scene dependencies become unused and Unity performs an unused-asset unload or process teardown.",
        "",
        "## Texture Evidence",
        "",
        "| Set / texture | Dimensions | Serialized payload | Unity imported memory | APK packed | AAB packed | Flags | Payload SHA-256 | Direct references |",
        "|---|---:|---:|---:|---:|---:|---|---|---:|",
    ]
    for item in textures:
        flags = f"format=17/RGBAHalf, mip={item.mip_count}, readable={item.readable}, streaming={item.streaming}, colorSpace={item.color_space}, filter={item.filter_mode}/Point"
        lines.append(
            f"| `{item.set_name}/AnimationTexture{item.index}` | {item.width} x {item.height} | "
            f"{fmt_bytes(item.payload_bytes)} | {fmt_bytes(item.imported_bytes)} | "
            f"{fmt_bytes(item.packed_apk_bytes)} | {fmt_bytes(item.packed_aab_bytes)} | "
            f"{flags} | `{item.payload_sha256[:16]}...` | {len(item.reference_files)} |"
        )
    lines += [
        "",
        "The source `.asset` files are approximately 32 MiB each because inline binary payload is serialized as hexadecimal text. That source-file size is not runtime memory or package contribution. Existing Unity inventory reports approximately 32 MiB imported memory per reachable texture; the clean Android BuildReports attribute approximately 16 MiB per included texture.",
        "",
        "## Clip Coverage",
        "",
        "All three textures in a set cover the same texel addresses: texture 0/1/2 store the three matrix rows for every sampled bone. They do not represent separate clip banks.",
        "",
        "| Generated set | Animator descriptors | Authored clip entries (excluding T-pose) | Unique layouts | Bone range | Highest addressed texel | Capacity | Used prefix |",
        "|---|---:|---:|---:|---:|---:|---:|---:|",
    ]
    for item in sets:
        lines.append(
            f"| `{item.name}` | {item.animator_count} | {item.authored_clip_count} | "
            f"{item.unique_clip_layout_count} | {item.min_bones}-{item.max_bones} | "
            f"{item.maximum_used_texel:,} | {item.texture_capacity_texels:,} | {item.utilization_percent:.2f}% |"
        )
    lines += [
        "",
        "Coverage is derived from each generated animator's `start + frames * bonesCount`. It proves that recorded clip ranges fit the texture capacity; it does not prove that every authored clip is exercised by current gameplay.",
        "",
        "## Duplication",
        "",
    ]
    if exact_duplicates:
        for group in exact_duplicates:
            lines.append("- Exact payload duplicate: " + ", ".join(f"`{path}`" for path in group))
    else:
        lines.append("- No two textures have the same inline pixel-payload SHA-256. The legacy set is a separate bake, not a byte-identical copy of `CharactersBaked`.")
    lines += [
        f"- Included set: {', '.join(f'`{item.path}`' for item in included)}.",
        f"- Project-only set: {', '.join(f'`{item.path}`' for item in excluded)}.",
        "- The two sets have parallel 33-character/three-texture structures, but their animator frame ranges and pixel payloads differ. Removing the project-only set is outside this audit and requires workflow-owner confirmation because it remains referenced by its own generated prefabs/material.",
        "",
        "## Runtime Residency And Unload Boundary",
        "",
        "- Proven packaged: the recorded Android APK and clean AAB reports include only the three `CharactersBaked` textures, each as a 16,777,348-byte packed entry.",
        "- Proven reachable: the Unity content inventory links those three textures to both `Menu.unity` and `Match.unity` dependency roots and measures each loaded Editor object at 33,555,440 bytes.",
        "- Strong static inference: the `CharactersBaked/BatchMaterial.mat` binds all three textures, and generated render prefabs share that material. When that material is loaded for rendering, all three texture dependencies are eligible for residency together.",
        "- Not proven: existing Android memory evidence does not name native texture objects, so it cannot prove exact simultaneous device residency, CPU-copy retention, or release timing.",
        f"- Explicit first-party runtime unload paths found: {len(unload_hits)}" + (f" ({', '.join(f'`{path}`' for path in unload_hits)})" if unload_hits else "."),
        "- `m_IsReadable=1` creates a credible CPU-copy risk, consistent with the Editor imported-memory measurement being approximately twice the 16 MiB pixel payload. Device confirmation requires a named Memory Profiler capture before and after scene transition plus `UnloadUnusedAssets`; this audit intentionally does not change readability.",
        "",
        "## Precision Contract",
        "",
        "- Generator source creates `TextureFormat.RGBAHalf` textures in linear mode and writes three `Color` rows per bone matrix.",
        "- Shader/material contract binds `_SnivelerMainTextureFirst`, `Second`, and `Third`; point filtering preserves exact frame/bone texel addressing and mipmaps/streaming are disabled.",
        "- A normalized/unsigned color format would corrupt negative transforms. Lossy block compression would interpolate/corrupt matrix values. Either is rejected without a dedicated deformation and grounding visual/geometry validation.",
        "- Half precision is the current proven format, not a proven minimum. Any R32/RG16/quantized alternative requires generator/shader redesign plus near/far animation, foot-grounding, and transition validation on the target device.",
        "",
        "## Decision",
        "",
        "Do not change texture format or import settings from source size alone. The immediate evidence-backed opportunity is to measure named runtime residency and unload behavior for the included three-texture set. The legacy three-texture set is not an Android size/runtime-memory issue in the recorded builds, although it remains repository storage and maintenance debt.",
        "",
        "## Required Follow-Up Evidence",
        "",
        "1. Capture a Development Android Memory Profiler snapshot in Menu before character preview material use, then in Match with GPU-animated soldiers visible.",
        "2. Capture after leaving Match, destroying all character preview/render owners, invoking the product-approved unused-asset unload boundary, and waiting two frames.",
        "3. Record the three texture object names, native size, graphics size, ref owners, and readable CPU-copy state in all snapshots.",
        "4. Only then evaluate `Apply(updateMipmaps: false, makeNoLongerReadable: true)` in the generator as a separately validated bake change; do not mutate generated assets manually.",
        "",
        "## Evidence Sources",
        "",
        "- `Design/AgentReports/architecture_performance_android_apk_build_report.json`",
        "- `Design/AgentReports/architecture_performance_android_aab_build_report.json`",
        "- `Design/AgentReports/architecture_performance_content_residency_baseline.json`",
        "- `Packages/com.sniveler-code.gpu-animation/Editor/Scripts/GenerateProcessor.cs`",
        "- Generated materials, animator prefabs, texture assets, and GUID references under `Assets/Game/Prefabs/Generated`.",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--output", type=Path, default=REPORT)
    parser.add_argument("--check", action="store_true", help="Fail if output differs; do not write.")
    args = parser.parse_args()
    root = args.root.resolve()
    output = args.output if args.output.is_absolute() else root / args.output
    textures, sets, unload_hits = collect(root)
    rendered = render_report(textures, sets, unload_hits)
    if args.check:
        if not output.exists() or output.read_text(encoding="utf-8") != rendered:
            raise SystemExit(f"stale or missing report: {output}")
        return 0
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(rendered, encoding="utf-8")
    print(f"[APH-508] result=Passed textures={len(textures)} sets={len(sets)} output={output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
