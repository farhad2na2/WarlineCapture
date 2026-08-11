#!/usr/bin/env python3
"""Generate the deterministic AM-007 lifecycle ownership inventory."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
from collections import Counter
from dataclasses import dataclass
from pathlib import Path
from typing import Any


SCHEMA_VERSION = 1
DEFAULT_JSON = "Design/AgentReports/ArchitectureMaturity/lifecycle_inventory.json"
DEFAULT_MARKDOWN = "Design/AgentReports/ArchitectureMaturity/lifecycle_inventory.md"
SOURCE_ROOT = "Assets/Game/Scripts"
AUTHORITY_PATHS = (
    "Design/AgentReports/2026-07-10_aph-703_default-world-and-gc-owner-inventory.md",
    "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json",
    "Design/AgentReports/ArchitectureMaturity/validator_registry.json",
    "Design/Architecture/phase7_monobehaviour_loop_baseline.md",
    "Design/Architecture/systembase_to_isystem_inventory.md",
)

TYPE_RE = re.compile(
    r"^[ \t]*(?:(?:\[[^\]\r\n]*(?:\r?\n[ \t]*\[[^\]\r\n]*)*\][ \t]*)\r?\n[ \t]*)*"
    r"(?:(?:public|internal|private|protected|sealed|abstract|static|partial|readonly|unsafe)\s+)*"
    r"(?P<kind>class|struct)\s+(?P<name>[A-Za-z_]\w*)"
    r"(?:\s*<[^>{};\r\n]+>)?\s*(?P<bases>:[^{;]+)?",
    re.MULTILINE,
)
FIELD_RE = re.compile(
    r"^[ \t]*(?P<modifiers>(?:(?:public|internal|private|protected|new|unsafe|volatile|static|readonly)\s+)*)"
    r"(?P<type>[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*"
    r"(?:\s*<[^;={}\r\n]+>)?(?:\s*\[\s*,*\s*\])*\s*\??)\s+"
    r"(?P<name>[A-Za-z_]\w*)\s*(?:=(?!>)|;)",
    re.MULTILINE,
)
SUBSCRIPTION_RE = re.compile(
    r"(?P<target>[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*\+=\s*"
    r"(?P<handler>(?:[A-Za-z_]\w*\.)*[A-Z][A-Za-z0-9_]*)\s*;"
)
LIFECYCLE_METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|sealed|override|virtual|abstract|async|unsafe|new|partial)\s+)*"
    r"(?:[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?:\s*<[^>{};\r\n]+>)?(?:\s*\[\])?\??\s+)"
    r"(?P<name>Awake|Start|OnEnable|OnDisable|OnCreate|OnDestroy|Dispose|Shutdown)\s*"
    r"\([^;{}]*\)\s*(?:where[^{}]+)?(?=\{)",
    re.MULTILINE,
)
LIFECYCLE_EXPRESSION_METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|sealed|override|virtual|abstract|async|unsafe|new|partial)\s+)*"
    r"(?:[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?:\s*<[^>{};\r\n]+>)?(?:\s*\[\])?\??\s+)"
    r"(?P<name>Awake|Start|OnEnable|OnDisable|OnCreate|OnDestroy|Dispose|Shutdown)\s*"
    r"\([^;{}]*\)\s*=>\s*(?P<body>[^;{}]*);",
    re.MULTILINE,
)
NATIVE_TYPE_RE = re.compile(
    r"\b(?:Native(?:Parallel)?(?:Array|List|HashMap|HashSet|MultiHashMap|Queue|Reference|Stream)|"
    r"Unsafe(?:List|HashMap|ParallelHashMap|ParallelMultiHashMap|Queue|RingQueue))\b"
)
QUERY_TYPE_RE = re.compile(r"\b(?:EntityQuery|ComponentLookup|BufferLookup|[A-Za-z_]\w*QueryCache)\b")
STATIC_CACHE_TOKEN_RE = re.compile(r"(?:cache|lookup|registry|dictionary|hashset)", re.IGNORECASE)
SCENE_ROOT_NAME_RE = re.compile(r"(?:Bootstrap|Scene|Root|Runtime|Host).*View$|(?:Bootstrap|Scene)Root$")
PRESENTATION_OWNER_RE = re.compile(r"(?:Presentation|Visual|Vfx|UI|Ui|AudioPlayback)")
POOL_COLLECTION_TYPE_RE = re.compile(
    r"(?:\b(?:Stack|Queue|List|Dictionary|HashSet|IReadOnlyList|IReadOnlyDictionary|ObjectPool)\b|"
    r"\b[A-Za-z_]\w*Pool(?:<|\b)|\bPooled[A-Za-z_]\w*)"
)
TEARDOWN_METHODS = frozenset({"OnDisable", "OnDestroy", "Dispose", "Shutdown"})


@dataclass(frozen=True)
class TypeSpan:
    path: str
    name: str
    kind: str
    bases: str
    start: int
    end: int
    body: str
    methods: tuple[str, ...]


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def relative(root: Path, path: Path) -> str:
    return path.relative_to(root).as_posix()


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


def find_body(text: str, declaration: re.Match[str]) -> tuple[int, int]:
    opening = text.find("{", declaration.end())
    if opening < 0:
        return declaration.end(), declaration.end()
    depth = 0
    for index in range(opening, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return opening, index + 1
    return opening, len(text)


def type_spans(path: str, clean: str) -> list[TypeSpan]:
    result: list[TypeSpan] = []
    for declaration in TYPE_RE.finditer(clean):
        start, end = find_body(clean, declaration)
        body = clean[start:end]
        depths = brace_depths(body)
        methods = tuple(sorted({
            match.group("name")
            for method_re in (LIFECYCLE_METHOD_RE, LIFECYCLE_EXPRESSION_METHOD_RE)
            for match in method_re.finditer(body)
            if depths[match.start()] == 1
        }))
        result.append(TypeSpan(
            path,
            declaration.group("name"),
            declaration.group("kind"),
            " ".join((declaration.group("bases") or "").lstrip(":").split()),
            start,
            end,
            body,
            methods,
        ))
    return result


def owner_at(spans: list[TypeSpan], position: int) -> TypeSpan | None:
    owners = [span for span in spans if span.start <= position < span.end]
    return min(owners, key=lambda span: span.end - span.start) if owners else None


def owner_key(owner: TypeSpan | None, path: str) -> tuple[str, str]:
    return path, owner.name if owner else "<file>"


def cleanup_methods(owner: TypeSpan | None) -> list[str]:
    return list(owner.methods) if owner else []


def brace_depths(clean: str) -> list[int]:
    depths = [0] * (len(clean) + 1)
    depth = 0
    for index, char in enumerate(clean):
        depths[index] = depth
        if char == "{":
            depth += 1
        elif char == "}":
            depth = max(0, depth - 1)
    depths[len(clean)] = depth
    return depths


def field_rows(clean: str, spans: list[TypeSpan]) -> list[re.Match[str]]:
    depths = brace_depths(clean)
    result: list[re.Match[str]] = []
    for match in FIELD_RE.finditer(clean):
        owner = owner_at(spans, match.start())
        if owner is None:
            continue
        member_depth = depths[owner.start] + 1
        if depths[match.start()] == member_depth:
            result.append(match)
    return result


def lifecycle_method_bodies(owner: TypeSpan | None) -> dict[str, list[str]]:
    if owner is None:
        return {}
    result: dict[str, list[str]] = {}
    depths = brace_depths(owner.body)
    for declaration in LIFECYCLE_METHOD_RE.finditer(owner.body):
        if depths[declaration.start()] != 1:
            continue
        start, end = find_body(owner.body, declaration)
        result.setdefault(declaration.group("name"), []).append(owner.body[start:end])
    for declaration in LIFECYCLE_EXPRESSION_METHOD_RE.finditer(owner.body):
        if depths[declaration.start()] == 1:
            result.setdefault(declaration.group("name"), []).append(declaration.group("body"))
    return result


def lifecycle_observations(
    owner: TypeSpan | None,
    pattern: str,
    allowed_methods: frozenset[str] = TEARDOWN_METHODS,
) -> list[str]:
    observed: list[str] = []
    for method_name, bodies in lifecycle_method_bodies(owner).items():
        if method_name in allowed_methods and any(re.search(pattern, body) for body in bodies):
            observed.append(method_name)
    return sorted(observed)


def scan_lifecycle(root: Path) -> dict[str, list[dict[str, Any]]]:
    worlds: dict[tuple[str, str], dict[str, Any]] = {}
    native: list[dict[str, Any]] = []
    queries: list[dict[str, Any]] = []
    pools: list[dict[str, Any]] = []
    scene_roots: list[dict[str, Any]] = []
    subscriptions: list[dict[str, Any]] = []
    static_caches: list[dict[str, Any]] = []

    for source in sorted((root / SOURCE_ROOT).rglob("*.cs")):
        path = relative(root, source)
        if "/Editor/" in path or path.startswith("Assets/Game/Scripts/Editor/"):
            continue
        clean = strip_comments_and_strings(source.read_text(encoding="utf-8"))
        spans = type_spans(path, clean)
        fields = field_rows(clean, spans)

        for owner in spans:
            mono = re.search(r"\b(?:MonoBehaviour|UnityEngine\.MonoBehaviour)\b", owner.bases)
            root_signal = (
                SCENE_ROOT_NAME_RE.search(owner.name)
                or "DontDestroyOnLoad" in owner.body
                or "SceneManager." in owner.body
            )
            if mono and root_signal:
                scene_roots.append({
                    "dontDestroyOnLoad": "DontDestroyOnLoad" in owner.body,
                    "lifecycleMethods": cleanup_methods(owner),
                    "ownerType": owner.name,
                    "path": path,
                    "sceneEventAccess": "SceneManager." in owner.body,
                })

        for match in re.finditer(r"\bWorld\.DefaultGameObjectInjectionWorld\b", clean):
            owner = owner_at(spans, match.start())
            key = owner_key(owner, path)
            row = worlds.setdefault(key, {
                "defaultWorldAccessCount": 0,
                "lifecycleMethods": cleanup_methods(owner),
                "ownerType": owner.name if owner else "<file>",
                "path": path,
                "worldFields": [],
            })
            row["defaultWorldAccessCount"] += 1

        for match in fields:
            owner = owner_at(spans, match.start())
            field_type = " ".join(match.group("type").split())
            field_name = match.group("name")
            modifiers = match.group("modifiers").split()
            line = clean.count("\n", 0, match.start()) + 1
            body = owner.body if owner else clean
            base = {
                "field": field_name,
                "fieldType": field_type,
                "lifecycleMethods": cleanup_methods(owner),
                "line": line,
                "ownerType": owner.name if owner else "<file>",
                "path": path,
            }
            if re.fullmatch(r"(?:Unity\.Entities\.)?World\??", field_type.replace(" ", "")):
                key = owner_key(owner, path)
                row = worlds.setdefault(key, {
                    "defaultWorldAccessCount": 0,
                    "lifecycleMethods": cleanup_methods(owner),
                    "ownerType": owner.name if owner else "<file>",
                    "path": path,
                    "worldFields": [],
                })
                row["worldFields"].append(field_name)
                row["worldFields"] = sorted(set(row["worldFields"]))
            if NATIVE_TYPE_RE.search(field_type):
                cleanup = lifecycle_observations(
                    owner,
                    rf"\b{re.escape(field_name)}\s*\.\s*Dispose\s*\(",
                )
                native.append({
                    **base,
                    "cleanupMethodsObserved": cleanup,
                    "cleanupObserved": bool(cleanup),
                    "persistentAllocatorObserved": bool(re.search(
                        rf"\b{re.escape(field_name)}\s*=\s*[^;]*\bAllocator\.Persistent\b",
                        body,
                    )),
                })
            if QUERY_TYPE_RE.search(field_type):
                system_owned = bool(owner and re.search(r"\b(?:ISystem|SystemBase)\b", owner.bases))
                queries.append({
                    **base,
                    "lifecycleDisposition": "ecs-system-owned" if system_owned else (
                        "explicit-dispose" if re.search(rf"\b{re.escape(field_name)}\s*\.\s*Dispose\s*\(", body)
                        else "classification-required"
                    ),
                })
            presentation_owner = bool(owner and PRESENTATION_OWNER_RE.search(owner.name)) or any(
                marker in path for marker in ("/Audio/", "/Effects/", "/UI/")
            ) or "presentation" in field_name.lower()
            pool_token = "pool" in field_name.lower() or "pool" in field_type.lower()
            if presentation_owner and pool_token and POOL_COLLECTION_TYPE_RE.search(field_type):
                cleanup = lifecycle_observations(
                    owner,
                    rf"\b{re.escape(field_name)}\s*\.\s*(?:Clear|Dispose)\s*\(",
                )
                pools.append({
                    **base,
                    "cleanupMethodsObserved": cleanup,
                    "cleanupObserved": bool(cleanup),
                })
            if "static" in modifiers and STATIC_CACHE_TOKEN_RE.search(f"{field_type} {field_name}"):
                reset = lifecycle_observations(
                    owner,
                    rf"\b{re.escape(field_name)}\s*\.\s*(?:Clear|Dispose)\s*\(",
                )
                static_caches.append({
                    **base,
                    "readonly": "readonly" in modifiers,
                    "resetMethodsObserved": reset,
                    "resetObserved": bool(reset),
                })

        for match in SUBSCRIPTION_RE.finditer(clean):
            owner = owner_at(spans, match.start())
            target = match.group("target")
            handler = match.group("handler")
            body = owner.body if owner else clean
            unsubscribe_pattern = rf"{re.escape(target)}\s*-=\s*{re.escape(handler)}\s*;"
            unsubscribe_methods = lifecycle_observations(
                owner,
                unsubscribe_pattern,
            )
            paired_unsubscribe = re.search(unsubscribe_pattern, body) is not None
            handler_name = handler.rsplit(".", 1)[-1]
            handler_callable_observed = re.search(
                rf"\b{re.escape(handler_name)}\s*\(",
                body,
            ) is not None
            if not paired_unsubscribe and not handler_callable_observed:
                continue
            subscriptions.append({
                "handler": handler,
                "lifecycleMethods": cleanup_methods(owner),
                "line": clean.count("\n", 0, match.start()) + 1,
                "ownerType": owner.name if owner else "<file>",
                "pairedUnsubscribeObserved": paired_unsubscribe,
                "path": path,
                "target": target,
                "teardownUnsubscribeObserved": bool(unsubscribe_methods),
                "unsubscribeMethodsObserved": unsubscribe_methods,
            })

    key = lambda item: (item["path"], item.get("ownerType", ""), item.get("line", 0), item.get("field", ""))
    return {
        "nativeContainers": sorted(native, key=key),
        "presentationPools": sorted(pools, key=key),
        "queryCaches": sorted(queries, key=key),
        "sceneRoots": sorted(scene_roots, key=key),
        "staticCaches": sorted(static_caches, key=key),
        "subscriptions": sorted(subscriptions, key=key),
        "worlds": [worlds[key] for key in sorted(worlds)],
    }


def source_authorities(root: Path) -> list[dict[str, str]]:
    result: list[dict[str, str]] = []
    for relative_path in AUTHORITY_PATHS:
        path = root / relative_path
        if not path.is_file():
            raise ValueError(f"required authority is missing: {relative_path}")
        result.append({"path": relative_path, "sha256": sha256(path)})
    return result


def active_exclusions(root: Path) -> list[dict[str, Any]]:
    path = root / "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
    data = json.loads(path.read_text(encoding="utf-8"))
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


def build_inventory(root: Path, revision: str, tree: str) -> dict[str, Any]:
    if not re.fullmatch(r"[0-9a-f]{40}", revision) or not re.fullmatch(r"[0-9a-f]{40}", tree):
        raise ValueError("revision and tree must be exact 40-character lowercase Git identities")
    categories = scan_lifecycle(root)
    summary = {
        "nativeContainerCount": len(categories["nativeContainers"]),
        "nativeWithoutObservedCleanupCount": sum(not item["cleanupObserved"] for item in categories["nativeContainers"]),
        "persistentNativeContainerCount": sum(item["persistentAllocatorObserved"] for item in categories["nativeContainers"]),
        "presentationPoolCount": len(categories["presentationPools"]),
        "presentationPoolWithoutObservedCleanupCount": sum(not item["cleanupObserved"] for item in categories["presentationPools"]),
        "queryCacheCount": len(categories["queryCaches"]),
        "queryClassificationRequiredCount": sum(item["lifecycleDisposition"] == "classification-required" for item in categories["queryCaches"]),
        "sceneRootCount": len(categories["sceneRoots"]),
        "staticCacheCount": len(categories["staticCaches"]),
        "staticCacheWithoutObservedResetCount": sum(not item["resetObserved"] for item in categories["staticCaches"]),
        "subscriptionCount": len(categories["subscriptions"]),
        "subscriptionWithoutObservedUnsubscribeCount": sum(not item["pairedUnsubscribeObserved"] for item in categories["subscriptions"]),
        "subscriptionWithoutDirectTeardownUnsubscribeCount": sum(not item["teardownUnsubscribeObserved"] for item in categories["subscriptions"]),
        "worldOwnerCount": len(categories["worlds"]),
        "worldDefaultAccessCount": sum(item["defaultWorldAccessCount"] for item in categories["worlds"]),
    }
    return {
        "activeWorkExclusions": active_exclusions(root),
        "artifactId": "AM-007",
        "baseline": {"branch": "main", "commit": revision, "tree": tree},
        "categories": categories,
        "determinism": {
            "arrays": "All lifecycle rows sort by path, owner type, source line, and field.",
            "serialization": "UTF-8, LF, two-space indentation, lexicographic object keys, one trailing LF.",
            "timestamps": "Excluded.",
        },
        "policy": {
            "candidateSemantics": "Lexical rows identify lifecycle review candidates; missing observed cleanup is not automatically a leak.",
            "requiredOwnerFields": ["creator", "capacityPolicy", "disposer", "lifecycleTest"],
            "worldRule": "Global default-world access remains boundary evidence or dependency-injection debt, never implicit gameplay authority.",
        },
        "schemaVersion": SCHEMA_VERSION,
        "scope": {
            "editorExcluded": True,
            "sourceRoot": SOURCE_ROOT,
        },
        "sourceAuthorities": source_authorities(root),
        "summary": summary,
    }


def render_markdown(data: dict[str, Any]) -> str:
    summary = data["summary"]
    lines = [
        "# Architecture Maturity Lifecycle Inventory",
        "",
        "> Generated by `python3 Tools/CI/architecture_lifecycle_inventory.py`; do not edit manually.",
        "",
        f"- Baseline commit: `{data['baseline']['commit']}`",
        f"- Baseline tree: `{data['baseline']['tree']}`",
        f"- World owners / default-world accesses: {summary['worldOwnerCount']} / {summary['worldDefaultAccessCount']}",
        f"- Native-container candidates: {summary['nativeContainerCount']}",
        f"- Persistent allocator observed: {summary['persistentNativeContainerCount']}",
        f"- Query-cache candidates: {summary['queryCacheCount']}",
        f"- Presentation-pool candidates: {summary['presentationPoolCount']}",
        f"- Scene-root candidates: {summary['sceneRootCount']}",
        f"- Event subscriptions: {summary['subscriptionCount']}",
        f"- Static-cache candidates: {summary['staticCacheCount']}",
        "",
        "Rows are lexical lifecycle candidates. A missing observed cleanup/reset is a classification requirement, not proof of a leak.",
        "",
        "## Lifecycle Attention",
        "",
        "| Category | Total | Needs classification |",
        "|---|---:|---:|",
        f"| Native containers | {summary['nativeContainerCount']} | {summary['nativeWithoutObservedCleanupCount']} |",
        f"| Query caches | {summary['queryCacheCount']} | {summary['queryClassificationRequiredCount']} |",
        f"| Presentation pools | {summary['presentationPoolCount']} | {summary['presentationPoolWithoutObservedCleanupCount']} |",
        f"| Event subscriptions without any matching unsubscribe | {summary['subscriptionCount']} | {summary['subscriptionWithoutObservedUnsubscribeCount']} |",
        f"| Event subscriptions without direct teardown unsubscribe | {summary['subscriptionCount']} | {summary['subscriptionWithoutDirectTeardownUnsubscribeCount']} |",
        f"| Static caches | {summary['staticCacheCount']} | {summary['staticCacheWithoutObservedResetCount']} |",
        "",
        "## World Owners",
        "",
        "| Owner | Default-world accesses | World fields | Lifecycle methods | Path |",
        "|---|---:|---|---|---|",
    ]
    for item in data["categories"]["worlds"]:
        fields = ", ".join(f"`{value}`" for value in item["worldFields"]) or "none"
        methods = ", ".join(f"`{value}`" for value in item["lifecycleMethods"]) or "none"
        lines.append(
            f"| `{item['ownerType']}` | {item['defaultWorldAccessCount']} | {fields} | {methods} | `{item['path']}` |"
        )
    lines.extend([
        "",
        "## Protected Ownership Lanes",
        "",
        "| Owner | Status | Authority |",
        "|---|---|---|",
    ])
    for item in data["activeWorkExclusions"]:
        lines.append(f"| `{item['id']}` | {item['status']} | `{item['authorityPath']}` |")
    lines.extend([
        "",
        "Full deterministic rows for every native container, query cache, presentation pool, scene root, subscription, static cache, authority hash, and protected path are in `lifecycle_inventory.json`.",
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
        print(f"[ArchitectureLifecycleInventory] result=Failed reason={error}")
        return 2
    print(
        "[ArchitectureLifecycleInventory] result=Generated "
        f"worlds={data['summary']['worldOwnerCount']} native={data['summary']['nativeContainerCount']} "
        f"queries={data['summary']['queryCacheCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
