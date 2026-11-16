"""Compare recorded diagnostic semantics; does not certify unrecorded world state."""
import argparse
import hashlib
import json
from pathlib import Path


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('baseline',type=Path)
    parser.add_argument('cached',type=Path)
    parser.add_argument('--output',type=Path,required=True)
    args=parser.parse_args()
    a,b=(json.loads((p/'receipt.json').read_text()) for p in (args.baseline,args.cached))
    if any(x.get('status')!='ObservedDiagnosticSourcePlacement' or x.get('mission_ready') is not False for x in (a,b)):
        raise ValueError('Two completed diagnostic receipts required')
    if a.get('setup_cache_scope') is not None or b.get('setup_cache_scope')!='ExperimentalSourceOnly_PerCallFileHashAndFullLiveComparison':
        raise ValueError('Exact baseline and cache provenance required')
    ignored=[];differences=[]
    def compare(left,right,path=''):
        if isinstance(left,dict) and isinstance(right,dict):
            for key in sorted(set(left)|set(right)):
                child=path+'/'+key
                if key in ('advance_wall_seconds','phase_wall_seconds','owner_phase_totals_seconds','enemy_subcall_timing_seconds','wall_seconds','progress_path','world_id','cached_guard_differential_controls_passed'):
                    ignored.append(child);continue
                if key not in left or key not in right:
                    differences.append({'path':child,'reason':'missing'});continue
                compare(left[key],right[key],child)
        elif isinstance(left,list) and isinstance(right,list):
            if len(left)!=len(right):differences.append({'path':path,'reason':'length'})
            else:
                for i,(x,y) in enumerate(zip(left,right)):compare(x,y,path+'/'+str(i))
        elif left!=right:differences.append({'path':path,'baseline':left,'cached':right})
    compare(a['result'],b['result'])
    progress=[p/'source-progress.jsonl' for p in (args.baseline,args.cached)]
    progress_equal=progress[0].read_bytes()==progress[1].read_bytes()
    report={'status':'MatchedRecordedSubset' if not differences and progress_equal else 'Different',
            'baseline_receipt_sha256':digest(args.baseline/'receipt.json'),'cached_receipt_sha256':digest(args.cached/'receipt.json'),
            'progress_sha256':[digest(p) for p in progress],'progress_bytes_equal':progress_equal,
            'ignored_paths':ignored,'differences':differences,
            'scope':'RecordedDiagnosticFieldsAndProgressOnly_NotFullStateOrNativeParity',
            'full_simulator_parity':False,'mission_ready':False}
    args.output.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'status':report['status'],'differences':len(differences),'progress_bytes_equal':progress_equal}))
    if report['status']!='MatchedRecordedSubset':raise SystemExit(1)


if __name__=='__main__':main()
