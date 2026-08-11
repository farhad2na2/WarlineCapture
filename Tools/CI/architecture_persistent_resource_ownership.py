#!/usr/bin/env python3
"""Generate the deterministic AM-021 persistent-resource ownership matrix."""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import re
import subprocess
from pathlib import Path
from typing import Any

import architecture_lifecycle_inventory as lifecycle


SCHEMA_VERSION = 1
ARTIFACT_ID = "AM-021"
SOURCE_ROOT = "Assets/Game/Scripts"
DEFAULT_JSON = "Design/AgentReports/ArchitectureMaturity/am021_persistent_resource_ownership.json"
DEFAULT_MARKDOWN = "Design/AgentReports/ArchitectureMaturity/am021_persistent_resource_ownership.md"
OWNERSHIP_PATH = "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
TEARDOWN_METHODS = frozenset({
    "Dispose",
    "OnDestroy",
    "OnDisable",
    "RegisterAsRuntimeGateway",
    "ReleaseAll",
    "ReleaseSubscription",
    "Reset",
    "ResetBeforeSubsystemRegistration",
    "Shutdown",
    "Unbind",
})
EVENT_HANDLER_RE = re.compile(
    r"^(?:Apply|Close|Enqueue|Handle|On|Open|Refresh|Release|Request|Sync|Toggle)[A-Z_]"
)
METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|sealed|override|virtual|abstract|async|unsafe|new|partial|readonly|extern)\s+)*"
    r"(?:[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*(?:\s*<[^>{};\r\n]+>)?(?:\s*\[\])?\??\s+)"
    r"(?P<name>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:where[^{}]+)?(?=\{)",
    re.MULTILINE,
)
ROOT_CREATION_RE = re.compile(
    r"(?P<target>(?:[A-Za-z_]\w*\.)*[A-Za-z_]\w*)\s*=\s*new\s+(?:GameObject\s*)?\("
)
PERSISTENT_NATIVE_RE = re.compile(
    r"^(?:Native(?:Parallel)?(?:Array|BitArray|List|HashMap|HashSet|MultiHashMap|Queue|Reference|Stream)"
    r"|Unsafe(?:List|HashMap|ParallelHashMap|ParallelMultiHashMap|Queue|RingQueue))(?:<.*>)?$"
)
EXTERNAL_NATIVE_OWNERS = {
    ("Assets/Game/Scripts/Components/GridComponents.cs", "DynamicBlockerComponent", "Counts"): (
        "RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage",
        "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage",
        True,
    ),
    ("Assets/Game/Scripts/Components/GridComponents.cs", "DynamicBlockerComponent", "Blocked"): (
        "RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage",
        "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage",
        True,
    ),
    ("Assets/Game/Scripts/Components/GridComponents.cs", "DynamicBlockerComponent", "FriendlyPassFactionIds"): (
        "RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage",
        "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage",
        True,
    ),
    ("Assets/Game/Scripts/Components/GridComponents.cs", "DynamicOccupancyComponent", "Occupied"): (
        "RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage",
        "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage",
        True,
    ),
    ("Assets/Game/Scripts/Components/GridComponents.cs", "PathPoolComponent", "Cells"): (
        "RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage",
        "RuntimeGridPersistentStorageUtilitySystemHelper.DisposeStorage",
        True,
    ),
    ("Assets/Game/Scripts/Systems/UnitPathfindingSystem.cs", "UnitPathfindingSystem", "_pendingPathStream"): (
        "UnitPathfindingScheduler->UnitPathfindingSystem",
        "UnitPathfindingApply/UnitPathfindingSystem.OnDestroy",
        True,
    ),
}
EXTERNAL_EVENT_OWNERS = {
    (
        "Assets/Game/Scripts/UI/Shell/Ecs/UiDiagnosticsReadModelSystem.cs",
        "UiDiagnosticsRuntimeLogBuffer",
        "Application.logMessageReceived",
    ): "UiDiagnosticsReadModelSystem.OnDestroy",
    (
        "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs",
        "UIShellContentView",
        "_mainMenuPlayUi.FullMapPopupRequested",
    ): "MenuBootstrapCompositionSystemHelper.Shutdown",
    (
        "Assets/Game/Scripts/UI/Shell/UIShellContentView.cs",
        "UIShellContentView",
        "_mainMenuPlayUi.FullMapPopupCloseRequested",
    ): "MenuBootstrapCompositionSystemHelper.Shutdown",
}


def sha256_bytes(content: bytes) -> str:
    return hashlib.sha256(content).hexdigest()


def git_value(root: Path, *args: str) -> str:
    return subprocess.check_output(["git", *args], cwd=root, text=True).strip()


def relative(root: Path, path: Path) -> str:
    return path.relative_to(root).as_posix()


def source_manifest(root: Path) -> list[dict[str, str]]:
    rows: list[dict[str, str]] = []
    for source in sorted((root / SOURCE_ROOT).rglob("*.cs")):
        path = relative(root, source)
        if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
            continue
        rows.append({"path": path, "sha256": sha256_bytes(source.read_bytes())})
    return rows


def load_exclusions(root: Path) -> list[dict[str, Any]]:
    data = json.loads((root / OWNERSHIP_PATH).read_text(encoding="utf-8"))
    owners = data["activeWorkOwnership"]["owners"]
    return [
        {
            "id": owner["id"],
            "status": owner["status"],
            "authorityPath": owner["authorityPath"],
            "handoffAuthorityPath": owner.get("handoffAuthorityPath"),
            "handoffPaths": owner.get("handoffPaths", []),
            "protectedPaths": owner["protectedPaths"],
        }
        for owner in owners
    ]


def protected_owner_ids(path: str, exclusions: list[dict[str, Any]]) -> list[str]:
    return sorted({
        owner["id"]
        for owner in exclusions
        for pattern in owner["protectedPaths"]
        if fnmatch.fnmatch(path, pattern)
        and not any(fnmatch.fnmatch(path, handoff) for handoff in owner.get("handoffPaths", []))
    })


def method_bodies(owner: lifecycle.TypeSpan | None) -> dict[str, str]:
    if owner is None:
        return {}
    result: dict[str, str] = {}
    depths = lifecycle.brace_depths(owner.body)
    for declaration in METHOD_RE.finditer(owner.body):
        if depths[declaration.start()] != 1:
            continue
        start, end = lifecycle.find_body(owner.body, declaration)
        result[declaration.group("name")] = owner.body[start:end]
    return result


def methods_matching(methods: dict[str, str], pattern: str) -> list[str]:
    matcher = re.compile(pattern)
    return sorted(name for name, body in methods.items() if matcher.search(body))


def teardown_path(methods: dict[str, str], action_methods: list[str]) -> list[str]:
    result: set[str] = set(name for name in action_methods if name in TEARDOWN_METHODS)
    for teardown in TEARDOWN_METHODS:
        if teardown not in methods:
            continue
        pending = [(teardown, methods[teardown])]
        visited = {teardown}
        while pending:
            path, body = pending.pop(0)
            for action in action_methods:
                if re.search(rf"\b{re.escape(action)}\s*\(", body):
                    result.add(f"{path}->{action}")
            for candidate, candidate_body in methods.items():
                if candidate in visited or not re.search(rf"\b{re.escape(candidate)}\s*\(", body):
                    continue
                visited.add(candidate)
                pending.append((f"{path}->{candidate}", candidate_body))
    return sorted(result)


def field_metadata(clean: str, spans: list[lifecycle.TypeSpan]) -> dict[tuple[str, int, str], dict[str, Any]]:
    result: dict[tuple[str, int, str], dict[str, Any]] = {}
    for match in lifecycle.field_rows(clean, spans):
        owner = lifecycle.owner_at(spans, match.start())
        line = clean.count("\n", 0, match.start()) + 1
        result[(owner.name if owner else "<file>", line, match.group("name"))] = {
            "modifiers": sorted(match.group("modifiers").split()),
            "owner": owner,
            "fieldType": " ".join(match.group("type").split()),
        }
    return result


def build_source_index(root: Path) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    partial_methods: dict[str, dict[str, str]] = {}
    for source in sorted((root / SOURCE_ROOT).rglob("*.cs")):
        path = relative(root, source)
        if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
            continue
        clean = lifecycle.strip_comments_and_strings(source.read_text(encoding="utf-8"))
        spans = lifecycle.type_spans(path, clean)
        result[path] = {
            "clean": clean,
            "spans": spans,
            "fields": field_metadata(clean, spans),
        }
        for span in spans:
            if not re.search(rf"\bpartial\s+(?:class|struct)\s+{re.escape(span.name)}\b", clean):
                continue
            partial_methods.setdefault(span.name, {}).update(method_bodies(span))
    for source in result.values():
        source["partialMethods"] = partial_methods
    return result


def owner_methods(source: dict[str, Any], owner: lifecycle.TypeSpan | None) -> dict[str, str]:
    methods = method_bodies(owner)
    if owner is not None and owner.name in source["partialMethods"]:
        methods.update(source["partialMethods"][owner.name])
    return methods


def native_rows(
    raw: dict[str, list[dict[str, Any]]],
    index: dict[str, dict[str, Any]],
    exclusions: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for path, source in index.items():
        for (owner_type, line, field_name), meta in source["fields"].items():
            field_type = meta["fieldType"].replace(" ", "")
            if not PERSISTENT_NATIVE_RE.fullmatch(field_type):
                continue
            owner = meta["owner"]
            methods = owner_methods(source, owner)
            field = re.escape(field_name)
            external_owner = EXTERNAL_NATIVE_OWNERS.get((path, owner_type, field_name))
            create = methods_matching(
                methods,
                rf"(?s)(?:\b{field}\b[^}};]*\bAllocator\.Persistent\b|\bAllocator\.Persistent\b[^}};]*\b{field}\b)",
            )
            dispose = methods_matching(methods, rf"\b{field}\s*\.\s*Dispose\s*\(")
            if not create and owner and "Allocator.Persistent" in owner.body and dispose:
                create = methods_matching(methods, rf"\b{field}\b")
            if not create and not external_owner:
                continue
            cleanup = teardown_path(methods, dispose)
            protected = protected_owner_ids(path, exclusions)
            creation_owner = external_owner[0] if external_owner else owner_type
            disposal_owner = external_owner[1] if external_owner else owner_type if cleanup else "unassigned"
            explicit = external_owner[2] if external_owner else bool(create and cleanup)
            rows.append({
                "path": path,
                "line": line,
                "ownerType": owner_type,
                "field": field_name,
                "fieldType": meta["fieldType"],
                "persistentAllocatorObserved": True,
                "creationMethods": create,
                "creationOwner": creation_owner,
                "disposalMethods": dispose,
                "disposalOwner": disposal_owner,
                "teardownPath": cleanup,
                "protectedOwnerIds": protected,
                "status": "protected-owner" if protected else "explicit" if explicit else "gap",
            })
    return rows


def persistent_query_rows(
    raw: dict[str, list[dict[str, Any]]],
    index: dict[str, dict[str, Any]],
    exclusions: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for item in raw["queryCaches"]:
        source = index[item["path"]]
        meta = source["fields"].get((item["ownerType"], item["line"], item["field"]))
        if meta is None:
            continue
        owner = meta["owner"]
        methods = owner_methods(source, owner)
        field = re.escape(item["field"])
        field_type = item["fieldType"].replace(" ", "")
        direct_query_type = bool(re.fullmatch(
            r"(?:EntityQuery|ComponentLookup<.+>|BufferLookup<.+>|[A-Za-z_]\w*QueryCache(?:<.+>)?|WorldScopedComponentQueryCache<.+>)",
            field_type,
        ))
        if not direct_query_type:
            continue
        is_ecs_system = bool(owner and re.search(r"\b(?:ISystem|SystemBase)\b", owner.bases))
        is_static = "static" in meta["modifiers"]
        assignment_methods = methods_matching(methods, rf"\b{field}\s*=(?!=)")
        cache_type = field_type.endswith("QueryCache") or "WorldScopedComponentQueryCache" in field_type
        is_lookup = field_type.startswith("ComponentLookup<") or field_type.startswith("BufferLookup<")
        if is_lookup and not is_ecs_system:
            continue
        if not (is_ecs_system or is_static or assignment_methods or cache_type):
            continue

        reset_methods = methods_matching(methods, rf"\b{field}\s*=\s*default\b")
        dispose_methods = methods_matching(methods, rf"\b{field}\s*\.\s*Dispose\s*\(")
        invalidate_methods = methods_matching(methods, rf"\b{field}\s*\.\s*Invalidate\s*\(")
        lifecycle_actions = sorted(set(reset_methods + dispose_methods + invalidate_methods))
        cleanup = teardown_path(methods, lifecycle_actions)
        protected = protected_owner_ids(item["path"], exclusions)
        if is_ecs_system:
            creation_owner = f"{item['ownerType']}.OnCreate/SystemState"
            disposal_owner = "owning World/SystemState"
            lifecycle_kind = "ecs-world-owned"
            explicit = True
        elif dispose_methods and cleanup:
            creation_owner = item["ownerType"]
            disposal_owner = item["ownerType"]
            lifecycle_kind = "owner-disposed-cache"
            explicit = True
        elif field_type == "EntityQuery" and assignment_methods:
            creation_owner = item["ownerType"]
            disposal_owner = "creating World"
            lifecycle_kind = "world-owned-borrowed-handle"
            explicit = not is_static or bool(cleanup)
        else:
            creation_owner = item["ownerType"]
            disposal_owner = item["ownerType"] if cleanup else "unassigned"
            lifecycle_kind = "owner-reset-cache" if cleanup else "unassigned-cache"
            explicit = bool(cleanup)
        status = "protected-owner" if protected else "explicit" if explicit else "gap"
        rows.append({
            **item,
            "assignmentMethods": assignment_methods,
            "creationOwner": creation_owner,
            "disposalOwner": disposal_owner,
            "lifecycleKind": lifecycle_kind,
            "resetMethods": reset_methods,
            "disposeMethods": dispose_methods,
            "invalidateMethods": invalidate_methods,
            "teardownPath": cleanup,
            "protectedOwnerIds": protected,
            "status": status,
        })
    return rows


def event_rows(
    raw: dict[str, list[dict[str, Any]]],
    index: dict[str, dict[str, Any]],
    exclusions: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    for item in raw["subscriptions"]:
        if not EVENT_HANDLER_RE.search(item["handler"]):
            continue
        source = index[item["path"]]
        owner = next((span for span in source["spans"] if span.name == item["ownerType"]), None)
        methods = owner_methods(source, owner)
        unsubscribe_methods = methods_matching(
            methods,
            rf"{re.escape(item['target'])}\s*-=\s*{re.escape(item['handler'])}\s*;",
        )
        cleanup = teardown_path(methods, unsubscribe_methods)
        external_owner = EXTERNAL_EVENT_OWNERS.get((item["path"], item["ownerType"], item["target"]))
        release_api_owner = item["pairedUnsubscribeObserved"] and "ReleaseAll" in methods
        protected = protected_owner_ids(item["path"], exclusions)
        status = "protected-owner" if protected else "explicit" if cleanup or external_owner or release_api_owner else "gap"
        rows.append({
            **item,
            "creationOwner": item["ownerType"],
            "disposalOwner": external_owner if external_owner else item["ownerType"] if cleanup or release_api_owner else "unassigned",
            "unsubscribeMethods": unsubscribe_methods,
            "teardownPath": cleanup,
            "protectedOwnerIds": protected,
            "status": status,
        })
    return rows


def presentation_root_rows(
    raw: dict[str, list[dict[str, Any]]],
    index: dict[str, dict[str, Any]],
    exclusions: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    rows: list[dict[str, Any]] = []
    seen: set[tuple[str, str, str]] = set()
    for item in raw["sceneRoots"]:
        protected = protected_owner_ids(item["path"], exclusions)
        process_owned = item["dontDestroyOnLoad"]
        explicit_process_teardown = process_owned and any(
            method in TEARDOWN_METHODS for method in item["lifecycleMethods"]
        )
        disposal_owner = "Unity scene unload" if not process_owned else (
            item["ownerType"] if explicit_process_teardown else "unassigned"
        )
        key = (item["path"], item["ownerType"], "<component-root>")
        seen.add(key)
        rows.append({
            **item,
            "line": 1,
            "root": "<component-root>",
            "creationOwner": item["ownerType"],
            "disposalOwner": disposal_owner,
            "lifecycleKind": "process-root" if process_owned else "scene-root",
            "protectedOwnerIds": protected,
            "status": "protected-owner" if protected else "explicit" if not process_owned or explicit_process_teardown else "gap",
        })

    for path, source in index.items():
        for match in ROOT_CREATION_RE.finditer(source["clean"]):
            target = match.group("target")
            if "root" not in target.lower():
                continue
            owner = lifecycle.owner_at(source["spans"], match.start())
            owner_name = owner.name if owner else "<file>"
            key = (path, owner_name, target)
            if key in seen:
                continue
            seen.add(key)
            methods = owner_methods(source, owner)
            destroy_methods = methods_matching(
                methods,
                rf"[A-Za-z_]*Destroy[A-Za-z_]*\s*\(\s*(?:{re.escape(target)}|{re.escape(target)}\.gameObject)\s*\)",
            )
            transfer_fields = sorted(set(re.findall(
                rf"\b(?P<field>_[A-Za-z_]\w*)\s*=\s*{re.escape(target)}(?:\.transform)?\s*;",
                owner.body if owner else "",
            )))
            for field_name in transfer_fields:
                destroy_methods.extend(methods_matching(
                    methods,
                    rf"[A-Za-z_]*Destroy[A-Za-z_]*\s*\(\s*{re.escape(field_name)}(?:\.gameObject)?\s*\)",
                ))
            destroy_methods = sorted(set(destroy_methods))
            cleanup = teardown_path(methods, destroy_methods)
            hierarchy_transfer = bool(re.search(
                rf"\b{re.escape(target)}(?:\.transform)?\.SetParent\s*\(",
                owner.body if owner else "",
            ))
            return_transfer = bool(re.search(
                rf"\breturn\s+{re.escape(target)}(?:\.transform)?\s*;",
                owner.body if owner else "",
            ))
            constructor_slice = source["clean"][match.start():match.start() + 180]
            named_root = bool(re.search(r"new\s+(?:GameObject\s*)?\([^;\n]*Root", constructor_slice))
            if not (target.startswith("_") or transfer_fields or return_transfer or named_root):
                continue
            protected = protected_owner_ids(path, exclusions)
            explicit = bool(cleanup or (hierarchy_transfer and return_transfer))
            disposal_owner = owner_name if cleanup else "returned parent hierarchy" if explicit else "unassigned"
            rows.append({
                "path": path,
                "line": source["clean"].count("\n", 0, match.start()) + 1,
                "ownerType": owner_name,
                "root": target,
                "creationOwner": owner_name,
                "disposalOwner": disposal_owner,
                "lifecycleKind": "runtime-created-root",
                "destroyMethods": destroy_methods,
                "ownershipTransfers": transfer_fields,
                "teardownPath": cleanup,
                "protectedOwnerIds": protected,
                "status": "protected-owner" if protected else "explicit" if explicit else "gap",
            })
    return sorted(rows, key=lambda row: (row["path"], row["ownerType"], row["line"], row["root"]))


def build_report(root: Path, baseline_ref: str = "HEAD") -> dict[str, Any]:
    exclusions = load_exclusions(root)
    index = build_source_index(root)
    raw = lifecycle.scan_lifecycle(root)
    categories = {
        "persistentNativeContainers": native_rows(raw, index, exclusions),
        "persistentQueries": persistent_query_rows(raw, index, exclusions),
        "eventSubscriptions": event_rows(raw, index, exclusions),
        "presentationRoots": presentation_root_rows(raw, index, exclusions),
    }
    for rows in categories.values():
        rows.sort(key=lambda row: (row["path"], row.get("ownerType", ""), row.get("line", 0), row.get("field", row.get("root", ""))))
    manifest = source_manifest(root)
    status_counts = {
        status: sum(1 for rows in categories.values() for row in rows if row["status"] == status)
        for status in ("explicit", "gap", "protected-owner")
    }
    return {
        "schemaVersion": SCHEMA_VERSION,
        "artifactId": ARTIFACT_ID,
        "baseline": {
            "branch": git_value(root, "branch", "--show-current"),
            "commit": git_value(root, "rev-parse", baseline_ref),
            "tree": git_value(root, "rev-parse", f"{baseline_ref}^{{tree}}"),
        },
        "scope": {
            "sourceRoot": SOURCE_ROOT,
            "productionFileCount": len(manifest),
            "sourceManifestSha256": sha256_bytes("".join(
                f"{row['path']}\0{row['sha256']}\n" for row in manifest
            ).encode("utf-8")),
        },
        "activeWorkExclusions": exclusions,
        "categories": categories,
        "summary": {
            "totalResourceCount": sum(len(rows) for rows in categories.values()),
            "explicitOwnerCount": status_counts["explicit"],
            "gapCount": status_counts["gap"],
            "protectedOwnerCount": status_counts["protected-owner"],
            "categoryCounts": {name: len(rows) for name, rows in categories.items()},
        },
        "sourceManifest": manifest,
    }


def render_markdown(report: dict[str, Any]) -> str:
    summary = report["summary"]
    lines = [
        "# AM-021 Persistent Resource Ownership Matrix",
        "",
        "> Generated by `python3 Tools/CI/architecture_persistent_resource_ownership.py`; do not edit manually.",
        "",
        f"- Baseline commit: `{report['baseline']['commit']}`",
        f"- Production C# files scanned: {report['scope']['productionFileCount']}",
        f"- Persistent resources: {summary['totalResourceCount']}",
        f"- Explicit / gap / protected-owner: {summary['explicitOwnerCount']} / {summary['gapCount']} / {summary['protectedOwnerCount']}",
        "",
        "Protected-owner rows remain visible but are not modified or accepted by AM-021.",
        "",
    ]
    for category, rows in report["categories"].items():
        lines.extend([
            f"## {category}",
            "",
            "| Status | Resource | Creation owner | Disposal owner | Path | Line |",
            "|---|---|---|---|---|---:|",
        ])
        for row in rows:
            resource = row.get("field", row.get("root", row.get("target", "<resource>")))
            lines.append(
                f"| `{row['status']}` | `{resource}` | `{row['creationOwner']}` | "
                f"`{row['disposalOwner']}` | `{row['path']}` | {row.get('line', 0)} |"
            )
        lines.append("")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--json", default=DEFAULT_JSON)
    parser.add_argument("--markdown", default=DEFAULT_MARKDOWN)
    parser.add_argument("--baseline-ref")
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    root = args.root.resolve()
    json_path = root / args.json
    baseline_ref = args.baseline_ref
    if args.check and baseline_ref is None:
        if not json_path.exists():
            raise SystemExit(f"missing AM-021 ownership artifact: {relative(root, json_path)}")
        try:
            baseline_ref = json.loads(json_path.read_text(encoding="utf-8"))["baseline"]["commit"]
        except (KeyError, TypeError, json.JSONDecodeError) as error:
            raise SystemExit(f"invalid AM-021 baseline identity: {error}") from error
    report = build_report(root, baseline_ref or "HEAD")
    json_content = json.dumps(report, indent=2, sort_keys=True) + "\n"
    markdown_content = render_markdown(report)
    outputs = ((json_path, json_content), (root / args.markdown, markdown_content))
    if args.check:
        stale = [relative(root, path) for path, content in outputs if not path.exists() or path.read_text(encoding="utf-8") != content]
        if stale:
            raise SystemExit("stale AM-021 ownership artifacts: " + ", ".join(stale))
        return 0
    for path, content in outputs:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
