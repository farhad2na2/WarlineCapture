#!/usr/bin/env python3
"""Compare source-only path budget completions and coarse actor samples."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
PAIR=ROOT/'T Logs/SkirmishProduction/s005-early-path-window-pair-20261106-T1'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--repeat-off',action='store_true');options=parser.parse_args()
    pair_dir=PAIR.parent/'s005-early-path-window-off-repeat-pair-20261106-T1' if options.repeat_off else PAIR
    receipt=pair_dir/'receipt.json';pair=json.loads(receipt.read_bytes())
    expected_modes=['off_a','off_b'] if options.repeat_off else ['off','on']
    if pair.get('status')!='CompletedBoth' or [x['mode'] for x in pair['modes']]!=expected_modes:
        raise ValueError('Complete controlled pair required')
    binary=Path(pair['binary'])
    if sha(binary)!=pair['binary_sha256']:raise ValueError('Instrumented binary changed')
    modes=pair['modes'];dirs=[Path(x['output']) for x in modes];pins=[];paths=[];actors=[];budget=[]
    for directory,mode in zip(dirs,modes):
        for name in ('run-receipt.json','enemy-phase-window.jsonl','path-budget-completions.jsonl','path-budget-window.json'):
            p=directory/name
            if sha(p)!=mode.get(name+'_sha256'):raise ValueError('Recorded evidence changed: '+str(p))
            paths.append(p)
        status=json.loads((directory/'path-budget-window.json').read_bytes())
        b=[json.loads(x) for x in (directory/'path-budget-completions.jsonl').read_bytes().splitlines()]
        a=[json.loads(x) for x in (directory/'enemy-phase-window.jsonl').read_bytes().splitlines()]
        if status['overflow'] or status['row_count']!=len(b):raise ValueError('Path observation overflow or count mismatch')
        budget.append(b);actors.append(a);pins.append(json.loads((directory/'input-pins-before.json').read_bytes())['pins'])
    shared=set(pins[0])&set(pins[1]);conflicts=[p for p in shared if pins[0][p]!=pins[1][p]]
    if conflicts:raise ValueError('Source pin conflict: '+repr(conflicts[:3]))
    def metrics(value):
        if isinstance(value,dict):
            for key,child in value.items():
                if key=='observation_scope_metrics':yield child
                else:yield from metrics(child)
        elif isinstance(value,list):
            for child in value:yield from metrics(child)
    scope=[list(metrics(json.loads((d/'source-result.json').read_bytes()))) for d in dirs]
    if any(len(x)!=1 for x in scope) or scope[0][0].get('scopes')!=0 or scope[1][0].get('scopes')!=(0 if options.repeat_off else 1200) or scope[0][0].get('enabled') is not False or scope[1][0].get('enabled') is not (not options.repeat_off):
        raise ValueError('Actual source observation scope modes not corroborated')
    def first_difference(a,b,ignored):
        for i in range(max(len(a),len(b))):
            x=a[i] if i<len(a) else None;y=b[i] if i<len(b) else None
            if x is None or y is None:return {'index':i,'first':x,'second':y}
            fields=sorted(k for k in set(x)|set(y) if k not in ignored and (k not in x or k not in y or x[k]!=y[k]))
            if fields:return {'index':i,'fields':fields,'first':x,'second':y}
        return None
    actor_first=first_difference(actors[0],actors[1],set())
    budget_first=first_difference(budget[0],budget[1],{'pending_wall_seconds'})
    report={'status':'ComparedEarlySourceObservations','mode_labels':expected_modes,'pair_sha256':sha(receipt),'binary_sha256':sha(binary),
            'artifact_sha256':{str(p):sha(p) for p in paths},'shared_pin_count':len(shared),'common_pin_conflicts':len(conflicts),
            'scope_metrics':[x[0] for x in scope],'actor_rows':[len(x) for x in actors],
            'budget_rows':[len(x) for x in budget],'first_actor_sample_difference':actor_first,
            'first_budget_structure_difference_excluding_wall_seconds':budget_first,
            'scope':'Original source budget post-decision callback and selected coarse actor state; instrumentation can alter wall time; not full-world, native or readiness evidence',
            'causal_attribution':'Unproven','candidate_promoted':False,'mission_ready':False}
    out=pair_dir/'comparison.json'
    with out.open('x') as f:json.dump(report,f,indent=2);f.write('\n')
    print(json.dumps({'status':report['status'],'actor_rows':report['actor_rows'],'budget_rows':report['budget_rows'],
        'first_actor':None if actor_first is None else {k:actor_first.get(k) for k in ('index','fields')},
        'first_budget':None if budget_first is None else {k:budget_first.get(k) for k in ('index','fields')},'mission_ready':False}))

if __name__=='__main__':main()
