"""Opt-in original scan and rope owner additions to an experimental tick."""
from pathlib import Path
import hashlib
import s004_transport_profile as transport

ROOT = transport.ROOT

def apply(arguments, output):
    pins = transport.verify()["source_and_reference_pins"]
    generated = transport.S026 / "source/generated/s026"
    for name in ("transport-UnitTransportRopeDisembarkSystem.cs", "transport-UnitScanOrderExecutionSystem.cs"):
        path = generated / name
        if pins.get(str(path)) != hashlib.sha256(path.read_bytes()).hexdigest(): raise ValueError("Exact scan/rope source pin required")
        arguments.append(str(path))
    prior = ROOT / "Tools/CI/SkirmishSourceSimulation/Adapters/S026/116-120_OwnedEntityJobs.combined.cs"
    if pins.get(str(prior)) != hashlib.sha256(prior.read_bytes()).hexdigest() or '"Game.Runtime.UnitScanOrderExecutionSystem+ExecuteScanOrdersJob"' not in prior.read_text():
        raise ValueError("Exact scan job admission required")
    matches = [i for i, value in enumerate(arguments) if value.endswith("/S004AirdropOwnedEntityJobs.cs")]
    if len(matches) != 1: raise ValueError("Exact source job projection required")
    text = Path(arguments[matches[0]]).read_text(); marker = "var admitted=new[]{"
    if text.count(marker) != 1: raise ValueError("Exact source job admission seam required")
    target = output / "S004ScanOwnedEntityJobs.cs"
    target.write_text(text.replace(marker, marker + '"Game.Runtime.UnitScanOrderExecutionSystem+ExecuteScanOrdersJob",'))
    arguments[matches[0]] = str(target)
    arguments.append(str(ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004ScanRopeTickOwners.cs"))

def apply_tick(arguments, output):
    def amend(suffix, filename, edits):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError("Exact scan/rope tick projection required: " + suffix)
        source = Path(arguments[matches[0]]).read_text()
        for old, new in edits:
            if source.count(old) != 1: raise ValueError("Exact scan/rope tick seam changed: " + old)
            source = source.replace(old, new)
        target = output / filename; target.write_text(source); arguments[matches[0]] = str(target)
    amend("/S004MissileSourceOwnerOrder.cs", "S004ScanRopeSourceOwnerOrder.cs", [
        ('"SkirmishRosterProjectionSystem","RuntimeGridDeduplicationSystem"', '"SkirmishRosterProjectionSystem","UnitScanOrderExecutionSystem","UnitTransportRopeDisembarkSystem","RuntimeGridDeduplicationSystem"'),
        ('"StaticGridBlockerUpdateSystem","MaterialFabricationSystem"', '"StaticGridBlockerUpdateSystem","ScanIntelCommandSystem","MaterialFabricationSystem"'),
        ('"UnitAttackOrderRequestSystem","EngageTargetValidateSystem"', '"UnitAttackOrderRequestSystem","UnitTransportRopeDisperseSystem","EngageTargetValidateSystem"'),
        ('"UnitTransportBoardingSystem","VehicleFuelConsumptionSystem"', '"UnitTransportBoardingSystem","TransportBoardingDiagnosticLogFlushSystem","VehicleFuelConsumptionSystem"'),
        ('"SkirmishEnemyStrategySystem","SkirmishOutcomeSystem"', '"SkirmishEnemyStrategySystem","UnitTransportRopeDropSystem","SkirmishOutcomeSystem"'),
        ('{"blockers",new[]{"RuntimeGridDeduplicationSystem"', '{"s004_scan_execute",new[]{"UnitScanOrderExecutionSystem"}},\n {"s004_rope_disembark",new[]{"UnitTransportRopeDisembarkSystem"}},\n {"blockers",new[]{"RuntimeGridDeduplicationSystem"'),
        ('{"fabrication",new[]{"MaterialFabricationSystem"}},', '{"s004_scan_command",new[]{"ScanIntelCommandSystem"}},\n {"fabrication",new[]{"MaterialFabricationSystem"}},'),
        ('{"validate",new[]{"EngageTargetValidateSystem"}},', '{"s004_rope_disperse",new[]{"UnitTransportRopeDisperseSystem"}},\n {"validate",new[]{"EngageTargetValidateSystem"}},'),
        ('{"fuel",new[]{"VehicleFuelConsumptionSystem"}},', '{"s004_boarding_log",new[]{"TransportBoardingDiagnosticLogFlushSystem"}},\n {"fuel",new[]{"VehicleFuelConsumptionSystem"}},'),
        ('{"objective_outcome",new[]{"SkirmishOutcomeSystem"}}', '{"s004_rope_drop",new[]{"UnitTransportRopeDropSystem"}},\n {"objective_outcome",new[]{"SkirmishOutcomeSystem"}}'),
    ])
    amend("/S004MissileOwnerStep.cs", "S004ScanRopeOwnerStep.cs", [
        ('Phase("roster",AdvanceActualRosterOwner);Phase("blockers",', 'Phase("roster",AdvanceActualRosterOwner);Phase("s004_scan_execute",()=>s004ScanRopeTickOwners.ScanExecute());Phase("s004_rope_disembark",()=>s004ScanRopeTickOwners.RopeDisembark());Phase("blockers",'),
        ('Phase("blockers",UpdateSourceBlockers);', 'Phase("blockers",UpdateSourceBlockers);Phase("s004_scan_command",()=>s004ScanRopeTickOwners.ScanCommand());'),
        ('Phase("s004_attack_requests",()=>s004SupplementalTickOwners.AttackRequests());Phase("validate",', 'Phase("s004_attack_requests",()=>s004SupplementalTickOwners.AttackRequests());Phase("s004_rope_disperse",()=>s004ScanRopeTickOwners.RopeDisperse());Phase("validate",'),
        ('Phase("s004_board",()=>s004SupplementalTickOwners.Boarding());Phase("fuel",', 'Phase("s004_board",()=>s004SupplementalTickOwners.Boarding());Phase("s004_boarding_log",()=>s004ScanRopeTickOwners.BoardingLog());Phase("fuel",'),
        ('Phase("objective_outcome",()=>fogObjectives.AfterEnemyOutcomeAndPublic(Tick));', 'Phase("s004_rope_drop",()=>s004ScanRopeTickOwners.RopeDrop());Phase("objective_outcome",()=>fogObjectives.AfterEnemyOutcomeAndPublic(Tick));'),
    ])
    matches = [i for i, value in enumerate(arguments) if value.startswith("-define:")]
    if len(matches) != 1: raise ValueError("One source define set required")
    arguments[matches[0]] += ",WARLINE_S004_SCAN_ROPE_TICK"
