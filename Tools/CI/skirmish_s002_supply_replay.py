"""Replay the exact starting-grant transaction from recorded preflight inputs.

This proves the grant delta only. Earlier physical production, factory construction,
later spending, full mission startup and terminal parity remain separate gates.
Native after-grant values and native grant order are assertions, never inputs.
"""
import argparse
import copy
import hashlib
import json
import re
import stat
import subprocess
from pathlib import Path
from xml.sax.saxutils import escape

ROOT = Path(__file__).resolve().parents[2]
SERVICE = 'Assets/Game/Scripts/Runtime/Skirmish/SkirmishStartingSupplyService.cs'
COMPONENTS = 'Assets/Game/Scripts/Components/BuildingRuntimeEcsComponents.cs'
PROGRAM = 'Tools/CI/SkirmishOfflineSim/StartupSupplyProgram.cs'


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def bounded_regular_file(path):
    info = path.lstat()
    if not stat.S_ISREG(info.st_mode) or not 0 < info.st_size <= 128 * 1024 * 1024:
        raise ValueError('Bounded regular supply input required')


def block(source, signature):
    if source.count(signature) != 1:
        raise ValueError('Unique exact native declaration required: ' + signature)
    start = source.index(signature)
    brace = source.index('{', start)
    arrow = source.find('=>', start, brace)
    if arrow >= 0:
        return source[start:source.index(';', arrow) + 1]
    level = 1
    end = brace + 1
    while level:
        level += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--capture', required=True, type=Path)
    parser.add_argument('--output', required=True, type=Path)
    parser.add_argument('--controls', action='store_true')
    args = parser.parse_args()
    bounded_regular_file(args.capture)
    capture = args.capture.resolve()
    observation = json.loads(capture.read_text())
    descriptor = observation['physical_startup_inputs']
    name = descriptor['artifact']
    if Path(name).name != name or (capture.parent / name).is_symlink():
        raise ValueError('Local regular physical journal required')
    journal_path = capture.parent / name
    bounded_regular_file(journal_path)
    if type(descriptor["bytes"]) is not int or not 0 < descriptor["bytes"] <= 128 * 1024 * 1024:
        raise ValueError("Bounded physical journal descriptor required")
    if journal_path.stat().st_size != descriptor['bytes'] or sha(journal_path) != descriptor['sha256']:
        raise ValueError('Physical journal descriptor differs')
    journal = json.loads(journal_path.read_text())
    if journal['session_id'] != observation['session_id'] or observation['expected_force_count'] != 52:
        raise ValueError('Same actual S002 session required')
    if len(journal['transactions']) != 18:
        raise ValueError('Complete18building causal journal required')
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    build = output / 'build'
    build.mkdir()
    pins = [ROOT / SERVICE, ROOT / COMPONENTS, ROOT / PROGRAM, Path(__file__).resolve(), capture, journal_path]
    before = {str(p): sha(p) for p in pins}
    source = (ROOT / SERVICE).read_text()
    initialize = block(source, 'public static bool Initialize(')
    usable = block(source, 'public static bool IsUsableFuelStorage(')
    priority = re.findall(r'private static int OilGrantPriority\(.*?;', source, re.S)
    if len(priority) != 1:
        raise ValueError('Exact native priority declaration required')
    generated = build / 'NativeGrant.cs'
    generated.write_text('using Game.Components;using Game.Configs;using Unity.Collections;using Unity.Entities;using Unity.Mathematics;namespace Game.Runtime { public static class SkirmishStartingSupplyService {' + initialize + priority[0] + usable + '} }')
    storage = block((ROOT / COMPONENTS).read_text(), 'public struct BuildingResourceStorageComponent : IComponentData')
    storage_file = build / 'NativeStorage.cs'
    storage_file.write_text('using Unity.Entities; namespace Game.Components {' + storage + '}')
    include = ''.join('<Compile Include="' + escape(str(p), {'"': '&quot;'}) + '" />' for p in [ROOT / PROGRAM, generated, storage_file])
    project = build / 'Supply.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems><RestoreIgnoreFailedSources>true</RestoreIgnoreFailedSources><NuGetAudit>false</NuGetAudit><NoWarn>CS8981</NoWarn></PropertyGroup><ItemGroup>' + include + '</ItemGroup></Project>')
    config = build / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>')
    proc = subprocess.run(['rtk', 'proxy', 'dotnet', 'build', str(project), '-o', str(build / 'bin'), '--configfile', str(config)], capture_output=True, text=True)
    (output / 'build.log').write_text(proc.stdout + proc.stderr)
    payload = {'journal': journal, 'setup': {k: observation['resolved_setup'][k] for k in ['OilEach', 'UsableFuelEach']}}
    results = []
    def run(value, label, error=None, input_path=None):
        path = input_path or output / (label + '.json')
        if input_path is None:
            path.write_text(json.dumps(value))
        proc = subprocess.run(['rtk', 'proxy', 'dotnet', str(build / 'bin/Supply.dll'), str(path)], capture_output=True, text=True, timeout=60)
        (output / (label + '.log')).write_text(proc.stdout + proc.stderr)
        actual = json.loads(proc.stdout)
        passed = proc.returncode == 0 if error is None else proc.returncode != 0 and error in actual.get('error', '')
        results.append(dict(label=label, passed=passed, exit_code=proc.returncode, error=actual.get('error'), expected_error=error))
        return actual
    actual = None
    if proc.returncode == 0:
        actual = run(payload, 'actual')
        if args.controls and results[0]['passed']:
            cases = []
            owned_index = next(i for i, r in enumerate(journal['supply_stages'][0]['all_enabled_stores_native_query_order']) if 'stable_id' in r)
            for field in ['StoredOilBarrels', 'StoredFuelBarrels', 'Version', 'OilStorageCapacity', 'ReservedFuelOutboundBarrels']:
                bad = copy.deepcopy(payload)
                bad['journal']['supply_stages'][1]['all_enabled_stores_native_query_order'][owned_index]['storage'][field] += 1
                cases.append(('after-' + field + '-assertion', bad, 'Derived storage differs'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'][0]['all_enabled_stores_native_query_order'][owned_index]['storage']['StoredOilBarrels'] += 1
            cases.append(('preflight-is-causal-input', bad, 'Derived storage differs'))
            bad = copy.deepcopy(payload)
            bad['setup']['UsableFuelEach'] += 1
            cases.append(('authored-grant-is-input', bad, 'Derived storage differs'))
            bad = copy.deepcopy(payload)
            bad['setup']['UsableFuelEach'] = 99999999
            cases.append(('insufficient-room-fails', bad, 'preflight rejected'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'][1]['native_grant_order_assertion'].reverse()
            cases.append(('grant-order-is-assertion', bad, 'Derived grant order differs'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'][1]['all_enabled_stores_native_query_order'].reverse()
            cases.append(('census-identity-required', bad, 'census identity/order'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'][0]['all_enabled_stores_native_query_order'].append(copy.deepcopy(journal['supply_stages'][0]['all_enabled_stores_native_query_order'][owned_index]))
            bad['journal']['supply_stages'][1]['all_enabled_stores_native_query_order'].append(copy.deepcopy(journal['supply_stages'][1]['all_enabled_stores_native_query_order'][owned_index]))
            cases.append(('duplicate-entity-rejected', bad, 'Duplicate storage entity'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'].pop()
            cases.append(('complete-stages-required', bad, 'Exactly preflight'))
            bad = copy.deepcopy(payload)
            foreign_index = next(i for i, r in enumerate(journal['supply_stages'][0]['all_enabled_stores_native_query_order']) if 'stable_id' not in r)
            bad['journal']['supply_stages'][1]['all_enabled_stores_native_query_order'][foreign_index]['storage']['Version'] += 1
            cases.append(('unowned-stores-unchanged', bad, 'Derived storage differs'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'][0]['all_enabled_stores_native_query_order'][owned_index]['attempt'] += '-foreign'
            cases.append(('session-scope-is-causal', bad, 'Derived storage differs'))
            bad = copy.deepcopy(payload)
            bad['journal']['supply_stages'][0]['all_enabled_stores_native_query_order'][owned_index]['storage']['UnknownField'] = 0
            cases.append(('exact-input-storage-schema', bad, 'Exact storage field set required'))
            for label, value, error in cases:
                run(value, label, error)
            oversized = output / 'oversized.json'
            with oversized.open('wb') as stream:
                stream.truncate(128 * 1024 * 1024 + 1)
            link = output / 'payload-link.json'
            link.symlink_to(output / 'actual.json')
            for label, path in [('oversized', oversized), ('symlink', link)]:
                run(None, 'csharp-' + label, 'Bounded regular supply payload required', path)
                try:
                    bounded_regular_file(path)
                except ValueError:
                    results.append(dict(label='python-' + label, passed=True))
                else:
                    results.append(dict(label='python-' + label, passed=False))
    after = {str(p): sha(p) for p in pins}
    result = dict(result='Passed' if results and all(r['passed'] for r in results) and before == after else 'Failed', checks=results, actual=actual, source_pins_before=before, source_pins_after=after, unchanged=before == after, extracted_native_methods_sha256=sha(generated), exact_storage_source_sha256=sha(storage_file), native_execution=False, preflight_state_is_input=True, earlier_production_derived=False, physical_startup_parity=False, full_mission_initialized=False, terminal_parity=False, player_ready=False)
    (output / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: result[k] for k in ['result', 'unchanged', 'preflight_state_is_input', 'physical_startup_parity']} | {'checks': len(results), 'evidence': str(output)}, indent=2))
    return 0 if result['result'] == 'Passed' else 1


if __name__ == '__main__':
    raise SystemExit(main())
