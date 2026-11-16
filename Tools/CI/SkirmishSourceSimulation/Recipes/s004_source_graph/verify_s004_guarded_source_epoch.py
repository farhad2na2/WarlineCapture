#!/usr/bin/env python3
"""Reconstruct the post-guard S004 source bundle without granting admission."""
import argparse
import json
from pathlib import Path
import build_s004_guarded_source_epoch as epoch
import verify_s004_merged_source_epoch as verifier

def verify(bundle,expected_sha,output):
    # Reuse the original strict byte, path, evidence and reconstruction checks;
    # change only the explicitly selected epoch builder.
    prior=verifier.epoch
    try:
        verifier.epoch=epoch
        return verifier.verify(bundle,expected_sha,output)
    finally:
        verifier.epoch=prior

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bundle',type=Path,required=True);parser.add_argument('--bundle-sha256',required=True)
    parser.add_argument('--output',type=Path,required=True);parser.add_argument('--receipt',type=Path,required=True)
    args=parser.parse_args();result=verify(args.bundle,args.bundle_sha256,args.output)
    with args.receipt.open('x') as stream:json.dump(result,stream,indent=2);stream.write('\n')
    print(json.dumps(result))
