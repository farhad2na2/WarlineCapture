#!/usr/bin/env python3
"""Compile current original getter bodies against source-only S004 inputs."""
import argparse
import hashlib
import json
import os
import re
import shlex
import subprocess
from pathlib import Path

import read_s004_catalog_source_inputs as inputs

ROOT = inputs.ROOT
HARNESS = Path(__file__).with_name('S004CatalogGetterControl.cs')
AUTHORING = ROOT / 'Assets/Game/Scripts/Authorings/UnitGridAuthoring.cs'
MARKER = '[S004CatalogGetterControl] result=Passed rows=3 world=0 native=0 readiness=0'
GETTERS = ('IsAirUnit', 'IsProductionTransportUnit', 'SoldierTransportCapacity',
           'ConfiguredAllowIdleWander', 'ConfiguredSpeed', 'ConfiguredResourceHaulerBarrelCapacity',
           'ConfiguredCanAttack', 'ConfiguredAttackRange', 'ConfiguredAttackDamage', 'ConfiguredMaxHealth',
           'PortraitSprite', 'PortraitCardSprite', 'PortraitActionSprite')


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def generate(source):
    getters = []
    for name in GETTERS:
        rows = re.findall(r'(?m)^\s*public (?:bool|int|float|Sprite) ' + name + r' => [^\r\n]+;', source)
        if len(rows) != 1:
            raise ValueError('Exact current original getter required: ' + name)
        getters.append(rows[0].strip())
    return '''using UnityEngine;
using Sprite = S004SpriteReference;
internal sealed class S004SpriteReference { internal readonly Newtonsoft.Json.Linq.JObject Reference; internal S004SpriteReference(Newtonsoft.Json.Linq.JObject value) { Reference=value; } }
internal sealed class S004GetterConfig {
 internal bool IsAirUnit, AllowIdleWander, CanAttack;
 internal int SoldierTransportCapacity, ResourceHaulerBarrelCapacity, AttackDamage, MaxHealth;
 internal float Speed, AttackRange;
 internal Sprite PortraitSprite, PortraitCardSprite, PortraitActionSprite;
}
internal sealed class S004GetterProjection {
 internal S004GetterConfig config;
 internal bool isAirUnit, isProductionTransportUnit, allowIdleWander, canAttack;
 internal int soldierTransportCapacity, resourceHaulerBarrelCapacity, attackDamage, maxHealth;
 internal float speed, attackRange;
 internal Sprite portraitSprite, portraitCardSprite, portraitActionSprite;
''' + '\n'.join(getters) + '\n}\n'


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input',type=Path,required=True); parser.add_argument('--probe',type=Path,required=True)
    parser.add_argument('--build',type=Path,required=True); parser.add_argument('--output',type=Path,required=True)
    a=parser.parse_args(); inp=a.input.resolve(strict=True); probe=a.probe.resolve(strict=True); bp=a.build.resolve(strict=True)
    out=a.output.resolve(); out.mkdir(parents=True,exist_ok=False)
    original=json.loads(inp.read_bytes())
    if inputs.read()!=original: raise ValueError('Current serialized catalog inputs differ')
    build=json.loads(bp.read_bytes())
    if build.get('status')!='Passed': raise ValueError('Passing source build required')
    args=[shlex.split(line)[0] for line in (probe/'compile.rsp').read_text().splitlines() if line.strip()]
    mains=[i for i,x in enumerate(args) if x.startswith('-main:')]; outputs=[i for i,x in enumerate(args) if x.startswith('-out:')]
    if len(mains)!=1 or len(outputs)!=1: raise ValueError('Exact source host compiler response required')
    source_paths=[Path(x).resolve(strict=True) for x in args if x.endswith('.cs')]
    pins={str(path):sha(path) for path in source_paths}
    for path,digest in original['source_pins'].items():
        if path in pins and pins[path]!=digest: raise ValueError('Source input epoch differs: '+path)
        pins[path]=digest
    for path in (HARNESS,AUTHORING,Path(__file__).resolve(),inp,bp): pins[str(path)]=sha(path)
    for path,digest in build['source_and_reference_pins'].items():
        if sha(path)!=digest: raise ValueError('Build pin changed: '+path)
    generated=out/'S004OriginalGetterProjection.cs';generated.write_text(generate(AUTHORING.read_text()))
    executable=out/'S004CatalogGetterControl.exe';args[mains[0]]='-main:S004CatalogGetterControl';args[outputs[0]]='-out:'+str(executable)
    args.extend((str(generated),str(HARNESS)));rsp=out/'compile.rsp';rsp.write_text('\n'.join(json.dumps(x) for x in args)+'\n')
    bpins=build['source_and_reference_pins'];dotnet=next(x for x in bpins if Path(x).name=='dotnet');csc=next(x for x in bpins if x.endswith('/Roslyn/bincore/csc.dll'))
    compiled=subprocess.run([dotnet,csc,'@'+str(rsp)],capture_output=True,text=True,timeout=300);(out/'compile.log').write_text(compiled.stdout+compiled.stderr)
    if compiled.returncode: raise RuntimeError('Original getter source compile failed')
    runtime=build['runtime_selection'];env=os.environ.copy();env['MONO_PATH']=os.pathsep.join((str(bp.parent/'placement'),runtime['engine'],str(Path(runtime['newtonsoft']).parent),str(ROOT/'Library/ScriptAssemblies'),str(out)))
    config=ROOT/'T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml';projection=out/'projection.json'
    run=subprocess.run([runtime['mono'],'--config',str(config),str(executable),str(inp),str(projection)],cwd=ROOT,env=env,capture_output=True,text=True,timeout=300)
    (out/'run.log').write_text(run.stdout+run.stderr)
    if run.returncode or MARKER not in run.stdout: raise RuntimeError('Original getter control failed')
    for path,digest in pins.items():
        if sha(path)!=digest: raise ValueError('Source changed during execution: '+path)
    for path,digest in build['source_and_reference_pins'].items():
        if sha(path)!=digest: raise ValueError('Build pin changed during execution: '+path)
    receipt={'status':'PassedSourceOnlyOriginalGetterProjection','input_sha256':sha(inp),'projection_sha256':sha(projection),
             'original_getters':len(GETTERS),'source_pins_checked':len(pins),'native_prefab_resolution':False,'runtime_admission':False,'mission_ready':False}
    (out/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(receipt))


if __name__=='__main__': main()
