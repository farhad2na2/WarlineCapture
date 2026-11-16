#!/usr/bin/env python3
"""Reconstruct the S004 merged source bundle; never grant runtime admission."""
import argparse
import hashlib
import json
from pathlib import Path

import build_s004_merged_source_epoch as epoch
from verify_s004_unit_metadata_candidate import unique_object


def read(path):
    return json.loads(path.read_bytes(),object_pairs_hook=unique_object,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError('Nonfinite JSON: '+value)))


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify(bundle, expected_sha, output):
    requested=Path(bundle)
    if not requested.is_absolute() or any(p.is_symlink() for p in (requested,*requested.parents)):
        raise ValueError('Canonical source bundle path without symlinks required')
    bundle=requested.resolve(strict=True); output=Path(output).absolute()
    if requested!=bundle or not bundle.is_relative_to(epoch.ROOT) or not bundle.is_file():
        raise ValueError('Regular repository-owned source bundle required')
    if len(expected_sha)!=64 or any(c not in '0123456789abcdef' for c in expected_sha) or sha(bundle)!=expected_sha:
        raise ValueError('Exact source bundle SHA-256 required')
    before=bundle.read_bytes(); actual=read(bundle)
    if (actual.get('kind')!='S004SourceDiagnosticInputBundle' or actual.get('mission_id')!='S004' or
        actual.get('scope')!='SourceHostDiagnosticOnly' or actual.get('qualification',{}).get('mission_ready') is not False):
        raise ValueError('Explicit unqualified S004 source bundle required')
    evidence=actual.get('evidence',{}); evidence_path=Path(evidence.get('path',''))
    if not evidence_path.is_absolute() or not evidence_path.is_file() or evidence_path.is_symlink() or sha(evidence_path)!=evidence.get('sha256'):
        raise ValueError('Exact original input evidence required')
    evidence_before=evidence_path.read_bytes(); original_evidence=read(evidence_path)
    rebuilt=epoch.build(output)
    reconstructed_path=Path(rebuilt['bundle']['path']); reconstructed=read(reconstructed_path)
    rebuilt_evidence=read(Path(rebuilt['evidence']['path']))
    left=dict(actual); right=dict(reconstructed); left.pop('evidence'); right.pop('evidence')
    if left!=right or original_evidence!=rebuilt_evidence:
        raise ValueError('S004 source bundle differs from independently reconstructed inputs')
    if bundle.read_bytes()!=before or evidence_path.read_bytes()!=evidence_before:
        raise ValueError('S004 evidence changed during reconstruction')
    return {'status':'PassedReconstructedSourceInputs','bundle':str(bundle),'bundle_sha256':expected_sha,
            'evidence_sha256':sha(evidence_path),'reconstructed_bundle':str(reconstructed_path),
            'reconstructed_bundle_sha256':sha(reconstructed_path),'current_world_observed':False,
            'runtime_admission':False,'mission_ready':False}


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--bundle',type=Path,required=True);parser.add_argument('--bundle-sha256',required=True)
    parser.add_argument('--output',type=Path,required=True);parser.add_argument('--receipt',type=Path,required=True)
    args=parser.parse_args(); result=verify(args.bundle,args.bundle_sha256,args.output)
    with args.receipt.open('x') as stream:json.dump(result,stream,indent=2);stream.write('\n')
    print(json.dumps(result))


if __name__=='__main__': main()
