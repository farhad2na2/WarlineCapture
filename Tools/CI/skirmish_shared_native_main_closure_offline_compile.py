"""Existing HEAD/projection compiler pattern, fixed to f733 plus sealed shared adoption."""
import hashlib,json,shutil,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE='f733f9763'
OWNERS=('Game.Skirmish.Contracts','Game.UI.Contracts','Game.UI.Shell.Contracts.Ecs','Game.Components','Game.Configs',
        'Game.Rendering.Contracts','Game.Rendering','Game.Authoring','Game.Runtime','Game.UI.Runtime',
        'Game.UI.Shell.Ecs','Game.Composition','Game.Editor','Game.Tests.Editor')
CLOSURE='Tools/CI/SkirmishProductionDrafts/SharedNativeProductionPreparationAria70/composition.manifest.json'
sha=lambda path:hashlib.sha256(Path(path).read_bytes()).hexdigest()
def main():
    manifest=ROOT/CLOSURE;manifest_before=sha(manifest);closure=json.loads(manifest.read_text());rows=closure['files']
    for item in closure['inputs']:
        if sha(ROOT/item['path'])!=item['sha256']:raise ValueError('Sealed composition input changed: '+item['path'])
    overrides={r['target']:ROOT/r['source'] for r in rows if r['target'].endswith('.cs')}
    expected={r['target']:r['sha256'] for r in rows if r['target'].endswith('.cs')}
    if len(rows)!=closure['target_count'] or len({r['target'] for r in rows})!=len(rows):
        raise ValueError('Exact unique sealed shared-native source map required')
    for row in rows:
        target=ROOT/row['target'];actual=sha(target) if target.is_file() else None
        if sha(ROOT/row['source'])!=row['sha256'] or actual!=row['before_sha256']:
            raise ValueError('Shared adoption source/base bytes changed: '+row['target'])
    definitions={json.loads(p.read_text())['name']:p.parent for p in (ROOT/'Assets/Game/Scripts').rglob('*.asmdef')}
    definitions['Game.Tests.Editor']=ROOT/'Assets/Tests/Editor'
    owner_by_source={}
    for path in overrides:
        choices=[(len(str(directory)),name) for name,directory in definitions.items() if directory== (ROOT/path).parent or directory in (ROOT/path).parents]
        if not choices:raise ValueError('No exact owner for closure: '+path)
        owner_by_source[path]=max(choices)[1]
        if owner_by_source[path] not in OWNERS:raise ValueError('Missing explicit owner: '+path)
    parent=ROOT/'Logs/SkirmishProduction/Offline/SharedNativeMainClosure';parent.mkdir(parents=True,exist_ok=True)
    output=Path(tempfile.mkdtemp(prefix='compile-',dir=parent))
    reports=ROOT/'Design/AgentReports/SkirmishExpansion/S005/shared-native-main-closure';reports.mkdir(parents=True,exist_ok=True)
    evidence=Path(tempfile.mkdtemp(prefix='owning-',dir=reports))
    shutil.copyfile(manifest,evidence/'composition.manifest.json')
    shutil.copyfile(Path(__file__),evidence/Path(__file__).name)
    baseline=subprocess.check_output(['rtk','proxy','git','rev-parse',BASE],cwd=ROOT).decode().strip()
    responses={owner:max((ROOT/'Library/Bee').rglob(owner+'.rsp'),key=lambda p:p.stat().st_mtime_ns) for owner in OWNERS}
    rsp_pins={str(p):sha(p) for p in responses.values()}
    assets=set()
    for rsp in responses.values():
        for line in rsp.read_text(encoding='utf-8-sig').splitlines():
            path=line.strip('"').replace('\\','/')
            if not line.startswith(('-','/')) and path.startswith('Assets/') and path.endswith('.cs'):assets.add(path)
    queries=sorted(assets)
    proc=subprocess.run(['rtk','proxy','git','cat-file','--batch'],cwd=ROOT,
                        input=''.join(BASE+':'+p+'\n' for p in queries).encode(),capture_output=True,check=True)
    raw=proc.stdout;offset=0;head={}
    for path in queries:
        end=raw.index(b'\n',offset);header=raw[offset:end].decode();offset=end+1
        if header.endswith(' missing'):continue
        _,kind,size=header.split();size=int(size)
        if kind!='blob':raise ValueError('Immutable baseline source must be blob: '+path)
        head[path]=raw[offset:offset+size];offset+=size+1
    if offset!=len(raw):raise ValueError('Exact Git batch stream required')
    sdk=subprocess.run(['rtk','proxy','dotnet','--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0]
    version,location=sdk.split(' ',1);csc=Path(location.strip('[]'))/version/'Roslyn/bincore/csc.dll'
    records=[];built={};consumed=set();excluded=set();failure=None
    for owner in OWNERS:
        rsp=responses[owner];lines=[];pins={};refs={};origins={};omitted=0
        for line in rsp.read_text(encoding='utf-8-sig').splitlines():
            low=line.lower()
            if low.startswith(('-out:','/out:','-refout:','/refout:')):continue
            if low.startswith(('-analyzer:','/analyzer:')):omitted+=1;continue
            if low.startswith(('-r:','/r:','-reference:','/reference:')):
                ref=Path(line.split(':',1)[1].strip('"'));ref=ref if ref.is_absolute() else ROOT/ref
                name=ref.name.replace('.ref.dll','.dll')
                if name in built:ref=built[name];line='-r:"'+str(ref)+'"'
                if not ref.is_file():raise ValueError('Missing exact actual5.8 reference; no fallback: '+str(ref))
                refs[str(ref)]=sha(ref)
            elif not line.startswith(('-','/')) and line.strip('"').endswith('.cs'):
                path=line.strip('"').replace('\\','/')
                if path.startswith('Assets/'):
                    if path in overrides:data=overrides[path].read_bytes();origin='Exact shared adoption closure';consumed.add(path)
                    elif path in head:data=head[path];origin='Immutable f733 blob'
                    else:excluded.add(path);continue
                    selected=output/'Sources'/path;selected.parent.mkdir(parents=True,exist_ok=True);selected.write_bytes(data)
                    lines.append('"'+str(selected)+'"');pins[path]=sha(selected);origins[path]=origin;continue
                selected=Path(path);selected=selected if selected.is_absolute() else ROOT/selected
                if not selected.is_file():raise ValueError('Actual package/generated source absent: '+path)
                pins[path]=sha(selected);origins[path]='Actual package/generated source outside Assets'
            lines.append(line)
        for path,source in overrides.items():
            if owner_by_source[path]!=owner or path in consumed:continue
            selected=output/'Sources'/path;selected.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(source,selected)
            lines.append('"'+str(selected)+'"');pins[path]=sha(selected);origins[path]='Exact shared adoption closure';consumed.add(path)
        dll=output/(owner+'.dll');reference=output/(owner+'.ref.dll')
        lines+=['-out:"'+str(dll)+'"','-refout:"'+str(reference)+'"']
        generated=output/(owner+'.rsp');generated.write_text('\n'.join(lines)+'\n',encoding='utf-8');shutil.copyfile(generated,evidence/generated.name)
        result=subprocess.run(['rtk','proxy','dotnet',str(csc),'@'+str(generated)],cwd=ROOT,capture_output=True,text=True)
        (evidence/(owner+'.log')).write_text('EXIT '+str(result.returncode)+'\n'+result.stdout+result.stderr,encoding='utf-8')
        stable=all(sha(Path(p))==digest for p,digest in refs.items())
        records.append(dict(owner=owner,exit_code=result.returncode,sources=len(pins),source_sha256=pins,source_origin=origins,
            external_reference_sha256=refs,references_stable=stable,analyzers_omitted=omitted,actual_rsp=str(rsp),actual_rsp_sha256=sha(rsp)))
        print('[SharedNativeMainClosure] owner='+owner+' exit='+str(result.returncode)+' sources='+str(len(pins))+' stable='+str(stable),flush=True)
        if result.returncode or not stable:
            failure='\n'.join(line for line in (result.stdout+result.stderr).splitlines() if 'error ' in line) or 'Reference stability failed'
            print(failure,flush=True);break
        built[owner+'.dll']=reference
    stable=all(sha(p)==digest for p,digest in rsp_pins.items()) and all(sha(p)==expected[path] for path,p in overrides.items())
    stable=stable and sha(manifest)==manifest_before and all(sha(ROOT/i['path'])==i['sha256'] for i in closure['inputs'])
    for row in rows:
        target=ROOT/row['target'];actual=sha(target) if target.is_file() else None
        stable=stable and actual==row['before_sha256'] and sha(ROOT/row['source'])==row['sha256']
    passed=len(records)==len(OWNERS) and consumed==set(overrides) and stable and all(r['exit_code']==0 and r['references_stable'] for r in records)
    receipt=dict(result='PassedMainExactSharedClosure' if passed else 'FailedMainExactSharedClosure',baseline_commit=baseline,
        closure=CLOSURE,closure_sha256=sha(ROOT/CLOSURE),projection_manifest=str(manifest.relative_to(ROOT)),projection_sha256=sha(manifest),
        proposed_source_sha256=expected,declared_adoption_targets=rows,records=records,consumed_overrides=sorted(consumed),excluded_current_only_assets=sorted(excluded),
        actual_rsp_and_override_bytes_stable=stable,failure=failure,native_execution=False,tests_executed=False,
        external_scope='Actual5.8 package/generated/external reference binaries; fourteen owning Assets source trees are immutable f733 plus only explicit sealed shared-native adoption overrides. No current WIP Assets fallback.',scratch=str(output))
    (evidence/'result.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'result':receipt['result'],'evidence':str(evidence),'consumed':len(consumed),'excluded':len(excluded),'stable':stable}),flush=True)
    return 0 if passed else 1
if __name__=='__main__':raise SystemExit(main())
