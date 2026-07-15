#!/usr/bin/env python3
"""Generate deterministic, read-only package-usage evidence for APH-509."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import subprocess
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MARKDOWN_REPORT = ROOT / "Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.md"
JSON_REPORT = ROOT / "Design/AgentReports/2026-07-10_aph-509_package_usage_inventory.json"
REPORT = MARKDOWN_REPORT
ORIGIN_REF = "origin/main"
EXPECTED_SUMMARY = {
    "totalPackageCount": 68,
    "manifestDeclaredCount": 47,
    "embeddedDepthZeroManifestAbsentCount": 1,
    "lockOnlyTransitiveCount": 20,
    "candidateUnusedStaticOnlyCount": 15,
    "unprovenStaticBlindSpotCount": 2,
}
CANDIDATE_REMOVAL_BLOCKERS = (
    "static-zero-evidence-is-not-runtime-proof",
    "isolated-package-resolution-not-run",
    "isolated-import-and-compile-not-run",
    "full-test-suite-not-run",
    "release-android-build-delta-not-measured",
    "release-device-smoke-not-run",
)
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
    "com.unity.modules.cloth": (
        "UnityEngine.Cloth", "ClothSkinningCoefficient", "ClothSphereColliderPair",
    ),
    "com.unity.modules.imageconversion": ("ImageConversion", "LoadImage("),
    "com.unity.modules.jsonserialize": ("JsonUtility",),
    "com.unity.modules.screencapture": ("ScreenCapture",),
    "com.unity.modules.tilemap": ("UnityEngine.Tilemaps",),
    "com.unity.modules.umbra": (
        "OcclusionArea", "OcclusionPortal", "StaticOcclusionCulling",
        "useOcclusionCulling", "allowOcclusionWhenDynamic",
    ),
    "com.unity.modules.unityanalytics": ("UnityEngine.Analytics",),
    "com.unity.modules.unitywebrequest": ("UnityWebRequest",),
    "com.unity.modules.unitywebrequestassetbundle": ("UnityWebRequestAssetBundle",),
    "com.unity.modules.unitywebrequestaudio": ("UnityWebRequestMultimedia",),
    "com.unity.modules.unitywebrequesttexture": ("UnityWebRequestTexture",),
    "com.unity.modules.video": ("UnityEngine.Video", "VideoPlayer"),
    "com.unity.modules.vehicles": ("WheelCollider",),
    "com.unity.modules.wind": ("UnityEngine.WindZone", "WindZoneMode"),
    "com.unity.modules.xr": ("UnityEngine.XR",),
    "com.unity.ide.rider": (
        "Unity.Rider.Editor", "Packages.Rider.Editor", "RiderScriptEditor",
    ),
    "com.unity.ide.visualstudio": (
        "Unity.VisualStudio.Editor", "VisualStudioEditor", "VisualStudioIntegration",
    ),
}
IDE_TRACKED_CONFIG_PREFIXES = {
    "com.unity.ide.rider": (".idea/",),
    "com.unity.ide.visualstudio": (".vs/", ".vsconfig"),
}
IDE_IGNORED_CONFIG_TOKENS = {
    "com.unity.ide.rider": (".idea/", "Rider"),
    "com.unity.ide.visualstudio": (".vs/", "Visual Studio"),
}
BUILTIN_SERIALIZED_PATTERNS = {
    "com.unity.modules.cloth": (
        re.compile(r"(?m)^--- !u!183\b[^\r\n]*\r?\nCloth:\s*$"),
    ),
    "com.unity.modules.umbra": (
        re.compile(r"(?m)^--- !u!41\b[^\r\n]*\r?\nOcclusionPortal:\s*$"),
        re.compile(r"(?m)^--- !u!192\b[^\r\n]*\r?\nOcclusionArea:\s*$"),
        re.compile(r"(?m)^--- !u!363\b[^\r\n]*\r?\nOcclusionCullingData:\s*$"),
        re.compile(r"m_OcclusionCullingData:\s*\{fileID:\s*(?!0(?:\D|$))-?\d+"),
    ),
    "com.unity.modules.wind": (
        re.compile(r"(?m)^--- !u!182\b[^\r\n]*\r?\nWindZone:\s*$"),
    ),
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
    ambiguous_files: set[str] = field(default_factory=set)

    @property
    def evidence_count(self) -> int:
        return sum(map(len, (
            self.source_files, self.serialized_files,
            self.build_files, self.editor_files,
        )))

    @property
    def ambiguity_count(self) -> int:
        return len(self.ambiguous_files)


def read_json(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


def sha256_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def read_git_object(root: Path, object_name: str) -> bytes | None:
    result = subprocess.run(
        ["git", "-C", str(root), "show", object_name],
        check=False,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    return result.stdout if result.returncode == 0 else None


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


def collect_static_probe_evidence(
    root: Path,
    paths: list[Path],
    evidence: dict[str, Evidence],
) -> None:
    gitignore = root / ".gitignore"
    if gitignore in paths:
        content = read_small_text(gitignore)
        for package, tokens in IDE_IGNORED_CONFIG_TOKENS.items():
            item = evidence.get(package)
            if item and contains_any(content, tokens):
                item.ambiguous_files.add(".gitignore")

    for path in paths:
        relative = path.relative_to(root).as_posix()
        for package, prefixes in IDE_TRACKED_CONFIG_PREFIXES.items():
            item = evidence.get(package)
            if item and any(
                relative == prefix or (prefix.endswith("/") and relative.startswith(prefix))
                for prefix in prefixes
            ):
                item.editor_files.add(relative)

        if (
            not relative.startswith(("Assets/", "ProjectSettings/"))
            or path.suffix.lower() not in {".asset", ".prefab", ".unity"}
        ):
            continue
        content = read_small_text(path)
        for package, patterns in BUILTIN_SERIALIZED_PATTERNS.items():
            item = evidence.get(package)
            if item and any(pattern.search(content) for pattern in patterns):
                item.serialized_files.add(relative)


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

    collect_static_probe_evidence(root, paths, evidence)

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
    if item.ambiguity_count:
        return "unproven-static-blind-spot"
    if item.state == "manifest-declared":
        return "candidate-unused-static-only"
    return "no-first-party-evidence"


def summarize(items: list[Evidence]) -> dict[str, int]:
    return {
        "totalPackageCount": len(items),
        "manifestDeclaredCount": sum(item.state == "manifest-declared" for item in items),
        "embeddedDepthZeroManifestAbsentCount": sum(
            item.state == "embedded-depth-zero-manifest-absent" for item in items
        ),
        "lockOnlyTransitiveCount": sum(item.state == "lock-only-transitive" for item in items),
        "candidateUnusedStaticOnlyCount": sum(
            classification(item) == "candidate-unused-static-only" for item in items
        ),
        "unprovenStaticBlindSpotCount": sum(
            classification(item) == "unproven-static-blind-spot" for item in items
        ),
    }


def summary_validation_errors(
    summary: dict[str, int],
    expected: dict[str, int] = EXPECTED_SUMMARY,
) -> list[str]:
    return [
        f"summary-mismatch:{key}:expected={value}:actual={summary.get(key)}"
        for key, value in expected.items()
        if summary.get(key) != value
    ]


def removal_blockers(item: Evidence) -> list[str]:
    result = classification(item)
    if result == "candidate-unused-static-only":
        return list(CANDIDATE_REMOVAL_BLOCKERS)
    if result == "unproven-static-blind-spot":
        return ["static-analysis-blind-spot-unresolved", *CANDIDATE_REMOVAL_BLOCKERS]
    if result == "usage-evidence-found":
        return ["first-party-usage-evidence-found"]
    if result == "dependency-graph-required":
        return ["current-lock-graph-requires-package"]
    return ["ordinary-lock-only-transitive-not-directly-removable"]


def package_row(item: Evidence) -> dict[str, object]:
    return {
        "package": item.package,
        "state": item.state,
        "version": item.version,
        "depth": item.depth,
        "source": item.source,
        "classification": classification(item),
        "sourceFiles": sorted(item.source_files),
        "serializedFiles": sorted(item.serialized_files),
        "buildFiles": sorted(item.build_files),
        "editorFiles": sorted(item.editor_files),
        "ambiguousFiles": sorted(item.ambiguous_files),
        "requiredBy": sorted(item.required_by),
        "removalAuthorized": False,
        "removalBlockers": removal_blockers(item),
    }


def build_report_data(root: Path = ROOT) -> dict[str, object]:
    items = collect(root)
    summary = summarize(items)
    manifest_bytes = (root / "Packages/manifest.json").read_bytes()
    lock_bytes = (root / "Packages/packages-lock.json").read_bytes()
    origin_manifest = read_git_object(root, f"{ORIGIN_REF}:Packages/manifest.json")
    origin_lock = read_git_object(root, f"{ORIGIN_REF}:Packages/packages-lock.json")
    origin_available = origin_manifest is not None and origin_lock is not None
    origin_matches = (
        origin_available
        and manifest_bytes == origin_manifest
        and lock_bytes == origin_lock
    )
    validation_errors = summary_validation_errors(summary)
    if not origin_available:
        validation_errors.append("origin-main-package-inputs-unavailable")
    elif not origin_matches:
        validation_errors.append("worktree-package-inputs-differ-from-origin-main")
    rows = [package_row(item) for item in sorted(items, key=lambda value: value.package)]
    return {
        "schemaVersion": 1,
        "taskId": "APH-509",
        "status": "current-removal-blocked" if not validation_errors else "invalid-removal-blocked",
        "inventoryValid": not validation_errors,
        "packageRemovalAuthorized": False,
        "validationErrors": validation_errors,
        "expectedSummary": dict(EXPECTED_SUMMARY),
        "summary": summary,
        "inputEvidence": {
            "manifestPath": "Packages/manifest.json",
            "manifestSha256": sha256_bytes(manifest_bytes),
            "lockPath": "Packages/packages-lock.json",
            "lockSha256": sha256_bytes(lock_bytes),
            "auditedAgainstRef": ORIGIN_REF,
            "originPackageInputsAvailable": origin_available,
            "originPackageInputsMatch": origin_matches,
            "originManifestSha256": sha256_bytes(origin_manifest) if origin_manifest is not None else None,
            "originLockSha256": sha256_bytes(origin_lock) if origin_lock is not None else None,
            "evidenceChannels": [
                "first-party-source-and-asmdef",
                "text-serialized-guid-ownership",
                "built-in-module-source-and-yaml-signatures",
                "build-automation",
                "editor-workflows",
                "external-editor-configuration-ambiguity",
                "manifest-lock-reverse-dependencies",
            ],
        },
        "candidatePackages": [
            row["package"]
            for row in rows
            if row["classification"] == "candidate-unused-static-only"
        ],
        "staticBlindSpotPackages": [
            row["package"]
            for row in rows
            if row["classification"] == "unproven-static-blind-spot"
        ],
        "candidateRemovalGate": {
            "authorized": False,
            "blockers": list(CANDIDATE_REMOVAL_BLOCKERS),
        },
        "packages": rows,
    }


def render_json(data: dict[str, object]) -> str:
    return json.dumps(data, indent=2, sort_keys=False) + "\n"


def row_example(row: dict[str, object]) -> str:
    values = sorted(
        row["sourceFiles"]
        + row["serializedFiles"]
        + row["buildFiles"]
        + row["editorFiles"]
        + row["ambiguousFiles"]
        + row["requiredBy"]
    )
    return values[0] if values else "-"


def render_report(data: dict[str, object] | list[Evidence]) -> str:
    if isinstance(data, list):
        items = sorted(data, key=lambda value: value.package)
        summary = summarize(items)
        rows = [package_row(item) for item in items]
        expected = summary
        input_evidence = {
            "manifestSha256": "not-collected",
            "lockSha256": "not-collected",
            "auditedAgainstRef": "not-collected",
            "originPackageInputsMatch": False,
        }
        inventory_valid = False
        candidate_packages = [
            row["package"] for row in rows
            if row["classification"] == "candidate-unused-static-only"
        ]
    else:
        summary = data["summary"]
        rows = data["packages"]
        expected = data["expectedSummary"]
        input_evidence = data["inputEvidence"]
        inventory_valid = data["inventoryValid"]
        candidate_packages = data["candidatePackages"]
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
        "- Built-in Cloth, Umbra, and Wind source/YAML signatures whose packages do not expose stable GUID ownership.",
        "- Build automation under `Tools/CI`, `.github`, build-named editor files, and Jenkins.",
        "- Editor workflows from editor source, package/assembly mentions, documentation, external IDE configuration, and SVG importer state.",
        "- Manifest/lock state and reverse package dependencies.",
        "",
        "## Summary",
        "",
        f"- Inventory valid: **{str(inventory_valid).lower()}**",
        "- Package removal authorized: **false**",
        f"- Total package graph entries: **{summary['totalPackageCount']}**",
        f"- Manifest-declared packages: **{summary['manifestDeclaredCount']}**",
        "- Embedded depth-zero manifest discrepancies: "
        f"**{summary['embeddedDepthZeroManifestAbsentCount']}**",
        f"- Ordinary lock-only transitives: **{summary['lockOnlyTransitiveCount']}**",
        "- Static-only candidate-unused declarations: "
        f"**{summary['candidateUnusedStaticOnlyCount']}**",
        f"- Unproven static blind spots: **{summary['unprovenStaticBlindSpotCount']}**",
        "",
        "## Accepted Current Count Contract",
        "",
        f"- Total graph entries: `{expected['totalPackageCount']}`",
        f"- Manifest declarations: `{expected['manifestDeclaredCount']}`",
        f"- Embedded depth-zero discrepancies: `{expected['embeddedDepthZeroManifestAbsentCount']}`",
        f"- Lock-only transitives: `{expected['lockOnlyTransitiveCount']}`",
        f"- Static-only candidates: `{expected['candidateUnusedStaticOnlyCount']}`",
        f"- Static blind spots: `{expected['unprovenStaticBlindSpotCount']}`",
        "",
        "## Audited Inputs",
        "",
        f"- Manifest SHA-256: `{input_evidence['manifestSha256']}`",
        f"- Lock SHA-256: `{input_evidence['lockSha256']}`",
        f"- Upstream comparison ref: `{input_evidence['auditedAgainstRef']}`",
        "- Worktree package inputs match upstream: "
        f"`{str(input_evidence['originPackageInputsMatch']).lower()}`",
        "",
        "## Candidate Removal Blockers",
        "",
        "No candidate is approved for removal. Static absence only starts an isolated validation lane.",
        "",
        "| Candidate | Removal authorized | Blocking evidence still required |",
        "|---|---|---|",
    ]
    row_by_package = {row["package"]: row for row in rows}
    for package in candidate_packages:
        blockers = ", ".join(row_by_package[package]["removalBlockers"])
        lines.append(f"| `{package}` | false | {blockers} |")
    lines += [
        "",
        "## Deterministic Evidence",
        "",
        "| Package | State | Classification | Source | Serialized | Build | Editor | Ambiguous | Required by | Example |",
        "|---|---|---|---:|---:|---:|---:|---:|---:|---|",
    ]
    for row in rows:
        lines.append(
            f"| `{row['package']}` | {row['state']} | {row['classification']} | "
            f"{len(row['sourceFiles'])} | {len(row['serializedFiles'])} | "
            f"{len(row['buildFiles'])} | {len(row['editorFiles'])} | "
            f"{len(row['ambiguousFiles'])} | {len(row['requiredBy'])} | "
            f"`{row_example(row)}` |"
        )
    lines += [
        "",
        "## Fail-Closed Limitations",
        "",
        "- Binary assets, reflection, generated code, native plugins, shader includes, and untracked external editor services can evade static attribution.",
        "- Explicit built-in component probes cover known Cloth, Umbra, and Wind source/YAML signatures; unknown signatures remain a fail-closed limitation.",
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
        "python3 Tools/CI/aph509_package_usage_inventory.py --json",
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
    mode.add_argument("--json", action="store_true")
    args = parser.parse_args()
    data = build_report_data(ROOT)
    rendered_markdown = render_report(data)
    rendered_json = render_json(data)
    if args.write_report:
        MARKDOWN_REPORT.write_text(rendered_markdown, encoding="utf-8")
        JSON_REPORT.write_text(rendered_json, encoding="utf-8")
        print(MARKDOWN_REPORT.relative_to(ROOT))
        print(JSON_REPORT.relative_to(ROOT))
        return 0 if data["inventoryValid"] else 1
    if args.check:
        stale = []
        for path, rendered in (
            (MARKDOWN_REPORT, rendered_markdown),
            (JSON_REPORT, rendered_json),
        ):
            if not path.exists() or path.read_text(encoding="utf-8") != rendered:
                stale.append(path)
                print(f"stale: {path.relative_to(ROOT)}")
            else:
                print(f"current: {path.relative_to(ROOT)}")
        summary = data["summary"]
        print(
            "counts: "
            f"total={summary['totalPackageCount']} "
            f"manifest={summary['manifestDeclaredCount']} "
            f"candidates={summary['candidateUnusedStaticOnlyCount']} "
            f"blind_spots={summary['unprovenStaticBlindSpotCount']} "
            "removal_authorized=false"
        )
        return 0 if not stale and data["inventoryValid"] else 1
    if args.json:
        print(rendered_json, end="")
        return 0
    print(rendered_markdown, end="")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
