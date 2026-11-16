#!/usr/bin/env python3
"""Specialize only the setup guard on the retained source opening-actor seam."""
import argparse,hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
SOURCE=ROOT/'T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/S004OpeningActorSource.cs'
GUARD=ROOT/'Tools/CI/SkirmishSourceSimulation/Recipes/S005CompiledSetupGuard.cs.txt'
SETUP=ROOT/'T Logs/SkirmishProduction/s005-experimental-setup-export-20261105-T1/receipt.json'
PREP=ROOT/'T Logs/SkirmishProduction/s005-terrain-placement-inputs-20261105-T5/preparation.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def prepare(out):
    if sha(SETUP)!='24a1cebbfd8e37015b218b175db3f33a8049f89a55aa28f18e5496ed971c7fe3':raise ValueError('S005 compiler receipt changed')
    text=SOURCE.read_text();begin=text.index(' SourceS004CompiledSetupInput.RequireExact(');end=text.index(';if(typedPlacement',begin)
    old=text[begin:end];guard=' SourceS005CompiledSetupGuard.RequireExact(setup,'+json.dumps(str(SETUP))+')'
    text=text[:begin]+guard+text[end:];text=text.replace('Exact S004 source force required','Exact S005 source force required')
    if 'SourceS004CompiledSetupInput.RequireExact' in text or text.count('SourceS005CompiledSetupGuard.RequireExact')!=1:raise ValueError('Unexpected setup guard inventory')
    pins={str(p):sha(p) for p in (SOURCE,GUARD,SETUP,PREP,Path(__file__).resolve())}
    for p,h in json.loads(PREP.read_text())['source_pins'].items():
        if p in pins and pins[p]!=h:raise ValueError('Conflicting source pin: '+p)
        pins[p]=h
    for p,h in pins.items():
        if not Path(p).is_file() or sha(Path(p))!=h:raise ValueError('Source pin changed: '+p)
    out.mkdir(parents=True,exist_ok=False);(out/'opening.cs').write_text(text)
    (out/'preparation.json').write_text(json.dumps({'status':'PreparedExperimentalOnly','source_pins':pins,'old_guard':old.strip(),
      'scope':'S005 full-field setup guard on retained source opening actor seam; no native input or mission readiness',
      'mission_ready':False},indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',required=True,type=Path);prepare(p.parse_args().output.resolve())
