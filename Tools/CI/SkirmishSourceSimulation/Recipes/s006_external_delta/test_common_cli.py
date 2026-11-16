#!/usr/bin/env python3
"""Exercise the explicit unqualified S006 inspection route through the common CLI."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[5]
LOGS=ROOT/'T Logs/SkirmishProduction'
OUT=LOGS/'s006-common-cli-controls-20261106-T3'
CLI=ROOT/'Tools/CI/run_skirmish_source_simulation.py'
INSPECTOR=Path(__file__).with_name('inspect_external_binding.py')
BUILD=LOGS/'s006-external-inspection-build-20261106-T1/build-receipt.json'
GRAPH=LOGS/'s005-view-profile-source-graph-20261105-T1/source-config-graph.json'
MATRIX=ROOT/'Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv'
CANDIDATE=LOGS/'s006-current-config-only-candidate-20261106-T22/binding-Regular-Standard.json'

def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def descriptor(path):return {'path':str(path),'sha256':sha(path)}

def main():
    OUT.mkdir(exist_ok=False)
    inputs=(Path(__file__),CLI,INSPECTOR,BUILD,GRAPH,MATRIX,CANDIDATE)
    before={str(p):sha(p) for p in inputs}
    request={'schema_version':1,'kind':'S006UnqualifiedBindingInspectionRequest','mission':'S006','seed':104735,'size':'Standard','difficulty':'Regular',
             'build':descriptor(BUILD),'graph':descriptor(GRAPH),'matrix':descriptor(MATRIX),'candidate':descriptor(CANDIDATE)}
    def write(name,raw):
        path=OUT/(name+'-request.json');path.write_text(raw);return path
    normal=write('valid',json.dumps(request,indent=2)+'\n')
    bad_candidate=json.loads(json.dumps(request));bad_candidate['candidate']['sha256']='0'*64
    wrong_candidate=write('wrong-candidate',json.dumps(bad_candidate,indent=2)+'\n')
    bad_size=json.loads(json.dumps(request));bad_size['size']='War'
    wrong_size=write('wrong-size',json.dumps(bad_size,indent=2)+'\n')
    bad_schema=json.loads(json.dumps(request));bad_schema['schema_version']=True
    wrong_schema=write('wrong-schema',json.dumps(bad_schema,indent=2)+'\n')
    oversize_data=OUT/'oversized-candidate.json';oversize_data.write_bytes(b' '+b' '*(2*1024*1024))
    oversized=json.loads(json.dumps(request));oversized['candidate']=descriptor(oversize_data)
    oversize_request=write('oversized-candidate',json.dumps(oversized,indent=2)+'\n')
    duplicate=write('duplicate','{"mission":"S006",'+json.dumps(request)[1:]+'\n')
    cases=[]
    def run(name,path,digest,extra=()):
        output=OUT/(name+'-output');command=[sys.executable,'-B',str(CLI),'s006-binding-inspect','--mission','S006','--seed','104735','--authoring-input',str(path),'--authoring-input-sha256',digest,'--output',str(output),*extra]
        result=subprocess.run(command,cwd=ROOT,capture_output=True,text=True,timeout=300)
        (OUT/(name+'.log')).write_text(result.stdout+result.stderr)
        return result,output
    result,output=run('valid',normal,sha(normal));cli=output/'cli-inspection-receipt.json'
    valid=json.loads(cli.read_bytes()) if cli.exists() else {}
    checks={'status':'InspectedUnqualifiedCandidate','binding_compiler_accepted':True,'world_allocated':False,'validation_passed':False,'source_fit_verified':False,'runtime_admission':False,'mission_ready':False}
    cases.append({'case':'valid','passed':result.returncode==0 and all(valid.get(k)==v for k,v in checks.items()),'exit_code':result.returncode})
    for name,path,digest,extra,expected in [('request_hash',normal,'0'*64,(), 'Inspection request hash mismatch'),
          ('candidate_hash',wrong_candidate,sha(wrong_candidate),(),'Inspection descriptor changed'),
          ('duplicate_key',duplicate,sha(duplicate),(),'duplicate'),
          ('boolean_schema',wrong_schema,sha(wrong_schema),(),'Exact unqualified S006 inspection request required'),
          ('oversized_candidate',oversize_request,sha(oversize_request),(),'Inspection descriptor size limit'),
          ('incompatible_flag',normal,sha(normal),('--data-bundle',str(CANDIDATE)),'accepts only its explicit'),
          ('timeout',normal,sha(normal),('--timeout','0.001'),'timed out'),
          ('size_mismatch',wrong_size,sha(wrong_size),(),'')]:
        result,output=run(name,path,digest,extra)
        text=result.stdout+result.stderr
        passed=result.returncode!=0 and (expected.lower() in text.lower() if expected else (output/'receipt.json').is_file() and json.loads((output/'receipt.json').read_bytes()).get('status')=='Failed')
        cases.append({'case':name,'passed':passed,'exit_code':result.returncode})
    other=subprocess.run([sys.executable,'-B',str(CLI),'resolve','--mission','S002','--authoring-input-sha256','0'*64,'--output',str(OUT/'other-command-output')],cwd=ROOT,capture_output=True,text=True,timeout=30)
    (OUT/'other-command.log').write_text(other.stdout+other.stderr)
    cases.append({'case':'other_command_rejects_hash','passed':other.returncode==2 and 'limited to s006-binding-inspect' in other.stderr,'exit_code':other.returncode})
    after={str(p):sha(p) for p in inputs};ok=all(x['passed'] for x in cases) and before==after
    report={'status':'PassedCommonCliSourceControls' if ok else 'Failed','cases':cases,'source_pins_before':before,'source_pins_after':after,
            'scope':'Unqualified external candidate inspection; no World, admission, source fit or native readiness','runtime_admission':False,'mission_ready':False}
    (OUT/'receipt.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps({'status':report['status'],'cases':cases}))
    return 0 if ok else 1

if __name__=='__main__':raise SystemExit(main())
