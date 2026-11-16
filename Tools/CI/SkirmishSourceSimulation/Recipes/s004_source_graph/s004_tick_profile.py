"""Opt-in experimental source tick; admission covers only the named subset."""
from pathlib import Path
import s004_transport_profile as transport

ROOT = transport.ROOT

def apply(arguments, output):
    build = transport.verify()
    metadata = transport.S026 / "source/generated/533_SourceOwnerOrderMetadata.cs"
    if build["source_and_reference_pins"].get(str(metadata)) != transport.sha(metadata):
        raise ValueError("Exact expanded ordering metadata pin required")
    metadata_matches = [i for i, value in enumerate(arguments) if value.endswith("/533_SourceOwnerOrderMetadata.cs")]
    if len(metadata_matches) != 1: raise ValueError("One source ordering metadata projection required")
    arguments[metadata_matches[0]] = str(metadata)
    def project(suffix, filename, edits):
        matches = [i for i, value in enumerate(arguments) if value.endswith(suffix)]
        if len(matches) != 1: raise ValueError("Exact tick source required: " + suffix)
        source = Path(arguments[matches[0]]).read_text()
        for original, replacement in edits:
            if source.count(original) != 1: raise ValueError("Exact tick source seam changed: " + original)
            source = source.replace(original, replacement)
        target = output / filename; target.write_text(source); arguments[matches[0]] = str(target)
    project("/534_SourceOwnerOrderAdmission.cs", "S004ExperimentalSourceOwnerOrder.cs", [
        ('"SkirmishSharedFogUpdateSystem","EngageTargetValidateSystem"', '"SkirmishSharedFogUpdateSystem","UnitAttackOrderRequestSystem","EngageTargetValidateSystem"'),
        ('"SkirmishResearchSystem","UnitDeathSystem"', '"SkirmishResearchSystem","SkirmishWorldMovementSystem","UnitDeathSystem"'),
        ('"UnitManualMoveRetrySystem","UnitPathfindingSystem"', '"UnitManualMoveRetrySystem","UnitAirMovementSystem","UnitPathfindingSystem"'),
        ('"UnitGridMovementSystem","VehicleFuelConsumptionSystem","UnitAttackSystem"', '"UnitGridMovementSystem","UnitTransportDeployOrderSystem","UnitTransportBoardingSystem","VehicleFuelConsumptionSystem","SupportAbilityRequestSystem","SupportFlightSystem","SupportSmokeSystem","UnitTransportAirdropSystem","UnitTransportDeployAttackSystem","UnitAttackSystem"'),
        ('{"path",new[]{"UnitPathfindingSystem"}},', '{"s004_air",new[]{"UnitAirMovementSystem"}},\n {"path",new[]{"UnitPathfindingSystem"}},'),
        ('{"validate",new[]{"EngageTargetValidateSystem"}},', '{"s004_attack_requests",new[]{"UnitAttackOrderRequestSystem"}},\n {"validate",new[]{"EngageTargetValidateSystem"}},'),
        ('{"death",new[]{"UnitDeathSystem"}},', '{"s004_expanded_movement",new[]{"SkirmishWorldMovementSystem"}},\n {"death",new[]{"UnitDeathSystem"}},'),
        ('{"fuel",new[]{"VehicleFuelConsumptionSystem"}},', '{"s004_deploy",new[]{"UnitTransportDeployOrderSystem"}},\n {"s004_board",new[]{"UnitTransportBoardingSystem"}},\n {"fuel",new[]{"VehicleFuelConsumptionSystem"}},\n {"s004_support_request",new[]{"SupportAbilityRequestSystem"}},\n {"s004_support_flight",new[]{"SupportFlightSystem"}},\n {"s004_smoke",new[]{"SupportSmokeSystem"}},\n {"s004_airdrop",new[]{"UnitTransportAirdropSystem"}},\n {"s004_deploy_attack",new[]{"UnitTransportDeployAttackSystem"}},'),
    ])
    project("/312_PaidSourceOwnerStep.retry.cs", "S004ExperimentalOwnerStep.cs", [
        ('Phase("fog",()=>fogObjectives.BeforeEngagementFog(Tick));Phase("validate",', 'Phase("fog",()=>fogObjectives.BeforeEngagementFog(Tick));Phase("s004_attack_requests",()=>s004SupplementalTickOwners.AttackRequests());Phase("validate",'),
        ('Phase("research",AdvanceActualResearch);Phase("death",', 'Phase("research",AdvanceActualResearch);Phase("s004_expanded_movement",()=>s004SupplementalTickOwners.ExpandedMovement());Phase("death",'),
        ('Phase("manual_retry",AdvanceActualManualMoveRetry);Phase("path",', 'Phase("manual_retry",AdvanceActualManualMoveRetry);Phase("s004_air",()=>s004SupplementalTickOwners.Air());Phase("path",'),
        ('Phase("movement",()=>ground.MovementPhase());Phase("fuel",', 'Phase("movement",()=>ground.MovementPhase());Phase("s004_deploy",()=>s004SupplementalTickOwners.Deploy());Phase("s004_board",()=>s004SupplementalTickOwners.Boarding());Phase("fuel",'),
        ('fuelState.End();});Phase("attack",', 'fuelState.End();});Phase("s004_support_request",()=>s004SupplementalTickOwners.SupportRequest());Phase("s004_support_flight",()=>s004SupplementalTickOwners.SupportFlight());Phase("s004_smoke",()=>s004SupplementalTickOwners.Smoke());Phase("s004_airdrop",()=>s004SupplementalTickOwners.Airdrop());Phase("s004_deploy_attack",()=>s004SupplementalTickOwners.DeployAttack());Phase("attack",'),
    ])
    defines = [i for i, value in enumerate(arguments) if value.startswith("-define:")]
    if len(defines) != 1: raise ValueError("One source define set required")
    arguments[defines[0]] += ",WARLINE_S004_EXPERIMENT_TICK"
    arguments.append(str(ROOT / "Tools/CI/SkirmishSourceSimulation/Recipes/s004_source_graph/S004SupplementalTickOwners.cs"))
