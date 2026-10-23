"""Compile and check the shared production standard-attack planner without Unity or downloads."""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time

sys.dont_write_bytecode = True
KERNEL = "Assets/Game/Scripts/Systems/Combat/StandardAttackPlanner.cs"
ADAPTER = "Assets/Game/Scripts/Systems/UnitAttackSystem.cs"
HARNESS = "Tools/CI/SkirmishOfflineSim/CombatProgram.cs"
DAMAGE = "Assets/Game/Scripts/Systems/Combat/DirectDamageSequencer.cs"
MOVEMENT = "Assets/Game/Scripts/Systems/GridPathMovementKernel.cs"
SEQUENCES = "Tools/CI/SkirmishOfflineSim/SequenceProgram.cs"
SOURCES = [KERNEL, DAMAGE, MOVEMENT, HARNESS, SEQUENCES]
PINNED = SOURCES + [ADAPTER, "Tools/CI/skirmish_combat_replay.py",
    "Assets/Game/Scripts/Systems/Support/SupportDamageUtilitySystemHelper.cs",
    "Assets/Game/Scripts/Systems/UnitGridMovementSystem.cs",
    "Assets/Game/Scripts/Systems/UnitGridMovementPolicy.cs"]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def new_output(project, output):
    project, output = Path(project).resolve(), Path(output).resolve()
    for name in ("Assets", "Packages", "ProjectSettings", ".git"):
        if output.is_relative_to(project / name):
            raise ValueError("Outputs must stay outside imported/configuration/Git trees")
    output.mkdir(parents=True, exist_ok=False)
    return output


def execute(command, output, name, timeout=120, cwd=None):
    env = os.environ.copy()
    env.update(DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1",
               DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1", DOTNET_CLI_HOME=str(output / "dotnet-home"))
    began = time.perf_counter()
    try:
        result = subprocess.run(["rtk", "proxy", *map(str, command)], cwd=cwd,
                                env=env, capture_output=True, timeout=timeout)
    except subprocess.TimeoutExpired as error:
        (output / (name + ".stdout.log")).write_bytes(error.stdout or b"")
        (output / (name + ".stderr.log")).write_bytes(error.stderr or b"")
        raise RuntimeError(name + " timed out; partial logs preserved") from error
    (output / (name + ".stdout.log")).write_bytes(result.stdout)
    (output / (name + ".stderr.log")).write_bytes(result.stderr)
    (output / (name + ".process.json")).write_text(json.dumps(dict(
        command=list(map(str, command)), exit_code=result.returncode,
        elapsed_seconds=time.perf_counter() - began), indent=2))
    return result


def build(project, output):
    project = Path(project).resolve()
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("Installed .NET 8 SDK required; no download attempted")
    output = new_output(project, output)
    listed = execute([dotnet, "--list-sdks"], output, "sdk-list")
    versions = re.findall(r"(?m)^(8\.\d+\.\d+) \[", listed.stdout.decode())
    if listed.returncode or not versions:
        raise ValueError("Installed .NET 8 SDK required; see sdk-list logs")
    version = max(versions, key=lambda v: tuple(map(int, v.split('.'))))
    stage = output / "source"
    stage.mkdir()
    (stage / "global.json").write_text(json.dumps({"sdk": {"version": version, "rollForward": "disable"}}))
    paths = PINNED
    pins = {path: sha(project/path) for path in paths}
    for path in SOURCES:
        shutil.copyfile(project/path, stage/Path(path).name)
    (stage/"NuGet.Config").write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    (stage/"CombatReplay.csproj").write_text('''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework>
<ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable>
<NuGetAudit>false</NuGetAudit><UseAppHost>false</UseAppHost>
<EnableNETAnalyzers>false</EnableNETAnalyzers><TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup></Project>''')
    compiled = execute([dotnet, "build", "CombatReplay.csproj", "--configfile", "NuGet.Config",
                        "--disable-build-servers", "-p:UseSharedCompilation=false", "-o", output/"bin"],
                       output, "compile", cwd=stage)
    if compiled.returncode:
        raise RuntimeError("Standalone compilation failed; see compile logs in " + str(output))
    if any(sha(project/path) != digest for path, digest in pins.items()):
        raise ValueError("Source changed during build; discard this candidate")
    if any(sha(stage/Path(path).name) != pins[path] for path in SOURCES):
        raise ValueError("Staged source differs from production bytes")
    manifest = dict(schema_version=1, project=str(project), dotnet=dotnet, sdk=version,
                    source_pins=pins,
                    build_files={str(p.relative_to(output)): sha(p) for p in sorted((output/'bin').iterdir()) if p.is_file()},
                    scope="ProductionCombatMovementReplay", native_runs=0, full_battle_simulated=False)
    (output/"build.json").write_text(json.dumps(manifest, indent=2)+"\n")
    return manifest


def run(build_path, input_path, output):
    build_path, input_path = Path(build_path).resolve(), Path(input_path).resolve()
    manifest_path = build_path/"build.json"
    manifest_digest = sha(manifest_path)
    manifest = json.loads(manifest_path.read_text())
    if manifest.get("schema_version") != 1 or set(manifest.get("source_pins", {})) != set(PINNED):
        raise ValueError("Incomplete/unsupported production source manifest")
    expected_bins = {"bin/CombatReplay" + suffix for suffix in (".dll", ".deps.json", ".runtimeconfig.json", ".pdb")}
    build_files = {path.replace(chr(92), "/"): digest for path, digest in manifest.get("build_files", {}).items()}
    if set(build_files) != expected_bins or len(build_files) != len(manifest["build_files"]):
        raise ValueError("Incomplete/unsupported standalone build manifest")
    for digest in list(manifest["source_pins"].values()) + list(build_files.values()):
        if not isinstance(digest, str) or not re.fullmatch("[a-f0-9]{64}", digest):
            raise ValueError("Invalid source/build digest")
    dotnet = shutil.which("dotnet")
    if not dotnet:
        raise ValueError("Installed .NET runtime required")
    project = Path(manifest["project"]).resolve()
    for path, digest in manifest["source_pins"].items():
        if sha(project/path) != digest:
            raise ValueError("Production source changed; rebuild: " + path)
    for path, digest in build_files.items():
        if sha(build_path/path) != digest:
            raise ValueError("Built artifact changed; rebuild: " + path)
    output = new_output(project, output)
    with input_path.open("rb") as stream:
        raw = stream.read(16*1024*1024+1)
    if len(raw) > 16*1024*1024:
        raise ValueError("Trace exceeds 16 MiB")
    captured = output/"input.json"
    captured.write_bytes(raw)
    result = execute([dotnet, build_path/"bin/CombatReplay.dll", captured], output, "replay")
    report = json.loads(result.stdout)
    if sha(manifest_path) != manifest_digest or any(sha(build_path/path) != digest for path, digest in build_files.items()):
        raise ValueError("Build changed during replay; results invalid")
    if captured.read_bytes() != raw:
        raise ValueError("Captured input changed during replay; results invalid")
    if any(sha(project/path) != digest for path, digest in manifest["source_pins"].items()):
        raise ValueError("Production source changed during replay; results are not current")
    report.update(input_sha256=hashlib.sha256(raw).hexdigest(), build_manifest_sha256=manifest_digest,
                  source_pins=manifest["source_pins"], process_exit=result.returncode)
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
            print(json.dumps(dict(result="built", sdk=result['sdk'], source_files=len(result['source_pins']), native_runs=0, output=str(args.output))))
            return 0
        result = run(args.build, args.input, args.output)
        print(json.dumps({k: result[k] for k in ('result', 'passed', 'failed', 'elapsed_seconds', 'native_runs', 'full_battle_simulated') if k in result}))
        return 0 if result.get('result') == 'passed' and result.get('process_exit') == 0 else 1
    except (OSError, ValueError, RuntimeError, KeyError, TypeError) as error:
        print(json.dumps(dict(result="failed", error=str(error), native_runs=0, mission_qualified=False)))
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
