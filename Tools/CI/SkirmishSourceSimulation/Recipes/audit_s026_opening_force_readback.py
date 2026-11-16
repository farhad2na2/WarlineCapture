"""Compare actual same-world opening roster counts with original compiled forces."""
import argparse
import collections
import hashlib
import json
import re
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
         run/'validator-state.jsonl', run/'aria-trace.jsonl']
pins = {str(f): sha(f) for f in files}
auth, receipt, result = [json.loads(f.read_bytes()) for f in (files[0], files[3], files[4])]
validator = [json.loads(row) for row in files[5].read_text().splitlines()]
trace = [json.loads(row) for row in files[6].read_text().splitlines()]
candidate_sha = '0a412bf51e718e3fba00d1a89f3699a9b8959c200933eb8d20c097fc0db86f61'
binding_sha = 'd44197811ecac5ccc94357bf6fc974abdd3beb3c9eb3c73337f88c32e4018e70'
if (pins[str(candidate)] != candidate_sha or pins[str(binding)] != binding_sha
        or auth.get('compiler_accepted') is not True or auth.get('catalog_id') != 'S026'
        or auth.get('declared_native_provider_context_matched') is not True
        or auth.get('resolved_setup', {}).get('setup_hash') != 116236341
        or receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0
        or receipt.get('validation_passed') is not False or receipt.get('source_result') != result
        or auth['input_pins'].get(str(candidate), {}).get('sha256') != candidate_sha
        or auth['input_pins'].get(str(binding), {}).get('sha256') != binding_sha
        or receipt['input_and_runtime_pins'].get(str(candidate)) != candidate_sha
        or receipt['input_and_runtime_pins'].get(str(binding)) != binding_sha):
    raise ValueError('Exact compiled setup, pinned inputs and completed diagnostic required')
if len(validator) != 3 or len(trace) != 2 or any(row['world_id'] != validator[0]['world_id'] for row in validator + trace):
    raise ValueError('Actual same-world two-tick diagnostic snapshots required')
forces = auth['resolved_setup']['forces']
expected = {}
for row in forces:
    key = (row['faction_id'], row['role_id'])
    if key in expected or key[0] not in (1, 2) or not re.fullmatch(r'role\.[a-z0-9_]+', key[1]) or type(row['quantity']) is not int or row['quantity'] < 1:
        raise ValueError('Unique compiled faction/role force rows required')
    expected[key] = row['quantity']
owner_key = 'Game.Components.SkirmishAttemptOwnedComponent'
actual = collections.Counter()
identities = set()
entities = set()
for actor in validator[0]['actors']:
    owner = actor.get('components', {}).get(owner_key)
    if owner is None or owner.get('IsStructure') != 0:
        continue
    stable = owner.get('StableObjectId', '')
    matches = [key for key in expected if stable.startswith(key[1] + '.' + str(key[0]) + '.')]
    if (len(matches) != 1 or stable in identities or actor['entity'] in entities
            or owner.get('FactionId') != actor.get('faction') or owner.get('FactionId') != matches[0][0]
            or not re.fullmatch(r'\d+\.\d+', stable[len(matches[0][1]) + len(str(matches[0][0])) + 2:])):
        raise ValueError('Unique actual source-world force identity and faction required: ' + stable)
    identities.add(stable)
    entities.add(actor['entity'])
    actual[matches[0]] += 1
if dict(actual) != expected:
    raise ValueError('Actual source-world roster differs from compiled force rows')
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Input evidence changed during force audit')
rows = [{'faction': faction, 'role': role, 'compiled_quantity': quantity,
         'actual_quantity': actual[(faction, role)]} for (faction, role), quantity in sorted(expected.items())]
report = {'kind': 'S026CompiledOpeningForceSourceWorldReadback', 'input_sha256': pins,
          'world_id': validator[0]['world_id'], 'snapshot_tick': validator[0]['tick'],
          'compiled_setup_hash': 116236341, 'force_rows': rows, 'actual_unit_count': sum(actual.values()),
          'scope': 'Opening source-world role and faction counts only; no resource conservation or native acceptance',
          'resource_conservation_proven': False, 'native_parity': False, 'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'actual_unit_count': report['actual_unit_count'], 'force_rows': len(rows),
                  'resource_conservation_proven': False, 'mission_ready': False}))
