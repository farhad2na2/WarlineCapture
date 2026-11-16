"""Compare original validator snapshots; only the host-generated world ID is ignored."""
import argparse
import hashlib
import json
from pathlib import Path


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('baseline',type=Path);parser.add_argument('cached',type=Path)
    parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    receipts=[json.loads((p/'receipt.json').read_text()) for p in (args.baseline,args.cached)]
    files=[p/'source-validator-snapshots.jsonl' for p in (args.baseline,args.cached)]
    if any(r.get('status')!='ObservedDiagnosticSourcePlacement' or r.get('mission_ready') is not False or
           r.get('validator_snapshots_sha256')!=sha(p) for r,p in zip(receipts,files)):
        raise ValueError('Completed source receipts must pin the actual snapshots')
    if receipts[0].get('setup_cache_scope') is not None or receipts[1].get('setup_cache_scope')!='ExperimentalSourceOnly_PerCallFileHashAndFullLiveComparison':
        raise ValueError('Baseline and opt-in cache provenance required')
    rows=[[json.loads(line) for line in p.read_text().splitlines()] for p in files]
    if len(rows[0])!=len(rows[1]) or not rows[0]:raise ValueError('Matching nonempty snapshot sequences required')
    diffs=[]
    for index,(left,right) in enumerate(zip(*rows)):
        for row in (left,right):
            if row.get('kind')!='ValidatorOnlyActualMissionState' or row.get('feeds_aria') is not False or not row.get('world_id'):
                raise ValueError('Original validator snapshot required')
            del row['world_id']
        if left!=right:diffs.append({'row':index,'tick_baseline':left.get('tick'),'tick_cached':right.get('tick')})
    report={'status':'MatchedSampledValidatorFields' if not diffs else 'Different',
            'receipt_sha256':[sha(p/'receipt.json') for p in (args.baseline,args.cached)],
            'snapshot_sha256':[sha(p) for p in files], 'samples':len(rows[0]),
            'sample_ticks':[r['tick'] for r in rows[0]],'first_actor_count':rows[0][0]['owned_count'],
            'last_actor_count':rows[0][-1]['owned_count'],'ignored_fields':['world_id'],
            'different_samples':diffs,'scope':'OriginalValidatorSelectedActorSessionAndReservationFields_NotFullWorldOrNative',
            'full_world_parity':False,'mission_ready':False}
    args.output.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'status':report['status'],'samples':report['samples'],'different_samples':len(diffs)}))
    if diffs:raise SystemExit(1)


if __name__=='__main__':main()
