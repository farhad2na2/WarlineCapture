#!/usr/bin/env python3
"""Rebuild an explicitly pinned S004 airport join without admitting gameplay."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_current_airport_source_definition as airports
from verify_s004_unit_metadata_candidate import unique_object


def verify(candidate, expected, *inputs):
    if len(inputs)!=9 or any(value is None for value in inputs):
        raise ValueError('All nine explicit current evidence inputs are required')
    if len(expected)!=64 or any(c not in '0123456789abcdef' for c in expected):
        raise ValueError('Exact lowercase candidate SHA-256 required')
    raw=candidate.read_bytes()
    if hashlib.sha256(raw).hexdigest()!=expected:
        raise ValueError('Airport candidate SHA-256 mismatch')
    actual=json.loads(raw,object_pairs_hook=unique_object,
        parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON: '+value)))
    rebuilt=airports.build(*inputs)
    canonical=lambda value:json.dumps(value,sort_keys=True,separators=(',',':'),allow_nan=False)
    if canonical(actual)!=canonical(rebuilt) or candidate.read_bytes()!=raw:
        raise ValueError('Airport candidate differs from its exact current source join')
    return {'status':'PassedSourceOnlyCurrentAirportRebuild','candidate_sha256':expected,
            'live_runway_registry_validated':False,'source_fit_certified':False,
            'runtime_admission':False,'mission_ready':False}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('candidate',type=Path);parser.add_argument('sha256')
    names=('source-build','prefab','prefab-audit','projection-path','metadata',
           'factory-path','factory-audit-path','s003-path','role-path')
    for name in names: parser.add_argument('--'+name,required=True,type=Path)
    parser.add_argument('--receipt',required=True,type=Path);args=parser.parse_args()
    values=[getattr(args,name.replace('-','_')) for name in names]
    result=verify(args.candidate,args.sha256,*values)
    with args.receipt.open('x') as output:
        json.dump(result,output,indent=2);output.write('\n')
    print(json.dumps(result))


if __name__=='__main__': main()
