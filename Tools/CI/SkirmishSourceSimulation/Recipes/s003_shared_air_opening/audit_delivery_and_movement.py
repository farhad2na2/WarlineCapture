#!/usr/bin/env python3
"""Join actual shared-policy receipts, deliveries and sampled actor state."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    h=hashlib.sha256()
    with path.open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--run',type=Path,required=True);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
    root=a.run.resolve(strict=True);files=[root/name for name in ('run-receipt.json','result.json','aria-trace.jsonl','validator-state.jsonl','process.log')];pins={str(f):sha(f) for f in files}
    receipt=json.loads(files[0].read_bytes());result=json.loads(files[1].read_bytes())
    bounded=receipt.get('status')=='BoundedPrefix' and result.get('status')=='BoundedPrefix'
    terminal=(receipt.get('status')=='DiagnosticSourceOutcomeObserved' and result.get('status')=='SourceTerminal'
              and result.get('source_outcome') in ('Victory','Defeat','Draw') and receipt.get('full_outcome_observed') is True)
    if not (bounded or terminal) or receipt.get('process_exit')!=0 or receipt.get('timed_out') is not False or receipt.get('validation_passed') is not False or receipt.get('source_result')!=result:raise ValueError('Completed unqualified same-run receipt required')
    traces=[json.loads(line) for line in files[2].read_text().splitlines()];snapshots=[json.loads(line) for line in files[3].read_text().splitlines()]
    if not traces or any(t.get('logical_policy')!='OriginalSharedAirOpeningSupplyAndGroundColumn' or t.get('native_rendered_input') is not False for t in traces):raise ValueError('Exact shared policy trace required')
    recruit=[t for t in traces if t.get('action')=='RecruitRifle' and isinstance(t.get('receipt'),dict) and t['receipt'].get('accepted') is True]
    moves=[t for t in traces if t.get('action')=='GroundObjective_PublicTerrainApproach' and isinstance(t.get('receipt'),dict) and t['receipt'].get('accepted') is True and t['receipt'].get('source_api')=='AttackMoveSystem.IssueSelected']
    if not recruit or not moves:raise ValueError('Actual accepted shared-policy recruitment and movement required')
    ids={int(t['receipt']['source_receipt_id']) for t in recruit if t['receipt'].get('source_receipt_id') is not None};events=[e for e in result['source_delivery_events'] if e.get('kind')=='unit_spawned_and_placed' and e.get('receipt_id') in ids and e.get('spawn_succeeded') is True]
    if not ids or not events:raise ValueError('Actual receipt-linked delivery events required')
    move=moves[0];before=max((s for s in snapshots if s['tick']<move['tick']),key=lambda x:x['tick']);after=min((s for s in snapshots if s['tick']>move['tick']),key=lambda x:x['tick']);last=snapshots[-1]
    maps=[{row['entity']:row for row in snapshot['actors']} for snapshot in (before,after,last)];delivered=[];moved_selected=[]
    def position(actor):return actor['components'].get('Unity.Transforms.LocalTransform',{}).get('Position')
    for event in events:
        entity=str(event['entity_index'])+':'+str(event['entity_version']);at=maps[1].get(entity);end=maps[2].get(entity);components=at['components'] if at else {}
        delivered.append({'entity':entity,'receipt_id':event['receipt_id'],'observed_after_command':at is not None,'selected_after_command':'Game.Components.SelectedUnitTag' in components,'attack_move_after_command':'Game.Components.AttackMoveOrder' in components,'position_changed_after_command':bool(at and end and position(at)!=position(end))})
    # A recruited group can receive its order on a later roster page. Join each
    # accepted command to the next sampled world state, not just the first one.
    delivered_move_samples=[]
    examined_intervals=set()
    for command in moves:
        preceding=[s for s in snapshots if s['tick']<command['tick']]
        subsequent=[s for s in snapshots if s['tick']>command['tick']]
        if not preceding or not subsequent: continue
        interval=(preceding[-1]['tick'], subsequent[0]['tick'])
        if interval in examined_intervals: continue
        examined_intervals.add(interval)
        candidate_ticks=[item['tick'] for item in moves if interval[0] < item['tick'] < interval[1]]
        before_actors={actor['entity']:actor for actor in preceding[-1]['actors']}
        first=subsequent[0]
        first_actors={actor['entity']:actor for actor in first['actors']}
        last_actors={actor['entity']:actor for actor in snapshots[-1]['actors']}
        for event in events:
            entity=str(event['entity_index'])+':'+str(event['entity_version'])
            actor=first_actors.get(entity); end=last_actors.get(entity)
            prior=before_actors.get(entity)
            if not actor or not end or not prior: continue
            order=actor['components'].get('Game.Components.AttackMoveOrder')
            prior_order=prior['components'].get('Game.Components.AttackMoveOrder')
            if order is not None and order!=prior_order and position(actor)!=position(end):
                delivered_move_samples.append({'entity':entity,'receipt_id':event['receipt_id'],
                    'candidate_command_ticks':candidate_ticks,'sample_tick':first['tick'],
                    'last_sample_tick':snapshots[-1]['tick'],'attack_move':order,
                    'position_after_command':position(actor),'position_at_last_sample':position(end)})
    for entity,actor in maps[1].items():
        components=actor['components'];end=maps[2].get(entity)
        if actor.get('faction')==1 and end and 'Game.Components.SelectedUnitTag' in components and 'Game.Components.AttackMoveOrder' in components and position(actor)!=position(end):
            moved_selected.append({'entity':entity,'position_after_command':position(actor),'position_at_last_sample':position(end),'attack_move':components['Game.Components.AttackMoveOrder']})
    if any(sha(Path(name))!=digest for name,digest in pins.items()):raise ValueError('Run evidence changed during audit')
    out=a.output.resolve();out.parent.mkdir(parents=True,exist_ok=False)
    report={'kind':'S003SharedPolicyDeliveryAndMovementAudit','input_sha256':pins,'recruit_receipt_ids':sorted(ids),'receipt_linked_delivery_count':len(events),
            'accepted_move_tick':move['tick'],'accepted_move_receipt':move['receipt'],'sample_ticks':[before['tick'],after['tick'],last['tick']],
            'delivered_units':delivered,'selected_attack_move_units_with_position_change':moved_selected,
            'delivered_move_samples':delivered_move_samples,
            'delivered_unit_move_observed':bool(delivered_move_samples),
            'scope':'Partial shared gameplay policies in source host; sampled state is not full planner priority, native input, physical fit or readiness',
            'full_native_planner_parity':False,'mission_ready':False}
    out.write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'kind':report['kind'],'paid_receipts':sorted(ids),'delivered':len(events),'accepted_move_tick':move['tick'],'sampled_selected_moved':len(moved_selected),'delivered_unit_move_observed':report['delivered_unit_move_observed'],'mission_ready':False}))


if __name__=='__main__':main()
