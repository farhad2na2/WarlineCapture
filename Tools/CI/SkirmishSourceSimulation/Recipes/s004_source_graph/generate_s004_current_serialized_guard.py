#!/usr/bin/env python3
"""Build an opt-in serialized comparison candidate from the current guard."""
import argparse
import hashlib
import json
from pathlib import Path
import s004_setup_guard_cache_projection as original

ROOT=Path(__file__).resolve().parents[5]
SOURCE=ROOT/'Tools/CI/SkirmishSourceSimulation/Adapters/DeclaredGround/SourceS004CompiledSetupInput.cs'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def replace_once(text,old,new):
    if text.count(old)!=1:raise ValueError('Exact guard projection anchor required: '+old)
    return text.replace(old,new,1)

def generate(output):
    source_sha=sha(SOURCE);output.mkdir(parents=True,exist_ok=False)
    args=[str(SOURCE)];original.apply(args,output,ROOT)
    cached=Path(args[0]).read_text();candidate=replace_once(cached,
        'if (actual == null || !JToken.DeepEquals(JObject.FromObject(actual), expected))',
        'if (actual == null || !StringComparer.Ordinal.Equals(JsonConvert.SerializeObject(actual, Formatting.None), expected))')
    candidate=replace_once(candidate,'static JObject CachedReference;','static string CachedReference;')
    candidate=replace_once(candidate,'static JObject CachedExpected(','static string CachedExpected(')
    candidate=replace_once(candidate,'CachedReference=JObject.FromObject(Read(path,expectedSha256));',
                           'CachedReference=JsonConvert.SerializeObject(Read(path,expectedSha256), Formatting.None);')
    candidate=replace_once(candidate,'SourceS004CompiledSetupInput','SourceS004SerializedGuardCandidate')
    target=output/'SourceS004SerializedGuardCandidate.cs'
    with target.open('x') as stream:stream.write(candidate)
    if sha(SOURCE)!=source_sha:raise ValueError('Original guard changed while generating candidate')
    proof={'status':'ExperimentalSourceOnlyCandidate','source_sha256':source_sha,'cached_sha256':sha(Path(args[0])),
           'candidate_sha256':sha(target),'per_call_input_hash_preserved':True,'full_serialized_field_comparison':True,
           'game_source_modified':False,'world_initialized':False,'runtime_admission':False,'mission_ready':False}
    with (output/'generation.json').open('x') as stream:json.dump(proof,stream,indent=2);stream.write('\n')
    return proof

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--output',type=Path,required=True);args=parser.parse_args()
    print(json.dumps(generate(args.output.resolve())))
