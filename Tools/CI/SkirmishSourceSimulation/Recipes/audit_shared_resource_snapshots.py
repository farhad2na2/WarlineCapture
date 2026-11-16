"""Cross-check sampled shared-validator bank reads against observed production events."""
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
p.add_argument('--validator-source', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
run, source = a.run.resolve(strict=True), a.validator_source.resolve(strict=True)
files = [run/n for n in ('run-receipt.json', 'result.json', 'validator-state.jsonl',
                         's026-production-ledger.jsonl', 's026-production-ledger-status.json')] + [source, Path(__file__).resolve()]
pins = {str(f): sha(f) for f in files}
receipt, result = [json.loads(f.read_bytes()) for f in files[:2]]
snapshots = [json.loads(x) for x in files[2].read_text().splitlines()]
events = [json.loads(x) for x in files[3].read_text().splitlines()]
status = json.loads(files[4].read_bytes())
if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0
        or receipt.get('validation_passed') is not False or receipt.get('source_result') != result
        or result.get('full_mission_ready') is not False or len(snapshots) < 2
        or status.get('observation_faults') != 0 or status.get('events') != len(events)
        or receipt['input_and_runtime_pins'].get(str(source)) != pins[str(source)]):
    raise ValueError('Completed source-only sampled run and pinned validator required')
world = snapshots[0]['world_id']
ticks = [x['tick'] for x in snapshots]
if ticks != sorted(set(ticks)) or any(x['world_id'] != world for x in snapshots + events) or status.get('world_id') != world:
    raise ValueError('Unique ordered same-world snapshot and observer records required')
if any(event['tick'] in ticks for event in events):
    raise ValueError('Event and snapshot in same tick have ambiguous ordering')
rows = []
for snapshot in snapshots:
    resource = snapshot.get('resource_accounting') or {}
    if (resource.get('kind') != 'SourceValidatorResourceSnapshot' or resource.get('available') is not True
            or resource.get('feeds_aria') is not False or resource.get('full_conservation_proven') is not False
            or resource.get('shared_materials_initialized') is not True
            or resource.get('native_parity') is not False or resource.get('mission_ready') is not False):
        raise ValueError('Actual unqualified shared-resource snapshot required')
    factions = {x['faction']: x for x in resource['factions']}
    if set(factions) != {1, 2} or any(factions[f]['materials_bank']['Current'] != factions[f]['materials'] for f in factions):
        raise ValueError('Actual source owner reads and bank components disagree')
    rows.append(factions)
intervals = []
for index in range(1, len(snapshots)):
    lo, hi = ticks[index-1], ticks[index]
    included = [x for x in events if lo < x['tick'] < hi]
    for faction in (1, 2):
        actual = rows[index][faction]['materials'] - rows[index-1][faction]['materials']
        observed = sum(x['after']['materials'] - x['before']['materials'] for x in included if x['before']['faction'] == faction)
        intervals.append({'from_tick': lo, 'to_tick': hi, 'faction': faction,
                          'sampled_material_delta': actual, 'observed_production_delta': observed,
                          'sample_matches_observed_production_delta': actual == observed,
                          'observer_events_in_interval': sum(x['before']['faction'] == faction for x in included)})
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Evidence changed during snapshot audit')
report = {'kind': 'SharedSourceResourceSnapshotsVsProductionObserver', 'input_sha256': pins,
          'world_id': world, 'snapshot_count': len(snapshots), 'observer_events': len(events),
          'intervals': intervals, 'all_sampled_material_deltas_match_observed_production': all(x['sample_matches_observed_production_delta'] for x in intervals),
          'scope': 'Sampled source bank deltas compared with production observations only; other economy events may exist',
          'full_resource_conservation_proven': False, 'native_parity': False, 'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'snapshot_count': len(snapshots), 'observer_events': len(events),
                  'matching_intervals': sum(x['sample_matches_observed_production_delta'] for x in intervals),
                  'intervals': len(intervals), 'full_resource_conservation_proven': False}))
