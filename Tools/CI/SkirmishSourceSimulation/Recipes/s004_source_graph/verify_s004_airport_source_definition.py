#!/usr/bin/env python3
"""Rebuild and verify a source-only, unplaced S004 airport definition candidate."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_airport_source_definition as candidate
from verify_s004_unit_metadata_candidate import unique_object


def verify(path,expected):
    if len(expected)!=64 or any(c not in '0123456789abcdef' for c in expected):raise ValueError('Exact lowercase SHA-256 required')
    raw=path.read_bytes();digest=hashlib.sha256(raw).hexdigest()
    if digest!=expected:raise ValueError('Airport source candidate hash mismatch')
    actual=json.loads(raw,object_pairs_hook=unique_object,parse_constant=lambda value:(_ for _ in ()).throw(ValueError('Nonfinite JSON: '+value)))
    canonical=lambda value:json.dumps(value,sort_keys=True,separators=(',',':'),allow_nan=False)
    if canonical(actual)!=canonical(candidate.build()):raise ValueError('Airport source candidate differs from verified source join')
    if path.read_bytes()!=raw:raise ValueError('Airport source candidate changed during verification')
    return {'status':'PassedSourceOnlyAirportDefinitionRebuild','candidate_sha256':digest,
            'live_runway_registry_validated':False,'runtime_admission':False,'mission_ready':False}


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('candidate',type=Path);p.add_argument('sha256');p.add_argument('--receipt',type=Path,required=True);a=p.parse_args()
    result=verify(a.candidate,a.sha256)
    with a.receipt.open('x') as f:json.dump(result,f,indent=2);f.write('\n')
    print(json.dumps(result))


if __name__=='__main__':main()
