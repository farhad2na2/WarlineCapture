"""Compare actual source-world opening structures to the original compiled setup."""
import argparse
import hashlib
import json
import math
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--author-result', type=Path, required=True)
p.add_argument('--run', type=Path, required=True)
p.add_argument('--candidate', type=Path, required=True)
p.add_argument('--binding', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
run = a.run.resolve(strict=True)
author, candidate, binding = [x.resolve(strict=True) for x in (a.author_result, a.candidate, a.binding)]
files = [author, candidate, binding, run/'run-receipt.json', run/'result.json',
         run/'validator-state.jsonl', run/'aria-trace.jsonl', run/'s026-building-blocker-census.json']
pins = {str(f): sha(f) for f in files}
auth = json.loads(author.read_bytes())
receipt = json.loads(files[3].read_bytes())
result = json.loads(files[4].read_bytes())
validator = [json.loads(row) for row in files[5].read_text().splitlines()]
trace = [json.loads(row) for row in files[6].read_text().splitlines()]
census = json.loads(files[7].read_bytes())
expected_candidate = '0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61'
expected_binding = 'd44197811ecac5ccc94357bf6fc974abdd3beb3c9eb3c73337f88c32e4018e70'
if (pins[str(candidate)] != expected_candidate or pins[str(binding)] != expected_binding
        or auth.get('compiler_accepted') is not True or auth.get('catalog_id') != 'S026'
        or auth.get('declared_native_provider_context_matched') is not True
        or auth.get('resolved_setup', {}).get('setup_hash') != 116236341
        or auth.get('layout_fingerprint') != 'd2d1dbb4825bb8ba692d4ed7090de65299cda119cce9af46c869fcebb70fb5b9'
        or receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0
        or receipt.get('validation_passed') is not False or receipt.get('source_result') != result
        or auth['input_pins'].get(str(candidate), {}).get('sha256') != expected_candidate
        or auth['input_pins'].get(str(binding), {}).get('sha256') != expected_binding
        or receipt['input_and_runtime_pins'].get(str(candidate)) != expected_candidate
        or receipt['input_and_runtime_pins'].get(str(binding)) != expected_binding):
    raise ValueError('Exact compiled setup, input authority and completed diagnostic required')
if len(validator) != 3 or len(trace) != 2 or any(row['world_id'] != validator[0]['world_id'] for row in validator + trace):
    raise ValueError('Actual same-world two-tick diagnostic snapshots required')
if census.get('kind') != 'S026ActualSourceRegisteredBuildingBlockerCensus' or census.get('mission_ready') is not False:
    raise ValueError('Actual source registry census required')
owner_key = 'Game.Components.SkirmishAttemptOwnedComponent'
position_key = 'Unity.Transforms.LocalTransform'
actual = [row for row in validator[0]['actors'] if row.get('components', {}).get(owner_key, {}).get('IsStructure') == 1]
expected = auth['resolved_setup']['structures']
if len(expected) != 20 or len(actual) != 20 or len(census['buildings']) != 20:
    raise ValueError('Complete 20-row original structure domain required')
by_identity = {}
for actor in actual:
    owner = actor['components'][owner_key]
    key = owner['StableObjectId']
    if key in by_identity or owner['FactionId'] != actor['faction']:
        raise ValueError('Unique actual source-world stable identity/faction required')
    by_identity[key] = actor
rows = []
for structure in expected:
    stable = structure['structure_id'] + '.' + str(structure['faction_id'])
    actor = by_identity.pop(stable, None)
    if actor is None:
        raise ValueError('Compiled stable structure missing: ' + stable)
    position = actor['components'][position_key]['Position']
    dx = abs(position['x'] - structure['spawn_world_x'])
    dz = abs(position['z'] - structure['spawn_world_z'])
    if not all(math.isfinite(position[k]) for k in ('x', 'y', 'z')) or dx > .0001 or dz > .0001:
        raise ValueError('Compiled versus actual source-world opening position differs: ' + stable)
    rows.append({'stable_id': stable, 'entity': actor['entity'], 'faction': actor['faction'],
                 'compiled_x': structure['spawn_world_x'], 'compiled_z': structure['spawn_world_z'],
                 'actual_x': position['x'], 'actual_y': position['y'], 'actual_z': position['z'],
                 'absolute_x_delta': dx, 'absolute_z_delta': dz})
if by_identity or any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Extra structures or changed evidence')
report = {'kind': 'S026CompiledOpeningStructureSourceWorldReadback', 'input_sha256': pins,
          'world_id': validator[0]['world_id'], 'snapshot_tick': validator[0]['tick'],
          'compiled_setup_hash': 116236341, 'structure_count': len(rows), 'rows': rows,
          'registry_count': len(census['buildings']), 'registry_linkage_proven': False,
          'scope': 'Source-world compiled opening positions only; no live native source-fit or player readiness',
          'native_parity': False, 'source_fit_certified': False, 'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'structure_count': len(rows), 'snapshot_tick': report['snapshot_tick'],
                  'registry_linkage_proven': False, 'mission_ready': False}))
