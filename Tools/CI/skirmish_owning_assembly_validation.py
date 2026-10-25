"""Exact-parent Unity owning-source validation, using installed compiler/reference inputs.

No Unity execution. Payload ownership coverage is mandatory; project references are
allowed only by the resulting asmdef and replaced with freshly compiled outputs.
"""
from pathlib import Path
import argparse,hashlib,json,shlex,subprocess,shutil,time

def digest(raw):return hashlib.sha256(raw).hexdigest()
def need(ok,text):
 if not ok:raise ValueError(text)
def tree(repo,parent):
 paths=subprocess.check_output(['git','ls-tree','-r','--name-only',parent,'Assets'],cwd=repo).decode().splitlines();wanted=[x for x in paths if x.endswith(('.cs','.asmdef','.asmref','.asmdef.meta'))];raw=subprocess.check_output(['git','cat-file','--batch'],input=''.join(parent+':'+p+'\n'for p in wanted).encode(),cwd=repo);out={};at=0
 for path in wanted:
  end=raw.index(b'\n',at);head=raw[at:end].split();need(len(head)==3 and head[1]==b'blob','Exact committed blob required: '+path);n=int(head[2]);out[path]=raw[end+1:end+1+n];at=end+2+n
 need(at==len(raw),'Git blob boundary');return out

def discover(files):
 defs={};guids={};boundaries={}
 for path,raw in files.items():
  if not path.endswith('.asmdef'):continue
  value=json.loads(raw);name=value['name'];need(name not in defs,'Duplicate assembly name: '+name);defs[name]={'path':path,'definition':value};boundaries[str(Path(path).parent)]=name
  meta=files.get(path+'.meta',b'').decode();guid=next((x.split(':',1)[1].strip()for x in meta.splitlines()if x.startswith('guid:')),None)
  if guid:need(guid not in guids,'Duplicate asmdef GUID');guids[guid]=name
 for path,raw in files.items():
  if not path.endswith('.asmref'):continue
  ref=json.loads(raw)['reference'];name=guids.get(ref[5:])if ref.startswith('GUID:')else ref;need(name in defs,'Unknown asmref: '+path);directory=str(Path(path).parent);need(directory not in boundaries,'Conflicting assembly boundary');boundaries[directory]=name
 owners={}
 for path in files:
  if not path.endswith('.cs'):continue
  eligible=[d for d in boundaries if path.startswith(d+'/')];owners[path]=boundaries[max(eligible,key=len)]if eligible else 'Assembly-CSharp-Editor'if'/Editor/'in path else'Assembly-CSharp'
 return defs,owners,guids

def project_references(defs,guids):
 result={}
 for name,item in defs.items():
  refs=[]
  for ref in item['definition'].get('references',[]):
   target=guids.get(ref[5:])if ref.startswith('GUID:')else ref
   need(target is not None,'Unresolved asmdef reference: '+ref)
   if target in defs:refs.append(target)
  result[name]=refs
 return result

def choose_owners(files,payload_paths,requested=()):
 defs,owners,guids=discover(files);affected={owners[x]for x in payload_paths if x.endswith('.cs')};affected|={json.loads(files[x])['name']for x in payload_paths if x.endswith('.asmdef')};targets=affected|set(requested);need(targets and all(x in defs for x in targets),'Explicit existing owning assemblies required');refs=project_references(defs,guids);order=[];active=set();done=set()
 def visit(name):
  if name in done:return
  need(name not in active,'Assembly dependency cycle: '+name);active.add(name)
  for dep in refs[name]:
   if dep in targets:visit(dep)
  active.remove(name);done.add(name);order.append(name)
 for name in sorted(targets):visit(name)
 coverage={x:{'owner':owners[x],'compiled_count':0}for x in payload_paths if x.endswith('.cs')}
 return defs,owners,refs,order,coverage

def check_supported_boundaries(before,after,payload_paths):
 for path in payload_paths:
  if path.endswith(('.asmref','.asmdef.meta')):
   need(before.get(path)==after[path],'Assembly boundary/meta edits unsupported: '+path)
  if path.endswith('.asmdef'):
   need(path in before,'New assembly boundary unsupported: '+path)
   old=json.loads(before[path]);new=json.loads(after[path]);old.pop('references',None);new.pop('references',None);need(old==new,'Only existing asmdef reference edits supported: '+path)

def check_profile(assembly,profile):
 need(not assembly.get('excludePlatforms'),'excludePlatforms unsupported')
 allowed=assembly.get('includePlatforms',[])
 need(not allowed or allowed==['Editor']and profile=='editor','includePlatforms unsupported for selected profile')
 need(not assembly.get('defineConstraints'),'defineConstraints unsupported')
 need(not assembly.get('versionDefines'),'versionDefines unsupported')

def check_defines(profile,defines):
 need(profile!='editor'or 'UNITY_EDITOR'in defines,'Editor response missing UNITY_EDITOR')
 need(profile!='noneditor'or not any(x.startswith('UNITY_EDITOR')for x in defines),'Editor define leaked')
 need('WARLINE_VALIDATION_PLAYER'not in defines,'Unpublished player define leaked')

def validate(repo,parent,payload,output,profile,requested,dotnet,csc,response_directory):
 repo=Path(repo).resolve();payload=Path(payload).resolve();out=Path(output).absolute();need(not out.exists(),'Fresh validation output required');out.mkdir(parents=True);files=tree(repo,parent);before=dict(files);payload_paths=[];pins={str(Path(__file__).resolve()):digest(Path(__file__).read_bytes())};preimages={}
 for p in sorted(payload.rglob('*')):
  if not p.is_file()or'__pycache__'in p.parts:continue
  rel=str(p.relative_to(payload))
  if not rel.startswith('Assets/')or not rel.endswith(('.cs','.asmdef','.asmref','.asmdef.meta')):continue
  raw=p.read_bytes();preimages[rel]=digest(files[rel])if rel in files else None;files[rel]=raw;payload_paths.append(rel);pins[str(p)]=digest(raw)
 check_supported_boundaries(before,files,payload_paths)
 defs,owners,refs,order,coverage=choose_owners(files,payload_paths,requested);source=out/'exact-source';emitted={}
 for rel,raw in files.items():
  if rel.endswith(('.asmdef','.asmref','.asmdef.meta'))or rel.endswith('.cs')and owners[rel]in order:
   p=source/rel;p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw);pins[str(p)]=digest(raw);emitted[rel]=p
 dotnet=Path(dotnet).resolve(strict=True);csc=Path(csc).resolve(strict=True);response_directory=Path(response_directory).resolve(strict=True);need(dotnet.is_file()and csc.is_file()and response_directory.is_dir(),'Explicit compiler/response inputs required');pins[str(dotnet)]=digest(dotnet.read_bytes());pins[str(csc)]=digest(csc.read_bytes());made={};records=[];reference_checks=[];all_sources=set()
 for name in order:
  own=sorted(x for x,o in owners.items()if o==name);need(own,'Full owning source set is empty: '+name);need(not(all_sources&set(own)),'Source compiled under multiple owners');all_sources.update(own)
  assembly=defs[name]['definition'];check_profile(assembly,profile);base=response_directory/(name+'.rsp');need(base.is_file(),'Installed owning response missing: '+name);raw=base.read_bytes();pins[str(base)]=digest(raw);args=[];seen=set();filtered=[];defines=[]
  for a in shlex.split(raw.decode()):
   if a.endswith('.cs')and not a.startswith('-')or a.startswith(('-out:','-refout:','-generatedfilesout:')):continue
   if a.startswith('-define:'):
    values=[x for x in a[8:].split(';')if x!='WARLINE_VALIDATION_PLAYER'and not(profile=='noneditor'and x.startswith('UNITY_EDITOR'))];defines+=values
    if values:args.append('-define:'+';'.join(values))
    continue
   prefix=next((v for v in ['-r:','-analyzer:','/additionalfile:','-additionalfile:','/analyzerconfig:','-analyzerconfig:']if a.startswith(v)),None)
   if prefix:
    value=a[len(prefix):];alias,sep,path=value.partition('=');path=path if sep else value;p=Path(path);p=p if p.is_absolute()else repo/p;project=p.name.removesuffix('.ref.dll').removesuffix('.dll')
    if prefix=='-r:':
     if project in defs and project not in refs[name]:filtered.append(str(p));continue
     if profile=='noneditor'and p.name.startswith('UnityEditor'):continue
     if project in made:p=made[project]
     seen.add(project);reference_checks.append({'owner':name,'reference':project,'declared':project not in defs or project in refs[name],'fresh':project in made,'path':str(p)})
    need(p.exists(),'Reference/input missing: '+str(p));pins[str(p)]=digest(p.read_bytes());args.append(prefix+((alias+'=')if sep else'')+str(p));continue
   args.append(a)
  for dep in refs[name]:
   if dep in seen:continue
   p=made.get(dep,response_directory/(dep+'.ref.dll'))
   if not p.exists():p=repo/'Library/ScriptAssemblies'/(dep+'.dll')
   need(p.exists(),'Declared project dependency unavailable: '+dep);args.append('-r:'+str(p));pins[str(p)]=digest(p.read_bytes());reference_checks.append({'owner':name,'reference':dep,'declared':True,'fresh':dep in made,'path':str(p)})
  check_defines(profile,defines)
  for rel in own:
   args.append(str(emitted[rel]))
   if rel in coverage:need(coverage[rel]['owner']==name,'Incorrect affected owner');coverage[rel]['compiled_count']+=1
  args+=['-out:'+str(out/(name+'.dll')),'-refout:'+str(out/(name+'.ref.dll'))];rsp=out/(name+'.rsp');rsp.write_text('\n'.join(json.dumps(x)for x in args)+'\n');start=time.monotonic();p=subprocess.run([str(dotnet),str(csc),'@'+str(rsp)],cwd=repo,capture_output=True,text=True,timeout=180);text=p.stdout+p.stderr;(out/(name+'.log')).write_text(text);record={'assembly':name,'asmdef_path':defs[name]['path'],'asmdef_sha256':digest(files[defs[name]['path']]),'source_count':len(own),'sources':own,'exit':p.returncode,'seconds':time.monotonic()-start,'defines':defines,'filtered_undeclared_refs':filtered,'errors':[x for x in text.splitlines()if'error 'in x]};records.append(record);print(name,p.returncode,len(own),flush=True)
  if p.returncode:break
  made[name]=out/(name+'.ref.dll')
 success=len(records)==len(order)and all(x['exit']==0 for x in records);covered=all(v['compiled_count']==1 for v in coverage.values());need(not success or covered,'Successful compiler plan skipped affected payload');drift=[p for p,d in pins.items()if digest(Path(p).read_bytes())!=d];need(not drift,'Validation input drift')
 report={'kind':'ExactOwningAssemblyValidation','parent':parent,'profile':profile,'resolved_compiler':{'dotnet':str(dotnet),'csc':str(csc),'response_directory':str(response_directory)},'status':'Passed'if success and covered else'Failed','plan':order,'payload_preimages':preimages,'affected_payload_coverage':coverage,'all_affected_sources_compiled_once':covered,'records':records,'project_reference_checks':reference_checks,'input_pins':pins,'input_drift':drift,'scope':'Actual nearest asmdef/asmref ownership; fullowner sources only; allowed direct refs; fresh outputs replace references downstream. Installed unchanged dependency binaries explicitly pinned. No Unity/world.'};(out/'result.json').write_text(json.dumps(report,indent=2)+'\n');return report
if __name__=='__main__':
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('--repo',required=True);p.add_argument('--parent',required=True);p.add_argument('--payload',required=True);p.add_argument('--output',required=True);p.add_argument('--profile',choices=['editor','noneditor'],required=True);p.add_argument('--dotnet',required=True);p.add_argument('--csc',required=True);p.add_argument('--response-directory',required=True);p.add_argument('--owners',nargs='*',default=['Game.Simulation','Game.Configs','Game.Runtime','Game.UI.Runtime']);a=p.parse_args();result=validate(a.repo,a.parent,a.payload,a.output,a.profile,a.owners,a.dotnet,a.csc,a.response_directory);raise SystemExit(0 if result['status']=='Passed'else 1)
