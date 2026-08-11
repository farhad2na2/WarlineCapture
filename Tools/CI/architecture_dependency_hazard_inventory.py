#!/usr/bin/env python3
"""Generate the deterministic AM-018 global dependency and lifecycle hazard inventory."""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import io
import json
import re
import subprocess
import tarfile
from functools import lru_cache
from pathlib import Path
from typing import Any, Iterable

import architecture_lifecycle_inventory as lifecycle


SCHEMA_VERSION = 2
SOURCE_ROOT = "Assets/Game/Scripts"
DEFAULT_JSON = "Design/AgentReports/ArchitectureMaturity/am018_dependency_hazard_inventory.json"
DEFAULT_MARKDOWN = "Design/AgentReports/ArchitectureMaturity/am018_dependency_hazard_inventory.md"
TOOL_PATHS = (
    "Tools/CI/architecture_dependency_hazard_inventory.py",
    "Tools/CI/tests/test_architecture_dependency_hazard_inventory.py",
    "Tools/CI/tests/test_architecture_dependency_hazard_inventory_evidence.py",
)
AUTHORITY_PATHS = (
    "Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs",
    "Design/AgentReports/2026-07-10_aph-703_default-world-and-gc-owner-inventory.md",
    "Design/AgentReports/ArchitectureMaturity/entry_scorecard.json",
    "Design/AgentReports/ArchitectureMaturity/exception_registry.json",
    "Design/Architecture/gameplay_solid_ecs_contract.md",
    "Design/Architecture/performance_regression_contract.md",
    "Design/Architecture/post_hardening_architecture_maturity_tracker.md",
    "Design/AgentReports/ArchitectureMaturity/lifecycle_inventory.json",
    "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json",
    "Design/AgentReports/ArchitectureMaturity/validator_registry.json",
    "Tools/CI/architecture_lifecycle_inventory.py",
    "Tools/CI/tests/test_architecture_lifecycle_inventory.py",
)

WORLD_ACCESS_RE = re.compile(
    r"\b(?P<symbol>World\.DefaultGameObjectInjectionWorld|World\.All|"
    r"ClientServerBootstrap\.(?:ServerWorld|ClientWorld))\b"
)
RUNTIME_DISCOVERY_RE = re.compile(
    r"\b(?P<symbol>(?:(?:UnityEngine\.)?Object\.)?"
    r"(?:FindFirstObjectByType|FindAnyObjectByType|FindObjectsByType|FindObjectOfType|FindObjectsOfType)"
    r"|GameObject\.Find|Resources\.FindObjectsOfTypeAll)\s*(?:<|\()"
)
HIERARCHY_FIND_RE = re.compile(
    r"\b(?P<symbol>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*\.Find)\s*\("
)
CAMERA_MAIN_RE = re.compile(r"\b(?P<symbol>Camera\.main)\b")
SCENE_ROOT_ENUMERATION_RE = re.compile(
    r"\b(?P<symbol>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*\.GetRootGameObjects)\s*\("
)
SERVICE_LOCATOR_RE = re.compile(
    r"\b(?P<symbol>(?:ServiceLocator|GlobalServices?|Services?)\s*\.\s*"
    r"(?:Resolve|Get|GetService|TryGet)\s*(?:<|\())"
)
STATIC_PROPERTY_RE = re.compile(
    r"^[ \t]*(?P<modifiers>(?:(?:public|internal|private|protected|new|unsafe|static|readonly)\s+)*)"
    r"(?P<type>[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*(?:\s*<[^;={}(\r\n]+>)?\s*\??)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*\{",
    re.MULTILINE,
)
SINGLETON_NAMES = frozenset({
    "activeinstance",
    "activeview",
    "current",
    "instance",
    "shared",
    "singleton",
})
EVENT_HANDLER_NAME_RE = re.compile(
    r"^(?:Handle|On|Apply|Enqueue|Refresh|Sync|Process|Respond|Receive|Set|Clear|Rebuild|Forward|Publish)"
    r"|Callback$"
)
CACHE_SIGNAL_RE = re.compile(
    r"(?:cache|lookup|query|registry|dictionary|hashset|memo|table|pool|scratch)",
    re.IGNORECASE,
)
CACHE_TYPE_RE = re.compile(
    r"\b(?:Dictionary|ConcurrentDictionary|HashSet|List|Queue|Stack|ConditionalWeakTable|"
    r"EntityQuery|ComponentLookup|BufferLookup|Native(?:Parallel)?(?:HashMap|HashSet|MultiHashMap)|"
    r"Unsafe(?:HashMap|ParallelHashMap))\b"
)
CACHE_EXCLUDED_TYPE_RE = re.compile(r"\b(?:ProfilerMarker)\b")
BASE_FOLLOW_UP_TASKS = {
    "globalWorldLookups": ("AM-019", "AM-022"),
    "hiddenSingletons": ("AM-020", "AM-021", "AM-022"),
    "mutableStaticCaches": ("AM-019", "AM-020", "AM-021", "AM-022"),
    "runtimeObjectDiscovery": ("AM-020", "AM-022"),
    "staticEventSubscriptions": ("AM-021", "AM-022"),
}
MUTABLE_STATIC_REFERENCE_RE = re.compile(
    r"(?:\[\s*,*\s*\]|\b(?:Material|GameObject|Camera|RenderTexture|Texture2D|Texture|Sprite|"
    r"Transform|Mesh|AudioClip|VisualEffect)\b)"
)

WORLD_HSL_OWNER_TYPES = frozenset({
    "MatchIntroEcsStateQuery",
    "RuntimeCityReadinessQueryCompositionSystemHelper",
    "RuntimeCityRoadBuildBridgeState",
    "RtsSelectionInputStateCompositionSystemHelper",
    "RuntimeGameplayStateSystem",
    "SelectionBuildingInteractionCompositionSystemHelper",
    "UnitPathfindingPendingStateReader",
})
WORLD_AD_OWNER_TYPES = frozenset({
    "BattleScenarioLabVisualPlayback",
    "GridAuthoring",
    "PerformanceDiagnosticsSystemHelper",
    "RuntimeDiagnosticsSystem",
    "SelectionRuntimeDiagnosticsSystemHelper",
})
WORLD_PE_OWNER_TYPES = frozenset({
    "AudioPlaybackPresentationRuntimeView",
    "BuildingProductionTransportPresentationSystemHelper",
    "MatchHudMinimapDataSourceAdapter",
    "RuntimeBuildingEntityLink",
    "RuntimeCityRAndDMapView",
    "RuntimeDecorationSpawnerPresentationSystemHelper",
    "RuntimeGridBlockerPresentationSystemHelper",
    "SelectionUiCameraSystemHelper",
    "SelectionUiCommandUiSystemHelper",
    "SelectionUiReadModelUiSystemHelper",
    "UnitAttackTracePresentationSystemHelper",
    "UnitImpostorPresentationSystemHelper",
})

METHOD_DECLARATION_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|sealed|override|virtual|abstract|async|unsafe|new|partial|readonly|extern)\s+)*"
    r"(?:[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*(?:\s*<[^>{};\r\n]+>)?(?:\s*\[\])?\??\s+)"
    r"(?P<name>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:where[^{}]+)?(?=\{)",
    re.MULTILINE,
)


def sha256_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def source_manifest_digest(rows: list[dict[str, str]]) -> str:
    encoded = "".join(f"{row['path']}\0{row['sha256']}\n" for row in rows).encode("utf-8")
    return sha256_bytes(encoded)


def parse_active_exclusions(content: bytes | str) -> list[dict[str, Any]]:
    data = json.loads(content.decode("utf-8") if isinstance(content, bytes) else content)
    ownership = data.get("activeWorkOwnership") if isinstance(data, dict) else None
    owners = ownership.get("owners") if isinstance(ownership, dict) else None
    if not isinstance(owners, list):
        raise ValueError("ownership inventory does not contain activeWorkOwnership.owners")
    result = [
        {
            "authorityPath": item["authorityPath"],
            "handoffAuthorityPath": item.get("handoffAuthorityPath"),
            "handoffPaths": item.get("handoffPaths", []),
            "id": item["id"],
            "protectedPaths": item["protectedPaths"],
            "status": item["status"],
        }
        for item in owners
        if isinstance(item, dict)
    ]
    if len(result) != len(owners) or [item["id"] for item in result] != sorted(item["id"] for item in result):
        raise ValueError("ownership exclusions must be valid and sorted by id")
    for item in result:
        if item["handoffPaths"] != sorted(set(item["handoffPaths"])):
            raise ValueError(f"ownership handoff paths must be valid and sorted: {item['id']}")
        if bool(item["handoffPaths"]) != bool(item["handoffAuthorityPath"]):
            raise ValueError(f"ownership handoff paths and authority must be paired: {item['id']}")
    return result


def active_exclusions(root: Path, snapshot: dict[str, bytes] | None = None) -> list[dict[str, Any]]:
    path = root / "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
    relative = "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
    return parse_active_exclusions(snapshot[relative] if snapshot is not None else path.read_bytes())


def revision_snapshot(root: Path, revision: str) -> dict[str, bytes] | None:
    if not (root / ".git").exists():
        return None
    result = subprocess.run(
        ["git", "archive", "--format=tar", revision, SOURCE_ROOT, *AUTHORITY_PATHS],
        cwd=root,
        check=True,
        capture_output=True,
    )
    files: dict[str, bytes] = {}
    with tarfile.open(fileobj=io.BytesIO(result.stdout), mode="r:") as archive:
        for member in archive.getmembers():
            if not member.isfile():
                continue
            extracted = archive.extractfile(member)
            if extracted is None:
                raise ValueError(f"cannot read archived baseline file: {member.name}")
            files[member.name] = extracted.read()
    for relative in AUTHORITY_PATHS:
        if relative not in files:
            raise ValueError(f"required baseline authority is missing: {relative}")
    return files


def production_sources(
    root: Path,
    snapshot: dict[str, bytes] | None = None,
) -> list[tuple[str, str, bytes]]:
    rows: list[tuple[str, str, bytes]] = []
    if snapshot is not None:
        candidates = [
            (path, content)
            for path, content in snapshot.items()
            if path.startswith(f"{SOURCE_ROOT}/") and path.endswith(".cs")
        ]
    else:
        candidates = [
            (lifecycle.relative(root, source), source.read_bytes())
            for source in (root / SOURCE_ROOT).rglob("*.cs")
        ]
    for path, content in sorted(candidates, key=lambda item: item[0]):
        if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
            continue
        rows.append((path, content.decode("utf-8"), content))
    return rows


def mask_editor_only_regions(text: str) -> str:
    false_expressions = {
        "UNITY_EDITOR",
        "UNITY_INCLUDE_TESTS",
        "UNITY_EDITOR||UNITY_INCLUDE_TESTS",
    }
    true_expressions = {
        "!UNITY_EDITOR",
        "!UNITY_INCLUDE_TESTS",
        "!UNITY_EDITOR&&!UNITY_INCLUDE_TESTS",
    }
    output: list[str] = []
    stack: list[tuple[bool, bool | None]] = []
    excluded = False
    for line in text.splitlines(keepends=True):
        directive = re.match(r"^\s*#(?P<kind>if|else|endif)\b(?P<expr>.*)$", line)
        line_excluded = excluded
        if directive:
            kind = directive.group("kind")
            if kind == "if":
                expression = re.sub(r"[\s()]", "", directive.group("expr"))
                known_value: bool | None = None
                if expression in false_expressions:
                    known_value = False
                elif expression in true_expressions:
                    known_value = True
                stack.append((excluded, known_value))
                excluded = excluded or known_value is False
            elif kind == "else" and stack:
                parent_excluded, known_value = stack[-1]
                excluded = parent_excluded or known_value is True
            elif kind == "endif" and stack:
                parent_excluded, _known_value = stack.pop()
                excluded = parent_excluded
            line_excluded = True
        if line_excluded:
            output.append("".join("\n" if char == "\n" else "\r" if char == "\r" else " " for char in line))
        else:
            output.append(line)
    return "".join(output)


def protected_owner_ids(path: str, exclusions: list[dict[str, Any]]) -> list[str]:
    return sorted({
        owner["id"]
        for owner in exclusions
        for pattern in owner["protectedPaths"]
        if fnmatch.fnmatch(path, pattern)
        and not any(fnmatch.fnmatch(path, handoff) for handoff in owner.get("handoffPaths", []))
    })


def normalized_identifier(name: str) -> str:
    normalized = name.lstrip("_")
    if normalized.lower().startswith("s_"):
        normalized = normalized[2:]
    return normalized.lower()


def is_singleton_name(name: str) -> bool:
    return normalized_identifier(name) in SINGLETON_NAMES


@lru_cache(maxsize=None)
def member_spans(owner: lifecycle.TypeSpan) -> tuple[tuple[int, int, str], ...]:
    spans: list[tuple[int, int, str]] = []
    for declaration in METHOD_DECLARATION_RE.finditer(owner.body):
        start, end = lifecycle.find_body(owner.body, declaration)
        spans.append((start, end, declaration.group("name")))
    return tuple(spans)


def member_name_at(owner: lifecycle.TypeSpan | None, position: int) -> str:
    if owner is None:
        return "<file>"
    local_position = position - owner.start
    candidates = [
        item
        for item in member_spans(owner)
        if item[0] <= local_position < item[1]
    ]
    if not candidates:
        return "<member>"
    return min(candidates, key=lambda item: item[1] - item[0])[2]


def block_end(text: str, opening: int) -> int:
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return index + 1
    return len(text)


def world_disposition(path: str, owner_type: str, member_name: str) -> tuple[str, str]:
    if owner_type == "GameplayRuntimeUpdateCompositionSystemHelper":
        if member_name == "GetInitialSpawnCounts":
            return "AD", "Diagnostic-only initial-spawn counts read global World state."
        return "HSL", "Recurring gameplay readiness resolves global World state instead of a lifecycle-bound cache."
    if owner_type == "SelectionGameplayStartupSystemHelper":
        if member_name == "TryGetDefaultEntityManager":
            return "HSL", "Recurring selection phases resolve the default EntityManager through a local service-locator function."
        return "CE", "Selection startup resolves managed ECS systems at the composition boundary."
    if owner_type in WORLD_HSL_OWNER_TYPES:
        return "HSL", "Reusable runtime/query logic resolves global World state instead of receiving a lifecycle-bound dependency."
    if owner_type in WORLD_AD_OWNER_TYPES or "/ScenarioLab/" in path or "/Authorings/" in path:
        return "AD", "The lookup supports authoring, diagnostics, or scenario proof rather than gameplay authority."
    if (
        owner_type in WORLD_PE_OWNER_TYPES
        or "/UI/" in path
        or "/Rendering/" in path
        or "Presentation" in owner_type
    ):
        return "PE", "The lookup is at an ECS-to-Unity presentation or UI boundary."
    return "CE", "The lookup is at startup, teardown, scene wiring, or another explicit composition edge."


def singleton_disposition(
    field_type: str,
    readonly: bool,
    setter_observed: bool,
) -> tuple[str, str]:
    if (readonly or not setter_observed) and re.search(r"\b(?:Null|Fallback)[A-Za-z_]\w*", field_type):
        return "IB", "Process-wide immutable fallback/null-object instance; verify it remains stateless."
    return "HSL", "Process-wide access or mutable state can outlive a World and requires explicit lifecycle ownership."


def cache_candidate_reasons(field_type: str, field_name: str) -> list[str]:
    if CACHE_EXCLUDED_TYPE_RE.search(field_type):
        return []
    reasons: list[str] = []
    if CACHE_SIGNAL_RE.search(field_name):
        reasons.append("name-signal")
    if CACHE_TYPE_RE.search(field_type):
        reasons.append("mutable-reference-type")
    return reasons


def is_cache_candidate(field_type: str, field_name: str) -> bool:
    return bool(
        CACHE_SIGNAL_RE.search(field_name)
        or re.search(r"\b(?:EntityQuery|ComponentLookup|BufferLookup)\b", field_type)
    )


def static_state_candidate(
    field_type: str,
    field_name: str,
    readonly: bool,
) -> tuple[bool, list[str], str]:
    cache_reasons = cache_candidate_reasons(field_type, field_name)
    if not readonly:
        return True, cache_reasons, "assignable-static-field"
    if cache_reasons or MUTABLE_STATIC_REFERENCE_RE.search(field_type):
        reasons = list(cache_reasons)
        if MUTABLE_STATIC_REFERENCE_RE.search(field_type):
            reasons.append("mutable-reference-shape")
        return True, sorted(set(reasons)), "readonly-mutable-reference"
    return False, [], "immutable-value-candidate"


def follow_up_tasks(category: str, disposition: str) -> list[str]:
    tasks = list(BASE_FOLLOW_UP_TASKS[category])
    if category == "globalWorldLookups" and disposition == "HSL":
        tasks.insert(1, "AM-020")
    elif category == "runtimeObjectDiscovery" and disposition == "AD":
        return []
    elif category == "hiddenSingletons" and disposition == "IB":
        return ["AM-022"]
    elif category == "mutableStaticCaches" and disposition == "MSL":
        return ["AM-020", "AM-021", "AM-022"]
    elif category == "mutableStaticCaches" and disposition == "IRC":
        return ["AM-021", "AM-022"]
    return tasks


def finding_contract(
    category: str,
    path: str,
    owner_type: str,
    member_name: str,
    **details: Any,
) -> tuple[str, str]:
    if category == "globalWorldLookups":
        return world_disposition(path, owner_type, member_name)
    if category == "hiddenSingletons":
        return singleton_disposition(
            details.get("fieldType", ""),
            bool(details.get("readonly")),
            bool(details.get("setterObserved")),
        )
    if category == "mutableStaticCaches":
        if details.get("cacheCandidate"):
            return "CLR", "Static cache/scratch state needs an explicit reset, rebind, disposal, and World-lifetime classification."
        if details.get("staticStateKind") == "readonly-mutable-reference":
            return "IRC", "Static readonly reference can still contain mutable process-wide data; classify table immutability or assign lifecycle ownership."
        return "MSL", "Assignable process-wide state needs an explicit lifecycle owner or migration to World-owned state."
    if category == "runtimeObjectDiscovery":
        if "/Authorings/" in path or owner_type.endswith("Baker"):
            return "AD", "Baker/authoring hierarchy lookup is classified separately from runtime discovery debt."
        return "ROD", "Runtime discovery must become a serialized, registered, or composition-provided dependency."
    if category == "staticEventSubscriptions":
        if not details.get("pairedUnsubscribeObserved"):
            return "ESU", "Static event subscription has no paired unsubscribe and can retain state across lifecycle transitions."
        if details.get("teardownUnsubscribeMethods"):
            return "ETO", "Static event subscription has a directly observed teardown owner; lifecycle tests remain required."
        return "EIP", "A paired unsubscribe exists, but direct teardown ownership is not lexically proven."
    raise ValueError(f"unsupported category: {category}")


def owner_row(
    path: str,
    clean: str,
    spans: list[lifecycle.TypeSpan],
    position: int,
    symbol: str,
    category: str,
    exclusions: list[dict[str, Any]],
    **extra: Any,
) -> dict[str, Any]:
    owner = lifecycle.owner_at(spans, position)
    owner_type = owner.name if owner else "<file>"
    member_name = member_name_at(owner, position)
    disposition, rationale = finding_contract(
        category,
        path,
        owner_type,
        member_name,
        **extra,
    )
    return {
        "category": category,
        "disposition": disposition,
        "followUpTasks": follow_up_tasks(category, disposition),
        "line": clean.count("\n", 0, position) + 1,
        "memberName": member_name,
        "ownerType": owner_type,
        "path": path,
        "protectedOwnerIds": protected_owner_ids(path, exclusions),
        "rationale": rationale,
        "responsibleOwner": owner_type,
        "symbol": " ".join(symbol.split()),
        **extra,
    }


def scan_source(
    path: str,
    text: str,
    exclusions: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    clean = lifecycle.strip_comments_and_strings(mask_editor_only_regions(text))
    spans = lifecycle.type_spans(path, clean)
    rows: list[dict[str, Any]] = []

    for match in WORLD_ACCESS_RE.finditer(clean):
        rows.append(owner_row(
            path, clean, spans, match.start(), match.group("symbol"), "globalWorldLookups", exclusions,
            accessKind="global-world-property",
        ))

    for match in RUNTIME_DISCOVERY_RE.finditer(clean):
        rows.append(owner_row(
            path, clean, spans, match.start(), match.group("symbol"), "runtimeObjectDiscovery", exclusions,
            accessKind="scene-or-resource-scan",
        ))

    explicit_discovery_starts = {match.start() for match in RUNTIME_DISCOVERY_RE.finditer(clean)}
    for match in HIERARCHY_FIND_RE.finditer(clean):
        symbol = match.group("symbol")
        if match.start() in explicit_discovery_starts or symbol == "Shader.Find":
            continue
        rows.append(owner_row(
            path, clean, spans, match.start(), symbol, "runtimeObjectDiscovery", exclusions,
            accessKind="hierarchy-or-generic-find",
        ))

    for match in CAMERA_MAIN_RE.finditer(clean):
        rows.append(owner_row(
            path, clean, spans, match.start(), match.group("symbol"), "runtimeObjectDiscovery", exclusions,
            accessKind="camera-main-lookup",
        ))

    for match in SCENE_ROOT_ENUMERATION_RE.finditer(clean):
        rows.append(owner_row(
            path, clean, spans, match.start(), match.group("symbol"), "runtimeObjectDiscovery", exclusions,
            accessKind="scene-root-enumeration",
        ))

    for match in SERVICE_LOCATOR_RE.finditer(clean):
        rows.append(owner_row(
            path, clean, spans, match.start(), match.group("symbol"), "hiddenSingletons", exclusions,
            accessKind="service-locator-access",
        ))

    for match in lifecycle.field_rows(clean, spans):
        modifiers = match.group("modifiers").split()
        if "static" not in modifiers:
            continue
        field_type = " ".join(match.group("type").split())
        field_name = match.group("name")
        readonly = "readonly" in modifiers
        is_static_state, cache_reasons, static_state_kind = static_state_candidate(
            field_type,
            field_name,
            readonly,
        )
        if is_static_state:
            rows.append(owner_row(
                path, clean, spans, match.start(), field_name, "mutableStaticCaches", exclusions,
                accessKind="static-field",
                cacheCandidate=is_cache_candidate(field_type, field_name),
                cacheCandidateReasons=cache_reasons,
                fieldType=field_type,
                readonly=readonly,
                staticStateKind=static_state_kind,
            ))
        if is_singleton_name(field_name):
            rows.append(owner_row(
                path, clean, spans, match.start(), field_name, "hiddenSingletons", exclusions,
                accessKind="static-field",
                fieldType=field_type,
                readonly="readonly" in modifiers,
                setterObserved="readonly" not in modifiers,
            ))

    for owner in spans:
        depths = lifecycle.brace_depths(owner.body)
        for match in STATIC_PROPERTY_RE.finditer(owner.body):
            if depths[match.start()] != 1 or "static" not in match.group("modifiers").split():
                continue
            position = owner.start + match.start()
            field_name = match.group("name")
            field_type = " ".join(match.group("type").split())
            property_start = owner.body.rfind("{", match.start(), match.end())
            property_end = block_end(owner.body, property_start)
            property_body = owner.body[property_start:property_end]
            setter_observed = re.search(r"\b(?:set|init)\s*(?:;|=>|\{)", property_body) is not None
            cache_reasons = cache_candidate_reasons(field_type, field_name)
            mutable_reference = CACHE_TYPE_RE.search(field_type) or MUTABLE_STATIC_REFERENCE_RE.search(field_type)
            if setter_observed or mutable_reference:
                rows.append(owner_row(
                    path, clean, spans, position, field_name, "mutableStaticCaches", exclusions,
                    accessKind="static-property",
                    cacheCandidate=is_cache_candidate(field_type, field_name),
                    cacheCandidateReasons=cache_reasons,
                    fieldType=field_type,
                    readonly=not setter_observed,
                    setterObserved=setter_observed,
                    staticStateKind="settable-static-property" if setter_observed else "readonly-mutable-reference",
                ))
            if is_singleton_name(field_name):
                rows.append(owner_row(
                    path, clean, spans, position, field_name, "hiddenSingletons", exclusions,
                    accessKind="static-property",
                    fieldType=field_type,
                    readonly=not setter_observed,
                    setterObserved=setter_observed,
                ))

    for match in lifecycle.SUBSCRIPTION_RE.finditer(clean):
        target = match.group("target")
        first_segment = target.split(".", 1)[0]
        if not first_segment or not first_segment[0].isupper():
            continue
        owner = lifecycle.owner_at(spans, match.start())
        body = owner.body if owner else clean
        handler = match.group("handler")
        if not EVENT_HANDLER_NAME_RE.search(handler.rsplit(".", 1)[-1]):
            continue
        unsubscribe = rf"{re.escape(target)}\s*-=\s*{re.escape(handler)}\s*;"
        teardown_methods = lifecycle.lifecycle_observations(owner, unsubscribe)
        rows.append(owner_row(
            path, clean, spans, match.start(), f"{target} += {handler}", "staticEventSubscriptions", exclusions,
            accessKind="likely-static-event",
            pairedUnsubscribeObserved=re.search(unsubscribe, body) is not None,
            teardownUnsubscribeMethods=teardown_methods,
        ))

    unique: dict[tuple[Any, ...], dict[str, Any]] = {}
    for row in rows:
        key = (row["category"], row["path"], row["line"], row["ownerType"], row["symbol"])
        unique[key] = row
    return [unique[key] for key in sorted(unique)]


def scan_sources(
    sources: Iterable[tuple[str, str]],
    exclusions: list[dict[str, Any]],
) -> dict[str, list[dict[str, Any]]]:
    categories = {name: [] for name in sorted(BASE_FOLLOW_UP_TASKS)}
    for path, text in sorted(sources):
        for row in scan_source(path, text, exclusions):
            categories[row["category"]].append(row)
    for rows in categories.values():
        rows.sort(key=lambda row: (row["path"], row["line"], row["ownerType"], row["symbol"]))
    return categories


def source_authorities(
    root: Path,
    snapshot: dict[str, bytes] | None = None,
) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for relative_path in AUTHORITY_PATHS:
        path = root / relative_path
        if not path.is_file():
            raise ValueError(f"required authority is missing: {relative_path}")
        digest = (
            sha256_bytes(snapshot[relative_path])
            if snapshot is not None
            else lifecycle.sha256(path)
        )
        rows.append({"path": relative_path, "sha256": digest})
    return rows


def tool_manifest(root: Path) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for relative_path in TOOL_PATHS:
        path = root / relative_path
        if not path.is_file():
            raise ValueError(f"required inventory tool is missing: {relative_path}")
        rows.append({"path": relative_path, "sha256": lifecycle.sha256(path)})
    return rows


def build_inventory(root: Path, revision: str, tree: str) -> dict[str, Any]:
    if not re.fullmatch(r"[0-9a-f]{40}", revision) or not re.fullmatch(r"[0-9a-f]{40}", tree):
        raise ValueError("revision and tree must be exact 40-character lowercase Git identities")
    snapshot = revision_snapshot(root, revision)
    exclusions = active_exclusions(root, snapshot)
    source_rows = production_sources(root, snapshot)
    source_manifest = [
        {"path": path, "sha256": sha256_bytes(content)}
        for path, _text, content in source_rows
    ]
    categories = scan_sources(((path, text) for path, text, _content in source_rows), exclusions)
    summary = {
        f"{category}Count": len(rows)
        for category, rows in categories.items()
    }
    summary["findingCount"] = sum(summary.values())
    summary["protectedFindingCount"] = sum(
        bool(row["protectedOwnerIds"])
        for rows in categories.values()
        for row in rows
    )
    summary["mutableStaticCacheCandidateCount"] = sum(
        row.get("cacheCandidate", False)
        for row in categories["mutableStaticCaches"]
    )
    summary["mutableStaticLifecycleStateCount"] = sum(
        row["disposition"] == "MSL"
        for row in categories["mutableStaticCaches"]
    )
    summary["immutableReferenceClassificationCount"] = sum(
        row["disposition"] == "IRC"
        for row in categories["mutableStaticCaches"]
    )
    return {
        "activeWorkExclusions": exclusions,
        "artifactId": "AM-018",
        "baseline": {"branch": "main", "commit": revision, "tree": tree},
        "categories": categories,
        "classification": {
            "globalWorldLookups": "CE=composition edge, AD=authoring/debug, PE=presentation edge, HSL=hidden service-locator debt. AM-019 defines the cache contract and AM-022 proves lifecycle recovery.",
            "hiddenSingletons": "IB=immutable boundary candidate; HSL=process-wide access or mutable authority. AM-020/AM-021 remove or bind state and AM-022 proves recovery.",
            "mutableStaticCaches": "CLR=cache lifecycle review; MSL=assignable mutable static lifecycle state; IRC=readonly reference requiring immutable-table or lifecycle classification.",
            "runtimeObjectDiscovery": "ROD=runtime object discovery; AD=authoring/Baker-only hierarchy access. Replace ROD with explicit dependencies in AM-020 and prove reload behavior in AM-022; AD is classified with no runtime-remediation route.",
            "staticEventSubscriptions": "ETO=direct teardown observed, EIP=indirect pair observed, ESU=no unsubscribe observed. AM-021 owns teardown and AM-022 owns lifecycle tests.",
        },
        "determinism": {
            "serialization": "UTF-8, LF, two-space indentation, lexicographic object keys, one trailing LF.",
            "sorting": "Sources sort by path; findings sort by path, line, owner, and symbol.",
            "timestamps": "Excluded.",
        },
        "schemaVersion": SCHEMA_VERSION,
        "scope": {"editorExcluded": True, "sourceRoot": SOURCE_ROOT},
        "sourceAuthorities": source_authorities(root, snapshot),
        "sourceManifest": {
            "digestSha256": source_manifest_digest(source_manifest),
            "fileCount": len(source_manifest),
            "files": source_manifest,
        },
        "summary": summary,
        "toolManifest": tool_manifest(root),
    }


def render_markdown(data: dict[str, Any]) -> str:
    summary = data["summary"]
    lines = [
        "# AM-018 Dependency And Lifecycle Hazard Inventory",
        "",
        "> Generated by `python3 Tools/CI/architecture_dependency_hazard_inventory.py`; do not edit manually.",
        "",
        f"- Baseline commit: `{data['baseline']['commit']}`",
        f"- Baseline tree: `{data['baseline']['tree']}`",
        f"- Production C# files scanned: {data['sourceManifest']['fileCount']}",
        f"- Total findings: {summary['findingCount']}",
        f"- Findings in separately owned paths: {summary['protectedFindingCount']}",
        f"- Static cache / mutable lifecycle / readonly-reference classification: {summary['mutableStaticCacheCandidateCount']} / {summary['mutableStaticLifecycleStateCount']} / {summary['immutableReferenceClassificationCount']}",
        "",
        "This is an inventory, not a defect verdict. Every row names its responsible owner, disposition, rationale, and complete AM-019 through AM-022 follow-up route.",
        "",
        "| Category | Findings | Follow-up tasks |",
        "|---|---:|---|",
    ]
    for category in sorted(data["categories"]):
        routed_tasks = sorted({task for row in data["categories"][category] for task in row["followUpTasks"]})
        tasks = ", ".join(f"`{task}`" for task in routed_tasks) or "classified only"
        lines.append(f"| `{category}` | {len(data['categories'][category])} | {tasks} |")
    for category in sorted(data["categories"]):
        lines.extend([
            "",
            f"## {category}",
            "",
            data["classification"][category],
            "",
            "| Owner | Member | Disposition | Symbol | Path | Line | Protected lane |",
            "|---|---|---|---|---|---:|---|",
        ])
        for row in data["categories"][category]:
            protected = ", ".join(f"`{value}`" for value in row["protectedOwnerIds"]) or "none"
            lines.append(
                f"| `{row['ownerType']}` | `{row['memberName']}` | `{row['disposition']}` | `{row['symbol']}` | `{row['path']}` | {row['line']} | {protected} |"
            )
        if not data["categories"][category]:
            lines.append("| none | none | none | none | none | 0 | none |")
    lines.extend([
        "",
        "The JSON artifact contains the complete source manifest, authority hashes, lifecycle details, and deterministic rows.",
        "",
    ])
    return "\n".join(lines)


def write_inventory(root: Path, revision: str, tree: str, json_output: str, markdown_output: str) -> dict[str, Any]:
    data = build_inventory(root, revision, tree)
    json_path = root / json_output
    markdown_path = root / markdown_output
    json_path.parent.mkdir(parents=True, exist_ok=True)
    markdown_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_bytes((json.dumps(data, indent=2, sort_keys=True) + "\n").encode("utf-8"))
    markdown_path.write_bytes(render_markdown(data).encode("utf-8"))
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
        print(f"[ArchitectureDependencyHazardInventory] result=Failed reason={error}")
        return 2
    print(
        "[ArchitectureDependencyHazardInventory] result=Generated "
        f"sources={data['sourceManifest']['fileCount']} findings={data['summary']['findingCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
