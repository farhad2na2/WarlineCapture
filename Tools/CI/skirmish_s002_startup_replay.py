"""Derive placement cells from a real S002 allocator capture using shared kernels.

Captured requested positions and surface/occupancy facts remain inputs. Grounding,
typed-pad policy, economy, combat and terminal mission parity are separate gates.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
from xml.sax.saxutils import escape


ROOT = Path(__file__).resolve().parents[2]
SOURCES = [
    'Tools/CI/SkirmishOfflineSim/StartupPlacementProgram.cs',
    'Tools/CI/SkirmishOfflineSim/SpawnJson.cs',
    'Assets/Game/Scripts/Utilities/SpawnCellSearchKernel.cs',
    'Assets/Game/Scripts/Components/GridCoordinateKernel.cs',
    'Assets/Game/Scripts/Systems/GridOccupancyKernel.cs',
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--controls', action='store_true')
    args = parser.parse_args()
    capture, output = args.capture.resolve(), args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    build = output / 'build'
    build.mkdir()
    pinned = SOURCES + ['Tools/CI/skirmish_s002_startup_replay.py']
    before = {p: sha(ROOT / p) for p in pinned}
    before[str(capture)] = sha(capture)
    items = ''.join('<Compile Include="' + escape(str(ROOT / p), {'"': '&quot;'}) + '" />' for p in SOURCES)
    (build / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>')
    config = build / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    proc = subprocess.run(['rtk', 'proxy', 'dotnet', 'build', str(build / 'Replay.csproj'), '-o', str(build / 'bin'), '--configfile', str(config)], capture_output=True, text=True)
    (output / 'build.log').write_text(proc.stdout + proc.stderr)
    results = []
    if proc.returncode:
        result = dict(result='FailedBuild', exit_code=proc.returncode)
    else:
        def run(path, label, expected_error=None):
            run = subprocess.run(['rtk', 'proxy', 'dotnet', str(build / 'bin/Replay.dll'), str(path)], capture_output=True, text=True)
            (output / (label + '.log')).write_text(run.stdout + run.stderr)
            value = json.loads(run.stdout)
            passed = run.returncode == 0 if expected_error is None else run.returncode != 0 and expected_error in value.get('error', '')
            results.append(dict(label=label, passed=passed, actual_exit_code=run.returncode, expected_error=expected_error, result=value.get('result'), error=value.get('error')))
            return value

        actual = run(capture, 'actual-derived-placement')
        (output / 'derived-placement.json').write_text(json.dumps(actual, indent=2) + '\n')
        if args.controls and results[0]['passed']:
            data = json.loads(capture.read_text())
            controls = output / 'controls'
            controls.mkdir()
            for name in data['artifacts']:
                if Path(name).name != name:
                    raise ValueError('Native artifact path escape')
                shutil.copyfile(capture.parent / name, controls / name)
            cases = []
            bad = copy.deepcopy(data)
            bad['allocator_transactions'][0]['reserved_u8_sha256'] = '0' * 64
            cases.append(('reserved-hash', bad, 'Native reservation differs'))
            bad = copy.deepcopy(data)
            bad['allocator_transactions'][0]['random_state_before_find'] = 0
            cases.append(('rng-state', bad, 'Derived native RNG differs'))
            bad = copy.deepcopy(data)
            cell = bad['allocator_transactions'][0]['placement_result']['actor']['components']['UnitGrid']['Cell']
            cell['x'] += 1
            cases.append(('native-result-is-assertion', bad, 'Derived placement differs from native result'))
            bad = copy.deepcopy(data)
            bad['allocator_transactions'][0]['placement_result']['actor']['entity_index'] += 1
            cases.append(('same-result-actor-required', bad, 'Native result actor identity differs'))
            bad = copy.deepcopy(data)
            actor_id = bad['allocator_transactions'][0]['stable_id']
            next(a for a in bad['actors'] if a['stable_id'] == actor_id)['entity_version'] += 1
            cases.append(('same-post-roster-actor-required', bad, 'Post-roster actor identity differs'))
            bad = copy.deepcopy(data)
            bad['footprint_surface_profiles'] = [p for p in bad['footprint_surface_profiles'] if not p['vehicle']]
            cases.append(('real-vehicle-profile-required', bad, 'Actual footprint admission absent'))
            bad = copy.deepcopy(data)
            census = bad['allocator_transactions'][1]['reservation_census_native_query_order']
            census[0]['cell'][0] += 1
            cases.append(('later-census-is-assertion', bad, 'Native census differs from initial actors plus DERIVED placements'))
            bad = copy.deepcopy(data)
            bad['artifacts']['walkable.u8']['sha256'] = '0' * 64
            cases.append(('artifact-integrity', bad, 'Artifact bytes differ'))
            bad = copy.deepcopy(data)
            initial = bad['allocator_transactions'][0]['reservation_census_native_query_order']
            initial.extend([initial[0]] * (65537 - len(initial)))
            cases.append(('initial-census-bound', bad, 'Bounded complete initial census required'))
            for label, value, error in cases:
                path = controls / (label + '.json')
                path.write_text(json.dumps(value))
                run(path, label, error)
            oversized = controls / 'oversized.json'
            with oversized.open('wb') as stream:
                stream.truncate(128 * 1024 * 1024 + 1)
            run(oversized, 'capture-file-bound', 'Capture JSON exceeds128MiB')
        result = dict(result='Passed' if results and all(r['passed'] for r in results) else 'Failed', checks=results, actual=actual)
    after = {p: sha(ROOT / p) for p in pinned}
    after[str(capture)] = sha(capture)
    result.update(sources_before=before, sources_after=after, unchanged=before == after,
                  native_execution=False, full_mission_initialized=False, terminal_parity=False, player_ready=False)
    if before != after:
        result['result'] = 'FailedSourceDrift'
    (output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(result=result['result'], checks=len(results), force_actors=result.get('actual', {}).get('force_actors'), unchanged=before == after, evidence=str(output), full_mission_initialized=False, terminal_parity=False), indent=2))
    return 0 if result['result'] == 'Passed' else 1


if __name__ == '__main__':
    raise SystemExit(main())
