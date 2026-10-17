"""Two owners on exact passed Batch/UI parent; no ambient reference fallback."""
import hashlib,json,shutil,subprocess,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];D=Path(__file__).resolve().parent
BASE=ROOT/'Logs/SkirmishProduction/Offline/ProductionBaselineBatch/after-storage-zmcrpuo6'
def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()
prior=json.loads((BASE/'result.json').read_text());assert prior['result']=='Passed' and prior['no_fallback'] and not prior['mismatches']
manifest=D/'manifest.json';m=json.loads(manifest.read_text());rows=m['files']
pins={str(manifest):sha(manifest),str(BASE/'result.json'):sha(BASE/'result.json'),str(Path(__file__)):sha(Path(__file__))}
for row in rows+m['tools']:
 p=ROOT/row['source'];assert sha(p)==row['sha256'];pins[str(p)]=row['sha256']
for record in prior['records']:
 for item in record['sources'].values():
  p=Path(item['path']);assert sha(p)==item['sha256'];pins[str(p)]=item['sha256']
 for path,value in record['references'].items():assert sha(path)==value;pins[path]=value
outbase=ROOT/'Logs/SkirmishProduction/Offline/ProductionBaselineBatchRuntimeGate';outbase.mkdir(parents=True,exist_ok=True)
out=Path(tempfile.mkdtemp(prefix='owning-',dir=outbase));dotnet=shutil.which('dotnet')
sdk=subprocess.run(['rtk','proxy',dotnet,'--list-sdks'],capture_output=True,text=True,check=True).stdout.splitlines()[0];version,location=sdk.split(' ',1)
csc=Path(location.strip('[]'))/version/'Roslyn/bincore/csc.dll';pins[str(csc)]=sha(csc)
records=[];consumed=set();built={}
for record in prior['records']:
 owner=record['owner'];rsp=BASE/(owner+'.rsp');pins[str(rsp)]=sha(rsp);lines=rsp.read_text().splitlines();sources=dict(record['sources']);refs=dict(record['references'])
 for row in rows:
  if row['target'] not in sources:continue
  old='"'+sources[row['target']]['path']+'"';assert lines.count(old)==1
  copied=out/row['target'];copied.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(ROOT/row['source'],copied)
  assert sha(copied)==row['sha256'];lines=[('"'+str(copied)+'"')if l==old else l for l in lines];sources[row['target']]={'path':str(copied),'sha256':row['sha256']};consumed.add(row['target'])
 for path,value in list(refs.items()):
  name=Path(path).name
  if name in built:
   replacement=built[name];matches=[i for i,l in enumerate(lines)if l.startswith(('-r:','-reference:'))and path in l];assert len(matches)==1,(owner,path,matches)
   lines[matches[0]]=lines[matches[0]].replace(path,str(replacement));del refs[path];refs[str(replacement)]=sha(replacement)
 lines=[l for l in lines if not l.lower().startswith(('-out:','-refout:'))]
 dll=out/(owner+'.dll');ref=out/(owner+'.ref.dll');lines+=['-out:"'+str(dll)+'"','-refout:"'+str(ref)+'"']
 response=out/(owner+'.rsp');response.write_text('\n'.join(lines)+'\n');result=subprocess.run(['rtk','proxy',dotnet,str(csc),'@'+str(response)],cwd=ROOT,capture_output=True,text=True)
 (out/(owner+'.log')).write_text(result.stdout+result.stderr,encoding='utf-8')
 records.append(dict(owner=owner,exit_code=result.returncode,sources=sources,references=refs,source_count=len(sources),reference_count=len(refs)))
 if result.returncode:break
 built[ref.name]=ref
mismatches=[p for p,v in pins.items()if not Path(p).exists()or sha(p)!=v]
for record in records:
 for item in record['sources'].values():
  if sha(item['path'])!=item['sha256']:mismatches.append(item['path'])
 for p,v in record['references'].items():
  if sha(p)!=v:mismatches.append(p)
passed=len(records)==2 and all(r['exit_code']==0 for r in records)and len(consumed)==9 and not mismatches
receipt=dict(result='Passed'if passed else'Failed',records=records,manifest_sha256=sha(manifest),parent=str(BASE/'result.json'),parent_sha256=sha(BASE/'result.json'),input_sha256=pins,mismatches=mismatches,consumed_overrides=sorted(consumed),no_fallback=True,native_executed=False,tests_executed=False)
(out/'result.json').write_text(json.dumps(receipt,indent=2)+'\n');print(json.dumps(dict(result=receipt['result'],output=str(out),mismatches=mismatches,owners=[(r['owner'],r['exit_code'])for r in records])))
raise SystemExit(0 if passed else 1)
