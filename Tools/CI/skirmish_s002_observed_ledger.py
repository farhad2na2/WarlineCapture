#!/usr/bin/env python3
"""Check one observed queued production chain; no whole-mission qualification."""
import copy
import hashlib
import json
import pathlib
import sys

from trace_skirmish_production_events import trace


def require(condition, message):
    if not condition:
        raise ValueError(message)


def analyze(observed, result, terminal, run_receipt, request):
    require(not observed['observer_faults_or_gaps'], 'observer fault or gap')
    require(run_receipt['source_result'] == result and run_receipt['mission'] == request['mission'] == 'S002' and
            run_receipt['seed'] == request['seed'] == 104731, 'receipt/request subject or result differs')
    ticks = [x['snapshot_tick'] for x in observed['events']]
    require(ticks == sorted(ticks) and all(t <= terminal['tick'] for t in ticks), 'event snapshot order mismatch')
    session = terminal['session_outcome_facts']['Game.Components.SkirmishMatchState']['SessionId']
    events = [x['event'] for x in observed['events']]
    chain = [e for e in events if e['operation'] != 'DeathRelease']
    require(len(chain) >= 4 and [e['operation'] for e in chain[:3]] ==
            ['Queue', 'BindProducer', 'StartNativeQueue'] and
            all(e['operation'] == 'Deliver' for e in chain[3:]), 'unsupported chain/order')
    queue = chain[0]; before, after = queue['before'], queue['after']
    decision = queue['decision']; receipt = after['Reservation']
    require(queue['has_decision'] is True and decision['Accepted'] is True and
            before['HasReservation'] is False and after['HasReservation'] is True,
            'queue was not an accepted new reservation')
    require(receipt['ReservationId'] == decision['ReservationId'] > 0 and
            receipt['MemberCount'] == decision['MemberCount'] > 0 and
            receipt['MaterialsPaid'] == decision['MaterialsCost'] and
            receipt['DeliveredMembers'] == 0 and receipt['RemainingMembers'] == receipt['MemberCount'] and
            receipt['Phase']['name'] == 'Reserved', 'invalid queued reservation')
    amount = decision['MaterialsCost']
    require(before['Materials'] - after['Materials'] == amount and
            before['Fuel'] - after['Fuel'] == decision['FuelCost'] and
            after['Bank']['LifetimeProductionSpent'] - before['Bank']['LifetimeProductionSpent'] == amount and
            after['Bank']['LifetimeSpent'] - before['Bank']['LifetimeSpent'] == amount and
            before['Bank']['Current'] == before['Materials'] and after['Bank']['Current'] == after['Materials'],
            'queue debit mismatch')
    last = receipt; actors = set()
    for event in chain[1:]:
        a, b = event['before'], event['after']; current = b['Reservation']
        require(a['HasReservation'] is True and b['HasReservation'] is True and a['Reservation'] == last,
                'reservation continuity mismatch')
        require(a['Bank'] == b['Bank'] and a['Materials'] == b['Materials'] and a['Fuel'] == b['Fuel'],
                'unexpected resources changed within operation')
        expected = copy.deepcopy(last); op = event['operation']
        if op == 'BindProducer':
            require(last['ProducerRuntimeId'] == 0 and current['ProducerRuntimeId'] > 0, 'producer binding invalid')
            expected['ProducerRuntimeId'] = current['ProducerRuntimeId']
        elif op == 'StartNativeQueue':
            require(last['Phase']['name'] == 'Reserved' and current['Phase']['name'] == 'Producing', 'start phase invalid')
            expected['Phase'] = current['Phase']
        else:
            require(event['has_actor_owner'] is True, 'delivery actor missing')
            owner, actor = event['actor_owner'], event['actor']
            identity = (actor['index'], actor['version'])
            require(identity[0] > 0 and identity[1] > 0 and identity not in actors and
                    owner['SessionId'] == session and owner['ReservationId'] == last['ReservationId'] and owner['FactionId'] == last['FactionId'] and
                    owner['IsStructure'] == 0 and owner['CapacityReleased'] == 0, 'delivery ownership mismatch')
            actors.add(identity)
            expected['DeliveredMembers'] += 1
            require(expected['DeliveredMembers'] <= expected['MemberCount'], 'excess delivery')
            if last['ProductionGroupId'] == 0: expected['ProductionGroupId'] = current['ProductionGroupId']
            if expected['DeliveredMembers'] == expected['MemberCount']:
                require(current['Phase']['name'] == 'Live', 'completed delivery phase invalid')
                expected['Phase'] = current['Phase']
        require(current == expected, 'unexpected reservation transition')
        last = current
    require(len(actors) == last['MemberCount'] == result['actual_delivered_members'], 'incomplete delivered chain')
    final = result['source_reservations']
    require(len(final) == 1 and all(final[0].get(k) == (v['value'] if isinstance(v, dict) else v)
                                    for k, v in last.items()), 'final reservation differs')
    produced = {(x['Unit']['Index'], x['Unit']['Version']) for x in result['source_produced_units']}
    require(produced == actors and len(result['source_produced_units']) == len(actors), 'produced actor set differs')
    require(all(x['BuildingRuntimeId'] == last['ProducerRuntimeId'] and
                x['ProductionSlotBuildingRuntimeId'] == last['ProducerRuntimeId'] and
                x['HasOwnerFaction'] == 1 and x['OwnerFactionId'] == last['FactionId']
                for x in result['source_produced_units']), 'produced producer/faction differs')
    facts = terminal['session_outcome_facts']; match = facts['Game.Components.SkirmishMatchState']
    require(result['tick'] == terminal['tick'] and result['source_outcome'] == terminal['source_outcome'] == match['Outcome']['name'] and
            match['Phase']['name'] == 'Finished' and match['Seed'] == request['seed'] and
            facts['Game.Components.SkirmishExpandedSessionComponent']['CatalogId'] == request['mission'], 'terminal outcome mismatch')
    return {'status':'ObservedChainConsistent', 'event_count':len(events), 'deliveries':len(actors),
            'reservation_id':last['ReservationId'], 'observed_queue_material_debit':amount,
            'terminal_outcome':result['source_outcome'], 'source_sha256':observed['source_sha256'],
            'diagnostic_only':True, 'validation_passed':False, 'full_conservation_proved':False,
            'limitations':['Source-only recorded events; snapshot tick is not event time.',
              'DeathRelease rows are counted but not validated.', 'Rejected operations, pre-install startup, other economy and fuel history are not covered.',
              'Command receipts and rendered input are not evaluated here.',
              'Produced source key exposes length/empty status only; source role identity cannot be checked.',
              'Artifact hashes identify bytes only; no signed or independent capture authentication is asserted.',
              'Terminal SessionId and setup identity are not cross-checked against request or receipt.',
              'Observed completion is not a general liveness guarantee or native/player acceptance.']}


if __name__ == '__main__':
    try:
        p = pathlib.Path(sys.argv[1]); names = ['result.json','source-terminal.json','run-receipt.json','run-request.json']
        raw = {name:(p/name).read_bytes() for name in names}
        output = analyze(trace(p/'validator-state.jsonl'), *(json.loads(raw[name]) for name in names))
        output['artifact_sha256'] = {name:hashlib.sha256(data).hexdigest() for name,data in raw.items()}
        print(json.dumps(output, indent=2, sort_keys=True))
    except (OSError, ValueError, KeyError, TypeError, IndexError) as error:
        print('Unsupported or inconsistent: '+str(error), file=sys.stderr); sys.exit(1)
