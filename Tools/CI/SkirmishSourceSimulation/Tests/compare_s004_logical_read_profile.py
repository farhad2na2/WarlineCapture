#!/usr/bin/env python3
"""Compare two retained S004 source diagnostics and aggregate opt-in read timings."""
import argparse
import collections
import hashlib
import json
from pathlib import Path


def load(path):
    raw=path.read_bytes()
    return json.loads(raw),hashlib.sha256(raw).hexdigest()


def compare(base,profiled):
    br,bh=load(base/'receipt.json'); pr,ph=load(profiled/'receipt.json')
    bd,bdh=load(base/'requests.json'); pd,pdh=load(profiled/'requests.json')
    def bank(d): return d['source_gameplay_diagnostic']['source_bank_diagnostic']
    b=bank(bd); p=bank(pd)
    x=b['source_logical_aria_candidate']['trace'];y=p['source_logical_aria_candidate']['trace']
    if not x or len(x)!=len(y): raise ValueError('matching nonempty controller traces required')
    fields=('frame','time','target_id','decision_kind','intent','action','executed','stopped')
    decisions=[a['frame'] for a,c in zip(x,y) if any(a.get(k)!=c.get(k) for k in fields)]
    observations=[a['frame'] for a,c in zip(x,y) if a['observation']!=c['observation']]
    expected={'scope_and_public_catalog','objective','contacts','target_and_home_threat','approach','orders_and_distances','facts_and_capabilities'}
    totals=collections.Counter()
    for item in y:
        values=item.get('source_read_profile_seconds')
        if not isinstance(values,dict) or set(values)!=expected or any(not isinstance(v,(float,int)) or v<0 for v in values.values()): raise ValueError('complete nonnegative phase timings required')
        totals.update(values)
    left=br['input_sha256'];right=pr['input_sha256'];common=sorted(set(left)&set(right))
    changed=[key for key in common if left[key]!=right[key]]
    return {'scope':'TwoRetainedSourceDiagnostics_ObservationDecisionComparison_NotFullStateOrNativeParity',
      'base_receipt_sha256':bh,'profile_receipt_sha256':ph,'base_requests_sha256':bdh,'profile_requests_sha256':pdh,
      'trace_rows':len(x),'decision_fields':list(fields),'decision_difference_count':len(decisions),'first_decision_differences':decisions[:10],
      'observation_difference_count':len(observations),'first_observation_differences':observations[:10],
      'phase_seconds':dict(totals),'base_worker_advance_seconds':b['source_production_ticks']['wall_seconds'],
      'profile_worker_advance_seconds':p['source_production_ticks']['wall_seconds'],
      'common_input_pin_count':len(common),'changed_common_input_pins':changed,
      'base_only_input_pins':sorted(set(left)-set(right)),'profile_only_input_pins':sorted(set(right)-set(left)),
      'both_readiness_false':br['mission_ready'] is False and pr['mission_ready'] is False,
      'speedup_claimed':False,'native_parity':False,'mission_ready':False}


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--base',type=Path,required=True);parser.add_argument('--profiled',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    result=compare(args.base,args.profiled);args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps({k:result[k] for k in ('trace_rows','decision_difference_count','observation_difference_count','phase_seconds','changed_common_input_pins','speedup_claimed')}))
