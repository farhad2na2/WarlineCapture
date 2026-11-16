"""Audit the observed S026 source-owner production debit, not full conservation."""
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
p.add_argument('--production-source', type=Path, required=True)
p.add_argument('--output', type=Path, required=True)
a = p.parse_args()
run, source = a.run.resolve(strict=True), a.production_source.resolve(strict=True)
files = [run/n for n in ('run-receipt.json', 'result.json', 's026-resource-census-startup.json',
                         's026-resource-census-after.json', 's026-production-ledger.jsonl',
                         's026-production-ledger-status.json', 'validator-state.jsonl', 'aria-trace.jsonl')] + [source, Path(__file__).resolve()]
pins = {str(f): sha(f) for f in files}
receipt, result, start, after = [json.loads(f.read_bytes()) for f in files[:4]]
events = [json.loads(x) for x in files[4].read_text().splitlines()]
status = json.loads(files[5].read_bytes())
validator = [json.loads(x) for x in files[6].read_text().splitlines()]
trace = [json.loads(x) for x in files[7].read_text().splitlines()]
world = start['world_id']
if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0
        or receipt.get('timed_out') is not False or receipt.get('validation_passed') is not False
        or receipt.get('source_result') != result or result.get('source_outcome') != 'None'
        or result.get('full_mission_ready') is not False or start.get('stage') != 'startup'
        or after.get('stage') != 'after' or start['world_id'] != after['world_id']
        or status.get('world_id') != world or status.get('observation_faults') != 0
        or status.get('events') != len(events) or len(events) != 4
        or not validator or not trace or any(x['world_id'] != world for x in validator + trace + events)
        or receipt['input_and_runtime_pins'].get(str(source)) != pins[str(source)]):
    raise ValueError('Completed same-world original source observation and pinned owner required')
if [x['operation'] for x in events] != ['Queue', 'BindProducer', 'StartNativeQueue', 'Deliver']:
    raise ValueError('Actual original production sequence changed')
queue = events[0]
before, after_queue = queue['before'], queue['after']
decision = queue['decision']
if (queue.get('has_decision') is not True or decision.get('Accepted') is not True
        or before['faction'] != 2 or after_queue['faction'] != 2
        or before['bank_index'] != after_queue['bank_index'] or before['bank_version'] != after_queue['bank_version']
        or before['materials'] != before['bank']['Current'] or after_queue['materials'] != after_queue['bank']['Current']
        or before['materials'] - after_queue['materials'] != decision['MaterialsCost']
        or after_queue['bank']['LifetimeProductionSpent'] - before['bank']['LifetimeProductionSpent'] != decision['MaterialsCost']
        or not after_queue['has_reservation'] or after_queue['reservation']['ReservationId'] != decision['ReservationId']
        or after_queue['reservation']['MaterialsPaid'] != decision['MaterialsCost']
        or after_queue['reservation']['FactionId'] != 2):
    raise ValueError('Observed source queue debit and actual reservation disagree')
rid = decision['ReservationId']
if any(not x['after']['has_reservation'] or x['after']['reservation']['ReservationId'] != rid for x in events):
    raise ValueError('Original observer reservation continuity differs')
deliveries = result['source_delivery_events']
if (len(deliveries) != 2 or [x['kind'] for x in deliveries] != ['queue_accepted', 'unit_spawned_and_placed']
        or any(x['receipt_id'] != rid for x in deliveries) or deliveries[1]['spawn_succeeded'] is not True
        or events[-1]['actor'] != str(deliveries[1]['entity_index']) + ':' + str(deliveries[1]['entity_version'])
        or result.get('actual_delivered_members') != 1):
    raise ValueError('Actual production and source delivery records disagree')
if (start.get('shared_materials_initialized') is not True or after.get('shared_materials_initialized') is not True
        or start['owner_read_materials'] != [900, 900] or after['owner_read_materials'] != [900, 760]
        or after['enemy_stock']['Materials'] != 900 or after['materials_banks'][1]['component']['Current'] != 760
        or after['materials_banks'][1]['component']['LifetimeProductionSpent'] != 140):
    raise ValueError('Actual source bank/readback or compatibility mirror differs')
if any(sha(Path(name)) != digest for name, digest in pins.items()):
    raise ValueError('Ledger evidence changed during audit')
report = {'kind': 'S026OriginalProductionDebitReconciliation', 'input_sha256': pins,
          'world_id': world, 'queue_tick': queue['tick'], 'delivery_tick': events[-1]['tick'],
          'reservation_id': rid, 'faction': 2, 'unit_prefab': deliveries[1]['unit_prefab'],
          'materials_before': before['materials'], 'materials_paid': decision['MaterialsCost'],
          'materials_after': after_queue['materials'], 'final_owner_materials': after['owner_read_materials'],
          'compatibility_enemy_stock_materials': after['enemy_stock']['Materials'],
          'observer_events': len(events), 'observer_faults': status['observation_faults'],
          'scope': 'One observed source production transaction, not total resource conservation or native acceptance',
          'full_resource_conservation_proven': False, 'native_parity': False, 'mission_ready': False}
with a.output.open('x') as stream:
    json.dump(report, stream, indent=2)
    stream.write('\n')
print(json.dumps({'reservation_id': rid, 'materials_before': before['materials'],
                  'materials_paid': decision['MaterialsCost'], 'materials_after': after_queue['materials'],
                  'observer_faults': status['observation_faults'], 'mission_ready': False}))
