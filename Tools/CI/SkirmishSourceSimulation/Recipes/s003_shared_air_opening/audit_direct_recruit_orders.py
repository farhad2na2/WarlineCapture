#!/usr/bin/env python3
"""Join paid delivery to actual selected-query and post-command world evidence."""
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


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = args.run.resolve(strict=True)
    paths = [root / name for name in ('run-receipt.json', 'result.json', 'aria-trace.jsonl', 'validator-state.jsonl')]
    pins = {str(path): sha(path) for path in paths}
    receipt, result = (json.loads(path.read_bytes()) for path in paths[:2])
    if (receipt.get('status') != 'BoundedPrefix' or result.get('status') != 'BoundedPrefix' or
            receipt.get('process_exit') != 0 or receipt.get('timed_out') is not False or
            receipt.get('validation_passed') is not False or receipt.get('source_result') != result):
        raise ValueError('Completed same-run unqualified diagnostic required')
    traces = [json.loads(line) for line in paths[2].read_text().splitlines()]
    snapshots = [json.loads(line) for line in paths[3].read_text().splitlines()]
    if not traces or not snapshots or any(row.get('logical_policy') != 'OriginalSharedAirOpeningSupplyAndGroundColumn'
                                           or row.get('native_rendered_input') is not False for row in traces):
        raise ValueError('Exact source shared-policy trace required')
    receipts = {row['receipt']['source_receipt_id'] for row in traces
                if row.get('action') == 'RecruitRifle' and isinstance(row.get('receipt'), dict)
                and row['receipt'].get('accepted') is True and row['receipt'].get('source_receipt_id') is not None}
    delivered = {f"{event['entity_index']}:{event['entity_version']}": event['receipt_id']
                 for event in result['source_delivery_events']
                 if event.get('kind') == 'unit_spawned_and_placed' and event.get('spawn_succeeded') is True
                 and event.get('receipt_id') in receipts}
    if not delivered:
        raise ValueError('Receipt-linked source delivery required')
    rows = []
    for command in traces:
        payload = command.get('receipt')
        if not isinstance(payload, dict) or payload.get('source_api') != 'AttackMoveSystem.IssueSelected' or payload.get('accepted') is not True:
            continue
        evidence = payload.get('validator_only_dispatch')
        if not isinstance(evidence, dict) or evidence.get('feeds_aria') is not False or evidence.get('accepted_member_count_available') is not False:
            raise ValueError('Explicit validator-only selected-query evidence required')
        before = {item['entity']: item['state'] for item in evidence['selected_query_before']}
        after = {item['entity']: item['state'] for item in evidence['selected_query_after']}
        later = [item for item in snapshots if item['tick'] > command['tick']]
        if not later:
            continue
        last = later[-1]
        actors = {item['entity']: item for item in last['actors']}
        for entity in sorted(set(delivered) & set(before) & set(after) & set(actors)):
            pre, post = before[entity], after[entity]
            if 'Game.Components.AttackMoveOrder' in pre or 'Game.Components.AttackMoveOrder' not in post:
                continue
            pre_position = pre.get('Unity.Transforms.LocalTransform', {}).get('Position')
            later_position = actors[entity]['components'].get('Unity.Transforms.LocalTransform', {}).get('Position')
            if pre_position is None or later_position is None:
                raise ValueError('Actual source position required')
            rows.append({'entity': entity, 'paid_receipt_id': delivered[entity], 'command_tick': command['tick'],
                         'aggregate_command_accepted': True, 'order_before': False, 'order_after': True,
                         'post_command_order': post['Game.Components.AttackMoveOrder'],
                         'later_sample_tick': last['tick'], 'position_before': pre_position,
                         'later_position': later_position, 'position_changed': pre_position != later_position})
    if not rows:
        raise ValueError('No delivered actor joined to an actual command and post-command order')
    if any(sha(Path(name)) != expected for name, expected in pins.items()):
        raise ValueError('Run evidence changed during audit')
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=False)
    report = {'kind': 'S003DirectRecruitOrderAudit', 'input_sha256': pins,
              'recruit_receipt_ids': sorted(receipts), 'delivered_count': len(delivered), 'actors': rows,
              'scope': 'Source-only partial shared planner; selected-query and post-command observations are not per-unit API acceptance counts or native input',
              'native_parity': False, 'mission_ready': False}
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'delivered': len(delivered), 'observed_post_orders': len(rows),
                      'position_changed': sum(row['position_changed'] for row in rows), 'mission_ready': False}))


if __name__ == '__main__':
    main()
