#!/usr/bin/env python3
"""Create a noncanonical compile-only S073 manifest from pinned decision + ARIA overlay."""
import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def require(ok, message):
    if not ok:
        raise ValueError(message)


def dump(path, value):
    Path(path).write_text(json.dumps(value, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def prepare(decision_path, base_build_path, overlay_dir, output_dir):
    decision_path = Path(decision_path).resolve(strict=True)
    base_build_path = Path(base_build_path).resolve(strict=True)
    overlay_dir = Path(overlay_dir).resolve(strict=True)
    output_dir = Path(output_dir).absolute()
    require(not output_dir.exists(), "Fresh output directory required")
    overlay_manifest_path = overlay_dir / "source-manifest-overlay.json"
    overlay_receipt_path = overlay_dir / "generation-receipt.json"
    decision_raw = decision_path.read_bytes()
    build_raw = base_build_path.read_bytes()
    overlay_raw = overlay_manifest_path.read_bytes()
    overlay_receipt_raw = overlay_receipt_path.read_bytes()
    decision, build = json.loads(decision_raw), json.loads(build_raw)
    overlay, overlay_receipt = json.loads(overlay_raw), json.loads(overlay_receipt_raw)
    require(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed" and
            build.get("mission") == "S073" and build.get("runtime_backend") == "LegacyS073OwnerV1",
            "Passing current S073 source build receipt required")
    require(build.get("source_manifest", {}).get("path") == str(decision_path) and
            build.get("source_manifest", {}).get("sha256") == sha(decision_path),
            "Base build must pin the requested camera-transport decision manifest")
    require(overlay.get("kind") == "AriaS073OriginalStepSourceOverlay" and
            overlay_receipt.get("kind") == "AriaS073OriginalStepOverlayGeneration" and
            overlay_receipt.get("status") == "Passed" and
            overlay_receipt.get("overlay_sha256") == sha(overlay_manifest_path),
            "Passing exact r2 ARIA overlay required")
    require(overlay.get("fixture_excluded") is True and
            overlay.get("raw_sources_shadowed_by_extern_alias_copies_excluded") is True,
            "Overlay must exclude fixture and raw/alias duplicates")

    pins = overlay.get("source_pins", {})
    stale = [path for path, expected in pins.items() if sha(path) != expected]
    require(not stale, "Overlay source proof has stale pins: " + ", ".join(stale))
    overlay_files = overlay.get("source_files", [])
    require(bool(overlay_files), "Nonempty ARIA overlay source list required")
    require(not any("Fixture" in Path(path).name for path in overlay_files), "Fixture source must not enter merged manifest")
    aliases = {Path(path).name.removesuffix(".extern-alias.extracted.cs") for path in overlay_files
               if path.endswith(".extern-alias.extracted.cs")}
    raw_duplicates = [path for path in overlay_files if not path.endswith(".extern-alias.extracted.cs") and
                      Path(path).stem in aliases]
    require(not raw_duplicates, "Raw source duplicates alias copies: " + ", ".join(raw_duplicates))
    for path in overlay_files:
        require(Path(path).is_file(), "Overlay source missing: " + path)

    merged = json.loads(decision_raw)
    entries = merged.get("sources")
    require(isinstance(entries, list), "Decision manifest source list required")
    existing_ids = {row.get("id") for row in entries}
    existing_paths = {row.get("path") for row in entries if isinstance(row.get("path"), str)}
    appended = []
    for index, absolute in enumerate(overlay_files):
        source = Path(absolute).resolve(strict=True)
        require(source.is_relative_to(ROOT), "Overlay source outside package root")
        relative = source.relative_to(ROOT).as_posix()
        if relative in existing_paths:
            continue
        identity = "S073OriginalAriaStepOverlay_" + str(index).zfill(2) + "_" + source.name.replace(".", "_")
        require(identity not in existing_ids, "Duplicate manifest source id: " + identity)
        appended.append({"id": identity, "kind": "native_direct", "path": relative})
        existing_ids.add(identity)
        existing_paths.add(relative)
    require(appended, "No new overlay sources to append")
    entries.extend(appended)
    ui_types_id = "S073OriginalAriaUiLifecycleTypes"
    ui_types_row = {
        "id": ui_types_id,
        "kind": "current_original",
        "generator": "Tools/CI/SkirmishSourceSimulation/Recipes/aria_s073_original_step/extract_ui_lifecycle_types.py",
        "generator_inputs": [
            "Assets/Game/Scripts/UI/Contracts/UiShellComponents.cs",
            "Assets/Game/Scripts/UI/Contracts/UIRoute.cs",
            "Assets/Game/Scripts/UI/Shell/Ecs/Contracts/UiShellEcsComponents.cs",
        ],
        "generator_output": "SourceS073OriginalAriaUiLifecycleTypes.cs",
        "output": "SourceS073OriginalAriaUiLifecycleTypes.cs",
        "scope": "Exact current UI lifecycle types needed by the opt-in ARIA inspector; no stubs and no duplicate UiShellRootComponent.",
    }
    if ui_types_id not in existing_ids:
        entries.append(ui_types_row)
        appended.append(ui_types_row)
        existing_ids.add(ui_types_id)
    else:
        existing = next(row for row in entries if row.get("id") == ui_types_id)
        require(existing == ui_types_row, "Unexpected original ARIA UI lifecycle type extraction row")

    adapter = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073OriginalAriaStepDiagnostic.cs"
    adapter_relative = adapter.relative_to(ROOT).as_posix()
    if adapter_relative not in existing_paths:
        adapter_id = "SourceS073OriginalAriaStepDiagnostic"
        require(adapter_id not in existing_ids, "Duplicate manifest source id: " + adapter_id)
        adapter_entry = {"id": adapter_id, "kind": "finite_adapter", "path": adapter_relative,
                         "role": "host_glue", "contains_original_gameplay": False,
                         "require_declarations": ["internal static class SourceS073OriginalAriaStepDiagnostic"],
                         "scope": "Opt-in read-only same-world projection of original UI-owned S073 ARIA state; invokes original Step on copied observed values only when the complete live UI lifecycle tuple exists; no state writes, input, action acceptance, or readiness claim."}
        entries.append(adapter_entry)
        appended.append(adapter_entry)
        existing_ids.add(adapter_id)
        existing_paths.add(adapter_relative)
    else:
        adapter_entry = next(row for row in entries if row.get("path") == adapter_relative)
        require(adapter_entry.get("id") == "SourceS073OriginalAriaStepDiagnostic",
                "Unexpected source-host diagnostic adapter identity")

    defines = merged.setdefault("compiler_defines", [])
    require(isinstance(defines, list), "Compiler defines must be a list")
    if "WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE" not in defines:
        defines.append("WARLINE_SOURCE_S073_ORIGINAL_ARIA_PROBE")
    require(adapter.is_file(), "Original ARIA diagnostic adapter missing")
    merged["source_diagnostic_classification"] = {
        "class": "ExplicitS073OriginalAriaStepCompileDiagnostic",
        "canonical_manifest": False,
        "experimental": True,
        "mission_ready": False,
        "behavior_activation": False,
        "gameplay_execution": False,
        "aria_planner_diagnostic_execution": "Explicit opt-in only; original Step receives copies of a complete live same-world UI-owned lifecycle tuple.",
        "scope": "Derived S073 diagnostic source decision plus pinned original ARIA Step closure and opt-in fail-closed host inspector; default path unchanged; no input, gameplay mutation, action acceptance, or readiness claim.",
        "base_decision_manifest": str(decision_path),
        "base_decision_sha256": sha(decision_path),
        "base_build_receipt": str(base_build_path),
        "base_build_receipt_sha256": hashlib.sha256(build_raw).hexdigest(),
        "aria_overlay_manifest": str(overlay_manifest_path),
        "aria_overlay_manifest_sha256": sha(overlay_manifest_path),
        "aria_source_pins": pins,
        "host_probe_source": str(adapter),
        "host_probe_source_sha256": sha(adapter),
    }
    merged["noncanonical_compile_diagnostic"] = {
        "canonical_manifest_modified": False,
        "runtime_behavior_enabled": False,
        "runtime_probe_compiled_only_when_derived_define_present": True,
        "probe_opt_in": "WARLINE_S073_ORIGINAL_ARIA_STEP_PROBE=1",
        "no_synthetic_state_or_actions": True,
        "source_overlay_rows": [row["id"] for row in appended],
        "source_overlay_count": len(appended),
    }

    output_dir.mkdir(parents=True)
    manifest_path = output_dir / "decision.json"
    proof_path = output_dir / "merge-proof.json"
    dump(manifest_path, merged)
    proof = {
        "schema_version": 1,
        "kind": "S073OriginalAriaStepMergedManifestProof",
        "status": "Prepared",
        "canonical_manifest": False,
        "behavior_activation": False,
        "compile_only_intent": True,
        "inputs": {
            str(decision_path): sha(decision_path),
            str(base_build_path): hashlib.sha256(build_raw).hexdigest(),
            str(overlay_manifest_path): sha(overlay_manifest_path),
            str(overlay_receipt_path): sha(overlay_receipt_path),
        },
        "overlay_source_pins": pins,
        "derived_host_sources": {str(adapter): sha(adapter)},
        "merged_manifest": str(manifest_path),
        "merged_manifest_sha256": sha(manifest_path),
        "appended_source_ids": [row["id"] for row in appended],
        "appended_source_count": len(appended),
        "fixture_sources": 0,
        "raw_alias_duplicate_sources": 0,
        "gameplay_run": False,
        "unity_launched": False,
    }
    dump(proof_path, proof)
    return proof


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--decision", type=Path, required=True)
    parser.add_argument("--base-build", type=Path, required=True)
    parser.add_argument("--overlay-dir", type=Path, required=True)
    parser.add_argument("--output-dir", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(prepare(args.decision, args.base_build, args.overlay_dir, args.output_dir), indent=2))
