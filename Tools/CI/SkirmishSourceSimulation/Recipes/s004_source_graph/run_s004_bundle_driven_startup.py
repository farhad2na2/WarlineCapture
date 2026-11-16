#!/usr/bin/env python3
"""Run original prefab import over the checked four-root S004 capture."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
from concurrent.futures import ThreadPoolExecutor
from source_s004_terminal_gate import activation_admitted
from source_s004_run_evidence import before as write_source_pins_before, after as write_source_pins_after

ROOT = Path(__file__).resolve().parents[5]
CAPTURE_DIR = ROOT / "Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-r3"
CAPTURE = CAPTURE_DIR / "data/prefab-reference-data.json"
CHECKED = ROOT / "T Logs/SkirmishProduction/s004-missing-four-current-capture-20261104-T3/receipt.json"
FINALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/finalize_s004_missing_prefab_capture.py"
S3 = ROOT / "Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-r1/data/prefab-reference-data.json"
S3LOG = ROOT / "T Logs/SkirmishProduction/s003-refreshed-prefab-capture-20261104-T1/editor.log"
S3RECEIPT = S3LOG.parent / "receipt.json"
sys.path.insert(0, str(ROOT / "Tools/CI/SkirmishSourceSimulation/Tests"))
from finalize_s003_refreshed_prefab_capture import qualify
HARNESS = ROOT / "Tools/CI/SkirmishSourceSimulation/Tests/S004BundleDrivenStartup.cs"
HELPER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs"
S004_LOADER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/GeneratedBuildingDefinitions.s004.cs"
RESOURCE_LIFECYCLE = ROOT / "Assets/Game/Scripts/Systems/FactionResourceCompositionSystemHelper.Lifecycle.cs"
SETUP = ROOT / "T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json"
SETUP_RECEIPT = SETUP.parent / "receipt.json"
FACTORY = ROOT / "Logs/SkirmishProduction/s004-current-full-factory-runway-20261104-r1/data/full-factory-runway-definitions.json"
PARITY = ROOT / "T Logs/SkirmishProduction/s004-current-full-factory-runway-parity-20261104-T1/receipt.json"
PARITY_CHECK = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/verify_s004_full_factory_runway_parity.py"
GRID_DIR = ROOT / "T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1"
GRID_RECEIPT, GRID_REPORT, GRID_MASK = GRID_DIR / "receipt.json", GRID_DIR / "source-projection.json", GRID_DIR / "authored-grid-walkable-emulation.u8"
BLOCK_DIR = ROOT / "Logs/SkirmishProduction/s004-static-blocker-source-20261104-r1"
BLOCK_CAPTURE = BLOCK_DIR / "data/static-blockers.json"
BLOCK_RECEIPT = ROOT / "T Logs/SkirmishProduction/s004-static-blocker-source-20261104-T1/receipt.json"
BLOCK_FINALIZER = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/finalize_s004_static_blocker_capture.py"
READ_DIR = ROOT / "Logs/SkirmishProduction/s004-current-configured-readmodel-20261104-r1"
READ_RAW, READ_LOG = READ_DIR / "data/configured-spawnable-readmodel.json", READ_DIR / "editor.log"
READ_RECEIPT = ROOT / "T Logs/SkirmishProduction/s004-current-configured-readmodel-20261104-T1/capture-receipt.json"
READ_FINALIZER = ROOT / "Tools/CI/finalize_s004_configured_building_readmodel_capture.py"
SESSION_CONTROL = ROOT / "T Logs/SkirmishProduction/s004-session-initialization-control-20261104-T1/receipt.json"
SESSION_FRAGMENT = ROOT / "T Logs/SkirmishProduction/s004-session-owner-compile-20261104-T1/S004OriginalOwnerFragments.cs"

ROAD = ROOT / "T Logs/SkirmishProduction/s004-current-source-road-mask-20261104-T2/base-surface-roads.u8"
ROAD_RECEIPT = ROAD.parent / "receipt.json"
LOADING_AUDIT = ROOT / "T Logs/SkirmishProduction/s004-authored-loading-input-audit-20261104-T1/receipt.json"
LOADING_TOOL = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/audit_s004_authored_loading_inputs.py"
BUNDLE = ROOT / "T Logs/SkirmishProduction/s004-source-diagnostic-bundle-20261104-T1/bundle.json"
BUNDLE_READER = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004BundlePreflight.cs"
RETAINED_WORLD = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/AutonomousWorld.S004Diagnostic.cs"
ROTATION = ROOT / "Logs/SkirmishProduction/s004-operation-map-rotation-capture-20261103-r2/rotation-reference.json"
GRAPH = GRID_DIR / "source-config-graph-with-cityedge-physical-inputs.json"
MONO_CONFIG = GRID_DIR / "mono-config.xml"
PLACEMENT_CONFIG = ROOT / "Assets/Game/Configs/Scene/Game_BuildingPlacement_Config.asset"
MARKER = "[S004BundleDrivenStartup] result=Observed "


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    global BUNDLE, CAPTURE_DIR, CAPTURE, CHECKED, S3, S3LOG, S3RECEIPT, FACTORY, PARITY, BLOCK_DIR, BLOCK_CAPTURE, BLOCK_RECEIPT, READ_RAW, READ_LOG, READ_RECEIPT, SETUP, SETUP_RECEIPT
    total_start=time.monotonic(); stage_start=total_start; timings={}
    def timed(name, action):
        start=time.monotonic()
        try: return action()
        finally: timings[name]=time.monotonic()-start
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--build", required=True); parser.add_argument("--output", required=True)
    parser.add_argument("--current-epoch", action="store_true", help="Use the exact checked 2026-11-05 source-only input epoch")
    parser.add_argument("--shared-focus-epoch", action="store_true", help="Use the separately audited shared-focus source-only capture epoch")
    parser.add_argument("--terminal-handoff-epoch", action="store_true", help="Use the separately rebuilt terminal-handoff source-only epoch")
    parser.add_argument("--merged-source-epoch", action="store_true", help="Reconstruct and use the explicitly captured merged source-only epoch")
    parser.add_argument("--authoritative-source-epoch", action="store_true", help="Use the independently reconstructed current source-data epoch")
    parser.add_argument("--nov06-source-epoch", action="store_true", help="Use independently reconstructed November 6 source-only inputs")
    parser.add_argument("--regular-numeric-readback", action="store_true", help="Compare the pinned Regular source numeric readback with the captured Regular setup")
    parser.add_argument("--standard-source-difficulty", choices=("Recruit", "Veteran", "Commander"), help="Opt into an exact source-only Standard difficulty readback; no runtime qualification")
    parser.add_argument("--historical-factory-source-diagnostic", action="store_true", help="Use the explicitly identified Regular factory capture after source setup compatibility checks; no selected-difficulty factory parity")
    parser.add_argument("--historical-unit-metadata-source-diagnostic", action="store_true", help="Use explicitly Regular-scoped unit metadata for the checked selected setup; no selected-difficulty metadata parity")
    parser.add_argument("--placement-state-readback", action="store_true", help="Record registry footprints and grid state after original placement")
    parser.add_argument("--historical-placement-input", help="Pinned S004 historical mask initialization JSON; source diagnostic only")
    parser.add_argument("--historical-placement-input-sha256", help="SHA256 of the exact historical mask initialization JSON")
    parser.add_argument("--fresh-canonical-build-sha256", help="Explicit current canonical S002 source-only build receipt SHA256")
    parser.add_argument("--bundle"); parser.add_argument("--bundle-sha256")
    parser.add_argument("--unit-metadata-candidate"); parser.add_argument("--unit-metadata-sha256")
    parser.add_argument("--unit-metadata-domain", choices=("factory", "roles"), default="factory")
    parser.add_argument("--production-input-probe", action="store_true")
    parser.add_argument("--allow-verified-catalog-rejections", action="store_true")
    parser.add_argument("--production-ticks", type=int, default=0)
    parser.add_argument("--semantic-rifle-probe", action="store_true",
                        help="Issue one explicit source-only rifle request if the same-world semantic query admits it")
    parser.add_argument("--semantic-aircraft-probe", action="store_true",
                        help="Issue one explicit source-only attack-helicopter request if admitted")
    parser.add_argument("--aircraft-readonly", action="store_true",
                        help="Read aircraft admission and transport facts without a paid request")
    parser.add_argument("--airself-helipad-readback-probe", action="store_true",
                        help="Probe original helipad owner against same-world source data with copied RNG")
    parser.add_argument("--airself-spawnpoint-publication-probe", action="store_true",
                        help="Invoke the existing source spawn-point publisher before the helipad probe")
    parser.add_argument("--airself-original-matcher-probe", action="store_true",
                        help="Bind the original runtime building-ID matcher in the helipad probe")
    parser.add_argument("--airself-slot-binding-probe", action="store_true",
                        help="Probe the original override-cell production slot resolver")
    parser.add_argument("--airself-launch-geometry-probe", action="store_true",
                        help="Read source-only AirSelf launch geometry without issuing a request")
    parser.add_argument("--airself-delivery-probe", action="store_true",
                        help="Run the bounded, single-transport source-only AirSelf owner")
    parser.add_argument("--original-aria-step-probe", action="store_true",
                        help="Inspect original S004 planner state only if genuine UI lifecycle data exists")
    parser.add_argument("--s004-logical-context-candidate-probe", action="store_true",
                        help="Test a source-only S004 logical context identity candidate")
    parser.add_argument("--source-logical-aria-probe", action="store_true",
                        help="Run the bounded S004 logical-controller candidate without native input")
    parser.add_argument("--source-logical-candidate", help="Explicit prepared unqualified source-controller candidate directory")
    parser.add_argument("--aircraft-fuel-safety-probe", action="store_true",
                        help="Schedule original aircraft fuel safety before air movement in the source-only diagnostic")
    parser.add_argument("--aircraft-return-readback-probe", action="store_true",
                        help="Read actual delivered aircraft return prerequisites without issuing a command")
    parser.add_argument("--source-logical-air-service-probe", action="store_true",
                        help="Enable selected owned-aircraft logical return in the experimental source controller")
    parser.add_argument("--source-logical-read-profile-probe", action="store_true",
                        help="Record source-controller read phase wall timings without changing decisions")
    parser.add_argument("--source-logical-read-detail-probe", action="store_true",
                        help="Record nested readiness and recruitment timing within the facts phase")
    parser.add_argument("--source-availability-timing-probe", action="store_true",
                        help="Time original availability adapter preflight, scope and legality kernel without caching")
    parser.add_argument("--source-single-scope-query-experiment", action="store_true",
                        help="Use diagnostic one-enumeration availability scope with original authority checks")
    parser.add_argument("--source-scope-timing-probe", action="store_true",
                        help="Time the original availability scope without changing its checks")
    parser.add_argument("--source-fuel-drift-readback-probe", action="store_true",
                        help="Read source fuel stores and consumers at ticks 800 through 910")
    parser.add_argument("--source-fuel-drift-start", type=int, help="First passive fuel sample tick")
    parser.add_argument("--source-fuel-drift-end", type=int, help="Last passive fuel sample tick")
    parser.add_argument("--source-terminal-diagnostic", action="store_true",
                        help="Allow up to 66000 ordinary source ticks to observe a natural terminal outcome")
    parser.add_argument("--semantic-air-opening-probe", action="store_true",
                        help="Evaluate one source-only shared air-opening segment and dispatch its admitted request")
    parser.add_argument("--semantic-supply-probe", action="store_true",
                        help="Evaluate one source-only shared supply segment and dispatch its admitted request")
    parser.add_argument("--public-threat-probe", action="store_true",
                        help="Gate the source-only supply segment on original public-threat selection")
    parser.add_argument("--semantic-supply-followup-probe", action="store_true",
                        help="Reevaluate the same source supply state after the requested owner ticks")
    parser.add_argument("--enemy-breakdown-probe", action="store_true",
                        help="Instrument exact original enemy owner subcalls in this source-only run")
    parser.add_argument("--cached-setup-guard-probe", action="store_true",
                        help="Use experimental source-only parsed setup reference cache")
    parser.add_argument("--serialized-setup-guard-experiment", action="store_true",
                        help="Use the separately tested, unqualified serialized comparison candidate")
    parser.add_argument("--validator-snapshots-probe", action="store_true",
                        help="Capture original validator actor/session state during source owner ticks")
    parser.add_argument("--worker-timeout", type=int, default=180,
                        help="Source worker timeout in seconds (1..1800)")
    parser.add_argument("--catalog-getter-probe", action="store_true")
    parser.add_argument("--catalog-binding-probe", action="store_true")
    parser.add_argument("--dispatcher-probe", action="store_true")
    parser.add_argument("--source-clearance"); parser.add_argument("--source-clearance-sha256")
    parser.add_argument("--airport-candidate"); parser.add_argument("--airport-candidate-sha256")
    parser.add_argument("--unplaced-airport-production-probe", action="store_true")
    parser.add_argument("--retained-map-probe", action="store_true")
    parser.add_argument("--aria-context-probe", action="store_true")
    parser.add_argument("--canonical-graph"); parser.add_argument("--canonical-graph-sha256")
    parser.add_argument("--guarded-source-epoch", action="store_true", help="Use the separately verified current setup-guard source epoch")
    args = parser.parse_args()
    if args.nov06_source_epoch and not args.authoritative_source_epoch:
        raise ValueError("November 6 inputs require the authoritative source-only diagnostic gates")
    if bool(args.historical_placement_input) != bool(args.historical_placement_input_sha256):
        raise ValueError("Historical placement input and SHA256 must be supplied together")
    historical_placement_input = None
    historical_mask_source = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS004HistoricalPlacementMasks.cs"
    historical_mask_pins = ()
    if args.historical_placement_input:
        if not args.authoritative_source_epoch:
            raise ValueError("Historical placement requires the explicit authoritative source-only epoch")
        historical_placement_input = Path(args.historical_placement_input).resolve(strict=True)
        if sha(historical_placement_input) != args.historical_placement_input_sha256:
            raise ValueError("Historical placement initialization SHA256 mismatch")
        spec = json.loads(historical_placement_input.read_text())["source_historical_placement_masks"]
        if set(spec) != {"kind", "capture", "audit"} or spec["kind"] != "HistoricalCityEdgePlacementMasks":
            raise ValueError("Exact historical placement mask descriptor required")
        historical_files = []
        for role in ("capture", "audit"):
            descriptor = spec[role]
            if set(descriptor) != {"path", "sha256"}:
                raise ValueError("Exact historical " + role + " descriptor required")
            path = Path(descriptor["path"]).resolve(strict=True)
            if sha(path) != descriptor["sha256"]:
                raise ValueError("Historical " + role + " SHA256 mismatch")
            historical_files.append(path)
        historical_mask_pins = (historical_placement_input, historical_mask_source, *historical_files)

    if args.regular_numeric_readback and args.standard_source_difficulty:
        raise ValueError("Select only one numeric readback")
    selected_numeric_requested = args.regular_numeric_readback or args.standard_source_difficulty is not None
    selected_difficulty = args.standard_source_difficulty or "Regular"
    if args.historical_unit_metadata_source_diagnostic and not args.historical_factory_source_diagnostic:
        raise ValueError("Historical unit metadata diagnostic requires the historical factory diagnostic")
    if args.historical_factory_source_diagnostic and not args.standard_source_difficulty:
        raise ValueError("Historical factory diagnostic requires a selected non-Regular source difficulty")
    if selected_numeric_requested and not args.authoritative_source_epoch:
        raise ValueError("Numeric readback comparison requires the exact current source epoch")
    if args.placement_state_readback and not args.authoritative_source_epoch:
        raise ValueError("Placement-state readback requires the exact current source epoch")
    if args.authoritative_source_epoch and (not args.merged_source_epoch or args.guarded_source_epoch):
        raise ValueError("Authoritative source epoch requires merged gates and excludes the older guarded epoch")
    if args.guarded_source_epoch and not args.merged_source_epoch:
        raise ValueError("Guarded source epoch requires merged source epoch gates")
    if args.shared_focus_epoch and not args.current_epoch:
        raise ValueError("Shared-focus epoch requires the current-epoch diagnostic gates")
    if args.terminal_handoff_epoch and not args.shared_focus_epoch:
        raise ValueError("Terminal-handoff epoch requires the shared-focus capture gates")
    if args.merged_source_epoch and not args.terminal_handoff_epoch:
        raise ValueError("Merged source epoch requires the terminal-handoff diagnostic gates")
    if args.serialized_setup_guard_experiment and not (args.terminal_handoff_epoch and args.cached_setup_guard_probe):
        raise ValueError("Serialized guard experiment requires the exact terminal-handoff and cached-guard diagnostics")
    logical_candidate_name = "s004-logical-controller-candidate-20261105-T5" if args.source_logical_read_detail_probe else ("s004-logical-controller-candidate-20261105-T4" if args.source_logical_read_profile_probe else ("s004-logical-controller-candidate-20261105-T3" if args.source_logical_air_service_probe else "s004-logical-controller-candidate-20261105-T2"))
    if args.source_logical_candidate:
        if not args.source_logical_aria_probe or ((args.source_logical_read_profile_probe or args.source_logical_read_detail_probe) and not args.shared_focus_epoch):
            raise ValueError("Explicit profiled candidate requires shared-focus source logical ARIA")
        candidate=Path(args.source_logical_candidate).resolve()
        allowed=(ROOT/"T Logs/SkirmishProduction").resolve()
        if candidate.parent!=allowed:
            raise ValueError("Explicit source candidate must be a direct retained diagnostic directory")
        logical_candidate_name=str(candidate)
    if args.source_logical_read_detail_probe and not args.source_logical_read_profile_probe:
        raise ValueError("Nested source read timing requires the main read profile")
    if args.source_availability_timing_probe and not args.source_logical_read_detail_probe:
        raise ValueError("Availability timing requires the nested logical read diagnostic")
    if args.source_single_scope_query_experiment and not args.source_availability_timing_probe:
        raise ValueError("Single scope query experiment requires availability timing")
    if args.source_scope_timing_probe and (not args.source_availability_timing_probe or args.source_single_scope_query_experiment):
        raise ValueError("Original scope timing requires availability timing without the experimental scope")
    if (args.source_fuel_drift_start is None) != (args.source_fuel_drift_end is None):
        raise ValueError("Both passive fuel window bounds are required")
    custom_fuel_window = args.source_fuel_drift_start is not None
    if custom_fuel_window and (not args.source_fuel_drift_readback_probe or args.source_fuel_drift_start < 1 or
        args.source_fuel_drift_end < args.source_fuel_drift_start or args.source_fuel_drift_end > args.production_ticks or
        args.source_fuel_drift_end - args.source_fuel_drift_start > 1000):
        raise ValueError("Bounded passive fuel window requires the source probe and requested ticks")
    if args.source_fuel_drift_readback_probe and (not args.source_scope_timing_probe or args.production_ticks < 900):
        raise ValueError("Fuel drift readback requires source scope timing and at least 900 fixed ticks")
    if args.source_logical_read_profile_probe and not args.source_logical_air_service_probe:
        raise ValueError("Source read profiling requires the logical air-service diagnostic")
    if args.semantic_supply_probe and not args.public_threat_probe:
        raise ValueError("Source supply segment requires an actual public-threat observation")
    if args.current_epoch:
        # The refreshed historical-mask lane may inspect ordinary startup with
        # no player-command or planner probe. It is still source-only and all
        # current input reconstruction and pin checks below remain mandatory.
        bounded_startup = (args.nov06_source_epoch and historical_placement_input is not None and
                           args.production_ticks == 0 and not args.retained_map_probe and
                           not any((args.semantic_air_opening_probe, args.semantic_supply_probe,
                                    args.semantic_aircraft_probe, args.original_aria_step_probe)))
        if not bounded_startup and (not (args.semantic_air_opening_probe or args.semantic_supply_probe or args.semantic_aircraft_probe or args.original_aria_step_probe) or args.unit_metadata_domain != "roles" or not args.retained_map_probe):
            raise ValueError("Current epoch requires a scoped source planner diagnostic")
        CAPTURE_DIR=ROOT/"Logs/SkirmishProduction/s004-post-planner-missing-prefabs-20261105-r1"
        CAPTURE=CAPTURE_DIR/"data/prefab-reference-data.json"
        CHECKED=ROOT/"T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/missing-prefab-receipt.json"
        S3=ROOT/"Logs/SkirmishProduction/s003-post-planner-prefabs-20261105-r1/data/prefab-reference-data.json"
        S3LOG=ROOT/"T Logs/SkirmishProduction/s003-post-planner-metadata-capture-20261104-T1/prefab-editor-r1.log"
        S3RECEIPT=ROOT/"T Logs/SkirmishProduction/s003-post-planner-metadata-capture-20261104-T1/prefab-capture-audit-r1.json"
        FACTORY=ROOT/"Logs/SkirmishProduction/s004-post-planner-factory-20261105-r1/data/full-factory-runway-definitions.json"
        PARITY=ROOT/"T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/factory-audit.json"
        BLOCK_DIR=ROOT/"Logs/SkirmishProduction/s004-post-planner-blockers-20261105-r1"
        BLOCK_CAPTURE=BLOCK_DIR/"data/static-blockers.json"
        BLOCK_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/blockers-audit.json"
        READ_RAW=ROOT/"Logs/SkirmishProduction/s004-post-planner-readmodel-20261105-r1/data/configured-spawnable-readmodel.json"
        READ_LOG=ROOT/"T Logs/SkirmishProduction/s004-post-planner-captures-20261105-T1/readmodel-editor.log"
        READ_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-post-planner-readmodel-finalizer-20261105-T1/capture-receipt.json"
        if args.shared_focus_epoch:
            CAPTURE_DIR=ROOT/"Logs/SkirmishProduction/s004-shared-focus-prefabs-20261105-r1"
            CAPTURE=CAPTURE_DIR/"data/prefab-reference-data.json"
            CHECKED=ROOT/"T Logs/SkirmishProduction/s004-shared-focus-prefabs-20261105-T1/receipt.json"
            S3=ROOT/"Logs/SkirmishProduction/s003-shared-focus-prefabs-20261105-r1/data/prefab-reference-data.json"
            S3LOG=ROOT/"T Logs/SkirmishProduction/s003-shared-focus-prefabs-20261105-T1/editor.log"
            S3RECEIPT=ROOT/"T Logs/SkirmishProduction/s003-shared-focus-prefabs-20261105-T1/independent-audit.json"
            FACTORY=ROOT/"Logs/SkirmishProduction/s004-shared-focus-factory-20261105-r1/data/full-factory-runway-definitions.json"
            PARITY=ROOT/"T Logs/SkirmishProduction/s004-shared-focus-factory-20261105-T1/factory-audit.json"
            BLOCK_DIR=ROOT/"Logs/SkirmishProduction/s004-shared-focus-blockers-20261105-r1"
            BLOCK_CAPTURE=BLOCK_DIR/"data/static-blockers.json"
            BLOCK_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-shared-focus-blockers-20261105-T1/receipt.json"
            READ_RAW=ROOT/"Logs/SkirmishProduction/s004-shared-focus-readmodel-20261105-r1/data/configured-spawnable-readmodel.json"
            READ_LOG=ROOT/"T Logs/SkirmishProduction/s004-shared-focus-readmodel-20261105-T1/editor.log"
            READ_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-shared-focus-readmodel-audit-20261105-T1/capture-receipt.json"
            if args.merged_source_epoch:
                CAPTURE_DIR=ROOT/"T Logs/SkirmishProduction/s004-merged-capture-links-20261105-T1/prefabs"
                CAPTURE=CAPTURE_DIR/"data/prefab-reference-data.json"
                CHECKED=CAPTURE_DIR/"receipt.json"
                S3=ROOT/"Logs/SkirmishProduction/s003-shared-rifle-prefabs-capture-20261105-T1/data/prefab-reference-data.json"
                S3LOG=ROOT/"T Logs/SkirmishProduction/s003-shared-rifle-prefabs-capture-20261105-T1-editor.log"
                S3RECEIPT=ROOT/"T Logs/SkirmishProduction/s003-shared-rifle-prefabs-capture-20261105-T1/independent-audit.json"
                FACTORY=ROOT/"Logs/SkirmishProduction/s004-current-merged-factory-20261105-T2/full-factory-runway-definitions.json"
                PARITY=ROOT/"T Logs/SkirmishProduction/s004-current-merged-factory-20261105-T2/factory-audit.json"
                BLOCK_DIR=ROOT/"T Logs/SkirmishProduction/s004-merged-capture-links-20261105-T1/blockers"
                BLOCK_CAPTURE=BLOCK_DIR/"data/static-blockers.json"
                BLOCK_RECEIPT=BLOCK_DIR/"receipt.json"
                READ_RAW=ROOT/"Logs/SkirmishProduction/s004-current-merged-readmodel-20261105-T1/configured-spawnable-readmodel.json"
                READ_LOG=ROOT/"T Logs/SkirmishProduction/s004-current-merged-readmodel-20261105-T1/editor.log"
                READ_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-current-merged-readmodel-audit-20261105-T1/capture-receipt.json"
                SETUP=ROOT/"T Logs/SkirmishProduction/s004-merged-standard-setup-export-20261105-T1/compiled-setup.json"
                SETUP_RECEIPT=SETUP.parent/"receipt.json"
                if args.guarded_source_epoch:
                    SETUP=ROOT/"T Logs/SkirmishProduction/s004-current-merged-guard-setup-export-20261105-T1/compiled-setup.json"
                    SETUP_RECEIPT=SETUP.parent/"receipt.json"
                if args.authoritative_source_epoch:
                    CAPTURE_DIR=ROOT/"Logs/SkirmishProduction/s004-current-authoritative-prefabs-20261105-r1"
                    CAPTURE=CAPTURE_DIR/"data/prefab-reference-data.json"
                    CHECKED=ROOT/"T Logs/SkirmishProduction/s004-current-authoritative-prefabs-20261105-T1/receipt.json"
                    S3=ROOT/"Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-r1/data/prefab-reference-data.json"
                    S3LOG=ROOT/"T Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-T1/editor.log"
                    S3RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-current-shared-prefabs-20261105-T1/independent-audit.json"
                    FACTORY=ROOT/"Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-r1/data/full-factory-runway-definitions.json"
                    PARITY=ROOT/"T Logs/SkirmishProduction/s004-current-authoritative-factory-20261105-T1/factory-audit.json"
                    BLOCK_DIR=ROOT/"Logs/SkirmishProduction/s004-current-authoritative-blockers-20261105-r1"
                    BLOCK_CAPTURE=BLOCK_DIR/"data/static-blockers.json"
                    BLOCK_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-current-authoritative-blockers-20261105-T1/receipt.json"
                    READ_RAW=ROOT/"Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-r3/data/configured-spawnable-readmodel.json"
                    READ_LOG=ROOT/"T Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-T3/editor.log"
                    READ_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-current-authoritative-readmodel-20261105-T3/finalization/capture-receipt.json"
                    SETUP=ROOT/"T Logs/SkirmishProduction/s004-current-scope-setup-export-20261105-T1/compiled-setup.json"
                    SETUP_RECEIPT=SETUP.parent/"receipt.json"
                    if args.nov06_source_epoch:
                        CAPTURE_DIR=ROOT/"Logs/SkirmishProduction/s004-current-input-prefabs-20261106-r1"
                        CAPTURE=CAPTURE_DIR/"data/prefab-reference-data.json"
                        CHECKED=ROOT/"T Logs/SkirmishProduction/s004-current-input-refresh-20261106-T1/prefabs-receipt.json"
                        S3=ROOT/"Logs/SkirmishProduction/s003-current-prefabs-refresh-20261106-r3/data/prefab-reference-data.json"
                        S3LOG=ROOT/"T Logs/SkirmishProduction/s003-current-metadata-refresh-20261106-T3/prefab-editor.log"
                        S3RECEIPT=S3LOG.parent/"prefab-audit.json"
                        FACTORY=ROOT/"Logs/SkirmishProduction/s004-current-factory-refresh-20261106-r1/data/full-factory-runway-definitions.json"
                        PARITY=ROOT/"T Logs/SkirmishProduction/s004-current-factory-refresh-20261106-T1/factory-audit.json"
                        BLOCK_DIR=ROOT/"Logs/SkirmishProduction/s004-current-input-blockers-20261106-r1"
                        BLOCK_CAPTURE=BLOCK_DIR/"data/static-blockers.json"
                        BLOCK_RECEIPT=ROOT/"T Logs/SkirmishProduction/s004-current-input-refresh-20261106-T1/blockers-receipt.json"
                        READ_RAW=ROOT/"Logs/SkirmishProduction/s004-current-input-readmodel-20261106-r1/data/configured-spawnable-readmodel.json"
                        READ_LOG=ROOT/"T Logs/SkirmishProduction/s004-current-input-refresh-20261106-T1/readmodel.log"
                        READ_RECEIPT=READ_LOG.parent/"readmodel-finalization/capture-receipt.json"
                        SETUP=ROOT/"T Logs/SkirmishProduction/s004-current-setup-refresh-20261106-T1/compiled-setup.json"
                        SETUP_RECEIPT=SETUP.parent/"receipt.json"
    wants_canonical=args.aria_context_probe or args.s004_logical_context_candidate_probe
    if bool(args.canonical_graph)!=bool(args.canonical_graph_sha256) or wants_canonical!=bool(args.canonical_graph):
        raise ValueError("ARIA context probe requires an explicit canonical source graph descriptor")
    if args.aria_context_probe and (not args.catalog_binding_probe or args.dispatcher_probe):
        raise ValueError("ARIA context probe requires the standalone same-world catalog binding")
    if args.s004_logical_context_candidate_probe and (not args.original_aria_step_probe or not args.dispatcher_probe or args.aria_context_probe):
        raise ValueError("S004 logical context candidate requires same-world dispatcher and original planner inspector")
    canonical_graph=Path(args.canonical_graph).resolve(strict=True) if wants_canonical else None
    if canonical_graph is not None:
        if sha(canonical_graph)!=args.canonical_graph_sha256:raise ValueError("Canonical source graph hash mismatch")
        canonical_data=json.loads(canonical_graph.read_bytes()); canonical_pins=canonical_data.get("source_pins")
        nodes=[row for row in canonical_data.get("nodes",[]) if row.get("source_type")=="SkirmishBattleCatalogConfig"]
        if not isinstance(canonical_pins,dict) or not canonical_pins or len(nodes)!=1 or nodes[0].get("source_path")!="Assets/Game/Resources/SkirmishBattleCatalog.asset":
            raise ValueError("Exact original canonical catalog source graph required")
        for name,digest in canonical_pins.items():
            path=Path(name);path=path if path.is_absolute() else ROOT/path
            if sha(path)!=digest:raise ValueError("Canonical graph source changed: "+name)
        matrix_descriptor=json.loads(GRAPH.read_bytes())["setup_matrix"]
        matrix_path=Path(matrix_descriptor["path"]).resolve(strict=True)
        if sha(matrix_path)!=matrix_descriptor["sha256"]:raise ValueError("Actual S004 setup matrix changed")
    if (args.unit_metadata_candidate is None) != (args.unit_metadata_sha256 is None):
        raise ValueError("Unit metadata candidate path and hash must be provided together")
    if args.current_epoch:
        if args.merged_source_epoch:
            import build_s004_merged_source_epoch as merged_epoch
            import verify_s004_current_role_catalog_metadata as merged_roles
            import verify_s004_current_airport_source_definition as merged_airport
            if args.authoritative_source_epoch:
                import build_s004_current_authoritative_epoch as authoritative_epoch
                authoritative_epoch.configure()
                if args.nov06_source_epoch:
                    import build_s004_nov06_source_epoch as nov06_epoch
                    nov06_epoch.configure()
            elif args.guarded_source_epoch:
                import build_s004_guarded_source_epoch as guarded_epoch
                guarded_epoch.configure()
            else:
                merged_epoch.configure()
        elif args.shared_focus_epoch:
            import build_s004_shared_focus_epoch as current_candidates
        else:
            import verify_s004_current_epoch_candidates as current_candidates
        class CurrentVerifier:
            def __init__(self, kind): self.kind = kind
            def verify(self, path, digest):
                if args.merged_source_epoch:
                    roles=merged_epoch.roles; airports=merged_epoch.airports; bundles=merged_epoch.bundles
                    if self.kind=="role":
                        if path!=bundles.ROLE: raise ValueError("Exact merged role candidate path required")
                        return merged_roles.verify(path,digest,roles.BUILD,roles.PREFAB,roles.PREFAB_AUDIT,roles.PROJECTION,roles.METADATA)
                    if self.kind=="airport":
                        if path!=bundles.AIRPORT: raise ValueError("Exact merged airport candidate path required")
                        return merged_airport.verify(path,digest,roles.BUILD,roles.PREFAB,roles.PREFAB_AUDIT,roles.PROJECTION,roles.METADATA,airports.FACTORY,airports.FACTORY_AUDIT,airports.S003,airports.ROLE)
                    raise ValueError("Unknown merged candidate kind")
                return current_candidates.verify(path, digest, self.kind, terminal_handoff=True) if args.terminal_handoff_epoch else current_candidates.verify(path, digest, self.kind)
        for raw in (args.unit_metadata_candidate, args.airport_candidate):
            if not raw or not Path(raw).is_absolute() or Path(raw).is_symlink():
                raise ValueError("Exact absolute current candidate paths required")
    unit_candidate = Path(args.unit_metadata_candidate).resolve(strict=True) if args.unit_metadata_candidate else None
    if bool(args.airport_candidate)!=bool(args.airport_candidate_sha256):
        raise ValueError("Airport candidate path and hash must be provided together")
    airport_candidate=Path(args.airport_candidate).resolve(strict=True) if args.airport_candidate else None
    if airport_candidate is not None:
        if unit_candidate is None or args.unit_metadata_domain!="roles": raise ValueError("Airport source probe requires the exact role metadata domain")
        if args.current_epoch: airport_verifier=CurrentVerifier("airport")
        else: import verify_s004_airport_source_definition as airport_verifier
        airport_verifier.verify(airport_candidate,args.airport_candidate_sha256)
    if args.unplaced_airport_production_probe and (airport_candidate is None or not args.dispatcher_probe or args.allow_verified_catalog_rejections):
        raise ValueError("Unplaced airport production probe requires airport and dispatcher without overlapping projections")
    if args.retained_map_probe and not args.unplaced_airport_production_probe:
        raise ValueError("Retained map probe requires the unplaced airport production context")
    if unit_candidate is not None:
        if os.environ.get("WARLINE_S004_SOURCE_BANK_DIAGNOSTIC") != "1":
            raise ValueError("S004 unit metadata diagnostic requires same-world source bank")
        if args.current_epoch:
            unit_verifier=CurrentVerifier("role")
        elif args.unit_metadata_domain=="roles":
            import verify_s004_role_catalog_metadata as unit_verifier
        else:
            import verify_s004_unit_metadata_candidate as unit_verifier
        import s004_unit_metadata_profile as unit_profile
        unit_verifier.verify(unit_candidate, args.unit_metadata_sha256)
    if args.production_input_probe and unit_candidate is None:
        raise ValueError("Production input probe requires verified S004 unit metadata")
    if args.catalog_getter_probe and unit_candidate is None:
        raise ValueError("Catalog getter probe requires verified S004 unit metadata")
    if args.catalog_binding_probe and (not args.catalog_getter_probe or os.environ.get("WARLINE_S004_TRANSFORM_BRIDGE")=="1" or os.environ.get("WARLINE_S004_SOURCE_LAUNCH_DIAGNOSTIC")!="1"):
        raise ValueError("Catalog binding probe requires catalog getters, original source launch and a single source family projection")
    if bool(args.source_clearance) != bool(args.source_clearance_sha256) or args.dispatcher_probe != bool(args.source_clearance):
        raise ValueError("Dispatcher probe requires an explicit source clearance descriptor")
    if args.dispatcher_probe and (not args.catalog_binding_probe or (args.unit_metadata_domain!="roles" and not args.allow_verified_catalog_rejections) or not args.production_input_probe):
        raise ValueError("Dispatcher probe requires the actual source catalog and production probes")
    clearance = Path(args.source_clearance).resolve(strict=True) if args.dispatcher_probe else None
    if clearance is not None:
        if sha(clearance)!=args.source_clearance_sha256: raise ValueError("Source clearance projection hash mismatch")
        clearance_data=json.loads(clearance.read_bytes())
        if clearance_data.get("scope")!="CurrentS002SourceOnlyInactiveFixture_NoActiveProviderEquivalence" or clearance_data.get("source_fit_certified") is not False or clearance_data.get("mission_ready") is not False:
            raise ValueError("Exact unqualified source-only clearance scope required")
        clearance_source=Path(clearance_data["source_path"]).resolve(strict=True)
        if sha(clearance_source)!=clearance_data["source_sha256"]: raise ValueError("Source clearance artifact mismatch")
    if args.allow_verified_catalog_rejections and not args.production_input_probe:
        raise ValueError("Catalog rejection projection requires the production input probe")
    if args.source_terminal_diagnostic and not (args.source_logical_aria_probe and args.cached_setup_guard_probe and args.source_scope_timing_probe):
        raise ValueError("Terminal source diagnostic requires the explicit logical controller and checked experimental guard")
    tick_limit=66000 if args.source_terminal_diagnostic else 60000
    if args.production_ticks < 0 or args.production_ticks > tick_limit or (args.production_ticks and not (args.allow_verified_catalog_rejections or args.unit_metadata_domain=="roles")):
        raise ValueError("Production tick diagnostic exceeds its explicit fixed-tick source limit")
    if args.semantic_rifle_probe and (not args.dispatcher_probe or not args.production_ticks):
        raise ValueError("Semantic rifle probe requires the actual dispatcher and fixed production ticks")
    if args.semantic_aircraft_probe and (not args.current_epoch or not args.dispatcher_probe or not args.production_ticks or args.semantic_rifle_probe or args.semantic_supply_probe or args.semantic_air_opening_probe):
        raise ValueError("Aircraft probe requires current dispatcher and fixed ticks without other semantic requests")
    if args.aircraft_readonly and not args.semantic_aircraft_probe:
        raise ValueError("Aircraft readonly requires the aircraft probe")
    if args.airself_helipad_readback_probe and (not args.semantic_aircraft_probe or not (args.aircraft_readonly or args.airself_delivery_probe)):
        raise ValueError("AirSelf helipad readback requires readonly or explicit source delivery aircraft probe")
    if args.airself_spawnpoint_publication_probe and not args.airself_helipad_readback_probe:
        raise ValueError("Spawn-point publication requires the AirSelf helipad readback")
    if args.airself_original_matcher_probe and not args.airself_spawnpoint_publication_probe:
        raise ValueError("Original matcher probe requires actual source spawn-point publication")
    if args.airself_slot_binding_probe and not args.airself_original_matcher_probe:
        raise ValueError("AirSelf slot binding requires the original matcher probe")
    if args.airself_launch_geometry_probe and not args.airself_slot_binding_probe:
        raise ValueError("AirSelf launch geometry requires the original source slot probe")
    if args.airself_delivery_probe and (not args.airself_launch_geometry_probe or (args.aircraft_readonly and not args.source_logical_aria_probe)):
        raise ValueError("AirSelf delivery requires launch readback and an explicit non-readonly source request")
    if args.original_aria_step_probe and not args.dispatcher_probe:
        raise ValueError("Original ARIA source inspector requires the same-world source dispatcher")
    if args.source_logical_aria_probe and (not args.s004_logical_context_candidate_probe or not args.airself_delivery_probe or not args.aircraft_readonly or args.production_ticks<=0):
        raise ValueError("S004 logical controller requires the checked context, source AirSelf owner, no direct aircraft request and bounded ticks")
    if args.aircraft_fuel_safety_probe and (not args.source_logical_aria_probe or os.environ.get("WARLINE_S004_SOURCE_SCAN_ROPE_TICK") != "1"):
        raise ValueError("Aircraft fuel safety requires the explicit logical diagnostic and current scan/rope tick")
    if args.aircraft_return_readback_probe and not args.aircraft_fuel_safety_probe:
        raise ValueError("Aircraft return readback requires the original fuel safety source diagnostic")
    if args.source_logical_air_service_probe and not args.aircraft_return_readback_probe:
        raise ValueError("Logical air service requires the original fuel owner and same-world return readback")
    if args.semantic_air_opening_probe and (not args.dispatcher_probe or not args.production_ticks or args.semantic_rifle_probe or args.semantic_supply_probe):
        raise ValueError("Air-opening segment requires the actual dispatcher and fixed ticks, without the direct rifle probe")
    if args.semantic_supply_probe and (not args.current_epoch or not args.dispatcher_probe or not args.production_ticks or args.semantic_rifle_probe or args.semantic_air_opening_probe):
        raise ValueError("Supply segment requires current actual dispatcher and fixed ticks, without other semantic probes")
    if args.public_threat_probe and not args.semantic_supply_probe:
        raise ValueError("Public threat probe requires the scoped supply segment")
    if args.semantic_supply_followup_probe and not args.public_threat_probe:
        raise ValueError("Supply followup requires the public threat probe")
    if args.enemy_breakdown_probe and (not args.semantic_supply_followup_probe or os.environ.get("WARLINE_S004_OWNER_PHASE_TOTALS")!="1"):
        raise ValueError("Enemy breakdown requires the timed supply followup diagnostic")
    if args.cached_setup_guard_probe and not (args.enemy_breakdown_probe or args.source_scope_timing_probe):
        raise ValueError("Cached setup experiment requires measured enemy breakdown or original scope timing")
    if args.validator_snapshots_probe and not args.semantic_supply_followup_probe:
        raise ValueError("Validator snapshots require the bounded source owner diagnostic")
    if not 1 <= args.worker_timeout <= 1800:
        raise ValueError("Worker timeout must be between 1 and 1800 seconds")
    if args.production_input_probe and os.environ.get("WARLINE_S004_SOURCE_EXPERIMENT_TICK") != "1":
        raise ValueError("Production input probe requires the original same-world actor census and source tick")
    if os.environ.get("WARLINE_S004_SOURCE_GAMEPLAY_DIAGNOSTIC")=="1" and os.environ.get("WARLINE_S004_SOURCE_ROAD_DIAGNOSTIC")!="1":
        raise ValueError("S004 source gameplay diagnostic requires same-world original road sync")
    if os.environ.get("WARLINE_S004_SOURCE_BANK_DIAGNOSTIC")=="1" and (os.environ.get("WARLINE_S004_SOURCE_GAMEPLAY_DIAGNOSTIC")!="1" or os.environ.get("WARLINE_S004_SOURCE_ROAD_DIAGNOSTIC")!="1"):
        raise ValueError("S004 source bank diagnostic requires same-world road and gameplay diagnostics")
    if os.environ.get("WARLINE_S004_SOURCE_LAUNCH_DIAGNOSTIC")=="1" and os.environ.get("WARLINE_S004_SOURCE_BANK_DIAGNOSTIC")!="1":
        raise ValueError("S004 source launch diagnostic requires the source bank diagnostic")
    if os.environ.get("WARLINE_S004_SOURCE_OWNER_INITIALIZATION")=="1" and os.environ.get("WARLINE_S004_SOURCE_LAUNCH_DIAGNOSTIC")!="1":
        raise ValueError("S004 source owner initialization requires the original source launch")
    if os.environ.get("WARLINE_S004_SOURCE_AIR_OWNER_DIAGNOSTIC")=="1" and os.environ.get("WARLINE_S004_SOURCE_OWNER_INITIALIZATION")!="1":
        raise ValueError("S004 source air owner diagnostic requires shared owner initialization")
    if os.environ.get("WARLINE_S004_SOURCE_TRANSPORT_OWNER_DIAGNOSTIC")=="1" and os.environ.get("WARLINE_S004_SOURCE_AIR_OWNER_DIAGNOSTIC")!="1":
        raise ValueError("S004 source transport diagnostic requires original air diagnostic")
    if os.environ.get("WARLINE_S004_SOURCE_AIRDROP_OWNER_DIAGNOSTIC")=="1" and os.environ.get("WARLINE_S004_SOURCE_TRANSPORT_OWNER_DIAGNOSTIC")!="1":
        raise ValueError("S004 source airdrop diagnostic requires original transport diagnostic")
    if os.environ.get("WARLINE_S004_SOURCE_SUPPORT_OWNER_DIAGNOSTIC")=="1" and os.environ.get("WARLINE_S004_SOURCE_AIRDROP_OWNER_DIAGNOSTIC")!="1":
        raise ValueError("S004 source Support diagnostic requires original airdrop diagnostic")
    if os.environ.get("WARLINE_S004_SOURCE_EXPERIMENT_TICK")=="1" and os.environ.get("WARLINE_S004_SOURCE_SUPPORT_OWNER_DIAGNOSTIC")!="1":
        raise ValueError("S004 experimental tick requires original Support diagnostic")
    if os.environ.get("WARLINE_S004_SOURCE_MISSILE_TICK")=="1" and os.environ.get("WARLINE_S004_SOURCE_EXPERIMENT_TICK")!="1":
        raise ValueError("S004 missile source owners require the experimental tick")
    if os.environ.get("WARLINE_S004_SOURCE_SCAN_ROPE_TICK")=="1" and os.environ.get("WARLINE_S004_SOURCE_MISSILE_TICK")!="1":
        raise ValueError("S004 scan and rope owners require the missile source tick")
    if os.environ.get("WARLINE_S004_TRANSFORM_PROJECTION")=="1" and (os.environ.get("WARLINE_S004_SOURCE_EXPERIMENT_TICK")!="1" or os.environ.get("WARLINE_S004_TRANSFORM_CENSUS")!="1"):
        raise ValueError("S004 read-only transform projection requires the retained tick and census")
    if os.environ.get("WARLINE_S004_TRANSFORM_BRIDGE")=="1" and (os.environ.get("WARLINE_S004_TRANSFORM_PROJECTION")!="1" or (os.environ.get("WARLINE_S004_TRANSFORM_SCHEDULE")!="1" and os.environ.get("WARLINE_S004_EXPERIMENT_TICK_COUNT")!="1")):
        raise ValueError("S004 bridge probe requires the projection and either one tick or explicit scheduled mode")
    if os.environ.get("WARLINE_S004_TRANSFORM_SCHEDULE")=="1" and (os.environ.get("WARLINE_S004_TRANSFORM_BRIDGE")!="1" or os.environ.get("WARLINE_S004_SOURCE_SCAN_ROPE_TICK")!="1"):
        raise ValueError("S004 scheduled transform probe requires the bridge and scan/rope owner subset")
    if os.environ.get("WARLINE_S004_TRANSFORM_PHASE_VERIFY")=="1" and os.environ.get("WARLINE_S004_TRANSFORM_SCHEDULE")!="1":
        raise ValueError("S004 transform phase readback requires scheduled source bridge mode")
    tick_count_text=os.environ.get("WARLINE_S004_EXPERIMENT_TICK_COUNT")
    if tick_count_text is not None and (os.environ.get("WARLINE_S004_SOURCE_EXPERIMENT_TICK")!="1" or not tick_count_text.isascii() or not tick_count_text.isdecimal() or not 1<=int(tick_count_text)<=120):
        raise ValueError("S004 experimental tick count requires the diagnostic and must be between 1 and 120")
    tick_count=int(tick_count_text) if tick_count_text is not None else 1
    if (args.bundle is None)!=(args.bundle_sha256 is None): raise ValueError("Bundle path and hash must be provided together")
    if args.bundle is not None:
        candidate=Path(args.bundle)
        accepted=({"a069d2344af27de7d1066fdf66743ce6748e28c8d049806ccf2d6faa8375bfa2"} if args.terminal_handoff_epoch else {"d66ec48f5087b461b2b7b637f76d81e7cf6d25b4b234ca5e298ee023b7cfbdab"} if args.shared_focus_epoch else {"7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc"}) if args.current_epoch else {"0d86a50b081a46415cbcab163f9e0bde0c9074eb922374c25f497f9d0e9b0af4", "5ebddd9cfaeeaa25e1f2b6a7f69e2bb8d04412f2b0254d2e5c5a4bac90aa415c", "4ed5f9620309bd32fd636b90a977c89fe7849e019eeca7fcb1d87e078e280550"}
        if args.merged_source_epoch:
            accepted={"8b7424f649119a32d391fccb02c5415fe44d6906b00c7ca6df18acd265c7065d"}
        if args.guarded_source_epoch: accepted={"58b3545812a945ffe36e7d4a6326ae19dd9fe352dbbb5b396a4504ee3522168b"}
        if args.authoritative_source_epoch: accepted={"5698dd00804737cc6b0515c3aeb8136cc3eb5534eaa7886662eabbdee4dde28a"}
        if args.nov06_source_epoch: accepted={"0cc54c117a78b8135adf559a6d9e075452f5d036ae1edf355b8c3b1bdf7fe90f"}
        if not candidate.is_absolute() or candidate.is_symlink() or not candidate.is_file() or sha(candidate)!=args.bundle_sha256 or args.bundle_sha256 not in accepted: raise ValueError("Exact S004 diagnostic bundle required")
        expected_bundle=ROOT/("T Logs/SkirmishProduction/s004-terminal-handoff-inputs-20261105-T1/bundle.json" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/s004-shared-focus-inputs-20261105-T1/bundle.json" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-current-epoch-inputs-20261105-T2/bundle.json")
        if args.merged_source_epoch: expected_bundle=ROOT/"T Logs/SkirmishProduction/s004-current-merged-input-bundle-20261105-T4/bundle.json"
        if args.guarded_source_epoch: expected_bundle=ROOT/"T Logs/SkirmishProduction/s004-current-guard-input-bundle-20261105-T1/bundle.json"
        if args.authoritative_source_epoch: expected_bundle=ROOT/"T Logs/SkirmishProduction/s004-current-authoritative-input-bundle-20261105-T1/bundle.json"
        if args.nov06_source_epoch:
            expected_bundle=ROOT/"T Logs/SkirmishProduction/s004-source-input-refresh-20261106-T3/bundle.json"
        if args.current_epoch and candidate != expected_bundle: raise ValueError("Exact current-epoch bundle path required")
        BUNDLE=candidate
    elif args.current_epoch: raise ValueError("Explicit current-epoch bundle required")
    output = Path(args.output).absolute(); output.mkdir(parents=True, exist_ok=False)
    if args.merged_source_epoch:
        import verify_s004_merged_source_epoch as merged_bundle_verifier
        if args.nov06_source_epoch:
            import verify_s004_nov06_source_epoch as nov06_bundle_verifier
            verified=nov06_bundle_verifier.verify(BUNDLE,args.bundle_sha256,output/"merged-input-reconstruction")
        elif args.authoritative_source_epoch:
            import verify_s004_current_authoritative_epoch as authoritative_bundle_verifier
            verified=authoritative_bundle_verifier.verify(BUNDLE,args.bundle_sha256,output/"merged-input-reconstruction")
        elif args.guarded_source_epoch:
            import verify_s004_guarded_source_epoch as guarded_bundle_verifier
            verified=guarded_bundle_verifier.verify(BUNDLE,args.bundle_sha256,output/"merged-input-reconstruction")
        else:
            verified=merged_bundle_verifier.verify(BUNDLE,args.bundle_sha256,output/"merged-input-reconstruction")
        (output/"merged-input-verification.json").write_text(json.dumps(verified,indent=2)+"\n")
    runtime_setup=SETUP
    handoff_projection=None
    if selected_numeric_requested:
        import verify_s004_difficulty_readback as selected_setup_verifier
        selected_hashes={"Recruit":"1ee73ac6aec5e03cefe57bbcee44c8f45c7774dc460dedc6fb4718c8a9d963f1", "Regular":"c7a79c005df91df6fab6f63ad2eb39c32033b8313d5c46b74ba6e37a27ea1954", "Veteran":"b488d8ef4866e3eed06d51d4951299c9dfdd895d99b2e6ca7ae9c2d4f8e01b41", "Commander":"12bfa2d8df07d1256f709f26e77e49c95ad261174ee874943b04233a610d0106"}
        if args.nov06_source_epoch:
            selected_setup_verifier.COMPILER=ROOT/"T Logs/SkirmishProduction/s004-nov06-difficulty-compiler-20261106-T1"
            selected_setup_verifier.READBACKS=ROOT/"T Logs/SkirmishProduction/s004-nov06-difficulty-readbacks-20261106-T1"
            selected_setup_verifier.COMPILER_RECEIPT_SHA="29222b4bb80a236cf6ed9361211c723e0a3107e438a6d210f3ca0f02f77947f4"
            selected_hashes={"Recruit":"7d478eb522f3855297d26639a0bf68791d67c0b7545255475a2c99a0e97a10cd", "Regular":"4c7cc143e13e865b618293c727ed65453de5dc54de67c98bca0560143f86c4e2", "Veteran":"3f2e1d6aff46a5d657d61c5e5782ae5f478578ef0ee6286396f1794b50f4cb0c", "Commander":"44c9feccb869560a4a9ce4bfa4e94fb2305b0396fb63b32e3b7f0f9b175b8d5e"}
        selected_sha=selected_hashes[selected_difficulty]
        selected=selected_setup_verifier.verify(selected_difficulty,selected_sha)
        (output/"selected-setup-verification.json").write_text(json.dumps(selected,indent=2)+"\n")
        runtime_setup=Path(selected["readback"]["path"])
        handoff_projection=ROOT/"T Logs/SkirmishProduction/s004-historical-difficulty-handoff-20261106-T4/S004HistoricalDifficultyStartup.cs"
        if args.nov06_source_epoch:
            handoff_projection=ROOT/"T Logs/SkirmishProduction/s004-nov06-difficulty-handoff-20261106-T1/S004HistoricalDifficultyStartup.cs"
        handoff_receipt=json.loads((handoff_projection.parent/"receipt.json").read_text())
        if handoff_receipt.get("source",{}).get("sha256")!=sha(HARNESS) or handoff_receipt.get("projection",{}).get("sha256")!=sha(handoff_projection) or handoff_receipt.get("runtime_admission") is not False:
            raise ValueError("Exact selected-setup handoff projection required")
    execution_harness=handoff_projection if handoff_projection else HARNESS
    historical_factory_audit = None
    if args.historical_factory_source_diagnostic:
        historical_factory_audit = ROOT/"T Logs/SkirmishProduction/s004-production-catalog-difficulty-input-audit-20261106-T1/result.json"
        if sha(historical_factory_audit) != "8df1be4e9534238962db2ddcfd18576ecb6cf34ecfd112a14526e1623b53de3e":
            raise ValueError("Exact source-only production input audit required")
        audit=json.loads(historical_factory_audit.read_text())
        owner=audit.get("setup_owner_source",{})
        if (audit.get("status") != "DirectSetupInputsMatch" or audit.get("runtime_admission") is not False or
            audit.get("mission_ready") is not False or sha(Path(owner.get("path", ""))) != owner.get("sha256")):
            raise ValueError("Historical production input audit changed")
        original=execution_harness.read_text()
        old_call="GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[7]), args[8], templates, setup, Path.GetFullPath(args[17]), args[18])"
        historical_call="GeneratedBuildingDefinitions.LoadS004Source(Path.GetFullPath(args[7]), args[8], templates, historicalSetup, Path.GetFullPath(args[0]), args[1])"
        if original.count(old_call) != 1 or original.count("SourceS004HistoricalReadmodelCompatibility.RequireCompatible(historicalSetup, setup, in compatibilityGrid);") != 1:
            raise ValueError("Exact historical factory and setup compatibility anchors required")
        execution_harness=output/"S004HistoricalFactorySourceDiagnostic.cs"
        execution_harness.write_text(original.replace(old_call,historical_call,1))
    fuel_window_source = None
    if custom_fuel_window:
        text=execution_harness.read_text()
        old_condition="retainedWorld.Tick>=800 && retainedWorld.Tick<=910"
        old_scope="PassiveAfterOwnerAndControllerTicks_800To910_SourceOnly"
        if text.count(old_condition) != 1 or text.count(old_scope) != 1:
            raise ValueError("Exact passive fuel window anchors required")
        new_condition="retainedWorld.Tick>="+str(args.source_fuel_drift_start)+" && retainedWorld.Tick<="+str(args.source_fuel_drift_end)
        new_scope="PassiveAfterOwnerAndControllerTicks_"+str(args.source_fuel_drift_start)+"To"+str(args.source_fuel_drift_end)+"_SourceOnly"
        fuel_window_source=output/"S004FuelWindowSourceDiagnostic.cs"
        fuel_window_source.write_text(text.replace(old_condition,new_condition,1).replace(old_scope,new_scope,1))
        execution_harness=fuel_window_source
    if args.placement_state_readback:
        import generate_s004_placement_state_readback as placement_state_generator
        projection=placement_state_generator.generate(execution_harness,sha(execution_harness),output/"placement-state-projection")
        execution_harness=Path(projection["projection"]["path"])
    build_path = Path(args.build).resolve(strict=True); build = json.loads(build_path.read_text())
    expected_build=ROOT/("T Logs/SkirmishProduction/s002-refreshed-merged-source-build-20261105-T4/build.json" if args.merged_source_epoch else "T Logs/SkirmishProduction/s004-terminal-handoff-s002-build-20261105-T1/build.json" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/shared-inspection-source-build-20261105-T2/build.json" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/build.json")
    if args.guarded_source_epoch: expected_build=ROOT/"T Logs/SkirmishProduction/s002-current-merged-guard-build-20261105-T1/build.json"
    if args.authoritative_source_epoch: expected_build=ROOT/"T Logs/SkirmishProduction/s004-unqualified-source-build-20261105-T1/build.json"
    if args.fresh_canonical_build_sha256:
        canonical = (ROOT / "Tools/CI/SkirmishSourceSimulation/source_manifest.plan.json").resolve(strict=True)
        selected_manifest = build.get("source_manifest") or {}
        authoritative = build.get("canonical_source_manifest") or {}
        if historical_placement_input is None or not args.authoritative_source_epoch or sha(build_path) != args.fresh_canonical_build_sha256:
            raise ValueError("Explicit hash-pinned fresh build is limited to historical placement diagnostics")
        if (build.get("mission"), build.get("seed"), build.get("runtime_backend")) != ("S002", 104731, "LegacyGroundS002V1"):
            raise ValueError("Fresh canonical S002 source build identity required")
        if (selected_manifest.get("path") != str(canonical) or authoritative.get("path") != str(canonical) or
            selected_manifest.get("sha256") != sha(canonical) or authoritative.get("sha256") != sha(canonical)):
            raise ValueError("Fresh S002 build must use the exact current canonical manifest")
    elif args.current_epoch and build_path != expected_build:
        raise ValueError("Exact current source build required")
    if build.get("kind") != "SkirmishSourceSimulationBuild" or build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passed source build required")
    checked = json.loads(CHECKED.read_text())
    if checked.get("status") != "PassedScopedSourceOnlyCapture" or checked.get("wrapper_exit_code") != 0:
        raise ValueError("Checked native scoped capture required")
    setup_receipt = json.loads(SETUP_RECEIPT.read_text()); parity = json.loads(PARITY.read_text())
    parity_ok = (parity.get("status") == "Passed" and parity.get("capture_sha256") == sha(FACTORY) and
                 parity.get("factory_buildings_equal_historical") is True and parity.get("mission_ready") is False) if args.current_epoch else (
                 parity.get("status") == "PassedExactSourceFactoryFieldParity" and parity["input_sha256"][str(FACTORY)] == sha(FACTORY))
    if setup_receipt.get("status") != "Passed" or setup_receipt["compiled_setup"]["sha256"] != sha(SETUP) or not parity_ok: raise ValueError("Checked S004 setup and full factory parity required")
    grid_receipt, block_receipt, read_receipt, session_receipt = (json.loads(path.read_text()) for path in (GRID_RECEIPT, BLOCK_RECEIPT, READ_RECEIPT, SESSION_CONTROL))
    if grid_receipt.get("status") != "Passed" or grid_receipt["walkability_projection"]["sha256"] != sha(GRID_MASK) or grid_receipt["output"]["sha256"] != sha(GRID_REPORT): raise ValueError("Checked authored grid required")
    if block_receipt.get("status") != "PassedIsolatedBakedBlockerCapture" or block_receipt.get("capture_sha256") != sha(BLOCK_CAPTURE) or block_receipt.get("blocker_count") != 348: raise ValueError("Checked physical-only blockers required")
    if read_receipt.get("schema") != "s004-configured-spawnable-readmodel-receipt-v1" or read_receipt.get("rawExportSha256") != sha(READ_RAW): raise ValueError("Checked configured readmodel required")
    if session_receipt.get("status") != "PassedOriginalSessionOwnerFixture" or session_receipt["input_sha256"].get(str(SESSION_FRAGMENT)) != sha(SESSION_FRAGMENT): raise ValueError("Checked original session initializer required")
    road_receipt = json.loads(ROAD_RECEIPT.read_text())
    if road_receipt.get("status") != "PassedSourceOnlyBaseRoadProjection" or road_receipt.get("road_mask_sha256") != sha(ROAD) or road_receipt.get("road_cells") != 23500 or road_receipt["input_sha256"].get(str(GRAPH)) != sha(GRAPH) or road_receipt["input_sha256"].get(str(MONO_CONFIG)) != sha(MONO_CONFIG): raise ValueError("Checked source road projection required")
    import re
    config = PLACEMENT_CONFIG.read_text()
    if re.findall(r"^  buildPlaneY: ([^\r\n]+)$", config, re.M) != ["0"]: raise ValueError("Actual placement plane changed")
    grid_inputs = tuple(Path(row["path"]) for row in grid_receipt["inputs"])
    for row in grid_receipt["inputs"]:
        if sha(row["path"]) != row["sha256"]: raise ValueError("Authored grid dependency changed")
    historical_runner = (ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-evidence-20261104-T1/historical-batch-runner.py", ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-evidence-20261104-T1/historical-paid-owner-step.cs", ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-evidence-20261104-T1/historical-storage-authority.cs", ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-evidence-20261104-T1/historical-autonomous-ecs.cs", ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-evidence-20261104-T1/historical-autonomous-command-buffer.cs", ROOT / "T Logs/SkirmishProduction/s004-batch-refresh-evidence-20261104-T1/historical-owned-source-queries.cs") if sha(BUNDLE)=="4ed5f9620309bd32fd636b90a977c89fe7849e019eeca7fcb1d87e078e280550" else ()
    pinned = (RETAINED_WORLD, RESOURCE_LIFECYCLE, build_path, CAPTURE, CHECKED, FINALIZER, HARNESS, S3, S3LOG, S3RECEIPT, HELPER, S004_LOADER, SETUP, SETUP_RECEIPT, FACTORY, PARITY, PARITY_CHECK, GRID_RECEIPT, GRID_REPORT, GRID_MASK, BLOCK_CAPTURE, BLOCK_RECEIPT, BLOCK_FINALIZER, READ_RAW, READ_LOG, READ_RECEIPT, READ_FINALIZER, SESSION_CONTROL, SESSION_FRAGMENT, ROAD, ROAD_RECEIPT, GRAPH, MONO_CONFIG, ROTATION, LOADING_AUDIT, LOADING_TOOL, BUNDLE, BUNDLE_READER, PLACEMENT_CONFIG, *historical_runner, *grid_inputs)
    pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/source_s004_terminal_gate.py",ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/source_s004_run_evidence.py")
    if args.merged_source_epoch:
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        pinned=(*pinned,recipe/"build_s004_merged_source_epoch.py",recipe/"build_s004_current_epoch_bundle.py",
                recipe/"build_s004_current_role_catalog_metadata.py",recipe/"build_s004_current_airport_source_definition.py",
                recipe/"verify_s004_merged_source_epoch.py",recipe/"verify_s004_current_role_catalog_metadata.py",
                recipe/"verify_s004_current_airport_source_definition.py",output/"merged-input-verification.json",
                BUNDLE.parent/"input-evidence.json",ROOT/"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json")
    if args.guarded_source_epoch:
        pinned=(*pinned,recipe/"build_s004_guarded_source_epoch.py",recipe/"verify_s004_guarded_source_epoch.py")
    if args.authoritative_source_epoch:
        pinned=(*pinned,recipe/"build_s004_current_authoritative_epoch.py",recipe/"verify_s004_current_authoritative_epoch.py")
    if args.nov06_source_epoch:
        pinned=(*pinned,recipe/"build_s004_nov06_source_epoch.py",recipe/"verify_s004_nov06_source_epoch.py",
                recipe/"audit_s004_explicit_factory_capture.py",recipe/"audit_s004_explicit_readmodel_capture.py")
    if selected_numeric_requested:
        pinned=(*pinned,runtime_setup,handoff_projection,handoff_projection.parent/"receipt.json",
                recipe/"verify_s004_difficulty_readback.py",output/"selected-setup-verification.json",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004HistoricalReadmodelCompatibility.cs")
    if historical_factory_audit is not None:
        pinned=(*pinned,historical_factory_audit,output/"S004HistoricalFactorySourceDiagnostic.cs",Path(audit["setup_owner_source"]["path"]))
    if fuel_window_source is not None:
        pinned=(*pinned,fuel_window_source)
    if args.placement_state_readback:
        pinned=(*pinned,execution_harness,execution_harness.parent/"receipt.json",recipe/"generate_s004_placement_state_readback.py")
    if unit_candidate is not None:
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        pinned=(*pinned,unit_candidate,recipe/"verify_s004_unit_metadata_candidate.py",recipe/"s004_unit_metadata_profile.py")
        if args.unit_metadata_domain=="roles":
            pinned=(*pinned,recipe/"verify_s004_role_catalog_metadata.py",recipe/"build_s004_role_catalog_metadata.py")
    if airport_candidate is not None:
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        pinned=(*pinned,airport_candidate,recipe/"verify_s004_airport_source_definition.py",recipe/"build_s004_airport_source_definition.py",recipe/"S004AirportSourceDefinition.cs")
    if args.retained_map_probe:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004RetainedMapPublication.cs")
    if args.semantic_air_opening_probe:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004AirOpeningSegment.cs")
    if args.semantic_supply_probe:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004SupplySegment.cs")
    if args.public_threat_probe:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/s004_public_threat_projection.py",ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004PublicThreatControls.cs")
    if args.enemy_breakdown_probe:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/s004_enemy_timing_projection.py",ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004EnemyTiming.cs")
    if args.cached_setup_guard_probe:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/s004_setup_guard_cache_projection.py",ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004SetupGuardCacheControls.cs")
    if args.airself_helipad_readback_probe:
        resolver_dir=ROOT/("T Logs/SkirmishProduction/s004-airself-helipad-compile-20261105-T6" if args.airself_delivery_probe else "T Logs/SkirmishProduction/s004-airself-helipad-compile-20261105-T5" if args.airself_slot_binding_probe else "T Logs/SkirmishProduction/s004-airself-helipad-compile-20261105-T4" if args.airself_original_matcher_probe else "T Logs/SkirmishProduction/s004-airself-helipad-compile-20261105-T3")
        fixture_dir=ROOT/("T Logs/SkirmishProduction/s004-airself-original-alignment-fixture-20261105-T1" if args.airself_delivery_probe else "T Logs/SkirmishProduction/s004-airself-original-slot-fixture-20261105-T1" if args.airself_slot_binding_probe else "T Logs/SkirmishProduction/s004-airself-original-matcher-fixture-20261105-T1" if args.airself_original_matcher_probe else "T Logs/SkirmishProduction/s004-airself-helipad-fixture-20261105-T2")
        pinned=(*pinned,resolver_dir/"receipt.json",resolver_dir/"SourceAirSelfHelipadResolverProbe.cs",fixture_dir/"receipt.json",fixture_dir/"run.log")
    if args.airself_launch_geometry_probe:
        launch_dir=ROOT/"T Logs/SkirmishProduction/s004-airself-launch-geometry-control-20261105-T1"
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceAirSelfLaunchGeometry.cs",
                ROOT/"Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs",
                launch_dir/"receipt.json",launch_dir/"run.log")
    if args.airself_delivery_probe:
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        overlay=ROOT/"T Logs/SkirmishProduction/s004-airself-owner-overlay-20261105-T1"
        report=ROOT/"T Logs/SkirmishProduction/s004-airself-transform-chain-20261105-T2/report.json"
        completion=ROOT/"T Logs/SkirmishProduction/s004-airself-completion-control-20261105-T1/SourceAirSelfCompletionProjection.cs"
        pinned=(*pinned,overlay/"receipt.json",overlay/"466_SourceHelicopterDelivery.cs",overlay/"467_SourceProductionDeliveryOwner.cs",report,completion,
                *(recipe/name for name in ("audit_s004_airself_authored_transform_chain.py","generate_s004_airself_completion_projection.py","SourceS004AirSelfDelivery.cs","SourceS004AirSelfAuthoredPose.cs","SourceAirSelfFlightProjection.cs","SourceAirSelfCompletionOwnerBoundary.cs","SourceS004AirSelfTransactionReadback.cs")),
                ROOT/"Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs")
    if args.original_aria_step_probe:
        aria_dir=ROOT/"T Logs/SkirmishProduction/s004-original-planner-world-overlay-20261105-T2"
        aria=json.loads((aria_dir/"receipt.json").read_text())
        if aria.get("kind")!="S004OriginalPlannerSameWorldProbeOverlay" or aria.get("status")!="Prepared" or aria.get("mission_ready") is not False:
            raise ValueError("Exact original S004 planner overlay required")
        pinned=(*pinned,aria_dir/"receipt.json",Path(aria["guard_fixture_source"]),*(Path(p) for p in aria["source_sha256"]),
                *(Path(p) for p in aria["original_planner_input_sha256"]),*(Path(p) for p in aria["ui_lifecycle_input_sha256"]),
                ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073OriginalAriaStepDiagnostic.cs",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/S073OriginalAriaStepSourceHostControls.cs",
                ROOT/"T Logs/SkirmishProduction/s004-original-planner-world-controls-20261105-T1/receipt.json",
                ROOT/"T Logs/SkirmishProduction/s004-original-planner-world-controls-20261105-T1/run.log",
                ROOT/"T Logs/SkirmishProduction/aria-air-opening-s004-control-20261104-T1/receipt.json",
                ROOT/"T Logs/SkirmishProduction/aria-air-opening-s004-control-20261104-T1/run.log")
    if args.s004_logical_context_candidate_probe:
        context_dir=ROOT/"T Logs/SkirmishProduction/s004-logical-context-candidate-20261105-T1"
        context=json.loads((context_dir/"receipt.json").read_text())
        control_dir=ROOT/"T Logs/SkirmishProduction/s004-logical-context-candidate-control-20261105-T1"
        comparison_dir=ROOT/"T Logs/SkirmishProduction/s004-original-vs-logical-policy-20261105-T2"
        pinned=(*pinned,context_dir/"receipt.json",*(Path(p) for p in context["source_sha256"]),*(Path(p) for p in context["output_sha256"]),
                control_dir/"receipt.json",control_dir/"run.log",comparison_dir/"receipt.json",comparison_dir/"run.log",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/aria_s004_original_step/generate_s004_logical_context_candidate.py",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/aria_s004_original_step/S004OriginalVsLogicalPolicyControl.cs")
    if args.source_logical_aria_probe:
        logical_dir=ROOT/"T Logs/SkirmishProduction"/logical_candidate_name
        pinned=(*pinned,logical_dir/"receipt.json",logical_dir/"SourceS004LogicalControllerCandidate.cs",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/aria_s004_original_step/generate_s004_logical_controller_candidate.py",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.cs")
        if args.source_logical_air_service_probe:
            control=ROOT/"T Logs/SkirmishProduction/s004-selected-aircraft-return-control-20261105-T3"
            pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004SelectedAircraftReturn.cs",ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/S004SelectedAircraftReturnControl.cs",control/"receipt.json",control/"run.log")
        if args.source_logical_read_profile_probe:
            base=ROOT/("T Logs/SkirmishProduction/s004-terminal-guard-controller-20261105-T1" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/s004-logical-camera-optional-candidate-20261105-T2" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-logical-controller-candidate-20261105-T3")
            if args.nov06_source_epoch:
                base=ROOT/"T Logs/SkirmishProduction/s004-logical-controller-air-current-20261106-T1"
            pinned=(*pinned,base/"receipt.json",base/"SourceS004LogicalControllerCandidate.cs",ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/aria_s004_original_step/generate_s004_logical_read_profile.py")
        if args.source_logical_read_detail_probe:
            base=ROOT/("T Logs/SkirmishProduction/s004-terminal-guard-profile-20261105-T1" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/s004-shared-inspection-profile-20261105-T1" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-logical-controller-candidate-20261105-T4")
            if args.nov06_source_epoch:
                base=ROOT/"T Logs/SkirmishProduction/s004-logical-read-profile-current-20261106-T1"
            pinned=(*pinned,base/"receipt.json",base/"SourceS004LogicalControllerCandidate.cs",ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/aria_s004_original_step/generate_s004_logical_read_detail.py")
    if args.current_epoch:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CurrentEpochPreflight.cs",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/verify_s004_current_epoch_candidates.py",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/build_s004_current_role_catalog_metadata.py",
                ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/build_s004_current_airport_source_definition.py")
    if args.shared_focus_epoch:
        pinned=(*pinned,ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/build_s004_shared_focus_epoch.py")
    if args.serialized_setup_guard_experiment:
        if args.authoritative_source_epoch:
            experiment=ROOT/"T Logs/SkirmishProduction/s004-current-serialized-guard-candidate-20261106-T1"
            control=ROOT/"T Logs/SkirmishProduction/s004-current-serialized-guard-controls-20261106-T2"
            proof=json.loads((control/"receipt.json").read_text());generation=json.loads((experiment/"generation.json").read_text())
            if (proof.get("status")!="PassedExperimentalFixtureControls" or proof.get("mission_ready") is not False or
                proof.get("generation_sha256")!=sha(experiment/"generation.json") or
                generation.get("source_sha256")!=sha(HELPER) or generation.get("candidate_sha256")!=sha(experiment/"SourceS004SerializedGuardCandidate.cs") or
                generation.get("per_call_input_hash_preserved") is not True or generation.get("full_serialized_field_comparison") is not True or
                set(proof.get("results",{}))!={"Recruit","Regular","Veteran","Commander"}):
                raise ValueError("Current serialized-guard fixture proof required")
            for name,row in proof["results"].items():
                log=control/(name+".log")
                if sha(log)!=row.get("log_sha256") or "leaf_mutations_compared=700 conversion_skipped=0" not in log.read_text() or "controls=Passed cases=7" not in log.read_text():
                    raise ValueError("Complete serialized-guard controls required: "+name)
            for path,digest in proof.get("source_pins",{}).items():
                if sha(path)!=digest: raise ValueError("Serialized-guard fixture source changed: "+path)
            pinned=(*pinned,experiment/"generation.json",experiment/"SourceS004SerializedGuardCandidate.cs",control/"receipt.json",
                    *(control/(name+".log") for name in ("Recruit","Regular","Veteran","Commander")),*(Path(path) for path in proof["source_pins"]))
        else:
            experiment=ROOT/"T Logs/SkirmishProduction/s004-setup-serialized-guard-experiment-20261105-T2"
            proof=json.loads((experiment/"receipt.json").read_text())
            if (proof.get("status")!="ExperimentalFixturePassed" or proof.get("serialized_leaf_mutations_compared")!=699 or
                proof.get("run_exit")!=0 or proof.get("mission_ready") is not False or
                sha(experiment/"receipt.json")!="84ab5db105804e7e80342a690a34cfa7b08dc50f634246a10105e2957f1e6590" or
                sha(experiment/"SourceS004SerializedGuardCandidate.cs")!="e7a13ee9113c8522e717c15422e24e2f07c6c8132330aaf5dd31833369c069a9"):
                raise ValueError("Exact experimental serialized-guard fixture proof required")
            pinned=(*pinned,experiment/"receipt.json",experiment/"SourceS004SerializedGuardCandidate.cs",experiment/"run.log")
    if canonical_graph is not None:
        pinned=(*pinned,canonical_graph,matrix_path,*(Path(name) if Path(name).is_absolute() else ROOT/name for name in canonical_pins))
    if args.production_input_probe:
        pinned=(*pinned,ROOT/"Assets/Game/Scripts/Systems/BuildingGameplaySourceCompositionSystemHelper.cs")
    if args.dispatcher_probe:
        pinned=(*pinned,clearance,clearance_source)
    bank_requested=os.environ.get("WARLINE_S004_SOURCE_BANK_DIAGNOSTIC")=="1"
    launch_requested=os.environ.get("WARLINE_S004_SOURCE_LAUNCH_DIAGNOSTIC")=="1"
    owner_init_requested=os.environ.get("WARLINE_S004_SOURCE_OWNER_INITIALIZATION")=="1"
    air_requested=os.environ.get("WARLINE_S004_SOURCE_AIR_OWNER_DIAGNOSTIC")=="1"
    transport_requested=os.environ.get("WARLINE_S004_SOURCE_TRANSPORT_OWNER_DIAGNOSTIC")=="1"
    airdrop_requested=os.environ.get("WARLINE_S004_SOURCE_AIRDROP_OWNER_DIAGNOSTIC")=="1"
    support_requested=os.environ.get("WARLINE_S004_SOURCE_SUPPORT_OWNER_DIAGNOSTIC")=="1"
    tick_requested=os.environ.get("WARLINE_S004_SOURCE_EXPERIMENT_TICK")=="1"
    missile_requested=os.environ.get("WARLINE_S004_SOURCE_MISSILE_TICK")=="1"
    scan_rope_requested=os.environ.get("WARLINE_S004_SOURCE_SCAN_ROPE_TICK")=="1"
    transform_projection_requested=os.environ.get("WARLINE_S004_TRANSFORM_PROJECTION")=="1"
    transform_bridge_requested=os.environ.get("WARLINE_S004_TRANSFORM_BRIDGE")=="1"
    if transport_requested:
        import s004_transport_profile as transport_profile
        if args.current_epoch:
            transport_profile.S026=ROOT/("T Logs/SkirmishProduction/s004-terminal-handoff-s026-build-20261105-T1" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/shared-inspection-s026-build-20261105-T1" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-current-epoch-s026-build-20261105-T1")
            transport_profile.BUILD_SHA="df3b0548d9800ba4414bbe603b43ac579a3b7fecf2dad943fa2dff269044ed58" if args.terminal_handoff_epoch else "e0e65044eb02b7b9af23cf468496792b87f3e26cb27486d793d8636c3ed99f57" if args.shared_focus_epoch else "f03be605c02208288769276a10a55cf1a463f85d502845951d06d6f132f150d1"
            if args.merged_source_epoch:
                transport_profile.S026=ROOT/"T Logs/SkirmishProduction/s026-merged-source-host-build-20261105-T1"
                transport_profile.BUILD_SHA="6456571c85ac7927f8e5c13eefae7c8cc614a3f6dd62435daee4eeea6341cf00"
                if args.guarded_source_epoch:
                    transport_profile.S026=ROOT/"T Logs/SkirmishProduction/s026-current-merged-guard-build-20261105-T1"
                    transport_profile.BUILD_SHA="885778dd4cd82b303cbe453fb997551c7be7cdc4feac98be8c202f2cbd75aaab"
                if args.authoritative_source_epoch:
                    transport_profile.S026=ROOT/"T Logs/SkirmishProduction/s026-four-difficulty-reader-build-20261105-T1"
                    transport_profile.BUILD_SHA="be4cfe7762f88d4489593253d4298520aabbf8c63383e8f335e15a751c6f1293"
                if args.nov06_source_epoch:
                    transport_profile.S026=ROOT/"T Logs/SkirmishProduction/s004-current-s026-build-20261106-T1"
                    transport_profile.BUILD_SHA="b017a645103bb5e94546816b32e3a7717457c32ce023b4f64f0e8f9ce8345c6f"
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        pinned=(*pinned,recipe/"s004_transport_profile.py",recipe/"SourceS004FixedList128Bytes.cs",recipe/"S004TransportOwnerDiagnostic.cs",transport_profile.S026/"build.json")
    if airdrop_requested:
        import s004_airdrop_profile as airdrop_profile
        pinned=(*pinned,recipe/"s004_airdrop_profile.py",recipe/"generate_s004_airdrop_projection.py",recipe/"S004AirdropOwnerDiagnostic.cs")
    if support_requested:
        import s004_support_profile as support_profile
        pinned=(*pinned,recipe/"s004_support_profile.py",recipe/"S004SupportEntityManagerBoundary.cs",recipe/"S004SupportOwnerDiagnostic.cs")
    if tick_requested:
        import s004_tick_profile as tick_profile
        pinned=(*pinned,recipe/"s004_tick_profile.py",recipe/"S004SupplementalTickOwners.cs")
    if missile_requested:
        import s004_missile_profile as missile_profile
        pinned=(*pinned,recipe/"s004_missile_profile.py",recipe/"S004MissileTickOwners.cs")
    if scan_rope_requested:
        import s004_scan_rope_profile as scan_rope_profile
        pinned=(*pinned,recipe/"s004_scan_rope_profile.py",recipe/"S004ScanRopeTickOwners.cs")
    if args.aircraft_fuel_safety_probe:
        import s004_air_fuel_safety_profile as air_fuel_safety_profile
        control=ROOT/"T Logs/SkirmishProduction/s004-aircraft-fuel-safety-control-20261105-T1"
        pinned=(*pinned,recipe/"s004_air_fuel_safety_profile.py",ROOT/"Assets/Game/Scripts/Systems/AircraftFuelSafetyReturnSystem.cs",ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/S004AircraftFuelSafetySourceControl.cs",control/"receipt.json",control/"run.log")
    if args.aircraft_return_readback_probe:
        pinned=(*pinned,recipe/"SourceS004AircraftReturnReadback.cs",ROOT/"Assets/Game/Scripts/Systems/UnitReturnToBaseCommandService.cs")
    if args.source_availability_timing_probe:
        pinned=(*pinned,recipe/"s004_availability_timing_projection.py",ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/451_SourceCampAvailability.cs")
    if args.source_scope_timing_probe:
        pinned=(*pinned,recipe/"s004_scope_timing_projection.py",ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/454_SourceCampReadScope.cs")
    if args.source_fuel_drift_readback_probe:
        pinned=(*pinned,recipe/"SourceS004FuelDriftReadback.cs")
    if args.source_single_scope_query_experiment:
        candidate=ROOT/"T Logs/SkirmishProduction/s004-single-scope-query-candidate-20261105-T1"
        control=ROOT/"T Logs/SkirmishProduction/s004-single-scope-query-control-20261105-T1"
        pinned=(*pinned,candidate/"receipt.json",candidate/"SourceCampReadScopeSingleQuery.cs",control/"receipt.json",control/"run.log",ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/S004SingletonEnumerationControl.cs",recipe/"generate_s004_single_scope_query_candidate.py")
    if transform_projection_requested:
        import s004_transform_projection_profile as transform_projection_profile
        pinned=(*pinned,recipe/"s004_transform_projection_profile.py",recipe/"S004TransformProjectionReadback.cs",transform_projection_profile.BRIDGE,transform_projection_profile.S026_ECS,transform_projection_profile.PROBE)
    if transform_bridge_requested:
        import s004_transform_bridge_profile as transform_bridge_profile
        pinned=(*pinned,recipe/"s004_transform_bridge_profile.py",transform_bridge_profile.BRIDGE,transform_bridge_profile.CLEANUP,transform_bridge_profile.CLOCK,transform_bridge_profile.HEADLESS,transform_bridge_profile.READSET,transform_bridge_profile.FAMILY,transform_bridge_profile.ECS,transform_bridge_profile.SETUP)
    if bank_requested:
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        json_adapter=recipe/"S004StandardSetupJsonUtilityAdapter.cs.txt";compiler_binding=recipe/"S004OriginalCompilerSourceBinding.cs.txt";export_binding=recipe/"S004StandardSetupExportSourceBinding.cs.txt"
        replay=ROOT/"T Logs/SkirmishProduction/s004-current-layout-replay-20261104-T1/replayed-layout.json"
        original=ROOT/"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/original-compiler-result.json"
        if sha(replay)!="e37af2f2f7b827017b8e30af40b2507d0863ef335e154e1eb67c89157a6eea00" or sha(original)!="df4c6f0a8c28909d450216c900db915b86e957f11bd9256f66da7dd29bd36a14":raise ValueError("Exact S004 source replay inputs required")
        pinned=(*pinned,json_adapter,compiler_binding,export_binding,replay,original)
    if launch_requested:
        launch_source=ROOT/"Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs"
        launch_generator=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/generate_s004_source_launch_owner.py"
        if sha(launch_source)!="1e4cd11bb6eea3eec4351ea90ceef09b548b15940b6d135b3fb99986a1109890":raise ValueError("Exact original S004 launch owner required")
        pinned=(*pinned,launch_source,launch_generator)
    if owner_init_requested:
        selected_preset=ROOT/"Assets/Game/Resources/SkirmishBaseAssault.asset"
        if sha(selected_preset)!="ff020b7c082ecbede2e0383731f0ca11443b715e8f625336174ebddf89c412fe":raise ValueError("Exact selected S004 resource preset required")
        pinned=(*pinned,selected_preset)
    if air_requested:
        metadata_source=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/112_MetadataSystemTypes.cs"
        air_sources=tuple(ROOT/p for p in ("Assets/Game/Scripts/Systems/UnitAirMovementSystem.cs","Assets/Game/Scripts/Systems/UnitAirMovementSystem.Altitude.cs","Assets/Game/Scripts/Systems/UnitTransportCapacitySystem.cs","Assets/Game/Scripts/Components/CampaignMissionArmorBreakComponents.cs","Assets/Game/Scripts/Runtime/Missions/CampaignMissionAircraftStandoffUtility.cs"))
        pinned=(*pinned,metadata_source,*air_sources)
    pinned = (*pinned, *historical_mask_pins)
    input_sha = {str(path): sha(path) for path in pinned}
    pins = build["source_and_reference_pins"]
    def verify():
        for path, value in pins.items():
            if sha(path) != value: raise ValueError("Source build pin changed: " + path)
    def capture_check(name):
        result = subprocess.run(["python3", str(FINALIZER), "--directory", str(CAPTURE_DIR), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, text=True, capture_output=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != checked:
            raise ValueError("Capture receipt or current source pins changed")
    def parity_check(name):
        if args.current_epoch:
            airport_verifier.verify(airport_candidate,args.airport_candidate_sha256)
            (output/name).write_text(json.dumps({"status":"PassedCurrentAuditedFactoryJoin","audit_sha256":sha(PARITY),"mission_ready":False},indent=2)+"\n")
            return
        result = subprocess.run(["python3", str(PARITY_CHECK), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, capture_output=True, text=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != parity: raise ValueError("Full factory parity evidence changed")
    def blocker_check(name):
        result = subprocess.run(["python3", str(BLOCK_FINALIZER), "--directory", str(BLOCK_DIR), "--wrapper-exit", "0", "--output", str(output / name)], cwd=ROOT, capture_output=True, text=True, timeout=180)
        (output / (name + ".log")).write_text(result.stdout + result.stderr)
        if result.returncode != 0 or json.loads((output / name).read_text()) != block_receipt: raise ValueError("Blocker capture evidence changed")
    spec = importlib.util.spec_from_file_location("s004_readmodel_finalizer", READ_FINALIZER)
    read_finalizer = importlib.util.module_from_spec(spec); spec.loader.exec_module(read_finalizer)
    loading_spec = importlib.util.spec_from_file_location("s004_authored_loading", LOADING_TOOL)
    loading_module = importlib.util.module_from_spec(loading_spec); loading_spec.loader.exec_module(loading_module)
    loading_receipt=json.loads(LOADING_AUDIT.read_text())
    def loading_check():
        if loading_receipt.get("status")!="PassedSerializedLoadingInputAudit": raise ValueError("Checked loading source audit required")
        files={k:ROOT/v for k,v in loading_module.FILES.items()}
        if {str(path):sha(path) for path in files.values()}!=loading_receipt["source_hashes"]: raise ValueError("Loading source input drift")
        if loading_module.audit({k:path.read_text() for k,path in files.items()})!=loading_receipt["authored_facts"]: raise ValueError("Loading audit facts differ")
    loading_check()
    def readmodel_check():
        capture_setup=ROOT/"T Logs/SkirmishProduction/s004-current-standard-setup-20261104-T1/compiled-setup.json" if args.merged_source_epoch and not args.authoritative_source_epoch else SETUP
        if read_finalizer.verify(READ_LOG, READ_RAW, capture_setup, ROOT, 0) != read_receipt: raise ValueError("Configured readmodel evidence changed")
    def shared_prefab_check():
        actual=qualify(ROOT,S3,S3LOG,0); expected=json.loads(S3RECEIPT.read_text())
        if args.current_epoch:
            actual.pop("native_metadata_predates_added_capture_observer",None)
            if any(expected.get(key)!=value for key,value in actual.items()) or expected.get("scope")!="CurrentDataOnlyPrefabCapture_IndependentlyAuditedMetadata_NotRuntimeAdmission":
                raise ValueError("Current S003 prefab capture evidence changed")
        elif actual!=expected: raise ValueError("S003 source capture changed")
        return actual
    timings["input_setup_seconds"]=time.monotonic()-stage_start; stage_start=time.monotonic()
    with ThreadPoolExecutor(max_workers=3) as executor:
        tasks=[executor.submit(timed,"build_pins_before_seconds",verify),executor.submit(timed,"prefab_capture_before_seconds",lambda:capture_check("capture-before.json")),executor.submit(timed,"factory_parity_before_seconds",lambda:parity_check("parity-before.json")),executor.submit(timed,"blocker_capture_before_seconds",lambda:blocker_check("blockers-before.json")),executor.submit(timed,"readmodel_before_seconds",readmodel_check),executor.submit(timed,"shared_prefab_before_seconds",shared_prefab_check)]
        results=[task.result() for task in tasks]
    timings["pre_validation_seconds"]=time.monotonic()-stage_start; stage_start=time.monotonic()
    csc = next(Path(path) for path in pins if path.endswith("/Roslyn/bincore/csc.dll"))
    dotnet = shutil.which("dotnet")
    if not dotnet or not any(Path(path).name == "dotnet" and Path(path).resolve() == Path(dotnet).resolve() for path in pins): raise ValueError("Pinned dotnet required")
    response = Path(build["compiler_response"]["path"])
    if sha(response) != build["compiler_response"]["sha256"]: raise ValueError("Compiler response changed")
    arguments = [json.loads(line) for line in response.read_text().splitlines() if line.strip()]
    if args.current_epoch:
        matches=[i for i,value in enumerate(arguments) if value.endswith("/513_FreshSourceStorageLifetimeAuthority.cs")]
        if len(matches)!=1: raise ValueError("Exact source storage authority required")
        authority_path=Path(arguments[matches[0]])
        if pins.get(str(authority_path))!=sha(authority_path): raise ValueError("Current source storage authority pin changed")
        authority=authority_path.read_text()
        old='if(bundleHash!=expectedBundle||setup==null||setup.CatalogId!="S004"'
        new=('if((bundleHash!=expectedBundle && !(bundleHash=='+json.dumps(args.bundle_sha256)+' && '
             'bundlePath=='+json.dumps(str(BUNDLE))+'))||setup==null||setup.CatalogId!="S004"')
        if authority.count(old)!=1: raise ValueError("Exact S004 source storage admission seam changed")
        projected=output/"S004CurrentEpochStorageAuthority.cs"
        projected.write_text(authority.replace(old,new,1));arguments[matches[0]]=str(projected)
    matches = [path for path in arguments if path.endswith("/typed-GeneratedTypedOpeningActorSource.cs")]
    if len(matches) != 1: raise ValueError("One original typed actor projection required")
    original = Path(matches[0]); original_text = original.read_text()
    old_guard = 'if(setup==null||!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)||typedPlacement==null)throw new NotSupportedException("Actual original launch-owned typed constraint required");SourceGroundMissionFamily.RequireCatalogSetup(setup);'
    if original_text.count(old_guard) != 1: raise ValueError("Exact original profile guard required")
    guard = ('SourceS004CompiledSetupInput.RequireExact(setup,' + json.dumps(str(runtime_setup)) + ',' + json.dumps(sha(runtime_setup)) + ');'
             'if(typedPlacement!=null||forceIndex<0||forceIndex>=setup.Forces.Length||member<0||member>=force.Quantity||'
             '!Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JObject.FromObject(force),Newtonsoft.Json.Linq.JObject.FromObject(setup.Forces[forceIndex]))||'
             'prefabKey!=force.RuntimePrefabKey||perMemberSupply!=force.SupplyCost/force.Quantity)throw new NotSupportedException("Exact S004 source force required");')
    derived = output / "S004OpeningActorSource.cs"; derived.write_text(original_text.replace(old_guard, guard))
    executable = output / "S004BundleDrivenStartup.exe"
    for i, value in enumerate(arguments):
        if value == str(original): arguments[i] = str(derived)
        elif value.startswith("-main:"): arguments[i] = "-main:S004BundleDrivenEntry"
        elif value.startswith("-out:"): arguments[i] = "-out:" + str(executable)
    owner = ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs"
    if pins.get(str(owner)) != sha(owner): raise ValueError("Original ledger owner pin required")
    owner_text = owner.read_text()
    start_marker = "            visualPending = (byte)(startingSetBound ? 0 : 1);"
    end_marker = "            if (Application.isPlaying && !SkirmishStartingSupplyService.Initialize(em, session, setup))"
    if owner_text.count(start_marker) != 1 or owner_text.count(end_marker) != 1: raise ValueError("Original ledger source seam changed")
    tail = owner_text[owner_text.index(start_marker):owner_text.index(end_marker)]
    preamble = """using Game.Components; using Game.Configs; using Game.Runtime; using Game.Skirmish.Contracts; using Unity.Entities;
internal static class S004OriginalLedgerTail {
 internal static void Apply(EntityManager em, Entity session, SkirmishResolvedSetup setup, Game.Simulation.SkirmishMissionBootstrapKernel opening, out byte visualPending) {
 bool startingSetBound=opening.StartingSetBound;
 int infantryLive=opening.Player.Infantry, groundLive=opening.Player.Ground, airLive=opening.Player.Air, supplyLive=opening.Player.Supply;
 int enemyInfantryLive=opening.Enemy.Infantry, enemyGroundLive=opening.Enemy.Ground, enemyAirLive=opening.Enemy.Air, enemySupplyLive=opening.Enemy.Supply;
"""
    ledger = output / "S004OriginalLedgerTail.cs"; ledger.write_text(preamble + tail + "} }\n")
    probe_path = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/probe_s004_spawn_owners_compile.py"
    spec = importlib.util.spec_from_file_location("s004_owner_extract", probe_path); probe = importlib.util.module_from_spec(spec); spec.loader.exec_module(probe)
    lookup_method = probe.extract_method(owner_text, "BuildPrefabEntityLookup")
    lookup = output / "S004OriginalPrefabLookup.cs"
    lookup.write_text("using System.Collections.Generic; using Game.Components; using Unity.Collections; using Unity.Entities; namespace Game.Runtime { internal static class S004OriginalPrefabLookup {\n" + lookup_method + "\n} }\n")
    full_probe = ROOT / "T Logs/SkirmishProduction/s004-cleanup-boundary-control-20261104-T1/S004FullSpawnLedgerProbe.cs"
    cleanup = full_probe.parent / "S004OriginalCleanup.cs"; probe_receipt = json.loads((full_probe.parent / "receipt.json").read_text())
    if probe_receipt.get("status") != "PassedSourceCleanupBoundaryControl" or probe_receipt.get("probe_sha256") != sha(full_probe) or probe_receipt.get("cleanup_sha256") != sha(cleanup): raise ValueError("Checked ledger compile probe required")
    outer = probe.extract_method(owner_text, "TrySpawn")
    checkpoint = """            if (em.HasComponent<SkirmishNativeCheckpointRecoveryRecord>(entity))
            {
                SkirmishNativeCheckpointRecoveryRunner.Step(em, entity, setup);
                return;
            }"""
    if outer.count(checkpoint)!=1 or outer.count("Application.isPlaying")!=2: raise ValueError("Outer source boundary changed")
    outer=outer.replace("private static void TrySpawn", "internal static void TrySpawn",1)
    outer=outer.replace("            SkirmishExpandedSessionComponent session =", "            const bool sourcePlayerSession = true;\n            SkirmishExpandedSessionComponent session =",1)
    outer=outer.replace("Application.isPlaying","sourcePlayerSession")
    outer=outer.replace(checkpoint, "            if (em.HasComponentNamed(entity, \"Game.Runtime.SkirmishNativeCheckpointRecoveryRecord\")) throw new NotSupportedException(\"Checkpoint recovery unsupported in source diagnostic\");")
    outer=outer.replace("SkirmishMissionLogicalBindingProvider.TryAttachAfterActualSpawn", "S004SourceOuterBoundaries.RejectLogicalBinding")
    outer=outer.replace("SkirmishS005OpeningViewPolicy.TryQueueAfterActualSpawn", "S004SourceOuterBoundaries.AssumeNoViewProfile")
    base=full_probe.read_text(); pos=base.rfind("\n} }")
    if pos<0: raise ValueError("Expected original source wrapper")
    outer_source=output/"S004OuterSpawnProjection.cs"; outer_source.write_text(base[:pos]+"\n"+outer+base[pos:])
    boundary=output/"S004OuterBoundaries.cs"; boundary.write_text("""using System; using Game.Components; using Unity.Entities; namespace Game.Runtime { internal static class S004SourceOuterBoundaries {
      internal static bool RejectLogicalBinding(EntityManager em, Entity session, out string error) { throw new NotSupportedException(\"Logical binding unsupported in this S004 diagnostic\"); }
      internal static bool AssumeNoViewProfile(EntityManager em, Entity session, out string error) {
        if(!em.HasComponent<SkirmishResolvedSetupRecord>(session) || em.GetComponentObject<SkirmishResolvedSetupRecord>(session).Setup?.CatalogId != \"S004\") throw new NotSupportedException(\"S004 setup required\");
        error=null; return true; }
    } }""")
    enemy_matches=[value for value in arguments if value.endswith("/404_SkirmishEnemyStrategySystem.source.cs")]
    if len(enemy_matches)!=1: raise ValueError("Exact generated enemy owner required")
    enemy_source=Path(enemy_matches[0]); enemy_text=enemy_source.read_text()
    old_guard='if (setup.ObjectiveKind != SkirmishObjectiveKind.BaseAssault || (setup.EnemyPolicy == null ? !((setup.CatalogId == "S002" && setup.ArmyProfileId == SkirmishArmyProfileId.GroundManeuver) || (setup.CatalogId == "S003" && setup.DefinitionId == "skirmish.s003" && setup.SetupHash == 2394754492u && setup.ArmyProfileId == SkirmishArmyProfileId.AirMobile)) : !SkirmishGenericGroundEnemyRollout.Supports(setup)))\n                throw new System.NotSupportedException("Current source S002 null-policy or actual generic-ground BaseAssault policy required");'
    if enemy_text.count(old_guard)!=1: raise ValueError("Exact source enemy guard changed")
    enemy_guard='SourceS004CompiledSetupInput.RequireExact(setup,'+json.dumps(str(runtime_setup))+','+json.dumps(sha(runtime_setup))+'); if(setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault || setup.EnemyPolicy!=null) throw new System.NotSupportedException("Exact S004 null-policy BaseAssault required");'
    enemy_derived=output/"S004EnemyStrategyProjection.cs"; enemy_derived.write_text(enemy_text.replace(old_guard,enemy_guard))
    arguments=[str(enemy_derived) if value==str(enemy_source) else value for value in arguments]
    movement_owner=ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishWorldMovementSystem.cs"
    movement_service=ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishWorldMovementService.cs"
    engagement_service=ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishExpandedEngagementService.cs"
    visual_service=ROOT / "Assets/Game/Scripts/Runtime/Skirmish/SkirmishVisualSpawnService.cs"
    for path in (movement_owner,movement_service,engagement_service,visual_service):
        if pins.get(str(path))!=sha(path): raise ValueError("Exact original movement source pin required: "+str(path))
    cmd_matches=[value for value in arguments if value.endswith("/249_SkirmishWorldMovementService.commands.cs")]
    if len(cmd_matches)!=1: raise ValueError("Exact movement commands source required")
    cmd_source=Path(cmd_matches[0]); cmd_text=cmd_source.read_text(); declaration="public static class SkirmishWorldMovementService {"
    if cmd_text.count(declaration)!=1: raise ValueError("Expected source movement class declaration")
    cmd_derived=output/"S004MovementCommands.cs"; cmd_derived.write_text(cmd_text.replace(declaration,"public static partial class SkirmishWorldMovementService {"))
    arguments=[str(cmd_derived) if value==str(cmd_source) else value for value in arguments]
    m=movement_service.read_text(); e=engagement_service.read_text()
    movement_methods=[probe.extract_method(m,name) for name in ("Step","HomeAttackDestination","HasCurrentNativeAttackGoal","TrySharedPathStep","ResolveSpeed","SyncVisual")]
    original_sync=movement_methods[-1]
    if "SkirmishVisualInstanceRecord" not in original_sync: raise ValueError("Original visual sync changed")
    movement_methods[-1]='private static void SyncVisual(EntityManager em, Entity unit) { if(em.HasComponent<SkirmishVisualInstanceRecord>(unit)) throw new System.NotSupportedException("Visual sync unsupported in source host"); }'
    engagement_methods=[probe.extract_method(e,name) for name in ("Step","IssueCommandedSiegeAttack","CanShoot","FindCombatant","OrderedStructure","ApplyShot")]
    header="using System; using System.Collections.Generic; using Game.Components; using Game.Configs; using Game.Runtime; using Game.Skirmish.Contracts; using Game.Tactical.Contracts; using Unity.Collections; using Unity.Entities; using Unity.Mathematics; using Unity.Transforms; using UnityEngine; namespace Game.Runtime {\n"
    closure=output/"S004MovementOwnerClosure.cs"; closure.write_text(header+"public static partial class SkirmishWorldMovementService {\n"+"\n".join(movement_methods)+"\n} public static partial class SkirmishExpandedEngagementService {\n"+"\n".join(engagement_methods)+"\n} }\n")
    outer_text=movement_owner.read_text(); call="SkirmishVisualSpawnService.EnsureMissingCombatTransforms(em, entity);"
    if outer_text.count(call)!=1: raise ValueError("Expected original visual transform call")
    outer_text=outer_text.replace(call,"S004MovementVisualBoundary.RequireExistingTransforms(em, entity);")
    movement_derived=output/"S004WorldMovementSystem.cs"; movement_derived.write_text(outer_text)
    visual_boundary=output/"S004MovementVisualBoundary.cs"; visual_boundary.write_text("""using System; using Game.Components; using Unity.Collections; using Unity.Entities; using Unity.Transforms; namespace Game.Runtime { internal static class S004MovementVisualBoundary { internal static void RequireExistingTransforms(EntityManager em, Entity session) { var id=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId; using var q=em.CreateEntityQuery(typeof(SkirmishAttemptOwnedComponent)); using var es=q.ToEntityArray(Allocator.Temp); foreach(var e in es) if(em.GetComponentData<SkirmishAttemptOwnedComponent>(e).SessionId.Equals(id) && !em.HasComponent<LocalTransform>(e)) throw new NotSupportedException(\"Missing original presentation transform unsupported\"); } } }""")
    attack_owner=ROOT / "Assets/Game/Scripts/Systems/UnitAttackOrderRequestSystem.cs"
    if pins.get(str(attack_owner))!=sha(attack_owner): raise ValueError("Pinned original attack request owner required")
    attack_components=ROOT / "Assets/Game/Scripts/Components/UnitAttackOrderRequestComponents.cs"
    target_owner=ROOT / "Assets/Game/Scripts/Systems/UnitTargetOrderSystem.cs"
    for path in (attack_components,target_owner):
        if pins.get(str(path))!=sha(path): raise ValueError("Pinned original attack dependency required: "+str(path))
    payload=ROOT / "Assets/Game/Scripts/Systems/UnitTransportAttackPayloadUtility.cs"
    text_source=ROOT / "Assets/Game/Scripts/Configs/GameText.cs"
    safety=ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/extra-621-ExplicitUnqualifiedBoundaries.cs"
    for path in (payload,text_source):
        if pins.get(str(path))!=sha(path): raise ValueError("Pinned target dependency required: "+str(path))
    safety_hash=sha(safety)
    text_boundary=ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073UiShellOwnerTextBoundary.cs"; text_hash=sha(text_boundary)
    if bank_requested:
        matches=[i for i,value in enumerate(arguments) if value.endswith("/050_SourceConfigJson.cs")]
        if len(matches)!=1:raise ValueError("One exact source JsonUtility adapter required")
        arguments[matches[0]]=str(json_adapter);arguments.extend((str(compiler_binding),str(export_binding)))
    if air_requested:
        indices=[i for i,value in enumerate(arguments) if value==str(metadata_source)]
        placeholder="namespace Game.Runtime{public struct UnitAirMovementSystem:Unity.Entities.ISystem{}}"
        metadata=metadata_source.read_text()
        if len(indices)!=1 or metadata.count(placeholder)!=1:raise ValueError("Exact metadata-only air placeholder required")
        alternate=output/"MetadataWithoutAirPlaceholder.cs";alternate.write_text(metadata.replace(placeholder,""))
        arguments[indices[0]]=str(alternate);arguments.extend(str(path) for path in air_sources)
    if transport_requested:
        transport_profile.apply(arguments,output)
    if airdrop_requested:
        airdrop_profile.apply(arguments,output)
        arguments.append(str(recipe/"S004AirdropOwnerDiagnostic.cs"))
    if support_requested:
        support_profile.apply(arguments,output)
    if missile_requested:
        missile_profile.apply(arguments,output)
    if scan_rope_requested:
        scan_rope_profile.apply(arguments,output)
    if tick_requested:
        tick_profile.apply(arguments,output)
    if missile_requested:
        missile_profile.apply_tick(arguments,output)
    if scan_rope_requested:
        scan_rope_profile.apply_tick(arguments,output)
    if args.aircraft_fuel_safety_probe:
        air_fuel_safety_profile.apply(arguments,output)
    if args.aircraft_return_readback_probe:
        indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(indices)!=1: raise ValueError("One source define set required")
        arguments[indices[0]]+=",WARLINE_S004_AIRCRAFT_RETURN_READBACK"
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004AircraftReturnReadback.cs"))
    if args.source_availability_timing_probe:
        import s004_availability_timing_projection
        s004_availability_timing_projection.apply(arguments,output)
    if args.source_scope_timing_probe:
        import s004_scope_timing_projection
        s004_scope_timing_projection.apply(arguments,output)
    if args.source_fuel_drift_readback_probe:
        indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(indices)!=1: raise ValueError("One source define set required")
        arguments[indices[0]]+=",WARLINE_S004_FUEL_DRIFT_READBACK"
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004FuelDriftReadback.cs"))
    if args.source_single_scope_query_experiment:
        candidate=ROOT/"T Logs/SkirmishProduction/s004-single-scope-query-candidate-20261105-T1"
        control=ROOT/"T Logs/SkirmishProduction/s004-single-scope-query-control-20261105-T1"
        original=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/454_SourceCampReadScope.cs"
        replacement=candidate/"SourceCampReadScopeSingleQuery.cs"
        proof=json.loads((candidate/"receipt.json").read_text()); test=json.loads((control/"receipt.json").read_text())
        if proof.get("source_sha256")!=sha(original) or proof.get("output_sha256")!=sha(replacement) or proof.get("full_behavior_parity_proven") is not False or proof.get("mission_ready") is not False or test.get("compile_exit")!=0 or test.get("run_exit")!=0 or test.get("pass_marker") is not True or test.get("source_pins_after",{}).get(str(replacement))!=sha(replacement) or "[S004SingletonEnumerationControl] result=Passed cases=8" not in (control/"run.log").read_text(): raise ValueError("Exact unqualified single-query candidate and source controls required")
        matches=[i for i,value in enumerate(arguments) if value==str(original)]
        if len(matches)!=1: raise ValueError("Exact source availability scope required")
        arguments[matches[0]]=str(replacement)
    if transform_projection_requested:
        transform_projection_profile.apply(arguments,output)
    if launch_requested:
        generated_launch=output/"SourceS004ExpandedLaunchProjection.cs"
        generated=subprocess.run([sys.executable,str(launch_generator),"--source-sha256",sha(launch_source),"--output",str(generated_launch)],capture_output=True,text=True,timeout=60)
        (output/"launch-generation.log").write_text(generated.stdout+generated.stderr)
        if generated.returncode!=0 or "[S004SourceLaunchOwnerExtraction] result=Passed" not in generated.stdout:raise ValueError("Original S004 launch owner generation failed")
        existing_launch = [Path(x) for x in arguments if Path(x).name == "SourceExpandedLaunchProjection.cs"] if args.nov06_source_epoch else []
        if existing_launch:
            if len(existing_launch) != 1 or sha(existing_launch[0]) != sha(generated_launch):
                raise ValueError("Current launch owner differs from original source projection")
        else:
            arguments.append(str(generated_launch))
    session_fragments = [SESSION_FRAGMENT]
    if args.nov06_source_epoch:
        existing_session = [Path(x) for x in arguments if Path(x).name == "SourceExpandedSessionInitialization.cs"]
        if existing_session:
            if len(existing_session) != 1:
                raise ValueError("One current original session owner required")
            def session_methods(path):
                text = path.read_text()
                body = re.sub(r"\s+", "", text[text.index("public void OnUpdate("):])
                if not body.endswith("}}"):
                    raise ValueError("Unexpected session owner closure")
                return body[:-2]
            if session_methods(existing_session[0]) != session_methods(SESSION_FRAGMENT):
                raise ValueError("Current original session owner differs from retained session methods")
            session_fragments = []
    arguments.extend(str(path) for path in (execution_harness, HELPER, S004_LOADER, *session_fragments, outer_source, cleanup, boundary, movement_derived, closure, visual_boundary, attack_owner, attack_components, target_owner, payload,text_boundary,safety,BUNDLE_READER,RETAINED_WORLD,RESOURCE_LIFECYCLE))
    if args.nov06_source_epoch:
        helper_indices = [i for i, value in enumerate(arguments) if value == str(HELPER)]
        if len(helper_indices) == 2:
            # The fresh build already includes the same source file that the
            # retained harness adds. Compile that exact source once.
            del arguments[helper_indices[-1]]
        elif len(helper_indices) != 1:
            raise ValueError("One actual S004 setup owner source required")
    if historical_placement_input is not None:
        if str(historical_mask_source) not in arguments:
            arguments.append(str(historical_mask_source))
        define_indices = [i for i, value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices) != 1:
            raise ValueError("One source define set required for historical placement")
        if "WARLINE_S004_HISTORICAL_MASKS" not in arguments[define_indices[0]].split(","):
            arguments[define_indices[0]] += ",WARLINE_S004_HISTORICAL_MASKS"
    if selected_numeric_requested:
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004HistoricalReadmodelCompatibility.cs"))
    if args.current_epoch:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_CURRENT_EPOCH"
        epoch_source=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CurrentEpochPreflight.cs"
        if args.shared_focus_epoch:
            text=epoch_source.read_text()
            target_dir="T Logs/SkirmishProduction/s004-terminal-handoff-inputs-20261105-T1/" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/s004-shared-focus-inputs-20261105-T1/"
            if args.merged_source_epoch:
                target_dir="T Logs/SkirmishProduction/s004-current-merged-input-bundle-20261105-T4/"
            if args.guarded_source_epoch: target_dir="T Logs/SkirmishProduction/s004-current-guard-input-bundle-20261105-T1/"
            if args.authoritative_source_epoch: target_dir="T Logs/SkirmishProduction/s004-current-authoritative-input-bundle-20261105-T1/"
            if args.nov06_source_epoch: target_dir="T Logs/SkirmishProduction/s004-source-input-refresh-20261106-T3/"
            replacements={
                "7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc":"a069d2344af27de7d1066fdf66743ce6748e28c8d049806ccf2d6faa8375bfa2" if args.terminal_handoff_epoch else "d66ec48f5087b461b2b7b637f76d81e7cf6d25b4b234ca5e298ee023b7cfbdab",
                "452076ef52b6582858e599efa503027b1815641844ee956f593db7a660039f45":"0874f3a98f181c32cfdf15b2f58d2914b3108cd079cedd1355b2939ba534d75f" if args.terminal_handoff_epoch else "a5a2d0c7053518eafbaf83e8ae421059d1c3c859324d196f9c66e4bb87a5d1e6",
                "T Logs/SkirmishProduction/s004-current-epoch-inputs-20261105-T2/bundle.json":target_dir+"bundle.json",
                "T Logs/SkirmishProduction/s004-current-epoch-inputs-20261105-T2/input-evidence.json":target_dir+"input-evidence.json"}
            if args.merged_source_epoch:
                replacements["7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc"]="8b7424f649119a32d391fccb02c5415fe44d6906b00c7ca6df18acd265c7065d"
                replacements["452076ef52b6582858e599efa503027b1815641844ee956f593db7a660039f45"]="81b1a7fb0a17a54b8dd36f3e57383be7cef4600a6f7238951e93c51d38a937c7"
            if args.guarded_source_epoch:
                replacements["7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc"]="58b3545812a945ffe36e7d4a6326ae19dd9fe352dbbb5b396a4504ee3522168b"
                replacements["452076ef52b6582858e599efa503027b1815641844ee956f593db7a660039f45"]="6d9b7291589b1162ffc8c6080690b6c600bba8bc80fa5e4554238c54b88495dd"
            if args.authoritative_source_epoch:
                replacements["7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc"]="5698dd00804737cc6b0515c3aeb8136cc3eb5534eaa7886662eabbdee4dde28a"
                replacements["452076ef52b6582858e599efa503027b1815641844ee956f593db7a660039f45"]="8d4f87c6c7f65037078f8e61e732aa92aaa7c8f82f85641183330193c68a75bb"
            if args.nov06_source_epoch:
                replacements["7e02f0fdbdbeacb098e00827ef5994a4551a59940ce3d7f8b76647402fae5cfc"]="0cc54c117a78b8135adf559a6d9e075452f5d036ae1edf355b8c3b1bdf7fe90f"
                replacements["452076ef52b6582858e599efa503027b1815641844ee956f593db7a660039f45"]="6884445281d763ffeef0cd1e97b33fe540026bdb60c1b7ab096cd5c0f57fa923"
            for old,new in replacements.items():
                if text.count(old)!=1: raise ValueError("Exact source preflight epoch anchor required: "+old)
                text=text.replace(old,new,1)
            if args.merged_source_epoch:
                old='} else Need(JToken.DeepEquals(oldInputs[row.Name], row.Value), "Retained input differs: " + row.Name);'
                new='''} else if (row.Name == "compiled_setup") {
                    var currentEvidence=evidence["current_setup_export"] as JObject;
                    Need(currentEvidence!=null && JToken.DeepEquals(currentEvidence["capture"],row.Value), "Current setup evidence differs");
                    var proof=Read(CheckDescriptor(currentEvidence["compiler_receipt"]));
                    Need((string)proof["status"]=="Passed" && (string)proof["compiled_setup"]?["path"]==path &&
                        (string)proof["compiled_setup"]?["sha256"]==(string)row.Value["sha256"] &&
                        (string)proof["source_build"]?["path"]==(string)evidence["source_builds"]?["s002"]?["path"] &&
                        (string)proof["source_build"]?["sha256"]==(string)evidence["source_builds"]?["s002"]?["sha256"] &&
                        (bool?)proof["world_initialized"]==false && (bool?)proof["physical_source_fit_verified"]==false &&
                        (bool?)proof["mission_readiness"]==false, "Current source setup proof differs");
                    var oldExport=Read(CheckDescriptor(oldInputs[row.Name])); var newExport=Read(path);
                    var setup=newExport["compiled_setup"] as JObject;
                    Need(setup!=null, "Current compiled setup required");
                    string serializedSchema=(string)currentEvidence["serialized_schema"];
                    if (serializedSchema==null || serializedSchema=="RetainedSetupWithDisabledCompatibilityField") {
                        Need((bool?)setup["RequireAuthoredStructureOrigins"]==false, "Disabled compatibility field required");
                        setup.Remove("RequireAuthoredStructureOrigins");
                    } else if (serializedSchema=="ExactRetainedSetupWithoutCompatibilityField") {
                        Need(setup.Property("RequireAuthoredStructureOrigins")==null, "Compatibility field must be absent");
                    } else throw new InvalidOperationException("Unknown serialized setup schema");
                    Need(JToken.DeepEquals(oldExport,newExport), "Current setup differs from retained serialized values");
                } else Need(JToken.DeepEquals(oldInputs[row.Name], row.Value), "Retained input differs: " + row.Name);'''
                if text.count(old)!=1: raise ValueError("Exact retained setup preflight anchor required")
                text=text.replace(old,new,1)
            epoch_source=output/"SourceS004SharedFocusEpochPreflight.cs"
            epoch_source.write_text(text)
        arguments.append(str(epoch_source))
    if unit_candidate is not None:
        unit_profile.apply(arguments,output,args.unit_metadata_sha256,args.unit_metadata_domain=="roles",args.current_epoch)
        if args.historical_unit_metadata_source_diagnostic:
            if args.unit_metadata_domain != "roles" or not args.authoritative_source_epoch:
                raise ValueError("Historical unit metadata requires the exact source role domain")
            guard_path=output/"S004DiagnosticUnitMetadataGuard.cs"
            guard=guard_path.read_text()
            old_guard='setupHash != 1130726651u || seed != 104733'
            old_root='(uint?)root["setup_hash"] != setupHash'
            if guard.count(old_guard) != 1 or guard.count(old_root) != 1:
                raise ValueError("Exact historical metadata identity anchors required")
            selected_setup_hash=selected_setup_verifier.EXPECTED[selected_difficulty][1]
            guard=guard.replace(old_guard,'setupHash != '+str(selected_setup_hash)+'u || seed != 104733',1)
            guard=guard.replace(old_root,'(uint?)root["setup_hash"] != 1130726651u',1)
            guard_path.write_text(guard)
    if args.catalog_getter_probe:
        unit_profile.apply_catalog_getter_probe(arguments,output,args.unit_metadata_sha256,args.unit_metadata_domain=="roles",args.current_epoch)
    if unit_candidate is not None and args.unit_metadata_domain=="roles":
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_ROLE_METADATA_DIAGNOSTIC"
    if airport_candidate is not None:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRPORT_SOURCE_DEFINITION"
        arguments.append(str(recipe/"S004AirportSourceDefinition.cs"))
    if args.catalog_binding_probe:
        unit_profile.apply_catalog_binding_probe(arguments,output,runtime_setup,sha(runtime_setup))
    if args.aria_context_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_ARIA_CONTEXT_PROBE"
    if args.dispatcher_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_DISPATCHER_PROBE"
    if args.allow_verified_catalog_rejections:
        unit_profile.apply_catalog_rejection_probe(arguments,output)
    if args.unplaced_airport_production_probe:
        unit_profile.apply_unplaced_airport_probe(arguments,output)
    if args.retained_map_probe:
        unit_profile.apply_retained_map_probe(arguments,output)
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_RETAINED_MAP_PROBE"
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004RetainedMapPublication.cs"))
    if args.semantic_air_opening_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIR_OPENING_SEGMENT"
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004AirOpeningSegment.cs"))
    if args.semantic_aircraft_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRCRAFT_REQUEST_PROBE"
    if args.aircraft_readonly:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRCRAFT_READONLY"
    if args.airself_helipad_readback_probe:
        resolver=json.loads((resolver_dir/"receipt.json").read_text());fixture=json.loads((fixture_dir/"receipt.json").read_text())
        projection=resolver_dir/"SourceAirSelfHelipadResolverProbe.cs"
        if resolver.get("status")!="PassedCompileOnly" or resolver.get("projection_sha256")!=sha(projection) or resolver.get("mission_ready") is not False or fixture.get("status")!="Passed" or fixture.get("original_projection_sha256")!=sha(projection) or "[S004AirSelfHelipadResolverControl] result=Passed cases=2" not in (fixture_dir/"run.log").read_text():
            raise ValueError("Pinned original helipad owner compile and fixture required")
        if args.airself_original_matcher_probe and (resolver.get("original_matcher_bound") is not True or "[S004OriginalBuildingMatcherControl] result=Passed cases=4" not in (fixture_dir/"run.log").read_text()):
            raise ValueError("Pinned original building matcher and fixture required")
        if args.airself_slot_binding_probe and (resolver.get("original_slot_resolver_included") is not True or "[S004OriginalAircraftSlotControl] result=Passed cases=2" not in (fixture_dir/"run.log").read_text()):
            raise ValueError("Pinned original aircraft slot resolver and fixture required")
        for name,digest in resolver["input_sha256"].items():
            if sha(name)!=digest: raise ValueError("Helipad source projection input changed: "+name)
        matches=[i for i,value in enumerate(arguments) if value.endswith('/468_SourceProductionGroundWorld.cs')]
        if len(matches)!=1: raise ValueError("Exact source ground owner required")
        arguments[matches[0]]=str(projection)
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRSELF_HELIPAD_READBACK"
    if args.airself_slot_binding_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRSELF_SLOT_BINDING"
    if args.airself_launch_geometry_probe:
        launch_dir=ROOT/"T Logs/SkirmishProduction/s004-airself-launch-geometry-control-20261105-T1"
        launch=json.loads((launch_dir/"receipt.json").read_text())
        launch_source=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceAirSelfLaunchGeometry.cs"
        if launch.get("status")!="Passed" or launch.get("input_sha256",{}).get(str(launch_source))!=sha(launch_source) or "[S004AirSelfLaunchGeometryControl] result=Passed" not in (launch_dir/"run.log").read_text():
            raise ValueError("Current source launch geometry control required")
        arguments.append(str(launch_source))
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRSELF_LAUNCH_GEOMETRY"
    if args.airself_delivery_probe:
        import audit_s004_airself_authored_transform_chain as authored_chain
        import generate_s004_airself_completion_projection as completion_projection
        import tempfile
        recipe=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
        overlay=ROOT/"T Logs/SkirmishProduction/s004-airself-owner-overlay-20261105-T1"
        report=ROOT/"T Logs/SkirmishProduction/s004-airself-transform-chain-20261105-T2/report.json"
        if json.loads(report.read_text())!=authored_chain.inventory(ROOT): raise ValueError("Current source-derived AirSelf transform inventory required")
        completion=ROOT/"T Logs/SkirmishProduction/s004-airself-completion-control-20261105-T1/SourceAirSelfCompletionProjection.cs"
        with tempfile.TemporaryDirectory(prefix="s004-airself-completion-") as tmp:
            fresh=Path(tmp)/"SourceAirSelfCompletionProjection.cs"
            completion_projection.generate(fresh)
            if fresh.read_bytes()!=completion.read_bytes(): raise ValueError("Current original AirSelf completion projection required")
        proof=json.loads((overlay/"receipt.json").read_text())
        if proof.get("kind")!="S004_AIRSELF_SINGLE_TRANSPORT_SOURCE_DIAGNOSTIC_OVERLAY" or proof.get("mission_ready") is not False: raise ValueError("Exact diagnostic owner overlay required")
        for name,digest in proof["source_sha256"].items():
            if sha(name)!=digest: raise ValueError("AirSelf overlay input changed: "+name)
        for name,digest in proof["outputs_sha256"].items():
            if sha(overlay/name)!=digest: raise ValueError("AirSelf owner overlay changed: "+name)
            matches=[i for i,value in enumerate(arguments) if value.endswith('/'+name)]
            if len(matches)!=1: raise ValueError("Exact original source owner required: "+name)
            arguments[matches[0]]=str(overlay/name)
        arguments.extend(str(p) for p in (ROOT/"T Logs/SkirmishProduction/s004-airself-completion-control-20261105-T1/SourceAirSelfCompletionProjection.cs",*(recipe/name for name in ("SourceS004AirSelfDelivery.cs","SourceS004AirSelfAuthoredPose.cs","SourceAirSelfFlightProjection.cs","SourceAirSelfCompletionOwnerBoundary.cs","SourceS004AirSelfTransactionReadback.cs"))))
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRSELF_DELIVERY"
    if args.original_aria_step_probe:
        aria_dir=ROOT/"T Logs/SkirmishProduction/s004-original-planner-world-overlay-20261105-T2"
        aria=json.loads((aria_dir/"receipt.json").read_text())
        guard_dir=ROOT/"T Logs/SkirmishProduction/s004-original-planner-world-controls-20261105-T1"
        guard=json.loads((guard_dir/"receipt.json").read_text())
        if guard.get("status")!="Passed" or guard.get("overlay_receipt_sha256")!=sha(aria_dir/"receipt.json") or guard.get("run_log_sha256")!=sha(guard_dir/"run.log") or "[S004OriginalAriaStepSourceHostControls] result=Passed cases=4" not in (guard_dir/"run.log").read_text(): raise ValueError("S004 original planner inspector guard controls required")
        if sha(ROOT/"Tools/CI/SkirmishSourceSimulation/Tests/S073OriginalAriaStepSourceHostControls.cs")!=aria["guard_fixture_original_sha256"] or sha(aria["guard_fixture_source"])!=aria["guard_fixture_source_sha256"]: raise ValueError("S004 inspector control fixture source changed")
        for group in ("source_sha256","original_planner_input_sha256","ui_lifecycle_input_sha256"):
            for name,digest in aria[group].items():
                if sha(name)!=digest: raise ValueError("Original ARIA source pin changed: "+name)
        if sha(ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/SourceS073OriginalAriaStepDiagnostic.cs")!=aria["original_inspector_sha256"]: raise ValueError("Original same-world inspector changed")
        arguments.extend(aria["source_files"])
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_ORIGINAL_ARIA_STEP_PROBE"
    if args.s004_logical_context_candidate_probe:
        context_dir=ROOT/"T Logs/SkirmishProduction/s004-logical-context-candidate-20261105-T1"
        context=json.loads((context_dir/"receipt.json").read_text())
        control_dir=ROOT/"T Logs/SkirmishProduction/s004-logical-context-candidate-control-20261105-T1"
        control=json.loads((control_dir/"receipt.json").read_text())
        comparison_dir=ROOT/"T Logs/SkirmishProduction/s004-original-vs-logical-policy-20261105-T2"
        comparison=json.loads((comparison_dir/"receipt.json").read_text())
        if context.get("kind")!="S004LogicalContextSourceCandidate" or context.get("policy_parity_proven") is not False or control.get("status")!="Passed" or control.get("candidate_receipt_sha256")!=sha(context_dir/"receipt.json") or comparison.get("status")!="Collected": raise ValueError("Exact unqualified S004 context controls required")
        if "[S004LogicalContextCandidateControl] result=Passed cases=7" not in (control_dir/"run.log").read_text() or "cases=768 decisionMismatch=0 stateMismatch=0 exceptionMismatch=0" not in (comparison_dir/"run.log").read_text(): raise ValueError("S004 context and bounded policy controls required")
        for group in ("source_sha256","output_sha256"):
            for name,digest in context[group].items():
                if sha(name)!=digest: raise ValueError("S004 context candidate pin changed: "+name)
        for name in context["source_sha256"]:
            matches=[i for i,value in enumerate(arguments) if value==name]
            if len(matches)!=1: raise ValueError("Exact source context candidate input required: "+name)
            arguments[matches[0]]=str(context_dir/Path(name).name)
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_LOGICAL_CONTEXT_CANDIDATE"
    if args.source_logical_aria_probe:
        logical_dir=ROOT/"T Logs/SkirmishProduction"/logical_candidate_name
        logical=json.loads((logical_dir/"receipt.json").read_text())
        original=ROOT/"Tools/CI/SkirmishSourceSimulation/Adapters/492_SourceAriaS002Controller.command-trace.cs"
        replacement=logical_dir/"SourceS004LogicalControllerCandidate.cs"
        if logical.get("kind")!="S004LogicalControllerSourceCandidate" or logical.get("full_original_policy_parity_proven") is not False or logical.get("mission_ready") is not False or logical.get("source_sha256")!=sha(original) or logical.get("output_sha256")!=sha(replacement): raise ValueError("Exact unqualified S004 logical controller candidate required")
        profile_base=ROOT/("T Logs/SkirmishProduction/s004-terminal-guard-controller-20261105-T1/SourceS004LogicalControllerCandidate.cs" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/s004-logical-camera-optional-candidate-20261105-T2/SourceS004LogicalControllerCandidate.cs" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-logical-controller-candidate-20261105-T3/SourceS004LogicalControllerCandidate.cs")
        if args.nov06_source_epoch:
            profile_base=ROOT/"T Logs/SkirmishProduction/s004-logical-controller-air-current-20261106-T1/SourceS004LogicalControllerCandidate.cs"
        detail_base=ROOT/("T Logs/SkirmishProduction/s004-terminal-guard-profile-20261105-T1/SourceS004LogicalControllerCandidate.cs" if args.terminal_handoff_epoch else "T Logs/SkirmishProduction/s004-shared-inspection-profile-20261105-T1/SourceS004LogicalControllerCandidate.cs" if args.shared_focus_epoch else "T Logs/SkirmishProduction/s004-logical-controller-candidate-20261105-T4/SourceS004LogicalControllerCandidate.cs")
        if args.nov06_source_epoch:
            detail_base=ROOT/"T Logs/SkirmishProduction/s004-logical-read-profile-current-20261106-T1/SourceS004LogicalControllerCandidate.cs"
        if args.source_logical_read_profile_probe and (logical.get("read_profile_only") is not True or logical.get("base_candidate_sha256")!=sha(profile_base)):
            raise ValueError("Exact opt-in profiling projection required")
        if args.source_logical_read_detail_probe and (logical.get("read_detail_profile") is not True or logical.get("detail_base_sha256")!=sha(detail_base)):
            raise ValueError("Exact opt-in nested read profiling projection required")
        if args.source_logical_air_service_probe:
            adapter=ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004SelectedAircraftReturn.cs"
            control=ROOT/"T Logs/SkirmishProduction/s004-selected-aircraft-return-control-20261105-T3"
            evidence=json.loads((control/"receipt.json").read_text())
            if logical.get("air_service_logical_boundary") is not True or evidence.get("compile_exit")!=0 or evidence.get("run_exit")!=0 or evidence.get("pass_marker") is not True or evidence.get("source_pins_after",{}).get(str(adapter))!=sha(adapter) or "[S004SelectedAircraftReturnControl] result=Passed cases=6" not in (control/"run.log").read_text(): raise ValueError("Current selected aircraft return source control required")
            arguments.append(str(adapter))
        matches=[i for i,value in enumerate(arguments) if value==str(original)]
        if len(matches)!=1: raise ValueError("Exact original logical controller source required")
        arguments[matches[0]]=str(replacement)
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_LOGICAL_ARIA"
    if args.airself_spawnpoint_publication_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_AIRSELF_SPAWNPOINT_PUBLICATION"
    if args.semantic_supply_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_SUPPLY_SEGMENT"
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004SupplySegment.cs"))
    if args.public_threat_probe:
        import s004_public_threat_projection
        s004_public_threat_projection.apply(arguments, output, ROOT)
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004PublicThreatControls.cs"))
        threat_projection_pins={str(output/name):sha(output/name) for name in ("S004PublicTargetProjection.cs","S004PublicHomeThreatProjection.cs")}
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_PUBLIC_THREAT_PROBE"
    if args.semantic_supply_followup_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_SUPPLY_FOLLOWUP_PROBE"
    if args.enemy_breakdown_probe:
        import s004_enemy_timing_projection
        s004_enemy_timing_projection.apply(arguments,output,ROOT)
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004EnemyTiming.cs"))
        enemy_timing_pins={str(output/("S004EnemyTimingProjection%d.cs"%i)):sha(output/("S004EnemyTimingProjection%d.cs"%i)) for i in (0,1,2)}
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_ENEMY_BREAKDOWN_PROBE"
    if args.cached_setup_guard_probe:
        import s004_setup_guard_cache_projection
        s004_setup_guard_cache_projection.apply(arguments,output,ROOT,
            serialized_candidate=(experiment/"SourceS004SerializedGuardCandidate.cs") if args.serialized_setup_guard_experiment else None)
        arguments.append(str(ROOT/"Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/SourceS004SetupGuardCacheControls.cs"))
        setup_cache_pins={str(output/name):sha(output/name) for name in ("S004CachedSetupGuardProjection.cs","S004BaselineSetupGuardProjection.cs")}
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_CACHED_SETUP_GUARD_PROBE"
    if args.validator_snapshots_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_VALIDATOR_SNAPSHOTS_PROBE"
    if args.production_input_probe:
        define_indices=[i for i,value in enumerate(arguments) if value.startswith("-define:")]
        if len(define_indices)!=1: raise ValueError("One source define set required")
        arguments[define_indices[0]]+=",WARLINE_S004_PRODUCTION_INPUT_PROBE"
    if transform_bridge_requested:
        transform_bridge_profile.apply(arguments,output,executable)
    rsp = output / "compile.rsp"; rsp.write_text("\n".join(json.dumps(value) for value in arguments) + "\n")
    timings["source_generation_seconds"]=time.monotonic()-stage_start; stage_start=time.monotonic()
    compiled = subprocess.run([dotnet, str(csc), "@" + str(rsp)], capture_output=True, text=True, timeout=180)
    (output / "compile.log").write_text(compiled.stdout + compiled.stderr)
    if compiled.returncode != 0: raise RuntimeError("Control compile failed")
    verify()
    if {str(path):sha(path) for path in pinned} != input_sha: raise ValueError("Control inputs changed before worker")
    source_pin_manifest=write_source_pins_before(output,input_sha,pins,arguments,executable)
    timings["compile_seconds"]=time.monotonic()-stage_start; stage_start=time.monotonic()
    runtime = build["runtime_selection"]; env = os.environ.copy()
    env.pop("WARLINE_S004_HISTORICAL_PLACEMENT_INPUT", None)
    env.pop("WARLINE_S004_HISTORICAL_PLACEMENT_INPUT_SHA256", None)
    if historical_placement_input is not None:
        env["WARLINE_S004_HISTORICAL_PLACEMENT_INPUT"] = str(historical_placement_input)
        env["WARLINE_S004_HISTORICAL_PLACEMENT_INPUT_SHA256"] = args.historical_placement_input_sha256
    if args.source_terminal_diagnostic: env["WARLINE_S004_TERMINAL_DIAGNOSTIC"]="1"
    else: env.pop("WARLINE_S004_TERMINAL_DIAGNOSTIC",None)
    if args.original_aria_step_probe: env["WARLINE_S004_ORIGINAL_ARIA_STEP_PROBE"]="1"
    if args.airself_delivery_probe:
        env["WARLINE_S004_AIRSELF_TRANSFORM_REPORT"]=str(report)
        env["WARLINE_S004_AIRSELF_TRANSFORM_REPORT_SHA256"]=sha(report)
    if unit_candidate is not None:
        env["WARLINE_S004_CHECKED_UNIT_METADATA_PATH"]=str(unit_candidate)
        env["WARLINE_S004_CHECKED_UNIT_METADATA_SHA256"]=args.unit_metadata_sha256
    if airport_candidate is not None:
        env["WARLINE_S004_AIRPORT_CANDIDATE_PATH"]=str(airport_candidate)
        env["WARLINE_S004_AIRPORT_CANDIDATE_SHA256"]=args.airport_candidate_sha256
    env.pop("WARLINE_S004_PRODUCTION_DIAGNOSTIC_TICKS",None)
    env.pop("WARLINE_S004_SEMANTIC_RIFLE_PROBE",None)
    if args.semantic_rifle_probe:
        env["WARLINE_S004_SEMANTIC_RIFLE_PROBE"]="1"
    if args.production_ticks:
        env["WARLINE_S004_PRODUCTION_DIAGNOSTIC_TICKS"]=str(args.production_ticks)
    if args.dispatcher_probe:
        env["WARLINE_S004_SOURCE_CLEARANCE_PATH"]=str(clearance)
        env["WARLINE_S004_SOURCE_CLEARANCE_SHA256"]=args.source_clearance_sha256
    if wants_canonical:
        env["WARLINE_S004_CANONICAL_SOURCE_GRAPH"]=str(canonical_graph)
        env["WARLINE_S004_CANONICAL_SOURCE_GRAPH_SHA256"]=args.canonical_graph_sha256
    env["MONO_PATH"] = os.pathsep.join((str(Path(build["placement_build"]).parent), runtime["engine"], str(Path(runtime["newtonsoft"]).parent), str(ROOT / "Library/ScriptAssemblies"), str(output)))
    run_command=[runtime["mono"], "--config", str(MONO_CONFIG), str(executable), str(ROOT), str(BUNDLE), sha(BUNDLE), str(output)]
    if selected_numeric_requested:
        run_command.extend([str(runtime_setup),sha(runtime_setup)])
    try:
        run = subprocess.run(run_command, cwd=ROOT, env=env, capture_output=True, text=True, timeout=args.worker_timeout)
    except subprocess.TimeoutExpired as error:
        def timeout_text(value):
            return value.decode("utf-8", errors="replace") if isinstance(value, bytes) else value or ""
        (output / "run.log").write_text(timeout_text(error.stdout) + timeout_text(error.stderr))
        (output / "worker-timeout.json").write_text(json.dumps({"status": "TimedOut", "timeout_seconds": args.worker_timeout,
            "requested_fixed_ticks": args.production_ticks, "mission_ready": False}, indent=2) + "\n")
        raise
    (output / "run.log").write_text(run.stdout + run.stderr)
    marker_count = sum(line.startswith(MARKER) and line.endswith(" readiness=0") for line in run.stdout.splitlines())
    if run.returncode != 0 or marker_count != 1:
        failure = {"status": "WorkerFailed", "worker_exit_code": run.returncode,
                   "required_marker_count": marker_count, "run_log_sha256": sha(output / "run.log"),
                   "selected_source_difficulty": selected_difficulty if selected_numeric_requested else None,
                   "historical_factory_source_diagnostic": args.historical_factory_source_diagnostic,
                   "historical_unit_metadata_source_diagnostic": args.historical_unit_metadata_source_diagnostic,
                   "selected_difficulty_unit_metadata_parity": False,
                   "selected_difficulty_factory_parity": False,
                   "runtime_admission": False, "mission_ready": False}
        with (output / "worker-failure.json").open("x") as stream:
            json.dump(failure, stream, indent=2); stream.write("\n")
        raise RuntimeError("Import control failed; worker exit=" + str(run.returncode) + ", marker count=" + str(marker_count))
    if args.cached_setup_guard_probe and run.stdout.count("[S004SetupGuardCacheControls] result=Passed cases=8 fixtureOnly=1 missionReady=0")!=1:
        raise RuntimeError("Cached setup guard source controls missing")
    if sha(text_boundary)!=text_hash: raise ValueError("Authored fallback boundary changed")
    if sha(safety)!=safety_hash: raise ValueError("Campaign inactive safety boundary changed")
    timings["worker_seconds"]=time.monotonic()-stage_start; stage_start=time.monotonic()
    loading_check()
    with ThreadPoolExecutor(max_workers=3) as executor:
        tasks=[executor.submit(timed,"prefab_capture_after_seconds",lambda:capture_check("capture-after.json")),executor.submit(timed,"factory_parity_after_seconds",lambda:parity_check("parity-after.json")),executor.submit(timed,"blocker_capture_after_seconds",lambda:blocker_check("blockers-after.json")),executor.submit(timed,"readmodel_after_seconds",readmodel_check),executor.submit(timed,"build_pins_after_seconds",verify),executor.submit(timed,"shared_prefab_after_seconds",shared_prefab_check)]
        results=[task.result() for task in tasks]
    if {str(path): sha(path) for path in pinned} != input_sha: raise ValueError("Control inputs changed")
    if selected_numeric_requested and selected_setup_verifier.verify(selected_difficulty,sha(runtime_setup)) != selected:
        raise ValueError("Selected source readback evidence changed during run")
    write_source_pins_after(output,source_pin_manifest)
    if args.public_threat_probe and {path:sha(Path(path)) for path in threat_projection_pins} != threat_projection_pins:
        raise ValueError("Public threat source projections changed during run")
    if args.enemy_breakdown_probe and {path:sha(Path(path)) for path in enemy_timing_pins} != enemy_timing_pins:
        raise ValueError("Enemy timing source projections changed during run")
    if args.cached_setup_guard_probe and {path:sha(Path(path)) for path in setup_cache_pins} != setup_cache_pins:
        raise ValueError("Cached setup source projections changed during run")
    if unit_candidate is not None:
        unit_verifier.verify(unit_candidate,args.unit_metadata_sha256)
    if airport_candidate is not None:
        airport_verifier.verify(airport_candidate,args.airport_candidate_sha256)
    if transport_requested: transport_profile.verify()
    preflight=json.loads((output / "bundle-preflight.json").read_text())
    expected_preflight="PassedCurrentSourceOnlyBundlePreflight" if args.current_epoch else "PassedSourceOnlyBundlePreflight"
    if preflight.get("status")!=expected_preflight or preflight.get("world_created") is not False or preflight.get("mission_ready") is not False: raise ValueError("Checked in-process bundle preflight required")
    observed = json.loads((output / "requests.json").read_text())
    if observed.get("request_count") != 20 or sum(observed[k] for k in ("succeeded", "failed", "pending")) != 20 or observed.get("mission_ready") is not False: raise ValueError("Invalid observed request results")
    if observed.get("source_launch_owner_invoked") is not launch_requested:raise ValueError("S004 source launch request/result differs")
    road_requested=env.get("WARLINE_S004_SOURCE_ROAD_DIAGNOSTIC")=="1"
    gameplay_requested=env.get("WARLINE_S004_SOURCE_GAMEPLAY_DIAGNOSTIC")=="1"
    gameplay_observed=observed.get("source_gameplay_diagnostic")
    if (gameplay_observed is not None)!=gameplay_requested or (gameplay_requested and not road_requested): raise ValueError("S004 source gameplay request/result differs")
    if gameplay_requested:
        enemy=gameplay_observed.get("enemy_owner") if isinstance(gameplay_observed,dict) else None
        if (gameplay_observed.get("scope")!="SourceHostOnly_ManagedNativeReadinessUnverified" or
            gameplay_observed.get("source_start_transition_activated") is not True or gameplay_observed.get("native_readiness_claimed") is not False or
            gameplay_observed.get("full_scheduler_executed") is not False or gameplay_observed.get("mission_ready") is not False or
            not activation_admitted(observed,args.source_terminal_diagnostic,output,args.production_ticks) or observed.get("retained_world_scheduler_admitted") is not False or
            not isinstance(enemy,dict) or enemy.get("owner_update_invoked") is not True or enemy.get("frame")!=9 or enemy.get("mission_ready") is not False):
            raise ValueError("Invalid S004 source gameplay diagnostic")
        bank=gameplay_observed.get("source_bank_diagnostic")
        if (bank is not None)!=bank_requested:raise ValueError("S004 source bank request/result differs")
        if bank_requested and (not isinstance(bank,dict) or bank.get("same_world_bank_bound") is not True or
            bank.get("before_after_rows_equal") is not True or bank.get("original_setup_compiler_rechecked") is not True or
            type(bank.get("storage_rows")) is not int or bank["storage_rows"]<=0 or bank.get("mission_ready") is not False or
            gameplay_observed.get("canonical_source_bank_bound") is not True):raise ValueError("Invalid S004 source bank diagnostic")
        if launch_requested and (bank.get("session_objective_kind")!="BaseAssault" or bank.get("resolved_setup_component_present") is not True or
            bank.get("source_owner_initialization_requested") is not owner_init_requested or bank.get("source_owner_initialization_completed") is not owner_init_requested):
            raise ValueError("S004 original source launch or owner initialization differs")
        if owner_init_requested and bank.get("source_owner_resource_day_seconds")!=120.0:raise ValueError("Original selected S004 preset day differs")
        if owner_init_requested:
            air=bank.get("source_air_owner_diagnostic")
            if (air is not None)!=air_requested:raise ValueError("S004 source air request/result differs")
            if air_requested and (not isinstance(air,dict) or air.get("scope")!="SingleOriginalAirMovementOwnerUpdate_NotFullScheduler" or
                air.get("frame")!=10 or type(air.get("matching_air_entities_before")) is not int or
                type(air.get("owner_update_invoked")) is not bool or type(air.get("original_transport_capacity_readback")) is not list or
                air.get("transport_capacity_mutated") is not False or air.get("mission_ready") is not False):raise ValueError("Invalid S004 source air owner diagnostic")
            transport=bank.get("source_transport_owner_diagnostic")
            if (transport is not None)!=transport_requested:raise ValueError("S004 source transport request/result differs")
            if transport_requested and (transport.get("scope")!="SingleOriginalDeployOwner_NotFullScheduler" or
                type(transport.get("matching_orders_before")) is not int or type(transport.get("matching_orders_after")) is not int or
                type(transport.get("owner_update_invoked")) is not bool or
                type(transport.get("boarding_targets_before")) is not int or type(transport.get("boarding_targets_after")) is not int or
                type(transport.get("boarding_update_invoked")) is not bool or type(transport.get("deploy_attack_targets_before")) is not int or
                type(transport.get("deploy_attack_update_invoked")) is not bool or transport.get("airdrop_owner_admitted") is not airdrop_requested or
                transport.get("native_input_claimed") is not False or transport.get("mission_ready") is not False):
                raise ValueError("Invalid S004 source transport owner diagnostic")
            if transport_requested:
                drop=transport.get("source_airdrop_diagnostic")
                if (drop is not None)!=airdrop_requested:raise ValueError("S004 source airdrop request/result differs")
                if (drop is None and transport.get("deploy_attack_update_invoked") is not False) or (drop is not None and transport.get("deploy_attack_update_invoked") is not drop.get("deploy_attack_update_invoked")):
                    raise ValueError("S004 deploy-attack owner observation differs")
                if airdrop_requested and not support_requested and (drop.get("scope")!="SourceOnly_NoSupport_AirdropThenDeployAttack_NotFullScheduler" or
                    any(drop.get(key)!=0 for key in ("support_sessions","support_flights","support_request_buffers")) or
                    drop.get("airdrop_update_invoked") is not True or type(drop.get("deploy_attack_update_invoked")) is not bool or
                    drop.get("native_input_claimed") is not False or drop.get("mission_ready") is not False):
                    raise ValueError("Invalid S004 source airdrop owner diagnostic")
                if support_requested and (drop.get("scope")!="OriginalSupportRequestFlightAirdropAttack_SourceSubset_NotFullScheduler" or
                    type(drop.get("support_request_roots_before")) is not int or type(drop.get("support_flights_before")) is not int or
                    type(drop.get("support_request_owner_update_invoked")) is not bool or drop.get("support_flight_owner_update_invoked") is not True or
                    drop.get("airdrop_update_invoked") is not True or type(drop.get("deploy_attack_update_invoked")) is not bool or
                    drop.get("native_input_claimed") is not False or drop.get("mission_ready") is not False):
                    raise ValueError("Invalid S004 original Support owner diagnostic")
                tick=bank.get("source_experimental_tick")
                if (tick is not None)!=tick_requested:raise ValueError("S004 experimental tick request/result differs")
                if tick_requested and (tick.get("scope")!="ExperimentalSourceOwnerSubset_NotFullScheduler" or
                    tick.get("requested_ticks")!=tick_count or type(tick.get("completed_ticks")) is not int or
                    not 1<=tick.get("completed_ticks")<=tick_count or tick.get("tick")!=10+tick.get("completed_ticks") or
                    not isinstance(tick.get("source_steps"),list) or len(tick.get("source_steps"))!=tick.get("completed_ticks") or
                    any(step.get("tick")!=11+i or type(step.get("terminal")) is not bool or
                        any(type(step.get(key)) is not int or not 0<=step[key]<=54 for key in
                            ("existing_opening_actors","actors_changed_grid_cell","actors_with_path_request","actors_with_unit_target","actors_with_engage_target"))
                        for i,step in enumerate(tick.get("source_steps"))) or
                    (tick.get("completed_ticks")<tick_count and not (tick["source_steps"][-1].get("terminal") or tick["source_steps"][-1].get("phase") in ("Finished","Failed"))) or
                    tick.get("observed_opening_actor_count")!=54 or
                    not isinstance(tick.get("session_phase_after"),str) or not isinstance(tick.get("objective_outcome_after"),str) or
                    tick.get("native_input_claimed") is not False or tick.get("mission_ready") is not False):
                    raise ValueError("Invalid S004 experimental source tick")
                if tick_requested:
                    missile=tick.get("source_missile_owners")
                    if (missile is not None)!=missile_requested:raise ValueError("S004 missile source request/result differs")
                    if missile_requested and any(type(missile.get(key)) is not int or missile[key]<0 for key in
                        ("fire_updates","visual_updates","flying_visual_updates","trail_updates","flight_updates","audio_updates","impact_updates","projectiles_remaining","impact_requests_remaining")):
                        raise ValueError("Invalid S004 missile owner observations")
                    scan_rope=tick.get("source_scan_rope_owners")
                    if (scan_rope is not None)!=scan_rope_requested:raise ValueError("S004 scan and rope source request/result differs")
                    if scan_rope_requested and any(type(scan_rope.get(key)) is not int or scan_rope[key]<0 for key in
                        ("scan_execute_updates","scan_command_updates","rope_disembark_updates","rope_disperse_updates","rope_drop_updates","boarding_log_updates")):
                        raise ValueError("Invalid S004 scan and rope owner observations")
        if not bank_requested and gameplay_observed.get("canonical_source_bank_bound") is not False:raise ValueError("Unexpected S004 source bank binding")
    elif observed.get("source_simulation_active")!=0: raise ValueError("Unexpected S004 source activation")
    road_observed=observed.get("source_initial_road_diagnostic")
    if (road_observed is not None)!=road_requested: raise ValueError("S004 road diagnostic request/result differs")
    road_receipts=None
    if road_requested:
        before=output/"initial-road-sync.json";after=output/"initial-road-after-run.json"
        before_data=json.loads(before.read_text());after_data=json.loads(after.read_text())
        if (before_data!=road_observed or after_data!=before_data or before_data.get("status")!="PassedFreshEmptyProjection" or
            before_data.get("actual_original_sync_executed") is not True or before_data.get("tiles")!=0 or before_data.get("strokes")!=0):
            raise ValueError("Original S004 source road sync or post-check differs")
        road_receipts={"sync_sha256":sha(before),"after_run_sha256":sha(after),"scope":"SourceOnly_EmptyDynamicRoadNetwork_BaseSurfaceSeparate"}
    timings["post_validation_seconds"]=time.monotonic()-stage_start; timings["total_before_receipt_seconds"]=time.monotonic()-total_start
    receipt = {"status": "ObservedDiagnosticSourcePlacement", "invocation_arguments": list(sys.argv[1:]), "timings_seconds": timings, "validation_workers": 3, "input_sha256": input_sha, "build_pins_rechecked": len(pins),
               "bundle_drives_startup_arguments": True, "in_process_bundle_preflight_sha256": sha(output / "bundle-preflight.json"), "result": observed, "target_feedback_text_boundary_sha256": text_hash, "target_feedback_text_scope": "Authored fallback only; localization unverified", "attack_intents_preflight_rejected": False, "campaign_inactive_safety_boundary_sha256": safety_hash, "enemy_source_sha256": sha(enemy_source), "enemy_projection_sha256": sha(enemy_derived), "enemy_projection_change": "Only source-profile guard replaced by exact S004 setup check", "outer_projection_sha256": sha(outer_source), "outer_boundary_sha256": sha(boundary), "source_view_profile_assumed_absent": True, "enemy_owner_updates_requested": 8, "world_movement_owner_updates_requested": 8, "path_owner_updates_requested": 8, "inactive_combat_owner_subset_requested": True, "source_start_transition_is_native_input": False, "scheduler_executed": False, "checkpoint_and_logical_binding_unsupported": True, "full_ledger_projection_sha256": sha(full_probe), "cleanup_projection_sha256": sha(cleanup), "original_prefab_lookup_sha256": sha(lookup), "source_registry_is_native_capture": False, "original_ledger_owner_sha256": sha(owner), "extracted_ledger_tail_sha256": sha(ledger), "ledger_scope": "Projected outer TrySpawn invoked and diagnostic session transitioned; source-host view profile explicitly assumed absent", "original_actor_projection_sha256": sha(original), "derived_actor_projection_sha256": sha(derived), "actor_projection_change": "Only profile guard replaced with exact pinned S004 setup and force checks", "requests_sha256": sha(output / "requests.json"), "physical_source_validated": False,
               "source_road_diagnostic":road_receipts,"source_gameplay_diagnostic_requested":gameplay_requested,"source_bank_diagnostic_requested":bank_requested,"source_launch_diagnostic_requested":launch_requested,"source_owner_initialization_requested":owner_init_requested,"source_air_owner_diagnostic_requested":air_requested,"source_transport_owner_diagnostic_requested":transport_requested,"source_airdrop_owner_diagnostic_requested":airdrop_requested,"source_support_owner_diagnostic_requested":support_requested,"source_experimental_tick_requested":tick_requested,"source_missile_tick_requested":missile_requested,"source_scan_rope_tick_requested":scan_rope_requested,"runtime_admission": False, "native_visual_readiness": False, "mission_ready": False,
               "compile_log_sha256": sha(output / "compile.log"), "run_log_sha256": sha(output / "run.log")}
    if args.historical_unit_metadata_source_diagnostic:
        receipt["unit_metadata_capture_scope"]="RegularCandidate_ExplicitHistoricalSourceDiagnostic_SelectedDifficultyMetadataParityUnproven"
        receipt["selected_difficulty_unit_metadata_parity"]=False
    if args.historical_factory_source_diagnostic:
        receipt["factory_capture_scope"]="RegularCapture_ExplicitHistoricalSourceDiagnostic_SelectedDifficultyFactoryParityUnproven"
        receipt["selected_source_difficulty"]=selected_difficulty
        receipt["selected_difficulty_factory_parity"]=False
    if args.airself_delivery_probe:
        receipt["airself_delivery_scope"]="SingleTransport_SourceDiagnostic_NoNativeOrRenderedParity"
        receipt["airself_mixed_transport_concurrency_supported"]=False
        receipt["airself_pose_scope"]="SourcePinnedRootAndRotors_RendererBoundsAndWriterParityUnverified"
    if args.source_logical_aria_probe:
        receipt["source_logical_aria_scope"]="S004CandidateSharedPolicy_BoundedDifferential_NotNativeInputOrFullParity"
    if args.source_single_scope_query_experiment:
        receipt["source_single_scope_query_scope"]="ExperimentalSourceHostOnly_NotFullBehaviorParityOrNativeReadiness"
    if args.source_fuel_drift_readback_probe:
        receipt["source_fuel_drift_readback_sha256"]=sha(output/"source-fuel-drift.jsonl")
        receipt["source_fuel_drift_scope"]="PassiveSourceStateAfterOwnerAndControllerTick_NotNativeParity"
        receipt["source_fuel_drift_window"]=[args.source_fuel_drift_start,args.source_fuel_drift_end] if custom_fuel_window else [800,910]
    if args.source_terminal_diagnostic:
        receipt["source_terminal_diagnostic_scope"]="ActualSourceOwnerTerminalIfObserved_ControllerFailureRetained_NotNativeInputOrReadiness"
    if args.public_threat_probe:
        receipt["public_threat_source_projection_sha256"]=threat_projection_pins
        receipt["public_threat_scope"]="SourceFogVisibleContactsOnly_NativePresentationParityUnverified"
    if args.enemy_breakdown_probe:
        receipt["enemy_timing_source_projection_sha256"]=enemy_timing_pins
        receipt["enemy_timing_scope"]="InstrumentedNestedSourceCalls_NoFullSchedulerOrSpeedupClaim"
    if args.cached_setup_guard_probe:
        receipt["setup_cache_projection_sha256"]=setup_cache_pins
        receipt["setup_cache_scope"]="ExperimentalSourceOnly_PerCallFileHashAndFullLiveComparison"
        receipt["serialized_setup_guard_experiment"]=bool(args.serialized_setup_guard_experiment)
        if args.serialized_setup_guard_experiment:
            receipt["setup_cache_scope"]="ExperimentalSourceOnly_PerCallFileHashAndSerializedFullComparison_NotQualified"
    if args.validator_snapshots_probe:
        receipt["validator_snapshots_sha256"]=sha(output/"source-validator-snapshots.jsonl")
        receipt["validator_snapshots_scope"]="OriginalSourceValidator_SelectedActorSessionAndReservationFields_NotFullWorld"
    if args.airself_helipad_readback_probe:
        receipt["airself_helipad_readback_sha256"]=sha(output/"airself-helipad-owner-readback.json")
        receipt["airself_helipad_readback_scope"]="ActualSourceOwnerProbe_CopiedRng_NoQueueOrDelivery"
    if args.placement_state_readback:
        state_path=output/"placement-state.json";state=json.loads(state_path.read_text())
        if state.get("kind")!="S004SourcePlacementStateReadback" or len(state.get("registry",[]))!=20 or state.get("occupancy_owner_update_asserted") is not False or state.get("native_parity") is not False or state.get("mission_ready") is not False:
            raise ValueError("Exact source-only placement-state readback required")
        receipt["placement_state"]={"path":str(state_path),"sha256":sha(state_path),"scope":state["scope"]}
    (output / "receipt.json").write_text(json.dumps(receipt, indent=2) + "\n"); print(json.dumps(receipt))


if __name__ == "__main__": main()
