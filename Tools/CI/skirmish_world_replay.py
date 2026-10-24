"""Replay shared production bounded world scheduling and targeting without Unity or downloads."""
from __future__ import annotations
import argparse
import json
import os
import stat
from xml.sax.saxutils import escape
from pathlib import Path
import re
import shutil
import sys
sys.dont_write_bytecode = True
import skirmish_offline_sim as common

SCHEDULING = "Assets/Game/Scripts/Systems/WorldSchedulingKernel.cs"
TARGETING = "Assets/Game/Scripts/Systems/TargetAcquisitionKernel.cs"
SOURCES = common.SOURCES + [SCHEDULING, TARGETING,
    "Assets/Game/Scripts/Systems/Combat/StandardAttackPlanner.cs",
    "Assets/Game/Scripts/Systems/Combat/DirectDamageSequencer.cs",
    "Assets/Game/Scripts/Systems/SkirmishSquadTargetingKernel.cs",
    "Tools/CI/SkirmishOfflineSim/SquadReplay.cs",
    "Tools/CI/SkirmishOfflineSim/WorldFrameReplay.cs", "Tools/CI/SkirmishOfflineSim/WorldAcquisition.cs", "Tools/CI/SkirmishOfflineSim/WorldSchedule.cs",
    "Tools/CI/SkirmishOfflineSim/WorldProgram.cs", "Tools/CI/SkirmishOfflineSim/WorldJson.cs"]
SOURCES += ["Tools/CI/SkirmishOfflineSim/MovingWorldMap.cs", "Tools/CI/SkirmishOfflineSim/MovingWorldReplay.cs",
    "Assets/Game/Scripts/Components/GridCoordinateKernel.cs",
    "Assets/Game/Scripts/Systems/GridOccupancyKernel.cs", "Assets/Game/Scripts/Systems/GridPathMovementKernel.cs",
    "Assets/Game/Scripts/Systems/InfantryPathLifecycleKernel.cs", "Assets/Game/Scripts/Systems/EngagedMovementKernel.cs",
    "Assets/Game/Scripts/Systems/EngageTargetLifecycleKernel.cs", "Assets/Game/Scripts/Systems/UnitDeathLifecycleKernel.cs",
    "Assets/Game/Scripts/Systems/Pathfinding/PathPlacementKernel.cs", "Assets/Game/Scripts/Systems/Pathfinding/GridPathSearchKernel.cs",
    "Assets/Game/Scripts/Systems/Pathfinding/GridPathRoutingKernel.cs", "Assets/Game/Scripts/Systems/Pathfinding/PathJobSchedulingKernel.cs", "Assets/Game/Scripts/Systems/Pathfinding/PathGoalPolicyKernel.cs",
    "Assets/Game/Scripts/Systems/Pathfinding/PathRequestBudgetKernel.cs", "Assets/Game/Scripts/Systems/Pathfinding/PathRequestAdmissionKernel.cs", "Assets/Game/Scripts/Systems/Pathfinding/PathRetryRestorationKernel.cs", "Assets/Game/Scripts/Systems/Pathfinding/PathGoalAssignmentKernel.cs"]
SOURCES += ["Tools/CI/SkirmishOfflineSim/StartupFullMapImport.cs"]
ADAPTERS = ["Assets/Game/Scripts/Systems/" + name + ".cs" for name in (
    "UnitEngagementSystem", "UnitAttackSystem", "UnitEngagedMovementSystem", "UnitGridMovementSystem", "SkirmishCombatPolicy", "AITargetingSystem", "DynamicOccupancyRebuildSystem", "CombatTargetPolicyUtility", "UnitDeathSystem")]
ADAPTERS += ["Assets/Game/Scripts/Runtime/Skirmish/" + name + ".cs" for name in (
    "SkirmishObjectiveFactProjectionSystem", "SkirmishBaseAssaultObjectiveSystem", "SkirmishOutcomeSystem")]
ADAPTERS += ["Assets/Game/Scripts/Systems/UnitAttackSystem.CampaignMissionSuppression.cs"]
ADAPTERS += ["Assets/Game/Scripts/Components/CombatTargetPolicyComponents.cs", "Assets/Game/Scripts/Components/FactionIdentity.cs",
    "Assets/Game/Scripts/Runtime/Skirmish/SkirmishFogConsumerUtility.cs"]
ADAPTERS += ["Assets/Game/Scripts/Systems/"+name+".cs" for name in (
    "EngageTargetValidateSystem", "EngageTargetSyncSystem", "UnitGridMovementPolicy", "UnitPathfindingSystem", "UnitPathfindingScheduler",
    "UnitPathfindingApply", "UnitPathResultApply", "UnitPathGoalAssignment", "UnitPathSegmentation", "UnitPathRequestCollection",
    "UnitPathfindingEntitySets", "UnitPathSurfaceMetadata", "UnitPathRetry", "UnitPathfindingBudget", "UnitManualMoveRetrySystem", "UnitPathReservedGoal")]
ADAPTERS += ["Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs", "Assets/Game/Scripts/Components/GridComponents.cs"]
PINS = list(dict.fromkeys(SOURCES + ADAPTERS + ["Tools/CI/skirmish_offline_sim.py", "Tools/CI/skirmish_world_replay.py"]))
SCOPE = "ProductionBoundedWorldReplay"
BINS = {"bin/WorldReplay" + suffix for suffix in (".dll", ".deps.json", ".runtimeconfig.json", ".pdb")} | {"profile.json"}


BODY_SOURCES = ["Tools/CI/SkirmishOfflineSim/" + name + ".cs" for name in (
    "BodyTransitionData", "CapturedPhysicalFuel", "ProductionGroundFuelKernel", "CapturedVehicleSteering", "CapturedBodyTransition", "BodyTransitionSourcePins", "RuntimeMathBootstrap")]
BODY_SOURCES += ["Assets/Game/Scripts/Components/UnitVehicleMovementUtility.cs"]
BODY_PINS = BODY_SOURCES + ["Assets/Game/Scripts/Systems/VehicleFuelConsumptionSystem.cs", "Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs", "Assets/Game/Scripts/Components/UnitCombatComponents.cs", "ProjectSettings/ProjectVersion.txt"]


def body_files(enabled):
    return (list(dict.fromkeys(SOURCES + (BODY_SOURCES if enabled else []))),
            list(dict.fromkeys(PINS + (BODY_PINS if enabled else []))),
            BINS | ({"math-module.json"} if enabled else set()))


def installed_body_math(project):
    explicit = os.environ.get("WARLINE_WORLD_MATH_MODULE")
    version = re.findall(r"(?m)^m_EditorVersion: ([0-9.]+[a-z][0-9]+)$", (project / "ProjectSettings/ProjectVersion.txt").read_text())
    if len(version) != 1:
        raise ValueError("Exact recorded Editor version required for body math")
    path = Path(explicit) if explicit else Path("/Applications/Unity/Hub/Editor") / version[0] / "Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.MathematicsModule.dll"
    info = path.lstat()
    if not stat.S_ISREG(info.st_mode) or not 0 < info.st_size <= 64 * 1024 * 1024:
        raise ValueError("Installed regular managed math module required; set WARLINE_WORLD_MATH_MODULE explicitly on another host")
    return dict(schema_version=1, project_root=str(project), module_path=str(path.resolve()), module_sha256=common.sha(path))


REQUIRED_NATIVE_MARKER = "[SharedWorldTargetingParity] result=Passed"


def schedule_profile(project, receipt, moving=False, multi_unit=False):
    moving = moving or multi_unit
    if receipt is None:
        return dict(enabled=False, reason="Native schedule receipt required for world cases")
    receipt = Path(receipt).resolve()
    data = json.loads(receipt.read_text())
    if set(data) != {"schema_version", "order", "native_log", "native_log_sha256", "source_pins", "settings_pins"} or data['schema_version'] != 1:
        raise ValueError("Unsupported native schedule receipt")
    order = data['order']
    expected = {"Unity.Entities.BeginSimulationEntityCommandBufferSystem", "Unity.Entities.EndSimulationEntityCommandBufferSystem"}
    expected |= {"Game.Runtime." + name for name in ("DynamicOccupancyRebuildSystem", "UnitEngagementSystem", "UnitEngagedMovementSystem", "UnitAttackSystem", "SkirmishObjectiveFactProjectionSystem", "SkirmishBaseAssaultObjectiveSystem", "SkirmishOutcomeSystem")}
    if moving: expected |= {'Game.Runtime.'+name for name in ('EngageTargetValidateSystem','EngageTargetSyncSystem','UnitPathfindingSystem','UnitGridMovementSystem','UnitDeathSystem')}
    if multi_unit: expected.add('Game.Runtime.UnitManualMoveRetrySystem')
    profile_name='IsolatedMultiUnitCombatScheduleV1' if multi_unit else 'IsolatedMovingCombatScheduleV1' if moving else 'IsolatedStationaryCombatScheduleV1'
    marker='[SharedMultiUnitPathParity] result=Passed' if multi_unit else '[SharedMovingLifecycleParity] result=Passed' if moving else REQUIRED_NATIVE_MARKER
    if set(order) != {"profile", "scope", "full_world", "systems"} or order['profile'] != profile_name or order['scope'] != 'NativeSortedSubsetOnly' or order['full_world'] is not False or len(order['systems']) != len(expected) or set(order['systems']) != expected:
        raise ValueError("Unsupported captured native order")
    names = order['systems']
    for first, last in [('DynamicOccupancyRebuildSystem','UnitEngagedMovementSystem' if moving else 'UnitEngagementSystem'),('UnitEngagementSystem','UnitEngagedMovementSystem'),('UnitEngagedMovementSystem','UnitAttackSystem'),('SkirmishObjectiveFactProjectionSystem','SkirmishBaseAssaultObjectiveSystem'),('SkirmishBaseAssaultObjectiveSystem','SkirmishOutcomeSystem')]:
        if names.index('Game.Runtime.'+first) >= names.index('Game.Runtime.'+last): raise ValueError("Invalid native partial order")
    if moving:
        for first,last in [('EngageTargetValidateSystem','UnitEngagementSystem'),('UnitEngagementSystem','EngageTargetSyncSystem'),('EngageTargetSyncSystem','UnitEngagedMovementSystem'),('UnitEngagedMovementSystem','UnitGridMovementSystem'),('UnitPathfindingSystem','UnitGridMovementSystem'),('UnitAttackSystem','UnitDeathSystem')]:
            if names.index('Game.Runtime.'+first)>=names.index('Game.Runtime.'+last): raise ValueError("Invalid moving native partial order")
    if multi_unit and names.index('Game.Runtime.UnitManualMoveRetrySystem') >= names.index('Game.Runtime.UnitPathfindingSystem'): raise ValueError('Invalid manual retry order')
    if not names[0].endswith('.BeginSimulationEntityCommandBufferSystem') or not names[-1].endswith('.EndSimulationEntityCommandBufferSystem'): raise ValueError("Invalid native playback order")
    log = Path(data['native_log']).resolve()
    if common.sha(log) != data['native_log_sha256']: raise ValueError("Native log hash differs")
    text = log.read_text(errors='replace')
    if not moving and '[SharedMovingLifecycleParity] result=Passed' in text:
        marker='[SharedMovingLifecycleParity] result=Passed'
    if not multi_unit and '[SharedMultiUnitPathParity] result=Passed' in text:
        marker='[SharedMultiUnitPathParity] result=Passed'
    order_marker='[MultiUnitWorldSchedulingNativeOrder] ' if multi_unit else '[MovingWorldSchedulingNativeOrder] ' if moving else '[WorldSchedulingNativeOrder] '
    matches = [json.loads(line.split(order_marker,1)[1]) for line in text.splitlines() if order_marker in line]
    if order not in matches or marker not in text or marker.replace('Passed','Failed') in text:
        raise ValueError("Missing captured order or successful native suite marker")
    required_sources = {p for p in PINS if p.startswith('Assets/')}
    if not required_sources <= set(data['source_pins']): raise ValueError("Native receipt lacks required source pins: " + ','.join(sorted(required_sources-set(data['source_pins']))))
    if not {'ProjectSettings/ProjectVersion.txt','Packages/manifest.json','Packages/packages-lock.json'} <= set(data['settings_pins']): raise ValueError("Native receipt lacks settings/package pins")
    for group in ('source_pins','settings_pins'):
        for relative,digest in data[group].items():
            path = (project/relative).resolve()
            if Path(relative).is_absolute() or not path.is_relative_to(project) or not relative.startswith(('Assets/','ProjectSettings/','Packages/')):
                raise ValueError("Unsafe native receipt pin")
            if not isinstance(digest,str) or not re.fullmatch('[a-f0-9]{64}',digest) or common.sha(path)!=digest: raise ValueError("Native receipt source/settings drift: " + relative)
    return dict(enabled=True,order=order,native_log_sha256=data['native_log_sha256'],required_marker=marker,
                source_pins=data['source_pins'],settings_pins=data['settings_pins'],receipt_sha256=common.sha(receipt))


def build(project, output, schedule_receipt=None, moving_schedule_receipt=None, multi_unit_schedule_receipt=None, body_transitions=False):
    project = Path(project).resolve()
    sources, required_pins, expected_bins = body_files(body_transitions)
    math_config = installed_body_math(project) if body_transitions else None
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("Installed .NET 8 SDK required; no download attempted")
    profile = schedule_profile(project, schedule_receipt)
    profile["moving"] = schedule_profile(project, moving_schedule_receipt, True)
    profile["multi_unit"] = schedule_profile(project, multi_unit_schedule_receipt, multi_unit=True)
    output = common.new_output(project, output)
    listed = common.execute([dotnet, "--list-sdks"], output, "sdk-list")
    versions = re.findall(r"(?m)^(8\.\d+\.\d+) \[", listed.stdout.decode())
    if listed.returncode or not versions:
        raise ValueError("Installed .NET 8 SDK required")
    version = max(versions, key=lambda v: tuple(map(int, v.split('.'))))
    stage = output/"source"; stage.mkdir()
    pins = {name: common.sha(project/name) for name in required_pins}
    for name in sources:
        shutil.copyfile(project/name, stage/Path(name).name)
    (stage/"global.json").write_text(json.dumps({"sdk": {"version": version, "rollForward": "disable"}}))
    (stage/"NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    project_xml = '''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><StartupObject>WorldProgram</StartupObject>
<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit>
<UseAppHost>false</UseAppHost><EnableNETAnalyzers>false</EnableNETAnalyzers><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>'''
    if body_transitions:
        module_xml = escape(math_config['module_path'])
        project_xml = project_xml.replace('</Project>', '<PropertyGroup><DefineConstants>WORLD_BODY_TRANSITIONS</DefineConstants></PropertyGroup><ItemGroup><Reference Include="UnityEngine.MathematicsModule"><HintPath>' + module_xml + '</HintPath><Private>false</Private></Reference></ItemGroup></Project>')
    (stage/"WorldReplay.csproj").write_text(project_xml)
    compiled = common.execute([dotnet, "build", "WorldReplay.csproj", "--configfile", "NuGet.Config",
        "--disable-build-servers", "-p:UseSharedCompilation=false", "-o", output/"bin"], output, "compile", cwd=stage)
    if compiled.returncode:
        raise RuntimeError("Standalone world compilation failed; see compile logs in " + str(output))
    if any(common.sha(project/name) != value for name, value in pins.items()) or any(common.sha(stage/Path(name).name) != pins[name] for name in sources):
        raise ValueError("Source changed during build; discard candidate")
    if dict(schedule_profile(project, schedule_receipt),moving=schedule_profile(project,moving_schedule_receipt,True),multi_unit=schedule_profile(project,multi_unit_schedule_receipt,multi_unit=True)) != profile:
        raise ValueError("Native schedule receipt changed during build")
    if body_transitions:
        if installed_body_math(project) != math_config:
            raise ValueError("Installed math changed during build")
        (output/"math-module.json").write_text(json.dumps(math_config, indent=2) + "\n")
    (output/"profile.json").write_text(json.dumps(profile, sort_keys=True, indent=2)+"\n")
    build_files={str(p.relative_to(output)): common.sha(p) for p in sorted((output/"bin").iterdir()) if p.is_file()}
    build_files["profile.json"]=common.sha(output/"profile.json")
    if body_transitions:
        build_files["math-module.json"] = common.sha(output/"math-module.json")
    if set(build_files) != expected_bins:
        raise ValueError("Unexpected copied runtime/build output")
    manifest = dict(schema_version=1, project=str(project), sdk=version, scope=SCOPE, source_pins=pins, body_transitions=body_transitions,
        build_files=build_files,
        native_runs=0, full_battle_simulated=False, mission_qualified=False)
    (output/"build.json").write_text(json.dumps(manifest, indent=2)+"\n")
    return manifest


def run(build_path, input_path, output):
    build_path, input_path = Path(build_path).resolve(), Path(input_path).resolve()
    manifest_path = build_path/"build.json"; manifest_sha = common.sha(manifest_path)
    manifest = json.loads(manifest_path.read_text())
    if type(manifest.get("body_transitions", False)) is not bool:
        raise ValueError("Invalid body feature flag")
    _, required_pins, expected_bins = body_files(manifest.get("body_transitions", False))
    if manifest.get("schema_version") != 1 or manifest.get("scope") != SCOPE or set(manifest.get("source_pins", {})) != set(required_pins):
        raise ValueError("Incomplete/unsupported world source manifest")
    bins = {p.replace(chr(92), "/"): v for p, v in manifest.get("build_files", {}).items()}
    if set(bins) != expected_bins or len(bins) != len(manifest["build_files"]):
        raise ValueError("Incomplete/unsupported world build manifest")
    if any(not isinstance(v, str) or not re.fullmatch("[a-f0-9]{64}", v) for v in list(bins.values())+list(manifest["source_pins"].values())):
        raise ValueError("Invalid source/build digest")
    project = Path(manifest["project"]).resolve()
    def verify():
        if common.sha(manifest_path) != manifest_sha:
            raise ValueError("Manifest changed during replay")
        if any(common.sha(project/p) != v for p,v in manifest["source_pins"].items()):
            raise ValueError("Production source changed; rebuild")
        if any(common.sha(build_path/p) != v for p,v in bins.items()):
            raise ValueError("Built artifact changed; rebuild")
        if manifest.get("body_transitions"):
            math_config = json.loads((build_path/"math-module.json").read_text())
            if installed_body_math(project) != math_config:
                raise ValueError("Pinned installed body math changed")
        profile = json.loads((build_path/"profile.json").read_text())
        for captured_profile in (profile,profile.get("moving",{}),profile.get("multi_unit",{})):
          if captured_profile.get("enabled"):
            for group in ("source_pins", "settings_pins"):
                for relative,digest in captured_profile[group].items():
                    path=(project/relative).resolve()
                    if Path(relative).is_absolute() or not path.is_relative_to(project) or not relative.startswith(("Assets/","ProjectSettings/","Packages/")):
                        raise ValueError("Unsafe captured profile pin")
                    if common.sha(path)!=digest: raise ValueError("Captured schedule dependency changed; rebuild: "+relative)
    verify()
    dotnet = shutil.which("dotnet")
    if not dotnet: raise ValueError("Installed .NET runtime required")
    output = common.new_output(project, output)
    with input_path.open("rb") as stream: raw = stream.read(16*1024*1024+1)
    if len(raw) > 16*1024*1024: raise ValueError("Trace exceeds 16 MiB")
    captured = output/"input.json"; captured.write_bytes(raw)
    process = common.execute([dotnet, build_path/"bin/WorldReplay.dll", captured, build_path/"profile.json"], output, "replay")
    report = json.loads(process.stdout)
    verify()
    if captured.read_bytes() != raw: raise ValueError("Captured input changed during replay")
    report.update(input_sha256=common.sha(captured), build_manifest_sha256=manifest_sha,
                  source_pins=manifest["source_pins"], process_exit=process.returncode)
    (output/"report.json").write_text(json.dumps(report, indent=2)+"\n")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest="command", required=True)
    b = sub.add_parser("build"); b.add_argument("--project", type=Path, required=True); b.add_argument("--output", type=Path, required=True); b.add_argument("--schedule-receipt", type=Path); b.add_argument("--moving-schedule-receipt", type=Path); b.add_argument("--multi-unit-schedule-receipt", type=Path)
    b.add_argument("--body-transitions", action="store_true")
    r = sub.add_parser("run"); r.add_argument("--build", type=Path, required=True); r.add_argument("--input", type=Path, required=True); r.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.command == "build":
            result = build(args.project, args.output, args.schedule_receipt, args.moving_schedule_receipt, args.multi_unit_schedule_receipt, args.body_transitions)
            print(json.dumps(dict(result="built", sdk=result["sdk"], source_files=len(result["source_pins"]), native_runs=0, output=str(args.output))))
            return 0
        result = run(args.build, args.input, args.output)
        print(json.dumps({k:result[k] for k in ("result", "passed", "failed", "elapsed_seconds", "native_runs", "full_battle_simulated") if k in result}))
        return 0 if result.get("result") == "passed" and result.get("process_exit") == 0 else 1
    except (OSError, ValueError, RuntimeError, KeyError, TypeError) as error:
        print(json.dumps(dict(result="failed", error=str(error), native_runs=0, mission_qualified=False)))
        return 1


if __name__ == "__main__": raise SystemExit(main())
