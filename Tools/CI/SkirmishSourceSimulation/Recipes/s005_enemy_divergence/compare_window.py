#!/usr/bin/env python3
"""Compare bounded read-only S005 enemy-phase source traces."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[5]
LOGS = ROOT / 'T Logs/SkirmishProduction'
PAIR = LOGS / 's005-enemy-window-pair-20261106-T1'

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def differences(a, b, path='$'):
    if type(a) is not type(b): return [{'path': path, 'off': a, 'on': b}]
    if isinstance(a, dict):
        out=[]
        for key in sorted(set(a)|set(b)):
            if key not in a or key not in b: out.append({'path':path+'.'+key,'off':a.get(key),'on':b.get(key)})
            else: out.extend(differences(a[key],b[key],path+'.'+key))
        return out
    if isinstance(a, list):
        if len(a)!=len(b): return [{'path':path+'.length','off':len(a),'on':len(b)}]
        return [v for i,(x,y) in enumerate(zip(a,b)) for v in differences(x,y,path+'['+str(i)+']')]
    return [] if a == b else [{'path':path,'off':a,'on':b}]

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repeat-off',action='store_true')
    options=parser.parse_args()
    pair_dir=LOGS/'s005-enemy-window-off-repeat-pair-20261106-T1' if options.repeat_off else PAIR
    expected_modes=['off_a','off_b'] if options.repeat_off else ['off','on']
    receipt=pair_dir/'receipt.json';pair=json.loads(receipt.read_bytes())
    if pair.get('status')!='CompletedBoth' or [x['mode'] for x in pair['modes']]!=expected_modes:
        raise ValueError('Complete same-binary pair required')
    binary=Path(pair['binary'])
    if sha(binary)!=pair['binary_sha256']:raise ValueError('Binary changed')
    paths=[Path(x['output'])/'enemy-phase-window.jsonl' for x in pair['modes']]
    for p,mode in zip(paths,pair['modes']):
        if sha(p)!=mode.get('enemy-phase-window.jsonl_sha256'):raise ValueError('Captured window changed')
    windows=[[json.loads(x) for x in p.read_bytes().splitlines()] for p in paths]
    def find_metrics(value):
        if isinstance(value,dict):
            for key,child in value.items():
                if key=='observation_scope_metrics':yield child
                else:yield from find_metrics(child)
        elif isinstance(value,list):
            for child in value:yield from find_metrics(child)
    metrics=[list(find_metrics(json.loads((Path(m['output'])/'source-result.json').read_bytes()))) for m in pair['modes']]
    if any(len(x)!=1 for x in metrics) or metrics[0][0].get('enabled') is not False or metrics[1][0].get('enabled') is not (not options.repeat_off):
        raise ValueError('Observed scope modes do not match the requested pair')
    if metrics[0][0].get('scopes') != 0 or metrics[1][0].get('scopes') != (0 if options.repeat_off else 1200):
        raise ValueError('Unexpected source observation scope counts')
    keys=[(r['tick'],r['phase']) for r in windows[0]]
    expected=[(t,phase) for t in range(1080,1181) for phase in ('before','after')]
    if keys!=expected or [(r['tick'],r['phase']) for r in windows[1]]!=expected:
        raise ValueError('Complete ordered source trace windows required')
    first=None
    for a,b in zip(*windows):
        diff=differences(a,b)
        if diff:
            first={'tick':a['tick'],'phase':a['phase'],'difference_count':len(diff),'differences':diff[:100]};break
    report={'status':'ComparedSourceEnemyPhaseWindow','pair_receipt_sha256':sha(receipt),
            'binary_sha256':sha(binary),'window_sha256':[sha(p) for p in paths],
            'rows_per_mode':len(keys),'observed_scope_metrics':[x[0] for x in metrics], 'first_recorded_difference':first,
            'scope':'Selected actual source ECS components around enemy update; instrumentation itself can affect host timing, no native or full-world parity',
            'causal_attribution':'Unproven','candidate_promoted':False,'mission_ready':False}
    out=pair_dir/'enemy-window-comparison.json'
    with out.open('x') as f: json.dump(report,f,indent=2);f.write('\n')
    print(json.dumps(report))

if __name__=='__main__': main()
