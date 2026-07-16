#!/usr/bin/env python3
"""Generate the deterministic AM-006 architecture ownership inventory."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import Counter
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
DEFAULT_JSON = "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
DEFAULT_MARKDOWN = "Design/AgentReports/ArchitectureMaturity/ownership_inventory.md"
SOURCE_ROOT = "Assets/Game/Scripts"
ASMDEF_ROOTS = ("Assets/Editor", "Assets/Game", "Assets/Tests")

AUTHORITY_PATHS = (
    "Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json",
    "Design/AgentReports/ArchitectureMaturity/validator_registry.json",
    "Design/Architecture/non_ecs_to_ecs_system_inventory.md",
    "Design/Architecture/phase7_monobehaviour_loop_baseline.md",
    "Design/Architecture/production_source_growth_baseline.md",
    "Design/Architecture/systembase_to_isystem_inventory.md",
)

ACTIVE_WORK_OWNERS = (
    {
        "authorityPath": "Design/Architecture/operation_map_scene_split_and_generator_tracker.md",
        "id": "operation-map",
        "protectedPaths": [
            "Assets/Game/Scenes/Game/OperationMaps/**",
            "Assets/Game/Scripts/**/*OperationMap*.cs",
            "Design/AgentReports/**/*operation_map*",
            "Design/Architecture/operation_map_*.md",
        ],
        "status": "active",
    },
    {
        "authorityPath": "Design/Audio_Config_Driven_Implementation_Spec.md",
        "id": "audio",
        "protectedPaths": [
            "Assets/Game/Scripts/Audio/**",
            "Assets/Game/Scripts/Components/AudioComponents.cs",
            "Assets/Game/Scripts/Configs/Audio/**",
            "Assets/Game/Scripts/UI/**/*Audio*.cs",
            "Design/Audio_Config_Driven_Implementation_Spec.md",
        ],
        "status": "active",
    },
    {
        "authorityPath": "Design/Architecture/first_launch_architecture_alignment_refactor_tracker.md",
        "id": "first-launch",
        "protectedPaths": [
            "Assets/Game/Scripts/**/FirstLaunch*.cs",
            "Design/Architecture/first_launch_architecture_alignment_refactor_tracker.md",
            "Design/NarrativeVision/FirstLaunch/**",
        ],
        "status": "complete-protected",
    },
    {
        "authorityPath": "Design/AgentTasks/ui_current.md",
        "id": "ui-visual-lock",
        "protectedPaths": [
            "Assets/Game/Art/UI/Generated/**",
            "Assets/Game/Prefabs/UI/**",
            "Design/AgentTasks/ui_current.md",
            "Design/VisualLockLayered/**",
        ],
        "status": "held-protected",
    },
)

OWNER_DOMAINS = (
    {
        "currentOwnerValidatorIds": ["architecture-source-growth"],
        "id": "source-size",
        "responsibility": "Production source line/byte ceilings and oversized-file ratchets.",
    },
    {
        "currentOwnerValidatorIds": ["architecture-assembly-boundary", "architecture-assembly-report"],
        "id": "assembly-dependencies",
        "responsibility": "First-party asmdef dependency direction, parity, and cycle prevention.",
    },
    {
        "currentOwnerValidatorIds": ["architecture-composition-static", "architecture-managed-ecs-loops"],
        "id": "runtime-loops-static-state",
        "responsibility": "Managed runtime loops, mutable static candidates, and composition ownership.",
    },
    {
        "currentOwnerValidatorIds": ["architecture-managed-ecs-loops", "architecture-source-growth"],
        "id": "managed-helpers",
        "responsibility": "SystemHelper inventory, managed-boundary justification, and growth control.",
    },
    {
        "currentOwnerValidatorIds": ["architecture-dashboard-freshness"],
        "id": "active-work-exclusions",
        "responsibility": "Cross-agent ownership exclusions and conflict prevention.",
    },
)

TYPE_DECLARATION_RE = re.compile(
    r"^[ \t]*(?:(?:\[[^\]\r\n]*(?:\r?\n[ \t]*\[[^\]\r\n]*)*\][ \t]*)\r?\n[ \t]*)*"
    r"(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|unsafe)\s+)*"
    r"class\s+(?P<name>[A-Za-z_]\w*)(?:\s*<[^>{};\r\n]+>)?\s*(?P<bases>:[^{;]+)?",
    re.MULTILINE,
)
LOOP_METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|virtual|override|sealed|async)\s+)*"
    r"(?:(?:void\s+(?P<update>Update|LateUpdate|FixedUpdate))|"
    r"(?:IEnumerator\s+(?P<coroutine>[A-Za-z_]\w*)))\s*\(",
    re.MULTILINE,
)
MUTABLE_STATIC_FIELD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|new|unsafe|volatile)\s+)*"
    r"static\s+(?!readonly\b)(?!class\b)(?!partial\b)(?!void\b)"
    r"(?P<type>[A-Za-z_]\w*(?:[.<>,?\[\] ]*[A-Za-z0-9_>\]])?)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*(?:=(?!>)|;)",
    re.MULTILINE,
)


@dataclass(frozen=True)
class SourceEntry:
    path: str
    lines: int
    bytes: int


@dataclass(frozen=True)
class LoopEntry:
    path: str
    type: str
    method: str
    line: int
    scope: str


@dataclass(frozen=True)
class StaticEntry:
    path: str
    type: str
    name: str
    line: int


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(root: Path, path: Path) -> str:
    return path.relative_to(root).as_posix()


def source_scope(path: str) -> str:
    if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
        return "editor"
    if path.startswith("Assets/Game/Scripts/UI/"):
        return "production-ui"
    return "production-non-ui"


def strip_comments_and_strings(text: str) -> str:
    output: list[str] = []
    index = 0
    state = "code"
    while index < len(text):
        char = text[index]
        next_char = text[index + 1] if index + 1 < len(text) else ""
        if state == "line-comment":
            if char == "\n":
                state = "code"
                output.append(char)
            else:
                output.append(" ")
        elif state == "block-comment":
            if char == "*" and next_char == "/":
                output.extend((" ", " "))
                index += 1
                state = "code"
            else:
                output.append("\n" if char == "\n" else " ")
        elif state in ("string", "char"):
            quote = '"' if state == "string" else "'"
            if char == "\\":
                output.append(" ")
                if index + 1 < len(text):
                    output.append(" ")
                    index += 1
            else:
                if char == quote:
                    state = "code"
                output.append("\n" if char == "\n" else " ")
        elif state == "verbatim-string":
            if char == '"' and next_char == '"':
                output.extend((" ", " "))
                index += 1
            else:
                if char == '"':
                    state = "code"
                output.append("\n" if char == "\n" else " ")
        elif char == "/" and next_char == "/":
            output.extend((" ", " "))
            index += 1
            state = "line-comment"
        elif char == "/" and next_char == "*":
            output.extend((" ", " "))
            index += 1
            state = "block-comment"
        elif char == "@" and next_char == '"':
            output.extend((" ", " "))
            index += 1
            state = "verbatim-string"
        elif char == '"':
            output.append(" ")
            state = "string"
        elif char == "'":
            output.append(" ")
            state = "char"
        else:
            output.append(char)
        index += 1
    return "".join(output)


def body_end(text: str, declaration: re.Match[str]) -> int:
    opening = text.find("{", declaration.end())
    if opening < 0:
        return declaration.end()
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return index + 1
    return len(text)


def scan_source(root: Path) -> tuple[list[SourceEntry], list[SourceEntry], list[LoopEntry], list[StaticEntry]]:
    production: list[SourceEntry] = []
    helpers: list[SourceEntry] = []
    loops: list[LoopEntry] = []
    statics: list[StaticEntry] = []
    source_root = root / SOURCE_ROOT
    for path in sorted(source_root.rglob("*.cs")):
        path_text = relative(root, path)
        text = path.read_text(encoding="utf-8")
        clean = strip_comments_and_strings(text)
        entry = SourceEntry(path_text, len(text.splitlines()), len(text.encode("utf-8")))
        if source_scope(path_text) != "editor":
            production.append(entry)
            if path.name.endswith("SystemHelper.cs"):
                helpers.append(entry)

        for declaration in TYPE_DECLARATION_RE.finditer(clean):
            bases = (declaration.group("bases") or "").lstrip(":")
            if not re.search(r"\b(?:MonoBehaviour|UnityEngine\.MonoBehaviour)\b", bases):
                continue
            end = body_end(clean, declaration)
            body = clean[declaration.end():end]
            for match in LOOP_METHOD_RE.finditer(body):
                method = match.group("update") or f"Coroutine:{match.group('coroutine')}"
                absolute = declaration.end() + match.start()
                loops.append(LoopEntry(
                    path_text,
                    declaration.group("name"),
                    method,
                    clean.count("\n", 0, absolute) + 1,
                    source_scope(path_text),
                ))
        if source_scope(path_text) != "editor":
            for match in MUTABLE_STATIC_FIELD_RE.finditer(clean):
                statics.append(StaticEntry(
                    path_text,
                    " ".join(match.group("type").split()),
                    match.group("name"),
                    clean.count("\n", 0, match.start()) + 1,
                ))

    return (
        sorted(production, key=lambda item: item.path),
        sorted(helpers, key=lambda item: item.path),
        sorted(loops, key=lambda item: (item.scope, item.path, item.type, item.method)),
        sorted(statics, key=lambda item: (item.path, item.line, item.name)),
    )


def asmdef_guid(path: Path) -> str | None:
    meta = Path(str(path) + ".meta")
    if not meta.is_file():
        return None
    match = re.search(r"^guid:\s*([0-9a-fA-F]+)\s*$", meta.read_text(encoding="utf-8"), re.MULTILINE)
    return match.group(1).lower() if match else None


def strongly_connected_components(graph: dict[str, set[str]]) -> list[list[str]]:
    next_index = 0
    indices: dict[str, int] = {}
    lowlinks: dict[str, int] = {}
    stack: list[str] = []
    on_stack: set[str] = set()
    cycles: list[list[str]] = []

    def visit(node: str) -> None:
        nonlocal next_index
        indices[node] = lowlinks[node] = next_index
        next_index += 1
        stack.append(node)
        on_stack.add(node)
        for target in sorted(graph[node]):
            if target not in indices:
                visit(target)
                lowlinks[node] = min(lowlinks[node], lowlinks[target])
            elif target in on_stack:
                lowlinks[node] = min(lowlinks[node], indices[target])
        if lowlinks[node] != indices[node]:
            return
        component: list[str] = []
        while True:
            target = stack.pop()
            on_stack.remove(target)
            component.append(target)
            if target == node:
                break
        if len(component) > 1 or node in graph[node]:
            cycles.append(sorted(component))

    for node in sorted(graph):
        if node not in indices:
            visit(node)
    return sorted(cycles)


def scan_assemblies(root: Path) -> dict[str, Any]:
    paths = sorted({
        path
        for base in ASMDEF_ROOTS
        if (root / base).is_dir()
        for path in (root / base).rglob("*.asmdef")
    })
    documents: list[tuple[Path, dict[str, Any]]] = []
    guid_to_name: dict[str, str] = {}
    for path in paths:
        value = json.loads(path.read_text(encoding="utf-8"))
        if not isinstance(value, dict) or not isinstance(value.get("name"), str):
            raise ValueError(f"asmdef is malformed: {relative(root, path)}")
        documents.append((path, value))
        guid = asmdef_guid(path)
        if guid:
            guid_to_name[guid] = value["name"]
    names = {value["name"] for _, value in documents}
    assemblies: list[dict[str, Any]] = []
    graph = {name: set() for name in names}
    external_count = 0
    for path, value in documents:
        resolved: set[str] = set()
        external: set[str] = set()
        for raw in value.get("references", []):
            if not isinstance(raw, str):
                continue
            target = guid_to_name.get(raw[5:].lower()) if raw.startswith("GUID:") else raw
            if target in names:
                resolved.add(target)
            else:
                external.add(raw)
        graph[value["name"]].update(resolved)
        external_count += len(external)
        assemblies.append({
            "externalReferences": sorted(external),
            "firstPartyReferences": sorted(resolved),
            "name": value["name"],
            "path": relative(root, path),
        })
    return {
        "assemblies": sorted(assemblies, key=lambda item: item["name"]),
        "assemblyCount": len(assemblies),
        "cycleCount": len(strongly_connected_components(graph)),
        "cycles": strongly_connected_components(graph),
        "externalReferenceCount": external_count,
        "firstPartyEdgeCount": sum(len(targets) for targets in graph.values()),
    }


def validated_authorities(root: Path) -> list[dict[str, str]]:
    result: list[dict[str, str]] = []
    for value in (*AUTHORITY_PATHS, *(item["authorityPath"] for item in ACTIVE_WORK_OWNERS)):
        path = root / value
        if not path.is_file():
            raise ValueError(f"required authority is missing: {value}")
        result.append({"path": value, "sha256": sha256(path)})
    unique = {item["path"]: item for item in result}
    return [unique[path] for path in sorted(unique)]


def validate_owner_domains(root: Path) -> None:
    registry_path = root / "Design/AgentReports/ArchitectureMaturity/validator_registry.json"
    registry = json.loads(registry_path.read_text(encoding="utf-8"))
    validators = registry.get("validators") if isinstance(registry, dict) else None
    if not isinstance(validators, list):
        raise ValueError("validator registry does not contain a validators array")
    validator_ids = {
        item.get("id") for item in validators
        if isinstance(item, dict) and isinstance(item.get("id"), str)
    }
    required_ids = {
        owner_id
        for domain in OWNER_DOMAINS
        for owner_id in domain["currentOwnerValidatorIds"]
    }
    missing = sorted(required_ids - validator_ids)
    if missing:
        raise ValueError(f"owner domains reference unknown validator ids: {', '.join(missing)}")
    for owner in ACTIVE_WORK_OWNERS:
        paths = owner["protectedPaths"]
        if paths != sorted(set(paths)):
            raise ValueError(f"active owner protectedPaths must be unique and sorted: {owner['id']}")


def build_inventory(root: Path, revision: str, tree: str) -> dict[str, Any]:
    if not re.fullmatch(r"[0-9a-f]{40}", revision) or not re.fullmatch(r"[0-9a-f]{40}", tree):
        raise ValueError("revision and tree must be exact 40-character lowercase Git identities")
    validate_owner_domains(root)
    production, helpers, loops, statics = scan_source(root)
    assemblies = scan_assemblies(root)
    oversized = [item for item in production if item.lines > 500]
    strict = [item for item in production if item.lines > 1000]
    helper_oversized = [item for item in helpers if item.lines > 500]
    loop_scopes = Counter(item.scope for item in loops)
    active_owners = sorted([
        {
            **owner,
            "authoritySha256": sha256(root / owner["authorityPath"]),
        }
        for owner in ACTIVE_WORK_OWNERS
    ], key=lambda item: item["id"])
    return {
        "activeWorkOwnership": {
            "activeOwnerCount": sum(item["status"] == "active" for item in active_owners),
            "owners": active_owners,
            "policy": "Paths remain excluded from maturity edits unless the exact authority owner hands them off.",
        },
        "artifactId": "AM-006",
        "assemblies": assemblies,
        "baseline": {"branch": "main", "commit": revision, "tree": tree},
        "determinism": {
            "arrays": "All path/id collections sort ascending by Unicode code point; source locations break ties.",
            "serialization": "UTF-8, LF, two-space indentation, lexicographic object keys, one trailing LF.",
            "timestamps": "Excluded.",
        },
        "managedHelpers": {
            "count": len(helpers),
            "entries": [asdict(item) for item in helpers],
            "over500Count": len(helper_oversized),
            "totalBytes": sum(item.bytes for item in helpers),
            "totalLines": sum(item.lines for item in helpers),
        },
        "ownerDomains": sorted(OWNER_DOMAINS, key=lambda item: item["id"]),
        "runtimeLoops": {
            "byScope": dict(sorted(loop_scopes.items())),
            "count": len(loops),
            "entries": [asdict(item) for item in loops],
        },
        "schemaVersion": SCHEMA_VERSION,
        "scope": {
            "excludedOwnershipIds": sorted(item["id"] for item in ACTIVE_WORK_OWNERS),
            "productionEditorPathSegment": "Editor",
            "sourceRoot": SOURCE_ROOT,
        },
        "sourceAuthorities": validated_authorities(root),
        "sourceSize": {
            "fileCount": len(production),
            "filesOver500": [asdict(item) for item in oversized],
            "over1000Count": len(strict),
            "over500Count": len(oversized),
            "totalBytes": sum(item.bytes for item in production),
            "totalLines": sum(item.lines for item in production),
        },
        "staticState": {
            "candidateCount": len(statics),
            "entries": [asdict(item) for item in statics],
            "policy": "Candidates require owner classification; this lexical inventory does not assert a violation.",
        },
        "summary": {
            "activeOwnerCount": sum(item["status"] == "active" for item in active_owners),
            "assemblyCount": assemblies["assemblyCount"],
            "assemblyCycleCount": assemblies["cycleCount"],
            "managedHelperCount": len(helpers),
            "mutableStaticCandidateCount": len(statics),
            "productionSourceFileCount": len(production),
            "productionSourceOver1000Count": len(strict),
            "productionSourceOver500Count": len(oversized),
            "runtimeLoopCount": len(loops),
        },
    }


def render_markdown(data: dict[str, Any]) -> str:
    summary = data["summary"]
    source = data["sourceSize"]
    lines = [
        "# Architecture Maturity Ownership Inventory",
        "",
        "> Generated by `python3 Tools/CI/architecture_ownership_inventory.py`; do not edit manually.",
        "",
        f"- Baseline commit: `{data['baseline']['commit']}`",
        f"- Baseline tree: `{data['baseline']['tree']}`",
        f"- Production source files: {summary['productionSourceFileCount']}",
        f"- Files over 500 / 1,000 lines: {summary['productionSourceOver500Count']} / {summary['productionSourceOver1000Count']}",
        f"- Assemblies / first-party cycles: {summary['assemblyCount']} / {summary['assemblyCycleCount']}",
        f"- Runtime loops: {summary['runtimeLoopCount']}",
        f"- Mutable static candidates: {summary['mutableStaticCandidateCount']}",
        f"- Managed helpers: {summary['managedHelperCount']}",
        "",
        "## Owner Domains",
        "",
        "| Domain | Current validator owners | Responsibility |",
        "|---|---|---|",
    ]
    for item in data["ownerDomains"]:
        owners = ", ".join(f"`{owner}`" for owner in item["currentOwnerValidatorIds"])
        lines.append(f"| `{item['id']}` | {owners} | {item['responsibility']} |")
    lines.extend([
        "",
        "## Active Work Exclusions",
        "",
        "| Owner | Status | Authority | Protected paths |",
        "|---|---|---|---|",
    ])
    for owner in data["activeWorkOwnership"]["owners"]:
        paths = "<br>".join(f"`{path}`" for path in owner["protectedPaths"])
        lines.append(
            f"| `{owner['id']}` | {owner['status']} | `{owner['authorityPath']}` | {paths} |"
        )
    lines.extend([
        "",
        "## Assembly Dependencies",
        "",
        f"- First-party edges: {data['assemblies']['firstPartyEdgeCount']}",
        f"- External references: {data['assemblies']['externalReferenceCount']}",
        f"- Cycles: {data['assemblies']['cycleCount']}",
        "",
        "| Assembly | First-party references | Path |",
        "|---|---|---|",
    ])
    for assembly in data["assemblies"]["assemblies"]:
        references = ", ".join(f"`{value}`" for value in assembly["firstPartyReferences"]) or "none"
        lines.append(f"| `{assembly['name']}` | {references} | `{assembly['path']}` |")
    lines.extend([
        "",
        "## Source Size",
        "",
        f"- Total lines: {source['totalLines']}",
        f"- Total UTF-8 bytes: {source['totalBytes']}",
        "",
        "| File over 500 lines | Lines | Bytes |",
        "|---|---:|---:|",
    ])
    for item in source["filesOver500"]:
        lines.append(f"| `{item['path']}` | {item['lines']} | {item['bytes']} |")
    lines.extend([
        "",
        "## Runtime And Managed Ownership",
        "",
        f"- Runtime-loop rows by scope: `{json.dumps(data['runtimeLoops']['byScope'], sort_keys=True)}`",
        f"- Mutable-static candidates: {data['staticState']['candidateCount']} (classification required; not automatically violations).",
        f"- Managed `*SystemHelper.cs` files: {data['managedHelpers']['count']}; over 500 lines: {data['managedHelpers']['over500Count']}.",
        "",
        "Full deterministic loop, static-candidate, helper, source-size, authority-hash, and ownership rows are in `ownership_inventory.json`.",
        "",
    ])
    return "\n".join(lines)


def write_inventory(root: Path, revision: str, tree: str, json_output: str, markdown_output: str) -> dict[str, Any]:
    data = build_inventory(root, revision, tree)
    json_path = root / json_output
    markdown_path = root / markdown_output
    json_path.parent.mkdir(parents=True, exist_ok=True)
    markdown_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    markdown_path.write_text(render_markdown(data), encoding="utf-8")
    return data


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--revision", required=True)
    parser.add_argument("--tree", required=True)
    parser.add_argument("--json-output", default=DEFAULT_JSON)
    parser.add_argument("--markdown-output", default=DEFAULT_MARKDOWN)
    args = parser.parse_args()
    try:
        data = write_inventory(args.root.resolve(), args.revision, args.tree, args.json_output, args.markdown_output)
    except (OSError, UnicodeDecodeError, json.JSONDecodeError, ValueError) as error:
        print(f"[ArchitectureOwnershipInventory] result=Failed reason={error}")
        return 2
    print(
        "[ArchitectureOwnershipInventory] result=Generated "
        f"sources={data['summary']['productionSourceFileCount']} "
        f"assemblies={data['summary']['assemblyCount']} cycles={data['summary']['assemblyCycleCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
