"""Audit actual source-world S026 stock reads without claiming full conservation."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--run', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
run = a.run.resolve(strict=True)
files = [run/n for n in ('run-receipt.json', 'result.json', 's026-resource-census-startup.json',
                           's026-resource-census-after.json', 'validator-state.jsonl', 'aria-trace.jsonl')]
pins = {str(f): sha(f) for f in files}
receipt, result, start, after = [json.loads(f.read_bytes()) for f in files[:4]]
validator = [json.loads(x) for x in files[4].read_text().splitlines()]
trace = [json.loads(x) for x in files[5].read_text().splitlines()]
if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0
        or receipt.get('validation_passed') is not False or receipt.get('source_result') != result
        or len(validator) != 3 or len(trace) != 2
        or any(x['world_id'] != start['world_id'] for x in validator + trace)
        or start.get('kind') != 'S026ActualSourceResourceCensus' or after.get('kind') != start['kind']
        or start.get('stage') != 'startup' or after.get('stage') != 'after'
        or start['world_id'] != after['world_id'] or start['tick'] >= after['tick']
        or start.get('shared_supply_initialized') is not True or after.get('shared_supply_initialized') is not True
        or start.get('mission_ready') is not False or after.get('mission_ready') is not False):
    raise ValueError('Exact completed same-world diagnostic census required')
compiled = start['compiled_start']
if compiled != after['compiled_start']:
    raise ValueError('Compiled source setup changed during bounded run')
for faction, key in ((1, 'player_stock'), (2, 'enemy_stock')):
    stock = start[key]
    if (stock['Materials'] != compiled['materials_each'] or stock['Oil'] != compiled['oil_each']
            or stock['Fuel'] != compiled['usable_fuel_each']
            or start['owner_read_oil'][faction-1] != compiled['oil_each']
            or start['owner_read_fuel'][faction-1] != compiled['usable_fuel_each']):
        raise ValueError('Actual starting owner resources differ from compiled setup')
if len(start['storage_entities']) != len(after['storage_entities']):
    raise ValueError('Storage entity census changed during bounded readback')
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Census evidence changed during audit')
report = {'kind': 'S026SourceStartingResourceCensusAudit', 'input_sha256': pins,
          'world_id': start['world_id'], 'startup_tick': start['tick'], 'after_tick': after['tick'],
          'compiled_start': compiled, 'starting_stock_matches_compiled_both_factions': True,
          'starting_original_owner_oil_fuel_match_compiled_both_factions': True,
          'storage_entity_count': len(start['storage_entities']),
          'after_player_stock': after['player_stock'], 'after_enemy_stock': after['enemy_stock'],
          'after_owner_read_oil': after['owner_read_oil'], 'after_owner_read_fuel': after['owner_read_fuel'],
          'scope': 'Two source snapshots only; no transaction ledger, full conservation or native acceptance',
          'full_resource_conservation_proven': False, 'native_parity': False, 'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'startup_tick': start['tick'], 'after_tick': after['tick'],
                  'compiled_start': compiled, 'mission_ready': False}))
