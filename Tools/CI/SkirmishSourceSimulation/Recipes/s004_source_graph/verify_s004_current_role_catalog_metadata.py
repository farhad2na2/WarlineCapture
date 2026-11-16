#!/usr/bin/env python3
"""Rebuild an explicitly pinned S004 role join without admitting gameplay."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_current_role_catalog_metadata as roles
from verify_s004_unit_metadata_candidate import unique_object


def verify(candidate, expected, source_build, prefab, prefab_audit, projection, metadata):
    if len(expected) != 64 or any(c not in '0123456789abcdef' for c in expected):
        raise ValueError('Exact lowercase candidate SHA-256 required')
    raw = candidate.read_bytes()
    if hashlib.sha256(raw).hexdigest() != expected:
        raise ValueError('Current role candidate SHA-256 mismatch')
    actual = json.loads(raw, object_pairs_hook=unique_object,
                        parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON: ' + value)))
    rebuilt = roles.build(source_build, prefab, prefab_audit, projection, metadata)
    canonical = lambda value: json.dumps(value, sort_keys=True, separators=(',', ':'), allow_nan=False)
    if canonical(actual) != canonical(rebuilt) or candidate.read_bytes() != raw:
        raise ValueError('Current role candidate differs from its exact source join')
    return {'status':'PassedSourceOnlyCurrentRoleRebuild', 'candidate_sha256':expected,
            'unit_count':len(actual['units']), 'historical_census':True,
            'native_prefab_resolution':False, 'runtime_admission':False, 'mission_ready':False}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('candidate',type=Path); parser.add_argument('sha256')
    parser.add_argument('--source-build',required=True,type=Path); parser.add_argument('--prefab',required=True,type=Path)
    parser.add_argument('--prefab-audit',required=True,type=Path); parser.add_argument('--projection',required=True,type=Path)
    parser.add_argument('--metadata',required=True,type=Path); parser.add_argument('--receipt',required=True,type=Path)
    args=parser.parse_args()
    result=verify(args.candidate,args.sha256,args.source_build,args.prefab,args.prefab_audit,args.projection,args.metadata)
    with args.receipt.open('x') as output:
        json.dump(result,output,indent=2); output.write('\n')
    print(json.dumps(result))


if __name__=='__main__':
    main()
