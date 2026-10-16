"""Four explicit owners on immutable strict160 + installed-resume inputs; no ambient fallback."""
from pathlib import Path
import hashlib,json,shutil,subprocess,tempfile
root=Path(__file__).resolve().parents[4];folder=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
strict=root/'Design/AgentReports/SkirmishExpansion/S005/shared-native-main-closure/owning-uesn01hn/result.json'
resume=root/'Design/AgentReports/SkirmishExpansion/S005/aircraft-framing-inventory/installed-resume-owning-nrt4blbn/result.json'
assert sha(strict)=='c63aabd75fb0fa6fb9f2f76d311c48da23fcac85038c417b9a12ff1a7ab503a2'
assert sha(resume)=='daa1c1d107884e694171af47bd4dbd68bd585ca5d6c47369be97c7026e4c1aee'
a=json.loads(strict.read_text());b=json.loads(resume.read_text());manifest=folder/'manifest.json'
rows=json.loads(manifest.read_text())['files'];overrides={r['target']:r for r in rows};consumed=set()
pins={str(p):sha(p) for p in (strict,resume,manifest,Path(__file__))}
for r in rows:
    p=root/r['source'];assert sha(p)==r['sha256'];pins[str(p)]=sha(p)
    actual=root/r['target'];assert (sha(actual) if actual.is_file() else None)==r['before_sha256'],r['target']
    if actual.is_file():pins[str(actual)]=sha(actual)
parent=root/'Logs/SkirmishProduction/Offline/AriaUiOwnedStorage';parent.mkdir(parents=True,exist_ok=True)
output=Path(tempfile.mkdtemp(prefix='owning-',dir=parent));records=[];built={}
dotnet=shutil.which('dotnet');sdk=subprocess.run(['rtk','proxy',dotnet,'--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0]
version,location=sdk.split(' ',1);compiler=Path(location.strip('[]'))/version/'Roslyn/bincore/csc.dll';pins[str(compiler)]=sha(compiler)
for owner in ('Game.UI.Shell.Contracts.Ecs','Game.UI.Shell.Ecs','Game.Editor','Game.Tests.Editor'):
    is_strict=owner.startswith('Game.UI.')
    old=next(r for r in (a if is_strict else b)['records'] if r['owner']==owner)
    rsp=Path(a['scratch'] if is_strict else b['output'])/(owner+'.rsp');pins[str(rsp)]=sha(rsp)
    lines=[];sources={};refs={}
    for line in rsp.read_text().splitlines():
        low=line.lower()
        if low.startswith(('-out:','-refout:')):continue
        if low.startswith(('-r:','/r:','-reference:','/reference:')):
            p=Path(line.split(':',1)[1].strip('"'));p=p if p.is_absolute() else root/p
            expected=old['external_reference_sha256' if is_strict else 'references'][str(p)]
            assert sha(p)==expected,'Inherited ref changed: '+str(p);pins[str(p)]=sha(p)
            if p.name in built:p=built[p.name];line='-r:"'+str(p)+'"'
            refs[str(p)]=sha(p);pins[str(p)]=sha(p)
        elif not line.startswith(('-','/')):
            p=Path(line.strip('"'));p=p if p.is_absolute() else root/p
            if not p.is_file():raise AssertionError('Missing explicit source '+str(p))
            if is_strict:
                scratch=Path(a['scratch'])/'Sources'
                target=p.relative_to(scratch).as_posix() if p.is_relative_to(scratch) else p.relative_to(root).as_posix()
                expected=old['source_sha256'][target]
            else:
                matches=[(k,v) for k,v in old['sources'].items() if Path(v['path'])==p]
                assert len(matches)==1,str(p);target,row=matches[0];expected=row['sha256']
            assert sha(p)==expected,'Inherited source changed: '+str(p);pins[str(p)]=sha(p)
            if target in overrides:
                row=overrides[target];assert row['assembly']==owner;consumed.add(target);p=root/row['source'];line='"'+str(p)+'"'
            sources[target]={'path':str(p),'sha256':sha(p)};pins[str(p)]=sha(p)
        lines.append(line)
    for target,row in overrides.items():
        if row['assembly']!=owner or target in consumed:continue
        p=root/row['source'];lines.append('"'+str(p)+'"');sources[target]={'path':str(p),'sha256':sha(p)};consumed.add(target)
    dll=output/(owner+'.dll');ref=output/(owner+'.ref.dll')
    lines+=['-out:"'+str(dll)+'"','-refout:"'+str(ref)+'"'];response=output/(owner+'.rsp');response.write_text('\n'.join(lines)+'\n')
    result=subprocess.run(['rtk','proxy',dotnet,str(compiler),'@'+str(response)],cwd=root,capture_output=True,text=True)
    (output/(owner+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
    records.append(dict(owner=owner,exit_code=result.returncode,sources=sources,references=refs))
    if result.returncode:break
    built[owner+'.ref.dll']=ref
mismatches=[p for p,h in pins.items() if not Path(p).is_file() or sha(Path(p))!=h]
passed=len(records)==4 and all(r['exit_code']==0 for r in records) and not mismatches and consumed==set(overrides)
receipt=dict(result='PassedFourOwningAssemblies' if passed else 'Failed',records=records,input_sha256=pins,mismatches=mismatches,consumed_overrides=sorted(consumed),expected_overrides=sorted(overrides),no_fallback=True,no_ambient_current_assets=True,native_executed=False,tests_executed=False,output=str(output))
(output/'result.json').write_text(json.dumps(receipt,indent=2)+'\n')
shutil.copyfile(manifest,output/'manifest.json')
for r in rows:
    dest=output/'Sources'/r['target'];dest.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(root/r['source'],dest)
print(json.dumps(dict(result=receipt['result'],owners=[dict(owner=r['owner'],exit_code=r['exit_code'],sources=len(r['sources']),references=len(r['references'])) for r in records],mismatches=mismatches,output=str(output))))
raise SystemExit(0 if passed else 1)
