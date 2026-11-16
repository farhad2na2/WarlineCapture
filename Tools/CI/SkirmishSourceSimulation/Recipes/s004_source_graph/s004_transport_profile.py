"""Opt-in S004 source-only transport closure; never an admission claim."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
S026 = ROOT / "T Logs/SkirmishProduction/s026-single-query-options-build-20261104-T2"
BUILD_SHA = "a4a1678dd2482a7208c00fda591493e5861284e0ed658302086730cadcbf69e8"

def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def verify():
    build_path = S026 / "build.json"
    if sha(build_path) != BUILD_SHA:
        raise ValueError("Exact source transport build receipt required")
    build = json.loads(build_path.read_text())
    if build.get("status") != "Passed" or build.get("compile_exit") != 0:
        raise ValueError("Passed source transport build required")
    for path, digest in build["source_and_reference_pins"].items():
        if sha(path) != digest:
            raise ValueError("Transport source build pin changed: " + path)
    return build

def apply(arguments, output):
    build = verify()
    pins = build["source_and_reference_pins"]
    generated = S026 / "source/generated"
    def pinned(path):
        path = Path(path)
        if pins.get(str(path)) != sha(path):
            raise ValueError("Exact transport source pin required: " + str(path))
        return str(path)
    def replace_suffix(suffix, replacement):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError("Exact transport projection required: " + suffix)
        arguments[matches[0]] = replacement
    replace_suffix("/141_SelectedMoveCachePredicate.cs", pinned(generated / "141_SelectedMoveCachePredicate.cs"))
    replace_suffix("/388_TransportBoardingCommandSystem.ground-guard.cs", pinned(generated / "388_TransportBoardingCommandSystem.ground-guard.cs"))
    replace_suffix("/generic-SkirmishEnemyCapabilityCommands.cs", pinned(generated / "s026/530-generic-SkirmishEnemyCapabilityCommands.cs"))
    replace_suffix("/147_ShippingDiagnosticBindings.cs", pinned(ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/142-147_ShippingDiagnosticBindings.cs"))
    # Retain the current AI job admission and add only the two transport jobs
    # already admitted by the pinned S026 source host.
    host = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/120_OwnedEntityJobs.combined.cs"
    transport_host = Path(pinned(ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/116-120_OwnedEntityJobs.combined.cs"))
    marker = 'var admitted=new[]{'
    entries = '"Game.Runtime.UnitTransportBoardingSystem+CollectBoardingDecisionsJob","Game.Runtime.UnitTransportDeployAttackSystem+ApplyDeployAttackJob",'
    current, prior = host.read_text(), transport_host.read_text()
    if current.count(marker) != 1 or prior.count(marker + entries) != 1 or entries in current:
        raise ValueError("Exact source job admission projections required")
    host_projection = output / "S004TransportOwnedEntityJobs.cs"
    host_projection.write_text(current.replace(marker, marker + entries))
    replace_suffix("/120_OwnedEntityJobs.combined.cs", str(host_projection))
    ecs = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/059_AutonomousEcs.cs"
    old = "[AttributeUsage(AttributeTargets.Struct)]public sealed class WithNoneAttribute"
    new = "[AttributeUsage(AttributeTargets.Struct,AllowMultiple=true)]public sealed class WithNoneAttribute"
    ecs_text = ecs.read_text()
    if ecs_text.count(old) != 1: raise ValueError("Exact source WithNone attribute required")
    ecs_projection = output / "S004TransportAutonomousEcs.cs"
    ecs_projection.write_text(ecs_text.replace(old, new))
    replace_suffix("/059_AutonomousEcs.cs", str(ecs_projection))
    boundary = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S073/SourceS073TacticalAIEngineBoundary.cs"
    source = boundary.read_text(); start = source.index("    public struct FixedList128Bytes<T>")
    end = source.index("\n}\n\nnamespace Unity.Entities", start)
    stripped = output / "SourceS073TacticalAIEngineBoundary.without-fixedlist.cs"
    stripped.write_text(source[:start] + source[end:])
    replace_suffix("/SourceS073TacticalAIEngineBoundary.cs", str(stripped))
    header = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/TransportFiniteEngineHeaders.cs"
    original = Path(pinned(header)).read_text().splitlines(keepends=True)
    hits = [line for line in original if line.startswith("namespace Unity.Collections{public struct FixedList128Bytes<T>")]
    if len(hits) != 1: raise ValueError("Exact S026 fixed-list boundary required")
    stripped_header = output / "TransportHeadersWithoutFixedList.cs"
    stripped_header.write_text("".join(line for line in original if line != hits[0]))
    arguments.append(str(stripped_header))
    names = ("TransportBoardingApproachSystemHelper", "TransportBoardingCapacitySystemHelper",
        "TransportBoardingCommandAirdropPlanning", "TransportBoardingCommandApproach",
        "TransportBoardingCommandBoardPassenger", "TransportBoardingCommandBoardTransport",
        "TransportBoardingCommandCandidates", "TransportBoardingCommandDisembarkApplication",
        "TransportBoardingCommandDisembarkPlanning", "TransportBoardingCommandOwned",
        "TransportBoardingCommandPlanning", "TransportBoardingCommandResults", "TransportBoardingCommandSystem",
        "TransportBoardingDiagnosticLogComponents", "TransportBoardingDiagnosticLogFlushSystem",
        "TransportBoardingDiagnosticSystemHelper", "TransportBoardingOrderPlanningSystemHelper",
        "UnitTransportAirPickupSystem", "UnitTransportBoardingCapacityRules", "UnitTransportBoardingDiagnosticSystem",
        "UnitTransportBoardingSystem", "UnitTransportDeployOrderSystem", "UnitTransportDeployAttackSystem", "UnitTransportPassengerStateSystem",
        "UnitTransportVisualUtility", "SkirmishConvoyObjectiveTruckComponent", "ScanIntelCommandSystem",
        "ScanSpecialistReadModel", "SkirmishPublicScanFeedback", "SkirmishPublicScanReceipt",
        "SkirmishPublicAircraftReturnFeedback", "SkirmishPublicAircraftReturnReceipt")
    arguments.extend(pinned(generated / ("s026/transport-" + name + ".cs")) for name in names)
    recipe = ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph"
    arguments.extend(str(recipe / name) for name in ("SourceS004FixedList128Bytes.cs", "S004TransportOwnerDiagnostic.cs"))
