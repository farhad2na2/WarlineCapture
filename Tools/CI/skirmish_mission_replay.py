"""Replay shared production mission orchestration without Unity or downloads."""
from __future__ import annotations
import argparse
import json
from pathlib import Path
import re
import shutil
import sys
sys.dont_write_bytecode = True
import skirmish_offline_sim as common

KERNEL = "Assets/Game/Scripts/Runtime/Skirmish/SkirmishMissionOrchestrationKernel.cs"
SOURCES = common.SOURCES + [common.HARNESS, "Tools/CI/SkirmishOfflineSim/MissionProgram.cs"]
ADAPTERS = ["Assets/Game/Scripts/Runtime/Skirmish/" + name + ".cs" for name in (
    "SkirmishObjectiveFactProjectionSystem", "SkirmishNativeObjectiveCollector",
    "SkirmishNativeObjectiveTick", "SkirmishBaseAssaultObjectiveSystem",
    "SkirmishOutcomeSystem", "SkirmishExpandedSessionControlService")]
PINS = list(dict.fromkeys(SOURCES + ADAPTERS + ["Tools/CI/skirmish_offline_sim.py", "Tools/CI/skirmish_mission_replay.py"]))
SCOPE = "ProductionMissionOrchestrationReplay"
BINS = {"bin/MissionReplay" + suffix for suffix in (".dll", ".deps.json", ".runtimeconfig.json", ".pdb")}


def build(project, output):
    project = Path(project).resolve()
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("Installed .NET 8 SDK required; no download attempted")
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
    # Existing objective harness is compiled only to share its strict JSON helpers.
    # Its BA facade delegates to the same shared production implementation.
    (stage/"OfflineBaseAssaultRule.cs").write_text("using Game.Skirmish.Contracts; namespace Game.Runtime { public static class OfflineBaseAssaultRule { public static bool TryEvaluate(in SkirmishBaseAssaultFacts f, out SkirmishOutcomeKind o, out SkirmishEndReasonKind r) => SkirmishMissionOrchestrationKernel.TryEvaluateBaseAssault(in f, out o, out r); } }")
    (stage/"global.json").write_text(json.dumps({"sdk": {"version": version, "rollForward": "disable"}}))
    (stage/"NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    (stage/"MissionReplay.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><StartupObject>MissionProgram</StartupObject>
<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable><NuGetAudit>false</NuGetAudit>
<UseAppHost>false</UseAppHost><EnableNETAnalyzers>false</EnableNETAnalyzers><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>''')
    compiled = common.execute([dotnet, "build", "MissionReplay.csproj", "--configfile", "NuGet.Config",
        "--disable-build-servers", "-p:UseSharedCompilation=false", "-o", output/"bin"], output, "compile", cwd=stage)
    if compiled.returncode:
        raise RuntimeError("Standalone mission compilation failed; see compile logs in " + str(output))
    if any(common.sha(project/name) != value for name, value in pins.items()) or any(common.sha(stage/Path(name).name) != pins[name] for name in SOURCES):
        raise ValueError("Source changed during build; discard candidate")
    manifest = dict(schema_version=1, project=str(project), sdk=version, scope=SCOPE, source_pins=pins,
        build_files={str(p.relative_to(output)): common.sha(p) for p in sorted((output/"bin").iterdir()) if p.is_file()},
        native_runs=0, full_battle_simulated=False, mission_qualified=False)
    (output/"build.json").write_text(json.dumps(manifest, indent=2)+"\n")
    return manifest


def run(build_path, input_path, output):
    build_path, input_path = Path(build_path).resolve(), Path(input_path).resolve()
    manifest_path = build_path/"build.json"; manifest_sha = common.sha(manifest_path)
    manifest = json.loads(manifest_path.read_text())
    if manifest.get("schema_version") != 1 or manifest.get("scope") != SCOPE or set(manifest.get("source_pins", {})) != set(PINS):
        raise ValueError("Incomplete/unsupported mission source manifest")
    bins = {p.replace(chr(92), "/"): v for p, v in manifest.get("build_files", {}).items()}
    if set(bins) != BINS or len(bins) != len(manifest["build_files"]):
        raise ValueError("Incomplete/unsupported mission build manifest")
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
    verify()
    dotnet = shutil.which("dotnet")
    if not dotnet: raise ValueError("Installed .NET runtime required")
    output = common.new_output(project, output)
    with input_path.open("rb") as stream: raw = stream.read(16*1024*1024+1)
    if len(raw) > 16*1024*1024: raise ValueError("Trace exceeds 16 MiB")
    captured = output/"input.json"; captured.write_bytes(raw)
    process = common.execute([dotnet, build_path/"bin/MissionReplay.dll", captured], output, "replay")
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
    b = sub.add_parser("build"); b.add_argument("--project", type=Path, required=True); b.add_argument("--output", type=Path, required=True)
    r = sub.add_parser("run"); r.add_argument("--build", type=Path, required=True); r.add_argument("--input", type=Path, required=True); r.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    try:
        if args.command == "build":
            result = build(args.project, args.output)
            print(json.dumps(dict(result="built", sdk=result["sdk"], source_files=len(result["source_pins"]), native_runs=0, output=str(args.output))))
            return 0
        result = run(args.build, args.input, args.output)
        print(json.dumps({k:result[k] for k in ("result", "passed", "failed", "elapsed_seconds", "native_runs", "full_battle_simulated") if k in result}))
        return 0 if result.get("result") == "passed" and result.get("process_exit") == 0 else 1
    except (OSError, ValueError, RuntimeError, KeyError, TypeError) as error:
        print(json.dumps(dict(result="failed", error=str(error), native_runs=0, mission_qualified=False)))
        return 1


if __name__ == "__main__": raise SystemExit(main())
