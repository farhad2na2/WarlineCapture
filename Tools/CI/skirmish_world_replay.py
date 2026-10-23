"""Replay shared production bounded world scheduling and targeting without Unity or downloads."""
from __future__ import annotations
import argparse
import json
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
ADAPTERS = ["Assets/Game/Scripts/Systems/" + name + ".cs" for name in (
    "UnitEngagementSystem", "UnitAttackSystem", "UnitEngagedMovementSystem", "UnitGridMovementSystem", "SkirmishCombatPolicy", "AITargetingSystem", "DynamicOccupancyRebuildSystem", "CombatTargetPolicyUtility", "UnitDeathSystem")]
ADAPTERS += ["Assets/Game/Scripts/Runtime/Skirmish/" + name + ".cs" for name in (
    "SkirmishObjectiveFactProjectionSystem", "SkirmishBaseAssaultObjectiveSystem", "SkirmishOutcomeSystem")]
ADAPTERS += ["Assets/Game/Scripts/Systems/UnitAttackSystem.CampaignMissionSuppression.cs"]
ADAPTERS += ["Assets/Game/Scripts/Components/CombatTargetPolicyComponents.cs", "Assets/Game/Scripts/Components/FactionIdentity.cs",
    "Assets/Game/Scripts/Runtime/Skirmish/SkirmishFogConsumerUtility.cs"]
PINS = list(dict.fromkeys(SOURCES + ADAPTERS + ["Tools/CI/skirmish_offline_sim.py", "Tools/CI/skirmish_world_replay.py"]))
SCOPE = "ProductionBoundedWorldReplay"
BINS = {"bin/WorldReplay" + suffix for suffix in (".dll", ".deps.json", ".runtimeconfig.json", ".pdb")} | {"profile.json"}


REQUIRED_NATIVE_MARKER = "[SharedWorldTargetingParity] result=Passed"


def schedule_profile(project, receipt):
    if receipt is None:
        return dict(enabled=False, reason="Native schedule receipt required for world cases")
    receipt = Path(receipt).resolve()
    data = json.loads(receipt.read_text())
    if set(data) != {"schema_version", "order", "native_log", "native_log_sha256", "source_pins", "settings_pins"} or data['schema_version'] != 1:
        raise ValueError("Unsupported native schedule receipt")
    order = data['order']
    expected = {"Unity.Entities.BeginSimulationEntityCommandBufferSystem", "Unity.Entities.EndSimulationEntityCommandBufferSystem"}
    expected |= {"Game.Runtime." + name for name in ("DynamicOccupancyRebuildSystem", "UnitEngagementSystem", "UnitEngagedMovementSystem", "UnitAttackSystem", "SkirmishObjectiveFactProjectionSystem", "SkirmishBaseAssaultObjectiveSystem", "SkirmishOutcomeSystem")}
    if set(order) != {"profile", "scope", "full_world", "systems"} or order['profile'] != 'IsolatedStationaryCombatScheduleV1' or order['scope'] != 'NativeSortedSubsetOnly' or order['full_world'] is not False or len(order['systems']) != len(expected) or set(order['systems']) != expected:
        raise ValueError("Unsupported captured native order")
    names = order['systems']
    for first, last in [('DynamicOccupancyRebuildSystem','UnitEngagementSystem'),('UnitEngagementSystem','UnitEngagedMovementSystem'),('UnitEngagedMovementSystem','UnitAttackSystem'),('SkirmishObjectiveFactProjectionSystem','SkirmishBaseAssaultObjectiveSystem'),('SkirmishBaseAssaultObjectiveSystem','SkirmishOutcomeSystem')]:
        if names.index('Game.Runtime.'+first) >= names.index('Game.Runtime.'+last): raise ValueError("Invalid native partial order")
    if not names[0].endswith('.BeginSimulationEntityCommandBufferSystem') or not names[-1].endswith('.EndSimulationEntityCommandBufferSystem'): raise ValueError("Invalid native playback order")
    log = Path(data['native_log']).resolve()
    if common.sha(log) != data['native_log_sha256']: raise ValueError("Native log hash differs")
    text = log.read_text(errors='replace')
    matches = [json.loads(line.split('[WorldSchedulingNativeOrder] ',1)[1]) for line in text.splitlines() if '[WorldSchedulingNativeOrder] ' in line]
    if order not in matches or REQUIRED_NATIVE_MARKER not in text or '[SharedWorldTargetingParity] result=Failed' in text:
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
    return dict(enabled=True,order=order,native_log_sha256=data['native_log_sha256'],required_marker=REQUIRED_NATIVE_MARKER,
                source_pins=data['source_pins'],settings_pins=data['settings_pins'],receipt_sha256=common.sha(receipt))


def build(project, output, schedule_receipt=None):
    project = Path(project).resolve()
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("Installed .NET 8 SDK required; no download attempted")
    profile = schedule_profile(project, schedule_receipt)
    output = common.new_output(project, output)
    listed = common.execute([dotnet, "--list-sdks"], output, "sdk-list")
    versions = re.findall(r"(?m)^(8\.\d+\.\d+) \[", listed.stdout.decode())
    if listed.returncode or not versions:
        raise ValueError("Installed .NET 8 SDK required")
    version = max(versions, key=lambda v: tuple(map(int, v.split('.'))))
    stage = output/"source"; stage.mkdir()
    pins = {name: common.sha(project/name) for name in PINS}
    for name in SOURCES:
        shutil.copyfile(project/name, stage/Path(name).name)
    (stage/"global.json").write_text(json.dumps({"sdk": {"version": version, "rollForward": "disable"}}))
    (stage/"NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    (stage/"WorldReplay.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><StartupObject>WorldProgram</StartupObject>
<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit>
<UseAppHost>false</UseAppHost><EnableNETAnalyzers>false</EnableNETAnalyzers><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>''')
    compiled = common.execute([dotnet, "build", "WorldReplay.csproj", "--configfile", "NuGet.Config",
        "--disable-build-servers", "-p:UseSharedCompilation=false", "-o", output/"bin"], output, "compile", cwd=stage)
    if compiled.returncode:
        raise RuntimeError("Standalone world compilation failed; see compile logs in " + str(output))
    if any(common.sha(project/name) != value for name, value in pins.items()) or any(common.sha(stage/Path(name).name) != pins[name] for name in SOURCES):
        raise ValueError("Source changed during build; discard candidate")
    if schedule_receipt is not None and schedule_profile(project, schedule_receipt) != profile:
        raise ValueError("Native schedule receipt changed during build")
    (output/"profile.json").write_text(json.dumps(profile, sort_keys=True, indent=2)+"\n")
    build_files={str(p.relative_to(output)): common.sha(p) for p in sorted((output/"bin").iterdir()) if p.is_file()}
    build_files["profile.json"]=common.sha(output/"profile.json")
    manifest = dict(schema_version=1, project=str(project), sdk=version, scope=SCOPE, source_pins=pins,
        build_files=build_files,
        native_runs=0, full_battle_simulated=False, mission_qualified=False)
    (output/"build.json").write_text(json.dumps(manifest, indent=2)+"\n")
    return manifest


def run(build_path, input_path, output):
    build_path, input_path = Path(build_path).resolve(), Path(input_path).resolve()
    manifest_path = build_path/"build.json"; manifest_sha = common.sha(manifest_path)
    manifest = json.loads(manifest_path.read_text())
    if manifest.get("schema_version") != 1 or manifest.get("scope") != SCOPE or set(manifest.get("source_pins", {})) != set(PINS):
        raise ValueError("Incomplete/unsupported world source manifest")
    bins = {p.replace(chr(92), "/"): v for p, v in manifest.get("build_files", {}).items()}
    if set(bins) != BINS or len(bins) != len(manifest["build_files"]):
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
        profile = json.loads((build_path/"profile.json").read_text())
        if profile.get("enabled"):
            for group in ("source_pins", "settings_pins"):
                for relative,digest in profile[group].items():
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
    b = sub.add_parser("build"); b.add_argument("--project", type=Path, required=True); b.add_argument("--output", type=Path, required=True); b.add_argument("--schedule-receipt", type=Path)
    r = sub.add_parser("run"); r.add_argument("--build", type=Path, required=True); r.add_argument("--input", type=Path, required=True); r.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.command == "build":
            result = build(args.project, args.output, args.schedule_receipt)
            print(json.dumps(dict(result="built", sdk=result["sdk"], source_files=len(result["source_pins"]), native_runs=0, output=str(args.output))))
            return 0
        result = run(args.build, args.input, args.output)
        print(json.dumps({k:result[k] for k in ("result", "passed", "failed", "elapsed_seconds", "native_runs", "full_battle_simulated") if k in result}))
        return 0 if result.get("result") == "passed" and result.get("process_exit") == 0 else 1
    except (OSError, ValueError, RuntimeError, KeyError, TypeError) as error:
        print(json.dumps(dict(result="failed", error=str(error), native_runs=0, mission_qualified=False)))
        return 1


if __name__ == "__main__": raise SystemExit(main())
