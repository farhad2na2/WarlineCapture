#!/usr/bin/env python3
"""Run a compiled, isolated S005 source experiment with explicit retained inputs."""
import argparse,hashlib,json,os,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
PRIOR=ROOT/'T Logs/SkirmishProduction/s005-terrain-placement-experiment-20261105-T1'
BUILD=ROOT/'T Logs/SkirmishProduction/s004-current-epoch-s002-build-20261105-T1/build.json'
BASEPINS=ROOT/'T Logs/SkirmishProduction/s004-source-pin-checkpoint-world-run-20261105-T1/source-pins-before.json'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
def run(out,prep):
    out=out.resolve();prep=prep.resolve();exe=out/'source-force-ledger.exe';assert exe.is_file()
    inputs=dict(json.loads((PRIOR/'run-receipt.json').read_text())['input_hashes'])
    del inputs[str(PRIOR/'terrain-placement.exe')];inputs[str(exe)]=sha(exe)
    pins={}
    for rel in ('T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json',
                'T Logs/SkirmishProduction/s005-building-factory-capture-20261105-T2/full-factory-runway-definitions.json',
                'T Logs/SkirmishProduction/s005-configured-readmodel-capture-20261105-T1/configured-spawnable-readmodel.json',
                'T Logs/SkirmishProduction/s005-prefab-object-reference-capture-20261105-T2/s005-prefab-object-references.json'):
        for p,h in json.loads((ROOT/rel).read_text())['source_pins'].items():
            if p in pins and pins[p]!=h:raise ValueError('Conflicting source pin: '+p)
            pins[p]=h
    base=json.loads(BASEPINS.read_text())
    for group in ('build_sha256','generated_source_sha256'):
        for p,h in base[group].items():
            if p in pins and pins[p]!=h:raise ValueError('Conflicting build pin: '+p)
            pins[p]=h
    for p,h in json.loads(prep.read_text())['source_pins'].items():
        if p in pins and pins[p]!=h:raise ValueError('Conflicting preparation pin: '+p)
        pins[p]=h
    def verify():
        for p,h in {**pins,**inputs}.items():
            if not Path(p).is_file() or sha(p)!=h:raise ValueError('Source changed: '+p)
    verify();(out/'input-pins-before.json').write_text(json.dumps({'inputs':inputs,'source_pin_count':len(pins),'mission_ready':False},indent=2)+'\n')
    rsp=[json.loads(l) for l in (out/'compile.rsp').read_text().splitlines() if l.strip()]
    env=os.environ.copy();env['MONO_PATH']=os.pathsep.join(sorted({str(Path(v[3:]).resolve().parent) for v in rsp if v.startswith('-r:') and Path(v[3:]).is_file()}))
    b=json.loads(BUILD.read_text());args=[ROOT/'T Logs/SkirmishProduction/s005-original-compiler-probe-inputs-20261105-T2/source-config-graph.json',
       ROOT/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv',ROOT/'T Logs/SkirmishProduction/s005-layout-native-capture-20261105-T1/editor.log',
       out/'replayed-layout.json',ROOT/'T Logs/SkirmishProduction/s005-prefab-object-projection-20261105-T2/projection.json',
       ROOT/'T Logs/SkirmishProduction/s005-building-factory-capture-20261105-T2/full-factory-runway-definitions.json',
       ROOT/'T Logs/SkirmishProduction/s005-configured-readmodel-capture-20261105-T1/configured-spawnable-readmodel.json',
       ROOT/'T Logs/SkirmishProduction/s005-campaign-placement-inputs-20261105-T1',
       ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/campaign-physical-source.json',
       ROOT/'Logs/SkirmishProduction/s073-source-inputs-20261101-r1/source-config-graph.json']
    config=ROOT/'T Logs/SkirmishProduction/s004-current-physical-input-projection-20261104-T1/mono-config.xml'
    command=[b['runtime_selection']['mono'],'--config',str(config),'--optimize=-float32',str(exe)]+[str(x) for x in args]
    try:r=subprocess.run(command,capture_output=True,text=True,timeout=300,env=env)
    except subprocess.TimeoutExpired as error:
        (out/'timeout.json').write_text(json.dumps({'status':'TimedOut','mission_ready':False},indent=2)+'\n');raise error
    (out/'run.log').write_text(r.stdout+r.stderr);verify()
    receipt={'exit_code':r.returncode,'source_pins_rechecked':len(pins),'input_hashes':inputs,
             'capture_assembly_currentness':'historical_not_asserted','active_water':'assumed_empty',
             'mountain_mesh_clearance_proven':False,'native_parity':False,'mission_ready':False}
    (out/'run-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
    if r.returncode!=0:print('worker_exit',r.returncode,(r.stderr or '')[-1200:]);return r.returncode
    d=json.loads(r.stdout.strip().splitlines()[-1]);probe=next(z for z in d['compiler'] if z['size']=='Standard')['session_probe']['starting_grant_probe']
    (out/'source-result.json').write_text(json.dumps(probe,indent=2)+'\n')
    print(json.dumps({k:probe[k] for k in ('status','succeeded','failed','pending','registered_buildings','adoption_succeeded',
          'bound_buildings','force_owner_attempted','force_owner_succeeded','force_owner_reason','force_owner_exception','mission_ready')}));return 0
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--output',type=Path,required=True);p.add_argument('--preparation',type=Path,required=True)
    a=p.parse_args();raise SystemExit(run(a.output,a.preparation))
