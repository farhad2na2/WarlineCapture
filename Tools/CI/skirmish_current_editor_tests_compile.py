"""Compile exact current Game.Editor + Tests owning sources, no projections or Unity."""
import hashlib
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import tempfile


def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    root=Path(__file__).resolve().parents[2]
    parser=argparse.ArgumentParser()
    parser.add_argument('--projection-manifest',type=Path)
    args=parser.parse_args()
    overrides={};consumed=set();extra_owners={}
    if args.projection_manifest:
        for row in json.loads(args.projection_manifest.read_text(encoding='utf-8-sig'))['files']:
            target=row['target'];source=root/row['source']
            before=sha(root/target) if (root/target).is_file() else None
            if target in overrides or not target.endswith('.cs') or before!=row['before_sha256'] or sha(source)!=row['sha256']:
                raise ValueError('Invalid or stale exact source projection: '+target)
            overrides[target]=source
            if before is None:
                if row.get('assembly') not in ('Game.Editor','Game.Tests.Editor'):
                    raise ValueError('New source requires explicit owning assembly: '+target)
                extra_owners[target]=row['assembly']
    parent=root/'Logs/SkirmishProduction/Offline/CurrentEditorTests';parent.mkdir(parents=True,exist_ok=True)
    output=Path(tempfile.mkdtemp(prefix='compile-',dir=parent))
    reports=root/'Design/AgentReports/SkirmishExpansion/ProductionAdapter';reports.mkdir(parents=True,exist_ok=True)
    evidence=Path(tempfile.mkdtemp(prefix='current-editor-tests-',dir=reports))
    dotnet=shutil.which('dotnet');sdk=subprocess.run(['rtk','proxy',dotnet,'--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0]
    version,location=sdk.split(' ',1);compiler=Path(location.strip('[]'))/version/'Roslyn/bincore/csc.dll'
    records=[];built={}
    required='Assets/Tests/Editor/SkirmishExpansion/SkirmishSharedProductionWaveValidation.cs'
    for assembly in ('Game.Editor','Game.Tests.Editor'):
        response=max((root/'Library/Bee').rglob(assembly+'.rsp'),key=lambda p:p.stat().st_mtime_ns)
        lines=[];source_pins={};refs={};omitted=0
        for line in response.read_text(encoding='utf-8-sig').splitlines():
            low=line.lower()
            if low.startswith(('-out:','/out:','-refout:','/refout:')):continue
            if low.startswith(('-analyzer:','/analyzer:')):omitted+=1;continue
            if low.startswith(('-r:','/r:','-reference:','/reference:')):
                path=Path(line.split(':',1)[1].strip('"'));path=path if path.is_absolute() else root/path
                name=path.name.replace('.ref.dll','.dll')
                if name in built:path=built[name];line='-r:"'+str(path)+'"'
                if not path.is_file():raise ValueError('Actual owning reference absent; no fallback: '+str(path))
                refs[str(path)]=sha(path)
            elif not line.startswith(('-','/')) and line.strip('"').endswith('.cs'):
                path=Path(line.strip('"'));path=path if path.is_absolute() else root/path
                relative=path.relative_to(root).as_posix()
                if not path.is_file():raise ValueError('Actual owning source absent: '+str(path))
                if relative in source_pins:raise ValueError('Duplicate actual owning source: '+relative)
                if relative in overrides:
                    if relative in consumed:raise ValueError('Projection consumed by multiple owners: '+relative)
                    consumed.add(relative);path=overrides[relative];line='"'+str(path)+'"'
                source_pins[relative]=sha(path)
            lines.append(line)
        for target,owner in extra_owners.items():
            if owner!=assembly:continue
            if target in consumed:raise ValueError('New projection consumed twice: '+target)
            path=overrides[target];lines.append('"'+str(path)+'"');source_pins[target]=sha(path);consumed.add(target)
        if not source_pins:raise ValueError('Empty actual owning source response')
        if assembly=='Game.Tests.Editor' and required not in source_pins:
            raise ValueError('Required corrected root aggregate missing from actual Tests input')
        destination=output/(assembly+'.dll');reference=output/(assembly+'.ref.dll')
        lines+=['-out:"'+str(destination)+'"','-refout:"'+str(reference)+'"']
        rsp=output/(assembly+'.rsp');rsp.write_text('\n'.join(lines)+'\n',encoding='utf-8')
        result=subprocess.run(['rtk','proxy',dotnet,str(compiler),'@'+str(rsp)],cwd=root,capture_output=True,text=True)
        (evidence/(assembly+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8');shutil.copyfile(rsp,evidence/rsp.name)
        stable=all(sha(overrides.get(path,root/path))==digest for path,digest in source_pins.items()) and all(sha(Path(path))==digest for path,digest in refs.items())
        records.append(dict(assembly=assembly,exit_code=result.returncode,actual_input_rsp=str(response),actual_input_rsp_sha256=sha(response),
            generated_rsp_sha256=sha(rsp),actual_sources=source_pins,actual_references=refs,
            all_source_reference_bytes_stable_before_after=stable,analyzers_generators_omitted=omitted,reference_fallbacks=[]))
        if result.returncode or not stable:
            print(result.stdout+result.stderr);break
        built[assembly+'.dll']=reference
    passed=len(records)==2 and all(row['exit_code']==0 and row['all_source_reference_bytes_stable_before_after'] for row in records) and consumed==set(overrides)
    shutil.copyfile(root/required,evidence/'SkirmishSharedProductionWaveValidation.current.cs.txt')
    receipt=dict(result='PassedCurrentEditorTestsTypeCompile' if passed else 'FailedCurrentEditorTestsTypeCompile',
        assemblies=records,required_corrected_aggregate=required,required_aggregate_sha256=sha(root/required),
        native_execution=False,tests_executed=False,import_ilpp_and_native_controls='Pending checked Unity wrapper',scratch=str(output))
    receipt['exact_projection_sources']={key:dict(source=str(value),sha256=sha(value)) for key,value in overrides.items()}
    receipt['projection_targets_consumed_exactly_once']=sorted(consumed)
    receipt['unconsumed_projection_targets']=sorted(set(overrides)-consumed)
    (evidence/'result.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(result=receipt['result'],assemblies=[dict(name=r['assembly'],exit_code=r['exit_code'],sources=len(r['actual_sources']),
        references=len(r['actual_references']),stable=r['all_source_reference_bytes_stable_before_after'],analyzers_omitted=r['analyzers_generators_omitted']) for r in records],
        aggregate_sha256=receipt['required_aggregate_sha256'],evidence=str(evidence),native_execution=False),indent=2))
    return 0 if passed else 1


if __name__=='__main__':raise SystemExit(main())
