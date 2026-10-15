"""Compile HEAD278 + exactly eleven reviewed G30 overrides and one new focused test entry."""
import hashlib,json,shutil,subprocess,tempfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
BASE='27881d4ee700dde9a046fc36b23d3d73d24fa36b'
OWNERS=('Game.UI.Contracts','Game.Components','Game.Configs','Game.Runtime','Game.UI.Runtime','Game.Composition','Game.Editor','Game.Tests.Editor')
EXTRA='Tools/CI/SkirmishProductionDrafts/Ground30Subset58/Sources/Assets/Tests/Editor/SkirmishExpansion/SkirmishGroundObjective30PreparationValidation.cs.txt'
EXTRA_TARGET='Assets/Tests/Editor/SkirmishExpansion/SkirmishGroundObjective30PreparationValidation.cs'
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()

def main():
    manifest=json.loads((ROOT/'Tools/CI/SkirmishProductionDrafts/Ground30Subset58/manifest.json').read_text())
    if manifest['baseline_commit']!=BASE:raise ValueError('Exact HEAD278 baseline required')
    for row in manifest['files']:
        if sha(ROOT/row['source'])!=row['sha256']:raise ValueError('Snapshot source changed: '+row['target'])
    for row in manifest['artifacts']:
        if sha(ROOT/row['path'])!=row['sha256']:raise ValueError('Subset artifact changed: '+row['path'])
    overrides={r['target']:ROOT/r['source'] for r in manifest['files'] if r['target']!=EXTRA_TARGET}
    if len(overrides)!=11:raise ValueError('Exact eleven G30 source overrides required')
    parent=ROOT/'Logs/SkirmishProduction/Offline/HeadGround30';parent.mkdir(parents=True,exist_ok=True)
    output=Path(tempfile.mkdtemp(prefix='compile-',dir=parent));evidence_parent=ROOT/'Design/AgentReports/SkirmishExpansion/EngineeringCheckpoint'
    evidence_parent.mkdir(parents=True,exist_ok=True);evidence=Path(tempfile.mkdtemp(prefix='head278-g30-58-compile-',dir=evidence_parent))
    baseline=subprocess.check_output(['rtk','proxy','git','rev-parse',BASE],cwd=ROOT).decode().strip()
    responses={owner:max((ROOT/'Library/Bee').rglob(owner+'.rsp'),key=lambda p:p.stat().st_mtime_ns) for owner in OWNERS}
    for rsp in responses.values():
        text=rsp.read_text(encoding='utf-8-sig')
        if '6000.5.2' in text or '6000.5.8' not in text:raise ValueError('Actual version-matched5.8 references required')
    assets=set()
    for rsp in responses.values():
        for line in rsp.read_text(encoding='utf-8-sig').splitlines():
            path=line.strip('"').replace('\\','/')
            if not line.startswith(('-','/')) and path.startswith('Assets/') and path.endswith('.cs'):assets.add(path)
    queries=sorted(assets)
    proc=subprocess.run(['rtk','proxy','git','cat-file','--batch'],cwd=ROOT,input=''.join(BASE+':'+p+'\n' for p in queries).encode(),capture_output=True,check=True)
    raw=proc.stdout;offset=0;head={}
    for path in queries:
        end=raw.index(b'\n',offset);header=raw[offset:end].decode();offset=end+1
        if header.endswith(' missing'):continue
        _,kind,size=header.split();size=int(size)
        if kind!='blob':raise ValueError('HEAD source must be blob: '+path)
        head[path]=raw[offset:offset+size];offset+=size+1
    if offset!=len(raw):raise ValueError('Exact Git batch source stream required')
    dotnet=shutil.which('dotnet');sdk=subprocess.run(['rtk','proxy',dotnet,'--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0]
    version,location=sdk.split(' ',1);csc=Path(location.strip('[]'))/version/'Roslyn/bincore/csc.dll'
    records=[];built={};consumed=set();excluded=set()
    for owner in OWNERS:
        rsp=responses[owner];lines=[];pins={};refs={};omitted=0;origins={}
        for line in rsp.read_text(encoding='utf-8-sig').splitlines():
            low=line.lower()
            if low.startswith(('-out:','/out:','-refout:','/refout:')):continue
            if low.startswith(('-analyzer:','/analyzer:')):omitted+=1;continue
            if low.startswith(('-r:','/r:','-reference:','/reference:')):
                ref=Path(line.split(':',1)[1].strip('"'));ref=ref if ref.is_absolute() else ROOT/ref
                name=ref.name.replace('.ref.dll','.dll')
                if name in built:ref=built[name];line='-r:"'+str(ref)+'"'
                if not ref.is_file():raise ValueError('Missing owning reference; no fallback: '+str(ref))
                refs[str(ref)]=sha(ref)
            elif not line.startswith(('-','/')) and line.strip('"').endswith('.cs'):
                path=line.strip('"').replace('\\','/')
                if path.startswith('Assets/'):
                    if path in overrides:
                        data=overrides[path].read_bytes();origin='Reviewed G30 override';consumed.add(path)
                    elif path in head:data=head[path];origin='Immutable HEAD278 blob'
                    else:excluded.add(path);continue
                    selected=output/'Sources'/path;selected.parent.mkdir(parents=True,exist_ok=True);selected.write_bytes(data)
                    lines.append('"'+str(selected)+'"');pins[path]=sha(selected);origins[path]=origin;continue
                selected=Path(path);selected=selected if selected.is_absolute() else ROOT/selected
                if not selected.is_file():raise ValueError('Current package/generated source absent: '+path)
                pins[path]=sha(selected);origins[path]='Current package/generated source; not Assets'
            lines.append(line)
        # New G30 files may not yet appear in Bee. Add exact designated owner only.
        expected={r['target']:r['assembly'] for r in manifest['files'] if r['target']!=EXTRA_TARGET}
        for path,assembly in expected.items():
            if assembly!=owner or path in consumed:continue
            selected=output/'Sources'/path;selected.parent.mkdir(parents=True,exist_ok=True);selected.write_bytes(overrides[path].read_bytes())
            lines.append('"'+str(selected)+'"');pins[path]=sha(selected);origins[path]='Reviewed G30 override';consumed.add(path)
        if owner=='Game.Tests.Editor':
            selected=output/'Sources'/EXTRA_TARGET;selected.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/EXTRA,selected)
            lines.append('"'+str(selected)+'"');pins[EXTRA_TARGET]=sha(selected);origins[EXTRA_TARGET]='New standalone G30 preparation entry'
        dll=output/(owner+'.dll');reference=output/(owner+'.ref.dll');lines+=['-out:"'+str(dll)+'"','-refout:"'+str(reference)+'"']
        generated=output/(owner+'.rsp');generated.write_text('\n'.join(lines)+'\n',encoding='utf-8')
        result=subprocess.run(['rtk','proxy',dotnet,str(csc),'@'+str(generated)],cwd=ROOT,capture_output=True,text=True)
        (evidence/(owner+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
        stable=all(sha(Path(p))==digest for p,digest in refs.items()) and all(sha(output/'Sources'/p if p.startswith('Assets/') else Path(p) if Path(p).is_absolute() else ROOT/p)==digest for p,digest in pins.items()) and all(sha(ROOT/r['source'])==r['sha256'] for r in manifest['files'])
        records.append(dict(owner=owner,exit_code=result.returncode,sources=len(pins),source_sha256=pins,source_origin=origins,
            external_reference_sha256=refs,references_stable=stable,analyzers_omitted=omitted,actual_rsp=str(rsp),actual_rsp_sha256=sha(rsp)))
        if result.returncode or not stable:
            print('\n'.join(line for line in (result.stdout+result.stderr).splitlines() if 'error ' in line));break
        built[owner+'.dll']=reference
    passed=len(records)==len(OWNERS) and consumed==set(overrides) and all(r['exit_code']==0 and r['references_stable'] for r in records)
    receipt=dict(result='PassedHead278Ground30Subset58TypeClosure' if passed else 'FailedHead278Ground30Subset58TypeClosure',baseline_commit=baseline,
        proposed_eleven_source_sha256={p:sha(q) for p,q in overrides.items()},standalone_entry_sha256=sha(ROOT/EXTRA),
        records=records,consumed_overrides=sorted(consumed),excluded_current_only_assets=sorted(excluded),native_execution=False,
        tests_executed=False,external_scope='Current exact package/generated/external reference binaries; selected eight owning Assets source trees (including HEAD UI.Contracts) are HEAD278 plus exactly11 reviewed G30 overrides and one entry',scratch=str(output))
    (evidence/'result.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(result=receipt['result'],owners=[dict(name=r['owner'],exit_code=r['exit_code'],sources=r['sources']) for r in records],
        overrides_consumed=len(consumed),current_only_assets_excluded=len(excluded),evidence=str(evidence),native_execution=False),indent=2))
    return 0 if passed else 1

if __name__=='__main__':raise SystemExit(main())
