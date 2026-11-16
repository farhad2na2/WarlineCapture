#!/usr/bin/env python3
"""Prepare an opt-in source-host scope candidate with one live singleton query."""
import argparse
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
SOURCE=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/454_SourceCampReadScope.cs'

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def generate(output):
    source=SOURCE.read_text()
    old='if(q.CalculateEntityCount()!=1)throw new NotSupportedException("One actual source session/catalog required");var owner=q.GetSingletonEntity();'
    new='using var sourceOwners=q.ToEntityArray(Unity.Collections.Allocator.Temp);if(sourceOwners.Length!=1)throw new NotSupportedException("One actual source session/catalog required");var owner=sourceOwners[0];'
    if source.count(old)!=1: raise ValueError('Exact source singleton read seam changed')
    output.mkdir(parents=True,exist_ok=False);target=output/'SourceCampReadScopeSingleQuery.cs';target.write_text(source.replace(old,new))
    receipt={'status':'Prepared','scope':'OptInSourceHostSingleEnumeration_NotGamePolicy_NotNativeParity','source_sha256':sha(SOURCE),
      'output_sha256':sha(target),'retains_all_authority_checks':True,'full_behavior_parity_proven':False,'speedup_proven':False,'mission_ready':False}
    (output/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');return receipt

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,required=True);print(json.dumps(generate(parser.parse_args().output.resolve())))
