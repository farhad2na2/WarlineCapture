#!/usr/bin/env python3
"""Generate the deterministic AM-009 production-owner risk ranking."""

from __future__ import annotations

import argparse
import fnmatch
import hashlib
import json
import re
import subprocess
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Iterable

import architecture_lifecycle_inventory as lifecycle_scanner


SCHEMA_VERSION = 1
SOURCE_ROOT = "Assets/Game/Scripts"
DEFAULT_JSON = "Design/AgentReports/ArchitectureMaturity/owner_risk_ranking.json"
DEFAULT_MARKDOWN = "Design/AgentReports/ArchitectureMaturity/owner_risk_ranking.md"
OWNERSHIP_PATH = "Design/AgentReports/ArchitectureMaturity/ownership_inventory.json"
LIFECYCLE_PATH = "Design/AgentReports/ArchitectureMaturity/lifecycle_inventory.json"
ASSEMBLY_PATH = "Design/AgentReports/2026-07-10_aph-700_first_party_assembly_dependencies.json"
CURRENT_RUNTIME_PATH = "Design/AgentReports/ArchitectureMaturity/owner_runtime_measurements.json"
TOOL_PATH = "Tools/CI/architecture_owner_risk_ranking.py"
TEST_PATH = "Tools/CI/tests/test_architecture_owner_risk_ranking.py"
HISTORY_COMMIT_LIMIT = 500
FIRST_WAVE_LIMIT = 3

TYPE_RE = re.compile(
    r"\b(?:class|struct|interface|enum|record)\s+(?:class\s+|struct\s+)?(?P<name>[A-Za-z_]\w*)"
)
METHOD_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|static|sealed|override|virtual|abstract|async|unsafe|new|partial|readonly)\s+)*"
    r"(?:[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*(?:\s*<[^>{};\r\n]+>)?(?:\s*\[\])?\??\s+)"
    r"(?P<name>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?:where[^{}]+)?(?=\{|=>)",
    re.MULTILINE,
)
IDENTIFIER_RE = re.compile(r"\b[A-Za-z_]\w*\b")
UPDATE_METHODS = frozenset({"Update", "LateUpdate", "FixedUpdate", "OnUpdate"})
FIELD_STATEMENT_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|new|unsafe|volatile|static|readonly|const|event)\s+)*"
    r"[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*(?:\s*<[^;={}\r\n]+>)?"
    r"(?:\s*\[\s*,*\s*\])*\s*\??\s+(?P<declarators>[^;\r\n]+);",
    re.MULTILINE,
)
PROPERTY_RE = re.compile(
    r"^[ \t]*(?:(?:public|internal|private|protected|new|unsafe|static|virtual|override|sealed|abstract)\s+)*"
    r"[A-Za-z_]\w*(?:(?:\.|::)[A-Za-z_]\w*)*(?:\s*<[^;={}\r\n]+>)?"
    r"(?:\s*\[\s*,*\s*\])*\s*\??\s+[A-Za-z_]\w*\s*\{(?P<body>[^{}]*)\}",
    re.MULTILINE,
)

# Only focused measurements with a bounded owner mapping are scored. Static update
# exposure is reported separately and never promoted into measured timing evidence.
MEASURED_RUNTIME_EVIDENCE: tuple[dict[str, Any], ...] = (
    {
        "averageMilliseconds": 2.973,
        "attribution": "Focused pathfinding batch fixture mapped to the batch job implementation.",
        "currency": "historical-focused",
        "metric": "26.76 ms across 9 focused pathfinding updates (2.973 ms/update)",
        "path": "Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs",
        "selectionEligible": False,
        "source": "Design/AgentReports/ecs_burst_hot_path_baseline_2026-06-12.md",
    },
)

RESPONSIBILITY_AUDITS: tuple[dict[str, Any], ...] = (
    {
        "initialAllowedPaths": ["Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs"],
        "modificationScope": "pathfinding-batch",
        "path": "Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs",
        "responsibilities": [
            "consume and emit one pathfinding batch request/result",
            "run segmented and full path search with fallback",
            "evaluate surface, footprint, slope, and traversal cost",
            "own heap and scratch-node mechanics for the search",
        ],
    },
    {
        "initialAllowedPaths": ["Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs"],
        "modificationScope": "ground-missile-runtime",
        "path": "Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs",
        "responsibilities": [
            "consume missile fire requests and create projectiles",
            "project launcher battery rotation and rocket-slot visibility",
            "advance flying rocket visuals and trajectory arcs",
            "resolve impacts, damage state, and impact VFX requests",
        ],
    },
    {
        "initialAllowedPaths": ["Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs"],
        "modificationScope": "transport-boarding-runtime",
        "path": "Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs",
        "responsibilities": [
            "consume transport boarding requests",
            "reissue interrupted boarding movement",
            "enforce passenger capacity and occupancy by passenger kind",
            "evaluate reach and landed-state boarding eligibility",
        ],
    },
)


@dataclass(frozen=True)
class SourceFacts:
    path: str
    lines: int
    bytes: int
    types: tuple[str, ...]
    methods: tuple[str, ...]
    state_slots: int
    update_methods: tuple[str, ...]


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


def production_path(path: str) -> bool:
    return path.startswith(f"{SOURCE_ROOT}/") and "/Editor/" not in path


def count_top_level_declarators(value: str) -> int:
    depth = 0
    count = 1
    pairs = {"(": ")", "[": "]", "{": "}", "<": ">"}
    closing = set(pairs.values())
    for char in value:
        if char in pairs:
            depth += 1
        elif char in closing:
            depth = max(0, depth - 1)
        elif char == "," and depth == 0:
            count += 1
    return count


def state_slot_count(path: str, clean: str) -> int:
    spans = lifecycle_scanner.type_spans(path, clean)
    depths = lifecycle_scanner.brace_depths(clean)
    count = 0
    for match in FIELD_STATEMENT_RE.finditer(clean):
        owner = lifecycle_scanner.owner_at(spans, match.start())
        if owner is None or depths[match.start()] != depths[owner.start] + 1:
            continue
        declarators = match.group("declarators")
        declaration_head = declarators.split("=", 1)[0]
        if any(char in declaration_head for char in "(){}"):
            continue
        count += count_top_level_declarators(declarators)
    for match in PROPERTY_RE.finditer(clean):
        owner = lifecycle_scanner.owner_at(spans, match.start())
        if owner is None or depths[match.start()] != depths[owner.start] + 1:
            continue
        if re.search(r"\bset\s*;", match.group("body")):
            count += 1
    return count


def scan_sources(root: Path) -> tuple[dict[str, SourceFacts], dict[str, str]]:
    facts: dict[str, SourceFacts] = {}
    clean_by_path: dict[str, str] = {}
    for source in sorted((root / SOURCE_ROOT).rglob("*.cs")):
        path = relative(root, source)
        if not production_path(path):
            continue
        text = source.read_text(encoding="utf-8")
        clean = strip_comments_and_strings(text)
        methods = tuple(match.group("name") for match in METHOD_RE.finditer(clean))
        facts[path] = SourceFacts(
            path=path,
            lines=len(text.splitlines()),
            bytes=len(text.encode("utf-8")),
            types=tuple(sorted(set(match.group("name") for match in TYPE_RE.finditer(clean)))),
            methods=methods,
            state_slots=state_slot_count(path, clean),
            update_methods=tuple(sorted(name for name in methods if name in UPDATE_METHODS)),
        )
        clean_by_path[path] = clean
    return facts, clean_by_path


def dependency_counts(
    facts: dict[str, SourceFacts], clean_by_path: dict[str, str]
) -> tuple[dict[str, int], dict[str, int], dict[str, int]]:
    type_owners: dict[str, set[str]] = defaultdict(set)
    for path, item in facts.items():
        for name in item.types:
            type_owners[name].add(path)
    outgoing: dict[str, set[str]] = {path: set() for path in facts}
    incoming: dict[str, set[str]] = {path: set() for path in facts}
    ambiguous_by_path: dict[str, int] = {}
    for path, clean in clean_by_path.items():
        identifiers = set(IDENTIFIER_RE.findall(clean))
        ambiguous_by_path[path] = sum(len(type_owners.get(identifier, ())) > 1 for identifier in identifiers)
        for identifier in identifiers:
            targets = type_owners.get(identifier, ())
            if len(targets) != 1 or not type_reference_observed(clean, identifier):
                continue
            target = next(iter(targets))
            if target != path:
                outgoing[path].add(target)
                incoming[target].add(path)
    return (
        {path: len(targets) for path, targets in outgoing.items()},
        {path: len(sources) for path, sources in incoming.items()},
        ambiguous_by_path,
    )


def type_reference_observed(clean: str, type_name: str) -> bool:
    name = re.escape(type_name)
    patterns = (
        rf"\bnew\s+{name}\b",
        rf"\b(?:typeof|default|sizeof)\s*\(\s*{name}\b",
        rf"\b{name}\s*(?:<[^;{{}}()\r\n]*>)?\s*(?:\[\s*,*\s*\])?\s*\??\s+[A-Za-z_]\w*\s*(?:[;=,\)({{]|=>)",
        rf"\b{name}\s*\.",
        rf"\b(?:in|ref|out)\s+{name}\b",
        rf"\b(?:Get|Has|Set|Add|Remove)Component(?:Data)?\s*<\s*{name}\b",
    )
    return any(re.search(pattern, clean) for pattern in patterns)


def score_responsibilities(count: int) -> int:
    return min(4, max(0, count))


def score_coupling(fan_in: int, fan_out: int) -> int:
    total = fan_in + fan_out
    if total == 0:
        return 0
    if total <= 5:
        return 1
    if total <= 15:
        return 2
    if total <= 30:
        return 3
    return 4


def score_state(signals: int) -> int:
    if signals == 0:
        return 0
    if signals <= 2:
        return 1
    if signals <= 5:
        return 2
    if signals <= 10:
        return 3
    return 4


def score_change_frequency(commits: int) -> int:
    if commits == 0:
        return 0
    if commits <= 2:
        return 1
    if commits <= 5:
        return 2
    if commits <= 10:
        return 3
    return 4


def score_measured_runtime(average_milliseconds: float) -> int:
    if average_milliseconds <= 0.1:
        return 0
    if average_milliseconds <= 0.25:
        return 1
    if average_milliseconds <= 1.0:
        return 2
    if average_milliseconds <= 4.0:
        return 3
    return 4


def git_change_counts(root: Path, revision: str) -> dict[str, int]:
    command = [
        "git", "log", "--first-parent", f"--max-count={HISTORY_COMMIT_LIMIT}",
        "--format=commit:%H", "--numstat", revision, "--", SOURCE_ROOT,
    ]
    result = subprocess.run(command, cwd=root, check=True, capture_output=True, text=True)
    current_commit = ""
    touched: dict[str, set[str]] = defaultdict(set)
    for line in result.stdout.splitlines():
        if line.startswith("commit:"):
            current_commit = line[7:]
            continue
        parts = line.split("\t", 2)
        if current_commit and len(parts) == 3 and production_path(parts[2]):
            touched[parts[2]].add(current_commit)
    return {path: len(commits) for path, commits in touched.items()}


def load_json(root: Path, path: str) -> dict[str, Any]:
    source = root / path
    if not source.is_file():
        raise ValueError(f"required authority is missing: {path}")
    value = json.loads(source.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"authority must contain a JSON object: {path}")
    return value


def load_current_runtime_evidence(root: Path) -> tuple[dict[str, Any], ...]:
    data = load_json(root, CURRENT_RUNTIME_PATH)
    baseline = data.get("baseline")
    if not isinstance(baseline, dict):
        raise ValueError("current runtime evidence does not contain baseline identity")
    revision = baseline.get("commit")
    tree = baseline.get("tree")
    if not isinstance(revision, str) or not isinstance(tree, str):
        raise ValueError("current runtime evidence baseline identity is incomplete")
    actual_tree = subprocess.run(
        ["git", "rev-parse", f"{revision}^{{tree}}"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()
    if actual_tree != tree:
        raise ValueError("current runtime evidence commit/tree identity does not reconcile")
    environment = data.get("environment")
    if not isinstance(environment, dict):
        raise ValueError("current runtime evidence does not contain environment hashes")
    environment_paths = {
        "packagesLockSha256": "Packages/packages-lock.json",
        "projectVersionSha256": "ProjectSettings/ProjectVersion.txt",
    }
    for field, path in environment_paths.items():
        if environment.get(field) != sha256(root / path):
            raise ValueError(f"current runtime evidence environment hash is stale: {path}")
    measurements = data.get("measurements")
    if not isinstance(measurements, list) or not measurements:
        raise ValueError("current runtime evidence does not contain measurements")
    result: list[dict[str, Any]] = []
    for measurement in measurements:
        if not isinstance(measurement, dict):
            raise ValueError("current runtime evidence measurement is malformed")
        source_path = measurement.get("path")
        harness_path = measurement.get("harnessPath")
        if not isinstance(source_path, str) or not isinstance(harness_path, str):
            raise ValueError("current runtime evidence measurement paths are incomplete")
        if measurement.get("sourceSha256") != sha256(root / source_path):
            raise ValueError(f"current runtime evidence production hash is stale: {source_path}")
        if measurement.get("harnessSha256") != sha256(root / harness_path):
            raise ValueError(f"current runtime evidence harness hash is stale: {harness_path}")
        if measurement.get("result") != "Passed" or measurement.get("allocatedBytesCurrentThread") != 0:
            raise ValueError(f"current runtime evidence is not accepted: {source_path}")
        result.append({**measurement, "source": CURRENT_RUNTIME_PATH})
    return tuple(sorted(result, key=lambda item: item["path"]))


def lifecycle_counts(data: dict[str, Any]) -> dict[str, Counter[str]]:
    result: dict[str, Counter[str]] = defaultdict(Counter)
    categories = data.get("categories")
    if not isinstance(categories, dict):
        raise ValueError("lifecycle inventory does not contain categories")
    for category, rows in categories.items():
        if not isinstance(rows, list):
            raise ValueError(f"lifecycle category is not an array: {category}")
        for row in rows:
            if isinstance(row, dict) and isinstance(row.get("path"), str):
                result[row["path"]][category] += 1
    return result


def recursive_glob_match(path: str, pattern: str) -> bool:
    variants = {pattern}
    pending = [pattern]
    while pending:
        value = pending.pop()
        for match in re.finditer(r"\*\*/", value):
            without_segment = value[:match.start()] + value[match.end():]
            if without_segment not in variants:
                variants.add(without_segment)
                pending.append(without_segment)
    return any(fnmatch.fnmatchcase(path, value) for value in variants)


def protected_owner(path: str, ownership: dict[str, Any]) -> tuple[dict[str, Any], str] | None:
    section = ownership.get("activeWorkOwnership")
    owners = section.get("owners") if isinstance(section, dict) else None
    if not isinstance(owners, list):
        raise ValueError("ownership inventory does not contain activeWorkOwnership.owners")
    for owner in owners:
        if not isinstance(owner, dict):
            continue
        patterns = owner.get("protectedPaths")
        if isinstance(patterns, list):
            for pattern in patterns:
                if isinstance(pattern, str) and recursive_glob_match(path, pattern):
                    return owner, pattern
    return None


def runtime_evidence_by_path(entries: Iterable[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for entry in entries:
        path = entry.get("path")
        average = entry.get("averageMilliseconds")
        if (
            not isinstance(path, str)
            or not isinstance(average, (float, int))
            or isinstance(average, bool)
            or average < 0
            or not isinstance(entry.get("selectionEligible"), bool)
        ):
            raise ValueError("measured runtime evidence requires path, non-negative averageMilliseconds, and selectionEligible")
        if path in result:
            raise ValueError(f"duplicate measured runtime evidence: {path}")
        result[path] = {**entry, "score": score_measured_runtime(float(average))}
    return result


def responsibility_audits_by_path(entries: Iterable[dict[str, Any]]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for entry in entries:
        path = entry.get("path")
        responsibilities = entry.get("responsibilities")
        scope = entry.get("modificationScope")
        allowed_paths = entry.get("initialAllowedPaths")
        if (
            not isinstance(path, str)
            or not isinstance(scope, str)
            or not scope
            or not isinstance(responsibilities, list)
            or not responsibilities
            or any(not isinstance(value, str) or not value for value in responsibilities)
            or not isinstance(allowed_paths, list)
            or not allowed_paths
            or allowed_paths != sorted(set(allowed_paths))
            or any(not isinstance(value, str) or not value for value in allowed_paths)
        ):
            raise ValueError(
                "responsibility audit requires path, modificationScope, sorted initialAllowedPaths, and named responsibilities"
            )
        if path in result:
            raise ValueError(f"duplicate responsibility audit: {path}")
        result[path] = dict(entry)
    return result


def verify_git_identity(root: Path, revision: str, tree: str, governed_paths: Iterable[str]) -> None:
    commit_tree = subprocess.run(
        ["git", "rev-parse", f"{revision}^{{tree}}"],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()
    if commit_tree != tree:
        raise ValueError(f"tree {tree} does not belong to commit {revision}")
    head = subprocess.run(
        ["git", "rev-parse", "HEAD"], cwd=root, check=True, capture_output=True, text=True
    ).stdout.strip()
    if head != revision:
        raise ValueError(f"baseline commit {revision} is not current HEAD {head}")
    diff = subprocess.run(
        ["git", "diff", "--quiet", revision, "--", SOURCE_ROOT, *sorted(set(governed_paths))],
        cwd=root,
        check=False,
    )
    if diff.returncode != 0:
        raise ValueError("governed source or authority paths differ from the baseline commit")
    status = subprocess.run(
        [
            "git", "status", "--porcelain=v1", "--untracked-files=all", "--",
            SOURCE_ROOT, *sorted(set(governed_paths)),
        ],
        cwd=root,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()
    if status:
        raise ValueError("governed source or authority paths contain tracked or untracked worktree changes")


def selection_reason(row: dict[str, Any], selected: bool, selected_allowed_paths: set[str]) -> str:
    scores = row["scores"]
    vector = "/".join("-" if scores[key] is None else str(scores[key]) for key in (
        "responsibilityCount", "coupling", "stateOwnership", "measuredRuntimeCost", "changeFrequency"
    ))
    if row["editEligibility"] == "protected":
        return (
            f"Rejected from first wave: protected by {row['protectedOwnerId']} pattern "
            f"{row['protectedMatchedPattern']}; screening rank {row['screeningRank']}, "
            f"(R/C/S/M/F {vector}); retained for visibility."
        )
    missing = []
    if row["responsibilityAudit"] is None:
        missing.append("responsibility audit")
    if row["measuredRuntimeEvidence"] is None:
        missing.append("owner-attributable runtime measurement")
    if missing:
        return (
            f"Rejected from first wave: screening rank {row['screeningRank']}; missing "
            f"{' and '.join(missing)}; unmeasured dimensions remain null, not zero."
        )
    if not row["measuredRuntimeEvidence"]["selectionEligible"]:
        return (
            f"Rejected from first wave: candidate rank {row['candidateRank']}, score "
            f"{row['compositeScore']}/20 (R/C/S/M/F {vector}); timing is historical post-fix "
            "context and requires current characterization before extraction."
        )
    if selected:
        return (
            f"Selected: candidate rank {row['candidateRank']}, score {row['compositeScore']}/20 "
            f"(R/C/S/M/F {vector}); lines {row['lines']} used only as a tie-breaker."
        )
    overlap = set(row["responsibilityAudit"]["initialAllowedPaths"]) & selected_allowed_paths
    if overlap:
        return (
            f"Rejected from first wave: candidate rank {row['candidateRank']}, score {row['compositeScore']}/20 "
            f"(R/C/S/M/F {vector}); initial allowed path already selected: {sorted(overlap)[0]}."
        )
    return (
        f"Rejected from first wave: candidate rank {row['candidateRank']}, score {row['compositeScore']}/20 "
        f"(R/C/S/M/F {vector}); outside the three-owner maximum."
    )


def build_ranking(
    root: Path,
    revision: str,
    tree: str,
    *,
    changes: dict[str, int] | None = None,
    measured_runtime: Iterable[dict[str, Any]] | None = None,
    responsibility_audits: Iterable[dict[str, Any]] = RESPONSIBILITY_AUDITS,
    verify_git: bool = True,
) -> dict[str, Any]:
    if not re.fullmatch(r"[0-9a-f]{40}", revision) or not re.fullmatch(r"[0-9a-f]{40}", tree):
        raise ValueError("revision and tree must be exact 40-character lowercase Git identities")
    measured_runtime = (
        (*load_current_runtime_evidence(root), *MEASURED_RUNTIME_EVIDENCE)
        if measured_runtime is None
        else tuple(measured_runtime)
    )
    responsibility_audits = tuple(responsibility_audits)
    authority_paths = (OWNERSHIP_PATH, LIFECYCLE_PATH, ASSEMBLY_PATH, TOOL_PATH, TEST_PATH, *sorted({
        entry["source"] for entry in measured_runtime if isinstance(entry.get("source"), str)
    }))
    governed_paths = (*authority_paths, *sorted({
        entry["harnessPath"] for entry in measured_runtime if isinstance(entry.get("harnessPath"), str)
    }))
    if verify_git:
        verify_git_identity(root, revision, tree, governed_paths)
    ownership = load_json(root, OWNERSHIP_PATH)
    lifecycle = load_json(root, LIFECYCLE_PATH)
    assembly = load_json(root, ASSEMBLY_PATH)
    facts, clean_by_path = scan_sources(root)
    fan_out, fan_in, ambiguous_references = dependency_counts(facts, clean_by_path)
    lifecycle_by_path = lifecycle_counts(lifecycle)
    change_counts = changes if changes is not None else git_change_counts(root, revision)
    runtime_by_path = runtime_evidence_by_path(measured_runtime)
    audits_by_path = responsibility_audits_by_path(responsibility_audits)
    missing_evidence_paths = sorted((set(runtime_by_path) | set(audits_by_path)) - set(facts))
    if missing_evidence_paths:
        raise ValueError(f"evidence references missing production owners: {', '.join(missing_evidence_paths)}")
    rows: list[dict[str, Any]] = []
    for path, item in facts.items():
        responsibility_audit = audits_by_path.get(path)
        responsibility_count = len(responsibility_audit["responsibilities"]) if responsibility_audit else None
        lifecycle_categories = lifecycle_by_path.get(path, Counter())
        lifecycle_signal_count = sum(lifecycle_categories.values())
        state_signal_count = item.state_slots + lifecycle_signal_count
        runtime = runtime_by_path.get(path)
        protection = protected_owner(path, ownership)
        owner = protection[0] if protection else None
        matched_pattern = protection[1] if protection else None
        scores: dict[str, int | None] = {
            "changeFrequency": score_change_frequency(change_counts.get(path, 0)),
            "coupling": score_coupling(fan_in[path], fan_out[path]),
            "measuredRuntimeCost": runtime["score"] if runtime else None,
            "responsibilityCount": score_responsibilities(responsibility_count) if responsibility_count is not None else None,
            "stateOwnership": score_state(state_signal_count),
        }
        complete = all(value is not None for value in scores.values())
        screening_score = scores["coupling"] + scores["stateOwnership"] + scores["changeFrequency"]
        rows.append({
            "ambiguousSimpleTypeReferencesExcluded": ambiguous_references[path],
            "bytes": item.bytes,
            "changeCommitCount": change_counts.get(path, 0),
            "compositeScore": sum(value for value in scores.values() if value is not None) if complete else None,
            "candidateRank": None,
            "dependencyFanIn": fan_in[path],
            "dependencyFanOut": fan_out[path],
            "editEligibility": "protected" if owner else "eligible",
            "evidenceComplete": complete,
            "firstWaveSelected": False,
            "lifecycleSignals": dict(sorted(lifecycle_categories.items())),
            "lines": item.lines,
            "measuredRuntimeEvidence": runtime,
            "modificationScope": responsibility_audit["modificationScope"] if responsibility_audit else None,
            "stateSlotCount": item.state_slots,
            "path": path,
            "protectedOwnerId": owner.get("id") if owner else None,
            "protectedOwnerStatus": owner.get("status") if owner else None,
            "protectedAuthorityPath": owner.get("authorityPath") if owner else None,
            "protectedMatchedPattern": matched_pattern,
            "responsibilityAudit": responsibility_audit,
            "responsibilityCount": responsibility_count,
            "screeningScore": screening_score,
            "scores": scores,
            "updateExposure": {
                "methods": list(item.update_methods),
                "recurring": bool(item.update_methods),
                "scoreContribution": 0,
            },
        })
    rows.sort(key=lambda row: (-row["screeningScore"], -row["lines"], row["path"]))
    for rank, row in enumerate(rows, start=1):
        row["screeningRank"] = rank
    ranked_candidates = sorted(
        (row for row in rows if row["evidenceComplete"]),
        key=lambda row: (-row["compositeScore"], -row["lines"], row["path"]),
    )
    for rank, row in enumerate(ranked_candidates, start=1):
        row["candidateRank"] = rank
    selected_paths: list[str] = []
    selected_scopes: set[str] = set()
    selected_allowed_paths: set[str] = set()
    for row in ranked_candidates:
        if len(selected_paths) >= FIRST_WAVE_LIMIT:
            break
        if (
            row["editEligibility"] != "eligible"
            or not row["measuredRuntimeEvidence"]["selectionEligible"]
            or bool(set(row["responsibilityAudit"]["initialAllowedPaths"]) & selected_allowed_paths)
        ):
            continue
        selected_paths.append(row["path"])
        selected_scopes.add(row["modificationScope"])
        selected_allowed_paths.update(row["responsibilityAudit"]["initialAllowedPaths"])
    for row in rows:
        row["firstWaveSelected"] = row["path"] in selected_paths
        row["selectionReason"] = selection_reason(row, row["firstWaveSelected"], selected_allowed_paths)

    authorities = []
    for path in authority_paths:
        authority = root / path
        if not authority.is_file():
            raise ValueError(f"required authority is missing: {path}")
        authorities.append({"path": path, "sha256": sha256(authority)})
    return {
        "artifactId": "AM-009",
        "baseline": {"branch": "main", "commit": revision, "tree": tree},
        "determinism": {
            "history": f"First-parent history, at most {HISTORY_COMMIT_LIMIT} commits from the exact baseline commit.",
            "serialization": "UTF-8, LF, two-space indentation, lexicographic object keys, one trailing LF.",
            "timestamps": "Excluded.",
        },
        "firstWave": {
            "limit": FIRST_WAVE_LIMIT,
            "selectedPaths": selected_paths,
            "selectedModificationScopes": sorted(selected_scopes),
            "selectedInitialAllowedPaths": sorted(selected_allowed_paths),
        },
        "assemblyDependencyContext": {
            "assemblyCount": assembly.get("summary", {}).get("assemblyCount"),
            "firstPartyEdgeCount": assembly.get("summary", {}).get("firstPartyEdgeCount"),
            "limitation": "File coupling counts only unique simple type declarations. Ambiguous simple names are excluded rather than multiplied across owners.",
        },
        "policy": {
            "measuredRuntime": "Only bounded owner-attributable timing evidence contributes. Missing timing is null and makes a first-wave candidate ineligible. Static update exposure contributes zero.",
            "ranking": "Complete candidates receive five 0-4 scores. All production owners receive a three-axis screening score; lines and bytes are tie-breakers only.",
            "selection": "At most three complete, eligible owners from explicit non-overlapping modification scopes. Protected owners remain visible but cannot be selected.",
        },
        "protectedOwners": [row for row in rows if row["editEligibility"] == "protected"],
        "rankedCandidates": ranked_candidates,
        "screenedOwners": rows,
        "schemaVersion": SCHEMA_VERSION,
        "scoreThresholds": {
            "changeFrequency": "0, 1-2, 3-5, 6-10, >10 commits map to 0-4.",
            "coupling": "fan-in + fan-out of 0, 1-5, 6-15, 16-30, >30 maps to 0-4.",
            "measuredRuntimeCost": "Measured average of <=0.1, <=0.25, <=1, <=4, >4 ms maps to 0-4; absent evidence is null.",
            "responsibilityCount": "Explicitly audited named responsibility count maps 0-4 with 4 as the ceiling; absent audit is null.",
            "stateOwnership": "member fields, settable properties, and lifecycle inventory rows of 0, 1-2, 3-5, 6-10, >10 maps to 0-4.",
        },
        "sourceAuthorities": sorted(authorities, key=lambda item: item["path"]),
        "summary": {
            "eligibleOwnerCount": sum(row["editEligibility"] == "eligible" for row in rows),
            "measuredOwnerCount": sum(row["measuredRuntimeEvidence"] is not None for row in rows),
            "productionOwnerCount": len(rows),
            "protectedOwnerCount": sum(row["editEligibility"] == "protected" for row in rows),
            "rankedCandidateCount": len(ranked_candidates),
            "responsibilityAuditedOwnerCount": sum(row["responsibilityAudit"] is not None for row in rows),
            "selectedOwnerCount": len(selected_paths),
        },
    }


def render_markdown(data: dict[str, Any]) -> str:
    summary = data["summary"]
    lines = [
        "# Architecture Maturity Owner Risk Ranking",
        "",
        "> Generated by `python3 Tools/CI/architecture_owner_risk_ranking.py`; do not edit manually.",
        "",
        f"- Baseline commit: `{data['baseline']['commit']}`",
        f"- Baseline tree: `{data['baseline']['tree']}`",
        f"- Production owners: {summary['productionOwnerCount']}",
        f"- Eligible / protected: {summary['eligibleOwnerCount']} / {summary['protectedOwnerCount']}",
        f"- Owners with attributable runtime timing: {summary['measuredOwnerCount']}",
        f"- Complete ranked candidates: {summary['rankedCandidateCount']}",
        f"- First-wave selections: {summary['selectedOwnerCount']} / {data['firstWave']['limit']}",
        "",
        "## Ranking Contract",
        "",
        "Complete candidates score each required dimension `0-4`; the composite is `0-20`. Lines and bytes are tie-breakers only.",
        "Missing responsibility audits or timing remain `null` and make an owner first-wave ineligible; they are never converted to zero.",
        "Static `Update`/`OnUpdate` exposure is visible but contributes zero to measured runtime cost.",
        "File coupling counts only unique simple-type declarations; ambiguous names are excluded rather than expanded into false edges.",
        "Protected owners remain ranked for transparency but cannot enter the first wave.",
        "",
        "## First Wave",
        "",
        "| Candidate rank | Owner | Modification scope | Score | Why |",
        "|---:|---|---|---:|---|",
    ]
    selected = [row for row in data["rankedCandidates"] if row["firstWaveSelected"]]
    for row in selected:
        lines.append(
            f"| {row['candidateRank']} | `{row['path']}` | `{row['modificationScope']}` | "
            f"{row['compositeScore']}/20 | {row['selectionReason']} |"
        )
    lines.extend([
        "",
        "## Complete Candidates",
        "",
        "| Rank | Owner | Score | R/C/S/M/F | In/Out | Changes | Lines | Decision |",
        "|---:|---|---:|---|---:|---:|---:|---|",
    ])
    for row in data["rankedCandidates"]:
        scores = row["scores"]
        score_text = "/".join("-" if scores[key] is None else str(scores[key]) for key in (
            "responsibilityCount", "coupling", "stateOwnership", "measuredRuntimeCost", "changeFrequency"
        ))
        lines.append(
            f"| {row['candidateRank']} | `{row['path']}` | {row['compositeScore']} | {score_text} | "
            f"{row['dependencyFanIn']}/{row['dependencyFanOut']} | {row['changeCommitCount']} | {row['lines']} | "
            f"{row['selectionReason']} |"
        )
    lines.extend([
        "",
        "`R/C/S/M/F` means responsibility count, coupling, state ownership, measured runtime cost, and change frequency.",
        "",
        "## Production Screening",
        "",
        "The screening score is coupling + state ownership + change frequency only. It discovers evidence gaps; it is not the five-axis final rank.",
        "",
        "| Screening rank | Owner | Screen score | In/Out | Changes | State signals | Lines | Decision |",
        "|---:|---|---:|---:|---:|---:|---:|---|",
    ])
    for row in data["screenedOwners"][:25]:
        lines.append(
            f"| {row['screeningRank']} | `{row['path']}` | {row['screeningScore']}/12 | "
            f"{row['dependencyFanIn']}/{row['dependencyFanOut']} | {row['changeCommitCount']} | "
            f"{row['stateSlotCount'] + sum(row['lifecycleSignals'].values())} | {row['lines']} | "
            f"{row['selectionReason']} |"
        )
    lines.extend([
        "",
        "The JSON artifact contains all production owners, raw signals, null evidence dimensions, thresholds, lifecycle categories, update exposure, and an exact selection/rejection reason for every row.",
        "",
        "## Runtime Evidence Limits",
        "",
        "The available owner-attributable timings are historical focused evidence, not a current performance claim.",
        "The post-fix pathfinding row remains visible but is selection-ineligible until current characterization exists.",
        "AM-011 must characterize selected owners, and AM-017 must recapture current focused and canonical performance before Phase 1 exits.",
        "",
        "## Protected Ownership",
        "",
        "All protected production owners remain in the JSON artifact with their screening rank and available evidence.",
        "",
        "| Screening rank | Owner | Protected by | Matched pattern | Screen score |",
        "|---:|---|---|---|---:|",
    ])
    for row in data["protectedOwners"]:
        lines.append(
            f"| {row['screeningRank']} | `{row['path']}` | `{row['protectedOwnerId']}` | "
            f"`{row['protectedMatchedPattern']}` | {row['screeningScore']}/12 |"
        )
    lines.append("")
    return "\n".join(lines)


def write_ranking(
    root: Path, revision: str, tree: str, json_output: str, markdown_output: str
) -> dict[str, Any]:
    data = build_ranking(root, revision, tree)
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
        data = write_ranking(
            args.root.resolve(), args.revision, args.tree, args.json_output, args.markdown_output
        )
    except (OSError, UnicodeDecodeError, json.JSONDecodeError, ValueError, subprocess.CalledProcessError) as error:
        print(f"[ArchitectureOwnerRiskRanking] result=Failed reason={error}")
        return 2
    print(
        "[ArchitectureOwnerRiskRanking] result=Generated "
        f"owners={data['summary']['productionOwnerCount']} "
        f"selected={data['summary']['selectedOwnerCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
