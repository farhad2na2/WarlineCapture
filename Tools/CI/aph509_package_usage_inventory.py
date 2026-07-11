#!/usr/bin/env python3
"""Generate deterministic, read-only package-usage evidence for APH-509."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
REPORT = ROOT / "Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md"
SERIALIZED_SUFFIXES = {
    ".anim", ".asset", ".controller", ".inputactions", ".mat",
    ".overridecontroller", ".playable", ".prefab", ".shadergraph",
    ".shadersubgraph", ".unity", ".vfx",
}
GUID_RE = re.compile(r"\bguid:\s*([0-9a-fA-F]{32})\b")
SOURCE_TOKENS = {
    "com.sniveler-code.gpu-animation": ("SnivelerCode.GpuAnimation",),
    "com.unity.burst": ("Unity.Burst", "[BurstCompile"),
    "com.unity.collections": ("Unity.Collections",),
    "com.unity.entities": ("Unity.Entities", "Unity.Transforms"),
    "com.unity.entities.graphics": ("Unity.Rendering", "EntitiesGraphics"),
    "com.unity.inputsystem": ("UnityEngine.InputSystem", "Unity.InputSystem"),
    "com.unity.mathematics": ("Unity.Mathematics",),
    "com.unity.probuilder": ("UnityEngine.ProBuilder", "UnityEditor.ProBuilder"),
    "com.unity.render-pipelines.core": ("UnityEngine.Rendering",),
    "com.unity.render-pipelines.universal": ("UnityEngine.Rendering.Universal",),
    "com.unity.test-framework": ("NUnit.Framework", "UnityEngine.TestTools"),
    "com.unity.timeline": ("UnityEngine.Timeline",),
    "com.unity.ugui": ("UnityEngine.UI", "UnityEngine.EventSystems"),
    "com.unity.visualscripting": ("Unity.VisualScripting",),
    "com.unity.modules.adaptiveperformance": ("UnityEngine.AdaptivePerformance",),
    "com.unity.modules.ai": ("UnityEngine.AI",),
    "com.unity.modules.androidjni": ("AndroidJava", "AndroidJNI"),
    "com.unity.modules.audio": ("AudioSource", "AudioClip", "AudioMixer"),
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
STATIC_BLIND_SPOT_PACKAGES = {
    "com.unity.ide.rider",
    "com.unity.ide.visualstudio",
    "com.unity.modules.cloth",
    "com.unity.modules.umbra",
    "com.unity.modules.wind",
}


@dataclass
class Evidence:
    package: str
    state: str
    version: str
    depth: int | str
    source: str
    required_by: set[str] = field(default_factory=set)
    source_files: set[str] = field(default_factory=set)
    serialized_files: set[str] = field(default_factory=set)
    build_files: set[str] = field(default_factory=set)
    editor_files: set[str] = field(default_factory=set)

    @property
    def evidence_count(self) -> int:
        return sum(map(len, (
            self.source_files, self.serialized_files,
            self.build_files, self.editor_files,
        )))


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def run_git(root: Path, *args: str, allow_empty: bool = False) -> bytes:
    result = subprocess.run(
        ["git", "-C", str(root), *args],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    if result.returncode and not (allow_empty and result.returncode == 1):
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace"))
    return result.stdout


def tracked_paths(root: Path) -> list[Path]:
    output = run_git(root, "ls-files", "-z")
    return [root / item for item in output.decode("utf-8", errors="surrogateescape").split("\0") if item]


def package_roots(root: Path, packages: set[str]) -> dict[str, Path]:
    roots: dict[str, Path] = {}
    candidates = list((root / "Packages").glob("*/package.json"))
    candidates += list((root / "Library/PackageCache").glob("*/package.json"))
    for package_json in sorted(candidates):
        try:
            name = read_json(package_json).get("name")
        except (OSError, UnicodeDecodeError, json.JSONDecodeError):
            continue
        if name in packages:
            # Embedded packages intentionally override cache copies.
            if name not in roots or "Packages" in package_json.parts:
                roots[name] = package_json.parent
    return roots


def package_metadata(roots: dict[str, Path]) -> tuple[dict[str, set[str]], dict[str, str]]:
    assemblies: dict[str, set[str]] = defaultdict(set)
    guid_owner: dict[str, str] = {}
    for package, package_root in sorted(roots.items()):
        for asmdef in sorted(package_root.rglob("*.asmdef")):
            try:
                name = read_json(asmdef).get("name")
            except (OSError, UnicodeDecodeError, json.JSONDecodeError):
                continue
            if name:
                assemblies[package].add(name)
        for meta in sorted(package_root.rglob("*.meta")):
            try:
                with meta.open(encoding="utf-8", errors="ignore") as handle:
                    for line in handle:
                        if line.startswith("guid:"):
                            match = GUID_RE.search(line)
                            if match:
                                guid_owner[match.group(1).lower()] = package
                            break
            except OSError:
                continue
    return assemblies, guid_owner


def read_small_text(path: Path, limit: int = 5_000_000) -> str:
    try:
        if path.stat().st_size > limit:
            return ""
        return path.read_text(encoding="utf-8", errors="ignore")
    except OSError:
        return ""


def contains_any(content: str, tokens: set[str] | tuple[str, ...]) -> bool:
    return any(token and token in content for token in tokens)


def collect(root: Path) -> list[Evidence]:
    manifest = read_json(root / "Packages/manifest.json")["dependencies"]
    locked = read_json(root / "Packages/packages-lock.json")["dependencies"]
    packages = set(manifest) | set(locked)
    paths = tracked_paths(root)
    roots = package_roots(root, packages)
    assemblies, guid_owner = package_metadata(roots)

    reverse: dict[str, set[str]] = defaultdict(set)
    for parent, data in locked.items():
        for dependency in data.get("dependencies", {}):
            reverse[dependency].add(parent)

    evidence: dict[str, Evidence] = {}
    for package in sorted(packages):
        lock = locked.get(package, {})
        if package in manifest:
            state = "manifest-declared"
            version = str(manifest[package])
        elif lock.get("depth") == 0 and lock.get("source") == "embedded":
            state = "embedded-depth-zero-manifest-absent"
            version = str(lock.get("version", "missing"))
        else:
            state = "lock-only-transitive"
            version = str(lock.get("version", "missing"))
        evidence[package] = Evidence(
            package=package,
            state=state,
            version=version,
            depth=lock.get("depth", "missing"),
            source=str(lock.get("source", "missing")),
            required_by=set(reverse.get(package, set())),
        )

    assembly_owner = {
        assembly: package
        for package, names in assemblies.items()
        for assembly in names
    }
    source_paths = []
    for path in paths:
        relative = path.relative_to(root).as_posix()
        if path.suffix == ".cs" and relative.startswith(
            ("Assets/Game/Scripts/", "Assets/Tests/", "Assets/Editor/")
        ):
            source_paths.append(path)
    for path in source_paths:
        relative = path.relative_to(root).as_posix()
        content = read_small_text(path)
        is_editor = "/Editor/" in f"/{relative}/" or relative.startswith("Assets/Editor/")
        for package in sorted(packages):
            tokens = set(SOURCE_TOKENS.get(package, ())) | assemblies.get(package, set())
            if contains_any(content, tokens):
                target = evidence[package].editor_files if is_editor else evidence[package].source_files
                target.add(relative)

    for path in (item for item in paths if item.suffix == ".asmdef"):
        relative = path.relative_to(root).as_posix()
        try:
            references = read_json(path).get("references", [])
        except (OSError, UnicodeDecodeError, json.JSONDecodeError):
            continue
        is_editor = "/Editor/" in f"/{relative}/"
        for reference in references:
            owner = assembly_owner.get(reference)
            if owner:
                target = evidence[owner].editor_files if is_editor else evidence[owner].source_files
                target.add(relative)

    grep_output = run_git(
        root, "grep", "-I", "-n", "-E", r"guid:[[:space:]]*[0-9a-fA-F]{32}", "--", "Assets",
        allow_empty=True,
    ).decode("utf-8", errors="ignore")
    for line in grep_output.splitlines():
        relative, _, content = line.partition(":")
        path = Path(relative)
        if path.suffix.lower() not in SERIALIZED_SUFFIXES:
            continue
        for guid in GUID_RE.findall(content):
            owner = guid_owner.get(guid.lower())
            if owner:
                evidence[owner].serialized_files.add(path.as_posix())

    text_suffixes = {".cs", ".command", ".groovy", ".json", ".md", ".ps1", ".sh", ".yaml", ".yml"}
    workflow_paths = [
        path for path in paths
        if path.suffix.lower() in text_suffixes
        and (
            path.name.startswith("Jenkinsfile")
            or "Tools/CI" in path.relative_to(root).as_posix()
            or ".github" in path.parts
            or "Design" in path.parts
            or "/Editor/" in f"/{path.relative_to(root).as_posix()}/"
        )
        and not path.relative_to(root).as_posix().startswith(
            ("Design/AgentReports/", "Design/Archive/", "Design/Archived/")
        )
        and path != REPORT
        and path != Path(__file__).resolve()
    ]
    for path in workflow_paths:
        relative = path.relative_to(root).as_posix()
        content = read_small_text(path)
        is_build = (
            path.name.startswith("Jenkinsfile")
            or relative.startswith("Tools/CI/")
            or relative.startswith(".github/")
            or "Build" in path.name
        )
        for package in sorted(packages):
            if relative.startswith("Design/") or relative == "README.md":
                tokens = {package}
            else:
                tokens = {package} | assemblies.get(package, set()) | set(SOURCE_TOKENS.get(package, ()))
            if contains_any(content, tokens):
                target = evidence[package].build_files if is_build else evidence[package].editor_files
                target.add(relative)

    # SVG ScriptedImporter state is direct editor-workflow proof for Vector Graphics.
    vector = evidence.get("com.unity.modules.vectorgraphics")
    if vector:
        tracked = {path.relative_to(root).as_posix() for path in paths}
        for svg in (path for path in paths if path.suffix.lower() == ".svg"):
            meta_relative = f"{svg.relative_to(root).as_posix()}.meta"
            if meta_relative in tracked:
                content = read_small_text(root / meta_relative)
                if "ScriptedImporter:" in content and re.search(r"^  svgType:\s*\d+", content, re.MULTILINE):
                    vector.editor_files.add(svg.relative_to(root).as_posix())

    return [evidence[package] for package in sorted(evidence)]


def classification(item: Evidence) -> str:
    if item.evidence_count:
        return "usage-evidence-found"
    if item.required_by:
        return "dependency-graph-required"
    if item.package in STATIC_BLIND_SPOT_PACKAGES:
        return "unproven-static-blind-spot"
    if item.state == "manifest-declared":
        return "candidate-unused-static-only"
    return "no-first-party-evidence"


def example(item: Evidence) -> str:
    values = sorted(
        item.source_files | item.serialized_files | item.build_files | item.editor_files | item.required_by
    )
    return values[0] if values else "-"


def render_report(items: list[Evidence]) -> str:
    direct = sum(item.state == "manifest-declared" for item in items)
    embedded = sum(item.state == "embedded-depth-zero-manifest-absent" for item in items)
    transitive = sum(item.state == "lock-only-transitive" for item in items)
    candidates = sum(classification(item) == "candidate-unused-static-only" for item in items)
    unproven = sum(classification(item) == "unproven-static-blind-spot" for item in items)
    lines = [
        "# APH-509 Package Usage Inventory",
        "",
        "This is deterministic, read-only static evidence. It does not approve package removal.",
        "A candidate still requires isolated import, compile, test, Android build, and device validation.",
        "",
        "## Coverage",
        "",
        "- First-party source and asmdef references under `Assets/`.",
        "- Text-serialized asset references resolved through package `.meta` GUID ownership.",
        "- Build automation under `Tools/CI`, `.github`, build-named editor files, and Jenkins.",
        "- Editor workflows from editor source, package/assembly mentions, documentation, and SVG importer state.",
        "- Manifest/lock state and reverse package dependencies.",
        "",
        "## Summary",
        "",
        f"- Manifest-declared packages: **{direct}**",
        f"- Embedded depth-zero manifest discrepancies: **{embedded}**",
        f"- Ordinary lock-only transitives: **{transitive}**",
        f"- Static-only candidate-unused declarations: **{candidates}**",
        f"- Unproven static blind spots: **{unproven}**",
        "",
        "## Deterministic Evidence",
        "",
        "| Package | State | Classification | Source | Serialized | Build | Editor | Required by | Example |",
        "|---|---|---|---:|---:|---:|---:|---:|---|",
    ]
    for item in sorted(items, key=lambda value: value.package):
        lines.append(
            f"| `{item.package}` | {item.state} | {classification(item)} | "
            f"{len(item.source_files)} | {len(item.serialized_files)} | "
            f"{len(item.build_files)} | {len(item.editor_files)} | "
            f"{len(item.required_by)} | `{example(item)}` |"
        )
    lines += [
        "",
        "## Fail-Closed Limitations",
        "",
        "- Built-in component class IDs, binary assets, reflection, generated code, native plugins, shader includes, and external editor services can evade static attribution.",
        "- Namespace and workflow text matches are evidence to inspect, not proof that every reference is semantically required.",
        "- A zero in all columns means only that this inventory found no static evidence. It never proves runtime safety.",
        "- Lock reverse dependencies describe the current graph; Unity must resolve the lock after any isolated manifest experiment.",
        "",
        "## Removal Gate",
        "",
        "1. Change one manifest declaration in an isolated clean worktree; never hand-edit ordinary lock-only transitives.",
        "2. Complete clean package resolution/import and require zero compile, import, missing-script, and missing-shader errors.",
        "3. Run full EditMode/PlayMode coverage and the affected editor workflows.",
        "4. Produce the release-equivalent Android build and compare BuildReport, warnings, assemblies, shaders, and size.",
        "5. Run device startup, menu, Match, input, rendering, audio, networking/content, and thermal smoke coverage.",
        "6. Retain the package when evidence is ambiguous; removal requires separate review and measured proof.",
        "",
        "## Reproduction",
        "",
        "```sh",
        "python3 Tools/CI/aph509_package_usage_inventory.py --check",
        "python3 Tools/CI/aph509_package_usage_inventory.py --write-report",
        "python3 -m unittest Tools.CI.tests.test_aph509_package_usage_inventory",
        "```",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--write-report", action="store_true")
    mode.add_argument("--check", action="store_true")
    args = parser.parse_args()
    rendered = render_report(collect(ROOT))
    if args.write_report:
        REPORT.write_text(rendered, encoding="utf-8")
        print(REPORT.relative_to(ROOT))
        return 0
    if args.check:
        if not REPORT.exists() or REPORT.read_text(encoding="utf-8") != rendered:
            print(f"stale: {REPORT.relative_to(ROOT)}")
            return 1
        print(f"current: {REPORT.relative_to(ROOT)}")
        return 0
    print(rendered, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
