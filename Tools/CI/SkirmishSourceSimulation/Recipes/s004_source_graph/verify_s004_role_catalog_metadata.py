#!/usr/bin/env python3
"""Recheck the complete source role-domain join before diagnostic use."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_role_catalog_metadata as role_candidate
from verify_s004_unit_metadata_candidate import unique_object


def verify(path, expected, source_build=None, prefabs=None, unit_metadata=None):
    if len(expected)!=64 or any(c not in '0123456789abcdef' for c in expected):
        raise ValueError('Exact lowercase SHA-256 required')
    before=path.read_bytes();digest=hashlib.sha256(before).hexdigest()
    if digest!=expected: raise ValueError('Role candidate SHA-256 mismatch')
    actual=json.loads(before,object_pairs_hook=unique_object,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON: '+value)))
    canonical=lambda value:json.dumps(value,sort_keys=True,separators=(',',':'),allow_nan=False)
    explicit = (source_build, prefabs, unit_metadata)
    if any(value is not None for value in explicit) and not all(value is not None for value in explicit):
        raise ValueError('All three explicit source inputs are required together')
    rebuilt = role_candidate.build(*explicit) if source_build is not None else role_candidate.build()
    if canonical(actual)!=canonical(rebuilt): raise ValueError('Role candidate differs from current source join')
    if path.read_bytes()!=before: raise ValueError('Role candidate changed during verification')
    return {'status':'PassedSourceOnlyRoleCatalogRebuild','candidate_sha256':digest,'unit_count':len(actual['units']),
            'native_prefab_resolution':False,'runtime_admission':False,'mission_ready':False}


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('candidate',type=Path);parser.add_argument('sha256');parser.add_argument('--receipt',type=Path,required=True)
    parser.add_argument('--source-build',type=Path);parser.add_argument('--prefabs',type=Path);parser.add_argument('--unit-metadata',type=Path);args=parser.parse_args()
    result=verify(args.candidate,args.sha256,args.source_build,args.prefabs,args.unit_metadata)
    with args.receipt.open('x') as output:json.dump(result,output,indent=2);output.write('\n')
    print(json.dumps(result))


if __name__=='__main__':main()
