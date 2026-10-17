"""Only the corrected terminal assertion; reuse the three proven owning artifacts."""
from pathlib import Path
import hashlib,json,shutil,subprocess,tempfile
root=Path(__file__).resolve().parents[4];folder=Path(__file__).resolve().parent
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
parent=root/'Logs/SkirmishProduction/Offline/AriaUiOwnedStorage/owning-142sui8j'
proof=parent/'result.json';old=json.loads(proof.read_text());assert old['result']=='PassedFourOwningAssemblies'
manifest=folder/'manifest.json';rows=json.loads(manifest.read_text())['files']
prior=json.loads((parent/'manifest.json').read_text())['files']
changed=[r['target'] for r in rows if r!=next(p for p in prior if p['target']==r['target'])]
target='Assets/Tests/Editor/SkirmishExpansion/AriaUiOwnedStorageTests.cs'
assert changed==[target]
pins={str(proof):sha(proof),str(manifest):sha(manifest),str(Path(__file__)):sha(Path(__file__))}
for record in old['records']:
    for key,row in record['sources'].items():
        if key==target:continue
        p=Path(row['path']);assert sha(p)==row['sha256'];pins[str(p)]=sha(p)
    for path,h in record['references'].items():
        p=Path(path);assert sha(p)==h;pins[str(p)]=h
for row in rows:
    p=root/row['source'];assert sha(p)==row['sha256'];pins[str(p)]=sha(p)
    p=root/row['target'];assert (sha(p) if p.exists() else None)==row['before_sha256']
    if p.exists():pins[str(p)]=sha(p)
source=root/next(r['source'] for r in rows if r['target']==target)
previous=parent/'Sources'/target
before=previous.read_text();after=source.read_text()
expected=before.replace('            Assert.AreEqual(f.Root,f.Em.GetComponentData<AriaSkirmishObservationOwner>(storage).Boundary);','            Assert.AreEqual(f.Root,f.Em.GetComponentData<AriaSkirmishObservationOwner>(storage).Boundary);\n            Assert.AreEqual(AriaSkirmishIntent.DefendBase,UiShellRuntimeGateway.ReadAriaSkirmishIntent());').replace('            f.Tick();\n            Assert.AreEqual(AriaPlayObservationKind.Finished','            f.Tick();\n            Assert.AreEqual(AriaPlayObservationKind.Finished')
# One old post-Tick assertion becomes the production planner's existing Handback.
i=expected.index('            f.Tick();\n            Assert.AreEqual(AriaPlayObservationKind.Finished')
expected=expected[:i]+expected[i:].replace('Assert.AreEqual(AriaSkirmishIntent.DefendBase,UiShellRuntimeGateway.ReadAriaSkirmishIntent());','Assert.AreEqual(AriaSkirmishIntent.Handback,UiShellRuntimeGateway.ReadAriaSkirmishIntent());',1)
assert after==expected
output=Path(tempfile.mkdtemp(prefix='tests-terminal-',dir=parent.parent))
rsp=parent/'Game.Tests.Editor.rsp';lines=[l for l in rsp.read_text().splitlines() if not l.lower().startswith(('-out:','-refout:'))];pins[str(rsp)]=sha(rsp)
lines+=['-out:"'+str(output/'Game.Tests.Editor.dll')+'"','-refout:"'+str(output/'Game.Tests.Editor.ref.dll')+'"']
response=output/'Game.Tests.Editor.rsp';response.write_text('\n'.join(lines)+'\n')
dotnet=shutil.which('dotnet');sdk=subprocess.run(['rtk','proxy',dotnet,'--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0];v,l=sdk.split(' ',1);csc=Path(l.strip('[]'))/v/'Roslyn/bincore/csc.dll';pins[str(csc)]=sha(csc)
result=subprocess.run(['rtk','proxy',dotnet,str(csc),'@'+str(response)],cwd=root,capture_output=True,text=True)
(output/'Game.Tests.Editor.log').write_text(result.stdout+result.stderr,encoding='utf-8')
mismatch=[p for p,h in pins.items() if not Path(p).exists() or sha(Path(p))!=h]
receipt=dict(result='PassedTestsTerminalAssertionSuccessor' if result.returncode==0 and not mismatch else 'Failed',parent_proof=str(proof),parent_proof_sha256=sha(proof),unchanged_parent_owners=[r['owner'] for r in old['records'][:3]],only_changed_target=target,source_sha256=sha(source),manifest_sha256=sha(manifest),input_sha256=pins,mismatches=mismatch,exit_code=result.returncode,native_executed=False,tests_executed=False)
(output/'result.json').write_text(json.dumps(receipt,indent=2)+'\n');shutil.copyfile(manifest,output/'manifest.json')
print(json.dumps(dict(result=receipt['result'],mismatches=mismatch,output=str(output))))
raise SystemExit(0 if result.returncode==0 and not mismatch else 1)
