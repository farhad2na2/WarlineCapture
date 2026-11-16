#!/usr/bin/env python3
"""Derive an opt-in non-native S073 source-host manifest for original UI shell OnCreate."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
BASE_DECISION_REL = Path("Logs/SkirmishProduction/s073-original-aria-probe-inputs-20261103-r3/decision.json")
BASE_BUILD_REL = Path("Logs/SkirmishProduction/s073-original-aria-probe-build-20261103-r3/build.json")
CANONICAL_MANIFEST_REL = Path("Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json")
RUNNER_PATH = "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073LegacyNormalRun.cs"
HOST_TYPES_PATH = "Tools/CI/SkirmishSourceSimulation/Adapters/112_MetadataSystemTypes.cs"
GENERATOR = "Tools/CI/SkirmishSourceSimulation/Recipes/s073_ui_shell_state_owner/generate.py"
GENERATOR_INPUTS = [
    RUNNER_PATH,
    "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStateSystem.cs",
    "Assets/Game/Scripts/UI/Shell/Ecs/UiShellStartupDispositionState.cs",
    "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs",
    "Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs",
    "Assets/Game/Scripts/UI/Contracts/UiShellPopupContracts.cs",
    HOST_TYPES_PATH,
]
DEFINE = "WARLINE_SOURCE_S073_UI_SHELL_OWNER_PROFILE"
PROFILE_ID = "S073UiShellStateOwnerOnCreateV1_NonNative"
RUNNER_ID = "292F2_SourceS073LegacyNormalRun.cs"
HOST_TYPES_ID = "112_MetadataSystemTypes.cs"
ROW_SPECS = {
    "UiShellStateSystem.source-host.cs": {
        "id": "S073UiShellStateOwnerSystem",
        "scope": "Exact original UiShellStateSystem owner/default methods; only runtime gateway registration is excluded as an external boundary.",
    },
    "UiShellStartupDispositionState.source.cs": {
        "id": "S073UiShellStateOwnerStartupDisposition",
        "scope": "Exact original startup disposition and shell-state ensure methods; no synthetic state implementation.",
    },
    "UiShellStateOwnerComponents.extracted.cs": {
        "id": "S073UiShellStateOwnerContracts",
        "scope": "Selected original component and enum declarations extracted verbatim; no fabricated component defaults.",
    },
}
GENERATED_IDS = {RUNNER_ID, HOST_TYPES_ID, *(value["id"] for value in ROW_SPECS.values()),
                 "SourceS073UiShellStateOwnerBootstrap", "SourceS073UiShellOwnerTextBoundary"}
ADDED_IDS = GENERATED_IDS - {RUNNER_ID, HOST_TYPES_ID}


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _pairs(rows):
    result = {}
    for key, value in rows:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def _need(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def validate_base(decision: dict, build: dict, repo: Path, build_path: Path | None = None) -> dict:
    expected_decision = (repo / BASE_DECISION_REL).resolve(strict=True)
    expected_build = (build_path or (repo / BASE_BUILD_REL)).resolve(strict=True)
    _need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
          build.get("compile_exit") == 0 and build.get("mission") == "S073" and build.get("seed") == 104729 and
          build.get("runtime_backend") == "LegacyS073OwnerV1" and build.get("native_or_player_launched") is False,
          "Passing non-native S073 r3 source-host build required")
    _need(expected_build.is_relative_to(repo.resolve(strict=True)),
          "Fresh S073 baseline build receipt must be inside this repository")
    _need(Path(build.get("repository_root", "")).resolve(strict=True) == repo.resolve(strict=True),
          "S073 r3 build repository root mismatch")
    source_manifest = build.get("source_manifest", {})
    _need(Path(source_manifest.get("path", "")).resolve(strict=True) == expected_decision and
          sha(expected_decision) == source_manifest.get("sha256"),
          "Current s073-original-aria-probe-inputs-r3 decision must be the pinned build source manifest")
    canonical = build.get("canonical_source_manifest", {})
    canonical_path = (repo / CANONICAL_MANIFEST_REL).resolve(strict=True)
    _need(Path(canonical.get("path", "")).resolve(strict=True) == canonical_path and
          sha(canonical_path) == canonical.get("sha256"),
          "Base canonical manifest pin/path changed")
    _need(decision.get("profile") == {"mission": "S002", "seed": 104731, "numeric_runtime": "InstalledMono"},
          "The r3 source decision's S002 base profile identity changed")
    expected_variant = [{"mission": "S073", "seed": 104729,
                         "runtime_backend": "LegacyS073OwnerV1", "scope": "LegacyStartupAndQueueOnly"}]
    _need(decision.get("source_profile_variants") == expected_variant,
          "The S073 source profile variant identity must remain exact")
    pins = build.get("source_and_reference_pins")
    _need(isinstance(pins, dict) and len(pins) >= 100, "Complete S073 r3 source/reference pins required")
    changed = [name for name, digest in pins.items() if not Path(name).is_file() or sha(Path(name)) != digest]
    _need(not changed, "S073 r3 source/reference pin mismatch: " + ", ".join(changed[:8]))
    _need(str(expected_decision) in pins and pins[str(expected_decision)] == sha(expected_decision),
          "S073 r3 decision must be inside the base build pin closure")
    return {
        "decision_path": str(expected_decision), "decision_sha256": sha(expected_decision),
        "build_path": str(expected_build), "build_sha256": sha(expected_build),
        "canonical_manifest_path": str(canonical_path), "canonical_manifest_sha256": sha(canonical_path),
        "baseline_pin_count": len(pins),
    }


def derive(decision: dict, build: dict, repo: Path = ROOT, build_path: Path | None = None) -> tuple[dict, dict]:
    repo = Path(repo).resolve(strict=True)
    provenance = validate_base(decision, build, repo, build_path)
    result = json.loads(json.dumps(decision))
    defines = result.setdefault("compiler_defines", [])
    _need(isinstance(defines, list) and DEFINE not in defines,
          "Dedicated shell-owner profile define must be absent from r3 base decision")
    defines.append(DEFINE)
    rows = result.get("sources")
    _need(isinstance(rows, list) and len(rows) > 0, "Source rows required in r3 decision")
    by_id = {row.get("id"): row for row in rows}
    _need(len(by_id) == len(rows), "Base decision contains duplicate source IDs")
    _need(not (ADDED_IDS & set(by_id)), "Derived UI shell source rows already exist in base decision")

    runner = by_id.get(RUNNER_ID)
    _need(runner is not None and runner.get("kind") == "native_direct" and runner.get("path") == RUNNER_PATH,
          "Exact original S073 retained runner source row required")
    runner_original = json.loads(json.dumps(runner))
    runner.update({
        "kind": "current_original", "generator": GENERATOR,
        "generator_inputs": list(GENERATOR_INPUTS),
        "generator_output": "SourceS073LegacyNormalRun.UiShellStateOwner.cs",
        "output": "SourceS073LegacyNormalRun.UiShellStateOwner.cs",
        "scope": "Diagnostic-only non-native optional S073 source-host profile; invokes original shell OnCreate immediately after retained host construction and before roads, map, or match/session startup only when matching explicit run-profile and environment opt-ins are present. No shell flow update or UI/mission readiness claim.",
    })
    runner.pop("path", None)

    host_types = by_id.get(HOST_TYPES_ID)
    _need(host_types is not None and host_types.get("kind") == "finite_adapter" and
          host_types.get("path") == HOST_TYPES_PATH,
          "Exact S073 metadata host adapter source row required")
    host_types_original = json.loads(json.dumps(host_types))
    host_types.update({
        "kind": "current_original", "generator": GENERATOR,
        "generator_inputs": list(GENERATOR_INPUTS),
        "generator_output": "S073HostMetadataSystemTypes.UiShellOwner.cs",
        "output": "S073HostMetadataSystemTypes.UiShellOwner.cs",
        "scope": "Generated S073 host adapter copy adds only the bounded FixedString4096Bytes constructor used by the original diagnostics default.",
    })
    host_types.pop("path", None)

    for output, spec in ROW_SPECS.items():
        rows.append({
            "id": spec["id"], "kind": "current_original", "generator": GENERATOR,
            "generator_inputs": list(GENERATOR_INPUTS), "generator_output": output,
            "output": output, "scope": spec["scope"],
        })
    rows.extend([
        {
            "id": "SourceS073UiShellStateOwnerBootstrap", "kind": "finite_adapter",
            "path": "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073UiShellStateOwnerBootstrap.cs",
            "role": "host_glue", "contains_original_gameplay": False,
            "require_declarations": ["internal static class SourceS073UiShellStateOwnerBootstrap"],
            "scope": "Diagnostic-only invocation of the exact extracted original UiShellStateSystem.OnCreate on the already retained S073 EntityManager after explicit matching run/environment admission; validates defaults and absence of route/completion/UI ARIA state. Never creates a shell entity itself.",
        },
        {
            "id": "SourceS073UiShellOwnerTextBoundary", "kind": "finite_adapter",
            "path": "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073UiShellOwnerTextBoundary.cs",
            "role": "host_glue", "contains_original_gameplay": False,
            "require_declarations": ["internal static class GameText"],
            "scope": "Fallback-only GameText.Get adapter for original static shell defaults; no localization or UI presentation claim.",
        },
    ])
    result["experimental_source_profile"] = {
        "id": PROFILE_ID,
        "base_build": provenance["build_path"],
        "base_build_sha256": provenance["build_sha256"],
        "scope": "Diagnostic-only, non-native S073 retained-source EntityManager profile; original shell OnCreate before all map/session startup after matching explicit run/environment admission; gateway registration excluded; no flow/update, route/completion, ARIA, player, or readiness claim.",
        "entity_identity_effect": "The original shell root consumes the first source-host logical entity identity; later source-host IDs shift by one. This profile is noncanonical.",
    }
    result["derived_source_row_provenance"] = [
        {"id": RUNNER_ID, "original_row": runner_original,
         "original_row_sha256": hashlib.sha256(json.dumps(runner_original, sort_keys=True,
             separators=(",", ":"), ensure_ascii=False).encode("utf-8")).hexdigest()},
        {"id": HOST_TYPES_ID, "original_row": host_types_original,
         "original_row_sha256": hashlib.sha256(json.dumps(host_types_original, sort_keys=True,
             separators=(",", ":"), ensure_ascii=False).encode("utf-8")).hexdigest()},
    ]
    provenance["derived_profile_id"] = PROFILE_ID
    provenance["define"] = DEFINE
    provenance["source_row_count"] = len(rows)
    return result, provenance


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-decision", type=Path, default=ROOT / BASE_DECISION_REL)
    parser.add_argument("--base-build", type=Path, default=ROOT / BASE_BUILD_REL,
                        help="Fresh passing no-shell-owner build from the pinned r3 decision; defaults to the original r3 receipt")
    parser.add_argument("--output", type=Path, required=True, help="Fresh derived manifest path")
    args = parser.parse_args()
    if args.output.exists():
        parser.error("Output must be fresh; existing evidence is preserved: " + str(args.output))
    decision_path = args.base_decision.resolve(strict=True)
    build_path = args.base_build.resolve(strict=True)
    _need(decision_path == (ROOT / BASE_DECISION_REL).resolve(strict=True), "Only the pinned r3 decision is admitted")
    _need(build_path.is_relative_to(ROOT.resolve(strict=True)), "Base build receipt must be inside this repository")
    decision = json.loads(decision_path.read_bytes(), object_pairs_hook=_pairs)
    build = json.loads(build_path.read_bytes(), object_pairs_hook=_pairs)
    derived, provenance = derive(decision, build, ROOT, build_path)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    payload = (json.dumps(derived, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    args.output.write_bytes(payload)
    provenance.update({"output_manifest": str(args.output.resolve()),
                       "output_manifest_sha256": hashlib.sha256(payload).hexdigest(),
                       "status": "DerivedOnly_NoBuildOrRun"})
    print(json.dumps(provenance, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
