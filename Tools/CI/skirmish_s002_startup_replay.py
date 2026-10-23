"""Derive placement cells from a real S002 allocator capture using shared kernels.

Captured requested positions and surface/occupancy facts remain inputs. Grounding,
typed-pad policy, economy, combat and terminal mission parity are separate gates.
"""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from xml.sax.saxutils import escape
from skirmish_startup_grounding_worker import build_worker, SOURCES as WORKER_SOURCES


ROOT = Path(__file__).resolve().parents[2]
SOURCES = [
    'Tools/CI/SkirmishOfflineSim/StartupPlacementProgram.cs',
    'Tools/CI/SkirmishOfflineSim/SpawnJson.cs',
    'Tools/CI/SkirmishOfflineSim/StartupActorState.cs',
    'Tools/CI/SkirmishOfflineSim/StartupSessionState.cs',
    'Assets/Game/Scripts/Configs/Skirmish/SkirmishCapacityLedger.cs',
    'Assets/Game/Scripts/Skirmish/Contracts/SkirmishObjectiveContracts.cs',
    'Assets/Game/Scripts/Runtime/Skirmish/SkirmishActorInitializationKernel.cs',
    'Assets/Game/Scripts/Skirmish/Contracts/SkirmishRosterContracts.cs',
    'Assets/Game/Scripts/Utilities/SpawnCellSearchKernel.cs',
    'Assets/Game/Scripts/Components/GridCoordinateKernel.cs',
    'Assets/Game/Scripts/Systems/GridOccupancyKernel.cs',
    'Tools/CI/SkirmishOfflineSim/StartupGrounding.cs',
    'Tools/CI/SkirmishOfflineSim/StartupGroundingShims.cs',
    'Tools/CI/SkirmishOfflineSim/StartupGroundingRuntime.cs',
    'Assets/Game/Scripts/Components/MapSurfaceComponents.cs',
    'Assets/Game/Scripts/Components/MapSurfaceMeshGeometryComponents.cs',
    'Assets/Game/Scripts/Components/MapSurfaceBlobAccess.cs',
    'Assets/Game/Scripts/Components/MapSurfaceSceneOverlaySampling.cs',
    'Assets/Game/Scripts/Systems/SpawnGroundingKernel.cs',
]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--controls', action='store_true')
    parser.add_argument('--grounding', action='store_true', help='Require actual runtime surface/road geometry and bit-exact grounded transforms')
    args = parser.parse_args()
    capture, output = args.capture.resolve(), args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    build = output / 'build'
    build.mkdir()
    constant_source = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishExpandedEngagementService.cs'
    version_source = 'ProjectSettings/ProjectVersion.txt'
    version = re.findall(r'^m_EditorVersion: ([0-9.]+[a-z][0-9]+)$', (ROOT / version_source).read_text(), re.M)
    if len(version) != 1:
        raise ValueError('Exact installed Editor version required for the managed math implementation')
    math_path = Path('/Applications/Unity/Hub/Editor') / version[0] / 'Unity.app/Contents/Resources/Scripting/Managed/UnityEngine/UnityEngine.MathematicsModule.dll'
    pinned = list(dict.fromkeys(SOURCES + WORKER_SOURCES)) + ['Tools/CI/skirmish_startup_grounding_worker.py', 'Tools/CI/skirmish_s002_startup_replay.py', constant_source,
                        'Assets/Game/Scripts/Runtime/Skirmish/SkirmishSharedCombatBinding.cs',
                        'Assets/Game/Scripts/Runtime/Skirmish/SkirmishRosterProjectionSystem.cs',
                        'Assets/Game/Scripts/Runtime/Skirmish/SkirmishSessionInitializationSystem.cs',
                        'Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs',
                        'Assets/Game/Scripts/Components/Skirmish/SkirmishExpandedSessionComponents.cs', version_source, str(math_path)]
    before = {p: sha(ROOT / p) for p in pinned}
    before[str(capture)] = sha(capture)
    interval = re.findall(r'public const float ShotIntervalSeconds\s*=\s*([0-9]+(?:\.[0-9]+)?f)\s*;', (ROOT / constant_source).read_text())
    if len(interval) != 1:
        raise ValueError('Exact supported production shot interval declaration required')
    constants = build / 'NativeStartupConstants.cs'
    constants.write_text('internal static class NativeStartupConstants { internal const float ShotIntervalSeconds = ' + interval[0] + '; internal const string MathImplementationPath = ' + json.dumps(str(math_path)) + '; internal const string MathImplementationSha256 = ' + json.dumps(sha(math_path)) + '; }')
    component_source = (ROOT / 'Assets/Game/Scripts/Components/Skirmish/SkirmishExpandedSessionComponents.cs').read_text()
    capacity = re.findall(r'public struct SkirmishCapacityComponent : IComponentData\s*\{([^{}]+)\}', component_source)
    if len(capacity) != 1:
        raise ValueError('Exact data-only native capacity component required')
    capacity_source = build / 'NativeCapacityComponent.cs'
    capacity_source.write_text('namespace Game.Components { public struct SkirmishCapacityComponent {' + capacity[0] + '} }')
    items = ''.join('<Compile Include="' + escape(str(ROOT / p), {'"': '&quot;'}) + '" />' for p in SOURCES)
    items += '<Compile Include="' + escape(str(capacity_source), {'"': '&quot;'}) + '" />'
    items += '<Compile Include="' + escape(str(constants), {'"': '&quot;'}) + '" />'
    items += '<Reference Include="UnityEngine.MathematicsModule"><HintPath>' + escape(str(math_path)) + '</HintPath><Private>false</Private></Reference>'
    (build / 'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources><NuGetAudit>false</NuGetAudit></PropertyGroup><ItemGroup>' + items + '</ItemGroup></Project>')
    config = build / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    proc = subprocess.run(['rtk', 'proxy', 'dotnet', 'build', str(build / 'Replay.csproj'), '-o', str(build / 'bin'), '--configfile', str(config)], capture_output=True, text=True)
    (output / 'build.log').write_text(proc.stdout + proc.stderr)
    results = []
    if proc.returncode:
        result = dict(result='FailedBuild', exit_code=proc.returncode)
    else:
        worker_config = build_worker(output / 'grounding-worker', math_module=math_path)
        run_env = os.environ.copy()
        run_env['WARLINE_STARTUP_GROUNDING_WORKER_CONFIG'] = str(worker_config)
        run_env['WARLINE_STARTUP_GROUNDING_WORKER_CONFIG_SHA256'] = sha(worker_config)
        def run(path, label, expected_error=None):
            command = ['rtk', 'proxy', 'dotnet', str(build / 'bin/Replay.dll'), str(path)]
            if args.grounding:
                command.append('--grounded')
            run = subprocess.run(command, capture_output=True, text=True, env=run_env, timeout=180)
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
            for fact, field in [('player', 'Fuel'), ('enemy', 'Oil'), ('capacity', 'SupplyLive'), ('enemy_capacity', 'NextReservationId'), ('objective', 'State'), ('clock', 'DeadlineSeconds')]:
                bad = copy.deepcopy(data)
                bad['session_facts'][fact][field] += 1
                cases.append(('session-' + fact + '-is-assertion', bad, 'derived session ' + fact))
            bad = copy.deepcopy(data)
            bad['resolved_setup']['MaterialsEach'] += 1
            cases.append(('authored-logical-stock-is-input', bad, 'derived session player'))
            bad = copy.deepcopy(data)
            building = next(a for a in bad['actors'] if a['is_structure'])
            building['source_key'] += '_wrong'
            cases.append(('structure-source-is-assertion', bad, 'Derived structure identity/key/faction differs'))
            bad = copy.deepcopy(data)
            bad['actors'].append(copy.deepcopy(next(a for a in bad['actors'] if a['is_structure'])))
            cases.append(('complete-structure-census', bad, 'Complete native structure census required'))
            bad = copy.deepcopy(data)
            next(s for s in bad['resolved_setup']['Structures'] if s['DesignatedBase'])['DesignatedBase'] = False
            cases.append(('original-designated-bases-required', bad, 'Exactly one original designated base per faction required'))
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
            first_id = bad['allocator_transactions'][0]['stable_id']
            next(a for a in bad['actors'] if a['stable_id'] == first_id)['components']['UnitHealth']['Current'] -= 1
            cases.append(('native-health-is-assertion', bad, 'derived actor components'))
            bad = copy.deepcopy(data)
            first_key = next(a for a in bad['actors'] if a['stable_id'] == first_id)['source_key']
            next(t for t in bad['registry_templates']['registry_entries'] if t['key'] == first_key and t['selected_by_native_lookup'])['components']['UnitMove']['Speed'] += 1
            cases.append(('baked-defaults-are-inputs', bad, 'instantiated baked defaults'))
            bad = copy.deepcopy(data)
            first_role = bad['resolved_setup']['Forces'][0]['RoleKind']
            next(o for o in bad['resolved_setup']['RoleOverlays'] if o['RoleKind'] == first_role)['Damage'] += 1
            cases.append(('authored-overlay-is-input', bad, 'derived actor components'))
            bad = copy.deepcopy(data)
            next(a for a in bad['actors'] if a['stable_id'] == first_id)['components']['UnitIdleWanderComponent']['RandomState'] ^= 1
            cases.append(('idle-random-is-baked-default', bad, 'derived actor components'))
            bad = copy.deepcopy(data)
            next(a for a in bad['actors'] if a['stable_id'] == first_id)['entity_enabled'] = False
            cases.append(('entity-enablement-is-derived', bad, 'derived entity enablement'))
            bad = copy.deepcopy(data)
            flags = next(a for a in bad['actors'] if a['stable_id'] == first_id)['enableable_components']
            flag = next(iter(flags))
            flags[flag] = not flags[flag]
            cases.append(('component-enablement-is-derived', bad, 'derived component enablement'))
            if args.grounding:
                bad = copy.deepcopy(data)
                next(a for a in bad['actors'] if a['stable_id'] == first_id)['components']['LocalTransform']['Position']['y'] += 0.1
                cases.append(('native-grounded-y-is-assertion', bad, 'Derived grounded Y differs from native assertion'))
                bad = copy.deepcopy(data)
                bad['allocator_transactions'][0]['placement_result']['actor']['components']['LocalTransform']['Position']['y'] += 0.1
                cases.append(('immediate-grounded-y-is-assertion', bad, 'Derived grounded Y differs from immediate-placement assertion'))
                bad = copy.deepcopy(data)
                next(a for a in bad['actors'] if a['stable_id'] == first_id)['components']['LocalTransform']['Position']['x'] += 1
                cases.append(('native-grounded-x-is-assertion', bad, 'Derived transform X/Z/scale differs'))
                bad = copy.deepcopy(data)
                bad['allocator_transactions'][0]['walkable_sha256'] = '0' * 64
                cases.append(('before-placement-walkable-required', bad, 'Native pre-placement walkable/match/surface context differs'))
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
    result.update(sources_before=before, sources_after=after, unchanged=before == after, grounding_required=args.grounding,
                  copied_math_dlls=[str(p) for p in (build / 'bin').glob('*Mathematics*.dll')],
                  native_execution=False, full_mission_initialized=False, terminal_parity=False, player_ready=False)
    if before != after:
        result['result'] = 'FailedSourceDrift'
    if result['copied_math_dlls']:
        result['result'] = 'FailedUnexpectedMathCopy'
    (output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(result=result['result'], checks=len(results), force_actors=result.get('actual', {}).get('force_actors'), unchanged=before == after, evidence=str(output), full_mission_initialized=False, terminal_parity=False), indent=2))
    return 0 if result['result'] == 'Passed' else 1


if __name__ == '__main__':
    raise SystemExit(main())
