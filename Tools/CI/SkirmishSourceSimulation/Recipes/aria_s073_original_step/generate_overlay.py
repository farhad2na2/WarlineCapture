#!/usr/bin/env python3
"""Generate a pinned, source-only overlay for the original S073 ARIA planner closure.

This projects the already validated r12 source set into a fresh directory. It does
not compile, execute the fixture, run Unity, or alter a source manifest used by the
production source host.
"""
import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parents[5]
RECIPE = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/aria_s073_original_step/run_compile_probe.py"


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def need(condition, message):
    if not condition:
        raise ValueError(message)


def emit_members(path):
    text = path.read_text(encoding="utf-8")
    marker = "public partial struct AriaSkirmishPlanSystem"
    start = text.index(marker)
    opening = text.index("{", start)
    closing = text.rfind("\n    }\n}")
    need(closing > opening, "Original planner partial boundary changed: " + str(path))
    members = text[opening + 1:closing]
    if path.name == "AriaSkirmishPlanSystem.cs":
        on_update = members.index("public void OnUpdate(")
        members = members[members.index("public static void Step(", on_update):]
    return members


def generate(r12_receipt, r12_pins, output):
    receipt_path = Path(r12_receipt).resolve(strict=True)
    pins_path = Path(r12_pins).resolve(strict=True)
    out = Path(output).absolute()
    need(not out.exists(), "Fresh output directory required: " + str(out))
    receipt_raw, pin_raw = receipt_path.read_bytes(), pins_path.read_bytes()
    receipt, evidence = json.loads(receipt_raw), json.loads(pin_raw)
    need(receipt.get("kind") == "AriaS073OriginalStepCompileProbe" and receipt.get("status") == "Passed",
         "Passing r12 source-only fixture receipt required")
    need(receipt.get("run_exit") == 0 and receipt.get("pass_marker_present") is True and
         receipt.get("gameplay_run") is False and receipt.get("unity_launched") is False,
         "r12 must be a passing source-only fixture, with no gameplay or Unity run")
    need(evidence.get("kind") == "AriaS073OriginalStepCompileProbeSources",
         "Expected r12 source pin manifest")
    need(receipt.get("recipe_sha256") == evidence.get("recipe_sha256") == sha(RECIPE),
         "Validated r12 recipe source pin mismatch")
    base_build = Path(receipt["base_build"]).resolve(strict=True)
    need(sha(base_build) == receipt["base_build_sha256"], "Pinned S073 build receipt changed")
    build = json.loads(base_build.read_bytes())
    need(build.get("kind") == "SkirmishSourceSimulationBuild" and build.get("status") == "Passed",
         "Passing S073 source build required")

    source_pins = evidence["source_pins"]
    changed = [p for p, expected in source_pins.items() if sha(p) != expected]
    need(not changed, "Pinned source changed since r12: " + ", ".join(changed))
    build_pins = build.get("source_and_reference_pins", {})
    changed_build = [p for p, expected in build_pins.items() if sha(p) != expected]
    need(not changed_build, "Base S073 build pin changed: " + ", ".join(changed_build))

    out.mkdir(parents=True)
    planner_parts = [
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.cs",
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.Objective.cs",
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.ObjectiveSupport.cs",
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.PublicScan.cs",
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.PublicBoarding.cs",
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.AircraftService.cs",
        ROOT / "Assets/Game/Scripts/UI/Shell/Ecs/AriaSkirmishPlanSystem.OffensiveAir.cs",
    ]
    planner = ("extern alias ActualCore;\nusing System;\nusing Game.Simulation;\n"
               "using Game.UI.Contracts;\nusing Game.UI.Shell.Contracts.Ecs;\n"
               "using Unity.Entities;\nnamespace Game.UI.Shell.Ecs {\n"
               "    public partial struct AriaSkirmishPlanSystem {\n" +
               "\n".join(emit_members(p) for p in planner_parts) +
               "\n    }\n}\n").replace("UnityEngine.", "ActualCore::UnityEngine.")
    planner_path = out / "AriaSkirmishPlanSystem.DataOnly.extracted.cs"
    planner_path.write_text(planner, encoding="utf-8")

    # Keep the exact contract and adapter files from the validated r12 closure,
    # but project UnityEngine references through the pinned CoreModule alias.
    r12_dir = receipt_path.parent
    response_path = Path(receipt["response"]).resolve(strict=True)
    need(sha(response_path) == receipt["response_sha256"], "r12 compile response changed")
    response_args = [json.loads(row) for row in response_path.read_text().splitlines() if row.strip()]
    overlay_sources = [planner_path]
    alias_stems = set()
    for source in response_args:
        candidate = Path(source)
        if candidate.is_absolute() and candidate.is_file() and candidate.parent == r12_dir:
            if candidate.name.endswith(".extern-alias.extracted.cs"):
                if candidate.stem.startswith("S073OriginalAriaStepFixture"):
                    continue
                destination = out / candidate.name
                destination.write_bytes(candidate.read_bytes())
                overlay_sources.append(destination)
                alias_stems.add(candidate.name.removesuffix(".extern-alias.extracted.cs"))
    # Contracts without UnityEngine qualification are source references; pin and
    # list them directly so consumers can add them to an isolated compile overlay.
    overlay_sources.extend(Path(p).resolve(strict=True) for p in source_pins
                           if ("/UI/Contracts/" in p or "/UI/Shell/Ecs/Contracts/" in p)
                           and Path(p).stem not in alias_stems)
    # Preserve order while removing repeated paths.
    overlay_sources = list(dict.fromkeys(overlay_sources))
    overlay = {
        "schema_version": 1,
        "kind": "AriaS073OriginalStepSourceOverlay",
        "status": "Prepared",
        "mission_scope": {"scenario_index": 3, "expanded_session": False},
        "source_files": [str(p) for p in overlay_sources],
        "source_pins": {p: sha(p) for p in source_pins},
        "generated_files": {str(planner_path): sha(planner_path)},
        "provenance": {
            "r12_receipt": str(receipt_path),
            "r12_receipt_sha256": hashlib.sha256(receipt_raw).hexdigest(),
            "r12_source_pins": str(pins_path),
            "r12_source_pins_sha256": hashlib.sha256(pin_raw).hexdigest(),
            "base_build": str(base_build),
            "base_build_sha256": receipt["base_build_sha256"],
            "generator": str(Path(__file__).resolve()),
            "generator_sha256": sha(__file__),
        },
        "extraction_seam": "Original static Step and original partial helper member bodies; excludes only ECS OnUpdate/scheduler shell.",
        "qualification": "UnityEngine references are aliased through ActualCore; planner decisions and helper bodies are unchanged original source text.",
        "overlay_boundary": "Compile input overlay only; no host dispatch, world observation, command acceptance, gameplay, Unity run, or production source-manifest mutation.",
        "fixture_excluded": True,
        "raw_sources_shadowed_by_extern_alias_copies_excluded": True,
    }
    overlay_path = out / "source-manifest-overlay.json"
    overlay_path.write_text(json.dumps(overlay, indent=2) + "\n", encoding="utf-8")
    receipt_out = {
        "schema_version": 1,
        "kind": "AriaS073OriginalStepOverlayGeneration",
        "status": "Passed",
        "output": str(out),
        "overlay": str(overlay_path),
        "overlay_sha256": sha(overlay_path),
        "generated_files": overlay["generated_files"],
        "source_file_count": len(overlay_sources),
        "input_pin_count": len(source_pins),
        "fixture_excluded": True,
        "raw_sources_shadowed_by_extern_alias_copies_excluded": True,
        "compile_attempted": False,
        "fixture_run": False,
        "gameplay_run": False,
        "unity_launched": False,
        "r12_receipt_sha256": hashlib.sha256(receipt_raw).hexdigest(),
        "base_build_sha256": receipt["base_build_sha256"],
    }
    receipt_out_path = out / "generation-receipt.json"
    receipt_out_path.write_text(json.dumps(receipt_out, indent=2) + "\n", encoding="utf-8")
    return receipt_out


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--r12-receipt", type=Path, required=True)
    parser.add_argument("--r12-source-pins", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    print(json.dumps(generate(args.r12_receipt, args.r12_source_pins, args.output), indent=2))
