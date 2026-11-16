#!/usr/bin/env python3
"""Join an observed approach refusal to the same run's real deliveries and selection."""
import argparse
import hashlib
import json
from pathlib import Path


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--run', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    root = args.run.resolve(strict=True)
    paths = [root / name for name in ('run-receipt.json', 'result.json', 'aria-trace.jsonl', 'validator-state.jsonl')]
    pins = {str(path): digest(path) for path in paths}
    receipt, result = (json.loads(path.read_bytes()) for path in paths[:2])
    if (receipt.get('status') != 'BoundedPrefix' or receipt.get('process_exit') != 0 or
            receipt.get('timed_out') is not False or receipt.get('validation_passed') is not False or
            receipt.get('source_result') != result):
        raise ValueError('Completed same-run diagnostic receipt required')
    traces = [json.loads(line) for line in paths[2].read_text().splitlines()]
    observations = [row for row in traces if row.get('action') == 'GroundObjective_InspectObjective'
                    and isinstance(row.get('observation', {}).get('selected_approach'), dict)]
    if not observations:
        raise ValueError('Actual selected-approach observation required')
    observed = observations[0]
    projection = observed['observation']['selected_approach']
    if (not projection.get('Required') or not projection.get('ContextKnown') or
            projection.get('StructureCapableCount') != 0 or projection.get('LegalGoalAvailable') or
            projection.get('UnavailableReason') != 'Selected fighters cannot attack structures.'):
        raise ValueError('Actual structure-capability refusal required')
    recruits = {row['receipt']['source_receipt_id'] for row in traces
                if row.get('action') == 'RecruitRifle' and isinstance(row.get('receipt'), dict)
                and row['receipt'].get('accepted') is True and row['receipt'].get('source_receipt_id') is not None}
    delivered = {f"{event['entity_index']}:{event['entity_version']}": event['receipt_id']
                 for event in result['source_delivery_events']
                 if event.get('kind') == 'unit_spawned_and_placed' and event.get('spawn_succeeded') is True
                 and event.get('receipt_id') in recruits}
    snapshots = [json.loads(line) for line in paths[3].read_text().splitlines()]
    sample = min((row for row in snapshots if row['tick'] > observed['tick']), key=lambda row: row['tick'])
    selected = [row for row in sample['actors']
                if 'Game.Components.SelectedUnitTag' in row.get('components', {})]
    joined = [{'entity': row['entity'], 'receipt_id': delivered[row['entity']]}
              for row in selected if row['entity'] in delivered]
    if len(selected) != projection['SelectedCount'] or len(joined) != len(selected) or not joined:
        raise ValueError('Sampled selected actors must all join actual paid deliveries')
    if any(digest(Path(name)) != value for name, value in pins.items()):
        raise ValueError('Evidence changed during audit')
    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=False)
    report = {'kind': 'S003RifleAssaultStall', 'input_sha256': pins,
              'observation_tick': observed['tick'], 'sample_tick': sample['tick'],
              'projection': projection, 'selected_delivered_actors': joined,
              'scope': 'Source-only partial shared planner and sampled actual world; no native parity or readiness',
              'mission_ready': False}
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'observation_tick': observed['tick'], 'sample_tick': sample['tick'],
                      'selected_delivered': len(joined), 'mission_ready': False}))


if __name__ == '__main__':
    main()
