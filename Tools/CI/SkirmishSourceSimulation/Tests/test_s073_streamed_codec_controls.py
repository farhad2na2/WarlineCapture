#!/usr/bin/env python3
"""Run fail-closed decoder controls using values from the pinned source capture."""
import copy
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys


def main():
    repo = Path.cwd()
    build = json.loads(Path(sys.argv[1]).read_text())
    capture = Path(sys.argv[2])
    out = Path(sys.argv[3]).resolve()
    out.mkdir(exist_ok=False)
    raw = capture.read_bytes()
    if hashlib.sha256(raw).hexdigest() != '1f375a666f474a73d2aaac5c0b1930a774b117349a1e074f618ecbbd57b01bd7':
        raise ValueError('Exact retained S073 source capture required')
    source = json.loads(raw)
    root = source['root_entity_ids'][0]
    original = next(row for row in source['entities'] if row['id'] == root)
    component_names = ['Game.Components.OperationMapBuildingComponent', 'Game.Components.Faction']
    baseline = {key: copy.deepcopy(source[key]) for key in [
        'schema_version', 'kind', 'scene_guid', 'closure_complete', 'mission_ready',
        'live_s073_session_observed', 'physical_source_validated', 'scheduler_admitted']}
    baseline.update(entities=[{'id': root, 'entity_enabled': True,
                               'components': {name: copy.deepcopy(original['components'][name]) for name in component_names}}],
                    root_entity_ids=[root], building_faction_query_count=1, unsupported_values=[])
    previous = copy.deepcopy(next(row['components']['Unity.Transforms.PreviousParent']
                                  for row in source['entities'] if 'Unity.Transforms.PreviousParent' in row['components']))
    previous['value']['fields']['Unity.Transforms.PreviousParent::Value']['id'] = root
    baseline['entities'][0]['components']['Unity.Transforms.PreviousParent'] = previous
    cases = {'valid': baseline}
    duplicate = copy.deepcopy(baseline)
    duplicate['entities'].append(copy.deepcopy(duplicate['entities'][0]))
    cases['duplicate'] = duplicate
    dangling = copy.deepcopy(baseline)
    dangling['root_entity_ids'] = ['999999:1']
    cases['missing_root'] = dangling
    unbound = copy.deepcopy(baseline)
    unbound['entities'][0]['components']['Unity.Transforms.PreviousParent']['value']['fields'][
        'Unity.Transforms.PreviousParent::Value']['id'] = '999999:1'
    cases['unbound_cleanup_reference'] = unbound
    bad_kind = copy.deepcopy(baseline)
    bad_kind['entities'][0]['components']['Game.Components.Faction']['buffer'] = True
    cases['wrong_component_kind'] = bad_kind
    bad_vfx = copy.deepcopy(baseline)
    bad_vfx['unsupported_values'] = [{'path': root + '/Game.Components.Faction/Id',
                                      'reason': 'unity_object_ref_requires_bound_source_identity',
                                      'type': 'Unity.Entities.UnityObjectRef`1[[UnityEngine.GameObject, fake]]'}]
    cases['unrecognized_unsupported'] = bad_vfx
    bad_disabled = copy.deepcopy(baseline)
    bad_disabled['entities'][0]['entity_enabled'] = False
    selection = build['runtime_selection']
    env = os.environ.copy()
    env['MONO_PATH'] = os.pathsep.join([str(Path(build['executable']['path']).parent),
        str(repo / 'Library/ScriptAssemblies'), selection['engine'],
        str(Path(selection['newtonsoft']).parent), str(Path(build['placement_build']).parent)])
    config = repo / 'Logs/SkirmishProduction/s073-native-order-20261101-r2/run/mono-config.xml'
    results = {}
    for name, data in list(cases.items()) + [('roundtrip_bad_disabled', bad_disabled)]:
        path = out / (name + '.json')
        path.write_text(json.dumps(data))
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        modes = ['roundtrip'] if name == 'roundtrip_bad_disabled' else ['preflight', 'roundtrip']
        for mode in modes:
            result = out / (name + '-' + mode + '-result.json')
            option = '--s073-streamed-isolated-roundtrip' if mode == 'roundtrip' else '--s073-streamed-codec-preflight'
            command = [selection['mono'], '--config', str(config), '--optimize=-float32',
                       build['executable']['path'], option, str(path), digest, str(result)]
            process = subprocess.run(command, env=env, capture_output=True, text=True, timeout=60)
            (out / (name + '-' + mode + '.log')).write_text(process.stdout + process.stderr)
            expected = name == 'valid'
            if (process.returncode == 0) != expected or result.exists() != expected:
                raise RuntimeError('Unexpected control result: ' + name + '/' + mode)
            results[name + '/' + mode] = {'exit_code': process.returncode, 'expected_acceptance': expected}
    (out / 'receipt.json').write_text(json.dumps({'status': 'Passed', 'fixture_only': True,
        'import_admitted': False, 'mission_ready': False, 'cases': results}, indent=2) + '\n')
    print('[S073StreamedCodecControls] result=Passed fixtureOnly=1 import=0')


if __name__ == '__main__':
    main()
