"""Replay the strict160 Editor/Tests responses plus the explicit fifteen-row map."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tempfile

root=Path(__file__).resolve().parents[4]
folder=Path(__file__).resolve().parent
sha=lambda p: hashlib.sha256(p.read_bytes()).hexdigest()
authority=root/'Design/AgentReports/SkirmishExpansion/S005/shared-native-main-closure/owning-uesn01hn/result.json'
assert sha(authority)=='c63aabd75fb0fa6fb9f2f76d311c48da23fcac85038c417b9a12ff1a7ab503a2'
base=json.loads(authority.read_text());assert base['result']=='PassedMainExactSharedClosure'
manifest=folder/'composition.manifest.json';composition=json.loads(manifest.read_text())
pins={str(authority):sha(authority),str(manifest):sha(manifest)}
overrides={row['target']:row for row in composition['files']};consumed=set()
assert len(overrides)==15
for item in composition['inputs']:
    p=root/item['path'];assert sha(p)==item['sha256'];pins[str(p)]=sha(p)
for target,row in overrides.items():
    p=root/row['source'];assert sha(p)==row['sha256'];pins[str(p)]=sha(p)
    actual=root/target
    assert (sha(actual) if actual.is_file() else None)==row['before_sha256'],target
    if actual.is_file():pins[str(actual)]=sha(actual)
parent=root/'Logs/SkirmishProduction/Offline/InstalledAircraftSharedPreparation';parent.mkdir(parents=True,exist_ok=True)
output=Path(tempfile.mkdtemp(prefix='compile-',dir=parent))
evidence=Path(tempfile.mkdtemp(prefix='installed-resume-owning-',dir=root/'Design/AgentReports/SkirmishExpansion/S005/aircraft-framing-inventory'))
dotnet=shutil.which('dotnet');sdk=subprocess.run(['rtk','proxy',dotnet,'--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0]
version,location=sdk.split(' ',1);compiler=Path(location.strip('[]'))/version/'Roslyn/bincore/csc.dll';pins[str(compiler)]=sha(compiler)
records=[];built_editor=None
for owner in ('Game.Editor','Game.Tests.Editor'):
    old=next(r for r in base['records'] if r['owner']==owner)
    response=Path(base['scratch'])/(owner+'.rsp');pins[str(response)]=sha(response)
    lines=[];sources={};references={};base_sources_checked=0;base_refs_checked=0
    for line in response.read_text().splitlines():
        low=line.lower()
        if low.startswith(('-out:','-refout:')):continue
        if low.startswith(('-r:','/r:','-reference:','/reference:')):
            p=Path(line.split(':',1)[1].strip('"'));p=p if p.is_absolute() else root/p
            assert p.is_file() and sha(p)==old['external_reference_sha256'][str(p)],'Strict inherited reference changed: '+str(p)
            pins[str(p)]=sha(p);base_refs_checked+=1
            if owner=='Game.Tests.Editor' and p.name=='Game.Editor.ref.dll':
                assert built_editor is not None;p=built_editor;line='-r:"'+str(p)+'"'
            references[str(p)]=sha(p);pins[str(p)]=sha(p)
        elif not line.startswith(('-','/')) and line.strip('"').endswith('.cs'):
            p=Path(line.strip('"'));p=p if p.is_absolute() else root/p
            scratch=Path(base['scratch'])/'Sources'
            target=p.relative_to(scratch).as_posix() if p.is_relative_to(scratch) else p.relative_to(root).as_posix()
            assert p.is_file() and sha(p)==old['source_sha256'][target],'Strict inherited source changed: '+str(p)
            pins[str(p)]=sha(p);base_sources_checked+=1
            if target in overrides:
                row=overrides[target];assert row['assembly']==owner and target not in consumed
                consumed.add(target);p=root/row['source'];line='"'+str(p)+'"'
            sources[target]=dict(path=str(p),sha256=sha(p));pins[str(p)]=sha(p)
        lines.append(line)
    assert base_sources_checked==old['sources']
    for target,row in overrides.items():
        if row['assembly']!=owner or target in consumed:continue
        p=root/row['source'];lines.append('"'+str(p)+'"');sources[target]=dict(path=str(p),sha256=sha(p));consumed.add(target)
    destination=output/(owner+'.dll');reference=output/(owner+'.ref.dll')
    lines+=['-out:"'+str(destination)+'"','-refout:"'+str(reference)+'"']
    rsp=output/(owner+'.rsp');rsp.write_text('\n'.join(lines)+'\n')
    result=subprocess.run(['rtk','proxy',dotnet,str(compiler),'@'+str(rsp)],cwd=root,capture_output=True,text=True)
    (evidence/(owner+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8');shutil.copyfile(rsp,evidence/rsp.name)
    records.append(dict(owner=owner,exit_code=result.returncode,sources=sources,references=references,
        inherited_source_checks=base_sources_checked,inherited_reference_checks=base_refs_checked,analyzers_omitted=old['analyzers_omitted']))
    if result.returncode:break
    if owner=='Game.Editor':built_editor=reference
mismatches=[p for p,digest in pins.items() if not Path(p).is_file() or sha(Path(p))!=digest]
passed=len(records)==2 and all(r['exit_code']==0 for r in records) and not mismatches and consumed==set(overrides)
receipt=dict(result='PassedStrict160EditorTestsExplicit15' if passed else 'Failed',records=records,input_sha256=pins,mismatches=mismatches,
    consumed_overrides=sorted(consumed),expected_overrides=15,no_fallback=True,no_ambient_current_assets=True,
    base_proof=str(authority.relative_to(root)),composition_sha256=sha(manifest),output=str(output),native_executed=False,tests_executed=False)
(evidence/'result.json').write_text(json.dumps(receipt,indent=2)+'\n')
for row in overrides.values():
    p=root/row['source'];dest=evidence/'overlays'/row['target'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(p,dest)
shutil.copyfile(Path(__file__),evidence/'compile_focused.py');shutil.copyfile(manifest,evidence/'composition.manifest.json')
print(json.dumps(dict(result=receipt['result'],owners=[dict(owner=r['owner'],exit_code=r['exit_code'],sources=len(r['sources']),references=len(r['references'])) for r in records],mismatches=mismatches,consumed=len(consumed),evidence=str(evidence))))
raise SystemExit(0 if passed else 1)
