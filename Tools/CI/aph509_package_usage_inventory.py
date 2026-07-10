#!/usr/bin/env python3
"""Read-only static package-usage evidence inventory for APH-509."""

from __future__ import annotations

import json
import re
import subprocess
from collections import defaultdict
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Packages/manifest.json"
LOCKFILE = ROOT / "Packages/packages-lock.json"
REPORT = ROOT / "Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md"
FIRST_PARTY_ROOTS = (
    ROOT / "Assets/Game/Scripts",
    ROOT / "Assets/Tests",
    ROOT / "Assets/Editor",
)
SERIALIZED_SUFFIXES = {
    ".anim", ".asset", ".controller", ".inputactions", ".mat", ".overridecontroller",
    ".playable", ".prefab", ".shadergraph", ".shadersubgraph", ".unity", ".vfx",
}
DOCUMENT_SUFFIXES = {".command", ".md", ".sh", ".yaml", ".yml"}
GUID_RE = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")

# Conservative API tokens: a hit is evidence to inspect, not automatic proof that
# the package owning the API can be independently removed or retained.
SOURCE_TOKENS = {
    "com.sniveler-code.gpu-animation": ("SnivelerCode.GpuAnimation",),
    "com.unity.burst": ("Unity.Burst", "[BurstCompile"),
    "com.unity.collections": ("Unity.Collections",),
    "com.unity.entities": ("Unity.Entities", "Unity.Transforms"),
    "com.unity.entities.graphics": ("Unity.Rendering", "EntitiesGraphics"),
    "com.unity.inputsystem": ("UnityEngine.InputSystem", "Unity.InputSystem"),
    "com.unity.mathematics": ("Unity.Mathematics",),
    "com.unity.probuilder": ("UnityEngine.ProBuilder", "UnityEditor.ProBuilder", "Unity.ProBuilder"),
    "com.unity.render-pipelines.core": ("UnityEngine.Rendering",),
    "com.unity.render-pipelines.universal": ("UnityEngine.Rendering.Universal", "Unity.RenderPipelines.Universal"),
    "com.unity.serialization": ("Unity.Serialization",),
    "com.unity.test-framework": ("NUnit.Framework", "UnityEngine.TestTools"),
    "com.unity.timeline": ("UnityEngine.Timeline", "UnityEngine.Playables"),
    "com.unity.ugui": ("UnityEngine.UI", "UnityEngine.EventSystems", "UnityEngine.UIElements"),
    "com.unity.visualscripting": ("Unity.VisualScripting",),
    "com.unity.modules.accessibility": ("UnityEngine.Accessibility",),
    "com.unity.modules.adaptiveperformance": ("UnityEngine.AdaptivePerformance",),
    "com.unity.modules.ai": ("UnityEngine.AI",),
    "com.unity.modules.androidjni": ("AndroidJava", "AndroidJNI"),
    "com.unity.modules.assetbundle": ("AssetBundle",),
    "com.unity.modules.audio": ("AudioSource", "AudioClip", "AudioMixer"),
    "com.unity.modules.cloth": ("Cloth",),
    "com.unity.modules.director": ("PlayableDirector",),
    "com.unity.modules.imageconversion": ("ImageConversion", "LoadImage("),
    "com.unity.modules.jsonserialize": ("JsonUtility",),
    "com.unity.modules.screencapture": ("ScreenCapture",),
    "com.unity.modules.tilemap": ("UnityEngine.Tilemaps",),
    "com.unity.modules.unityanalytics": ("UnityEngine.Analytics",),
    "com.unity.modules.unitywebrequest": ("UnityWebRequest",),
    "com.unity.modules.unitywebrequestassetbundle": ("UnityWebRequestAssetBundle",),
    "com.unity.modules.unitywebrequestaudio": ("UnityWebRequestMultimedia",),
    "com.unity.modules.unitywebrequesttexture": ("UnityWebRequestTexture",),
    "com.unity.modules.video": ("UnityEngine.Video", "VideoPlayer"),
    "com.unity.modules.vehicles": ("WheelCollider",),
    "com.unity.modules.xr": ("UnityEngine.XR",),
}


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def package_roots() -> dict[str, Path]:
    roots: dict[str, Path] = {}
    candidates = list((ROOT / "Packages").glob("*/package.json"))
    candidates += list((ROOT / "Library/PackageCache").glob("*/package.json"))
    for package_json in sorted(candidates):
        try:
            name = read_json(package_json).get("name")
        except (OSError, UnicodeDecodeError, json.JSONDecodeError):
            continue
        if name:
            roots[name] = package_json.parent
    return roots


def first_party_files(suffix: str) -> list[Path]:
    files: list[Path] = []
    for root in FIRST_PARTY_ROOTS:
        if root.exists():
            files.extend(root.rglob(f"*{suffix}"))
    return sorted(set(files))


def tracked_files(pathspec: str) -> list[Path]:
    result = subprocess.run(
        ["git", "-C", str(ROOT), "ls-files", "-z", "--", pathspec],
        check=True,
        stdout=subprocess.PIPE,
    )
    paths = result.stdout.decode("utf-8", errors="surrogateescape").split("\0")
    return [ROOT / path for path in paths if path]


def main() -> None:
    manifest = read_json(MANIFEST)["dependencies"]
    locked = read_json(LOCKFILE)["dependencies"]
    roots = package_roots()

    package_assemblies: dict[str, set[str]] = defaultdict(set)
    for package, package_root in roots.items():
        for asmdef in package_root.rglob("*.asmdef"):
            try:
                name = read_json(asmdef).get("name")
            except (OSError, UnicodeDecodeError, json.JSONDecodeError):
                continue
            if name:
                package_assemblies[package].add(name)

    assembly_consumers: dict[str, set[str]] = defaultdict(set)
    for asmdef in first_party_files(".asmdef"):
        data = read_json(asmdef)
        for reference in data.get("references", []):
            assembly_consumers[reference].add(str(asmdef.relative_to(ROOT)))

    source_text: dict[Path, str] = {}
    for source in first_party_files(".cs"):
        try:
            source_text[source] = source.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            pass

    source_hits: dict[str, set[Path]] = defaultdict(set)
    for package, tokens in SOURCE_TOKENS.items():
        for source, content in source_text.items():
            if any(token in content for token in tokens):
                source_hits[package].add(source)

    tracked_svg_metas = set(tracked_files("Assets/**/*.svg.meta"))
    svg_importer_assets: set[Path] = set()
    for svg in tracked_files("Assets/**/*.svg"):
        meta = Path(f"{svg}.meta")
        if meta not in tracked_svg_metas:
            continue
        try:
            content = meta.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        if re.search(r"^ScriptedImporter:\s*$", content, re.MULTILINE) and re.search(
            r"^  svgType:\s*\d+\s*$", content, re.MULTILINE
        ):
            svg_importer_assets.add(svg)

    importer_hits: dict[str, set[Path]] = defaultdict(set)
    importer_hits["com.unity.modules.vectorgraphics"] = svg_importer_assets

    guid_owner: dict[str, str] = {}
    for package, package_root in roots.items():
        for meta in package_root.rglob("*.meta"):
            try:
                match = re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$", meta.read_text(encoding="utf-8"), re.MULTILINE)
            except (OSError, UnicodeDecodeError):
                continue
            if match:
                guid_owner[match.group(1).lower()] = package

    serialized_hits: dict[str, set[Path]] = defaultdict(set)
    assets = ROOT / "Assets"
    for asset in assets.rglob("*"):
        if not asset.is_file() or asset.suffix.lower() not in SERIALIZED_SUFFIXES:
            continue
        try:
            content = asset.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            continue
        for guid in GUID_RE.findall(content):
            owner = guid_owner.get(guid.lower())
            if owner:
                serialized_hits[owner].add(asset)

    doc_files: list[Path] = []
    for relative in ("Design", "Tools", ".github"):
        base = ROOT / relative
        if base.exists():
            doc_files.extend(
                path for path in base.rglob("*")
                if path.is_file()
                and path != REPORT
                and path.suffix.lower() in DOCUMENT_SUFFIXES
            )
    for relative in ("README.md",):
        path = ROOT / relative
        if path.exists():
            doc_files.append(path)
    doc_text: dict[Path, str] = {}
    for path in sorted(set(doc_files)):
        try:
            if path.stat().st_size <= 5_000_000:
                doc_text[path] = path.read_text(encoding="utf-8")
        except (OSError, UnicodeDecodeError):
            pass

    reverse_dependencies: dict[str, set[str]] = defaultdict(set)
    for parent, data in locked.items():
        for dependency in data.get("dependencies", {}):
            reverse_dependencies[dependency].add(parent)

    all_packages = sorted(set(manifest) | set(locked))
    print("package\tpackage_state\tdepth\tsource\trequired_by\tasmdef_files\tsource_files\tserialized_files\timporter_assets\tdoc_files\texamples")
    for package in all_packages:
        assemblies = package_assemblies.get(package, set())
        asmdef_files = set()
        for assembly in assemblies:
            asmdef_files.update(assembly_consumers.get(assembly, set()))
        docs = {path for path, content in doc_text.items() if package in content}
        examples = []
        for paths in (
            asmdef_files,
            source_hits.get(package, set()),
            serialized_hits.get(package, set()),
            importer_hits.get(package, set()),
            docs,
        ):
            if paths:
                item = sorted(str(path.relative_to(ROOT)) if isinstance(path, Path) else path for path in paths)[0]
                examples.append(item)
        lock = locked.get(package, {})
        if package in manifest:
            package_state = "manifest-declared"
        elif lock.get("depth") == 0 and lock.get("source") == "embedded":
            package_state = "embedded-depth-zero-manifest-absent"
        else:
            package_state = "lock-only-transitive"
        row = (
            package,
            package_state,
            str(lock.get("depth", "missing")),
            str(lock.get("source", "missing")),
            str(len(reverse_dependencies.get(package, set()))),
            str(len(asmdef_files)),
            str(len(source_hits.get(package, set()))),
            str(len(serialized_hits.get(package, set()))),
            str(len(importer_hits.get(package, set()))),
            str(len(docs)),
            ";".join(examples[:4]),
        )
        print("\t".join(row))


if __name__ == "__main__":
    main()
