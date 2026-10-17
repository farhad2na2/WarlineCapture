"""Archived composite owning receipts. No physical source or external SDK reads."""
from pathlib import Path
from collections import Counter
import ntpath
from skirmish_production_operational_inspector import need,parse

def validate(read,proof,data):
 def key(value):
  p=Path(value);p=p if p.is_absolute()else read.root/p
  need('..'not in p.parts,'Compiler metadata traversal')
  return ntpath.normcase(ntpath.normpath(str(p)))
 def index(values):
  answer={}
  for p,h in values.items():
   k=key(p);need(k not in answer or answer[k]==h,'Conflicting compiler metadata alias');answer[k]=h
  return answer
 off=data['offline_result'];scope=data['offline_scope'];now=index(data['offline_pins_before'])
 graph=data['owner_graph'];names=graph['owners'];deps=graph['dependencies']
 need(len(names)==len(set(names))==24 and set(deps)==set(names)and all(set(deps[n])<=set(names[:i])for i,n in enumerate(names)),'Actual24 topological owners')
 owner_map={}
 for owner,paths in graph.get('headOwnershipAudit',{}).get('owners',{}).items():
  for path in paths:
   need(path not in owner_map or owner_map[path]==owner,'Conflicting original HEAD ownership');owner_map[path]=owner
 additions={r['path']:r for r in graph.get('reviewedNewSourceMembership',[])}
 for path,row in additions.items():
  need(path not in owner_map or owner_map[path]==row['owner'],'Reviewed original membership owner conflict');owner_map[path]=row['owner']
 def normalized_sources(rows):
  result=[]
  for row in rows:
   item=dict(row)
   if 'owner'not in item:
    if item['path']in additions:need(item['sha256']==additions[item['path']]['sha256'],'Exact reviewed original new source bytes')
    need(item['path']in owner_map,'Original schema source missing actual pinned HEAD owner');item['owner']=owner_map[item['path']]
   result.append(item)
  return result
 fresh=scope['ownersFresh'];retained=scope['ownersReused']
 need(len(fresh)==off['ownersFresh'] and len(set(fresh))==len(fresh) and len(retained)==off['ownersReused'] and set(retained)==set(names)-set(fresh)and all(not(set(deps[n])&set(fresh))for n in retained),'Actual two-fresh dependency closure')
 manifest=data['compiler_lineage'];need(set(manifest)=={'runs','responses'},'Exact composite lineage schema')
 need(set(manifest['responses'])==set(names),'Every actual owning response required')
 results={key(proof['offline_result']['path']):(off,data['offline_pins_before'],data['offline_sources'])}
 runrefs={key(r['result']['path']):r for r in manifest['runs']}
 need(len(runrefs)==len(manifest['runs']),'Unique historical terminal refs')
 def load_result(path):
  k=key(path)
  if k in results:return results[k]
  need(k in runrefs,'Missing actual retained terminal '+str(path));refs=runrefs[k]
  need(set(refs)in ({'result','pinsBefore','sources'},{'result','pinsBefore','pinsAfter','sources'}),'Exact historical run refs')
  q,result=read.obj(refs['result']);_,before=read.obj(refs['pinsBefore']);sourcePath,sourceText=read.read(refs['sources']);originSources=normalized_sources(parse(sourceText))
  need(sourcePath==q.parent/'source-manifest.json' and isinstance(originSources,list),'Exact origin source manifest sibling')
  if 'pinsAfter'in refs:
   _,after=read.obj(refs['pinsAfter'])
   need(read.path(refs['pinsAfter']['path'])==q.parent/'pins-after.json','Historical after-map sibling')
   need(before==after,'Actual historical composite input map equality')
  else:need(result.get('ownersFresh')==24 and result.get('ownersReused')==0 and result.get('stopBeforeLaunchReason')is None,'Only original r3 full24 schema admits before map plus mismatch result')
  need(q==read.path(path)and read.path(refs['pinsBefore']['path'])==q.parent/'pins-before.json','Historical terminal siblings')
  need(now.get(k)==refs['result']['sha256']and all(now.get(key(v['path']))==v['sha256']for v in refs.values()),'Historical refs must be actual latest compiler pins')
  need(result['result']=='Passed'and result['mismatches']==[] ,'Actual historical frozen successful result')
  need(len(result['owners'])==24 and len({r['owner']for r in result['owners']})==24 and all(r['exit']==0 and r.get('stopReason')is None for r in result['owners'])and len(result['outputs'])==72,'Actual historical24/72 terminals')
  results[k]=(result,before,originSources);return results[k]
 def outputs(result,owner):
  found={}
  for suffix in ('.dll','.ref.dll','.pdb'):
   matches=[(p,h)for p,h in result['outputs'].items()if Path(p).name==owner+suffix]
   need(len(matches)==1,'Unique composite output '+owner+suffix)
   found[suffix]=matches[0]
  return found
 def actual_origin(owner,path,seen=()):
  k=key(path);need(k not in seen and len(seen)<12,'Cyclic/deep retained result lineage')
  result,pins,originSources=load_result(path);rows=[r for r in result['owners']if r['owner']==owner];need(len(rows)==1,'Unique actual owner')
  row=rows[0]
  if row.get('reused')is not True:
   need(all(read.path(p).parent==read.path(path).parent for p,h in outputs(result,owner).values()),'Fresh owner outputs belong to its actual terminal')
   return result,pins,read.path(path),originSources
  previous=row['priorResult'];prior,_,_=load_result(previous);old=next(r for r in prior['owners']if r['owner']==owner)
  need({k:v for k,v in row.items()if k not in ('reused','priorResult')}=={k:v for k,v in old.items()if k not in ('reused','priorResult')},'Retained owner facts changed')
  need(outputs(result,owner)==outputs(prior,owner),'Retained output paths/hashes changed')
  return actual_origin(owner,previous,seen+(k,))
 sources=data['offline_sources'];need(len({r['path']for r in sources})==len(sources),'Unique current clean source mapping')
 all_sources=[];required=set();latest_outputs=index(off['outputs'])
 flags_allowed={'-target','-langversion','/deterministic','/optimize-','/debug','/nologo','/RuntimeMetadataVersion','/utf8output','/preferreduilang','/unsafe+','/nowarn','-define'}
 prefixes=('-r:','/r:','-reference:','/reference:','-analyzer:','/analyzer:','-additionalfile:','/additionalfile:','-analyzerconfig:','/analyzerconfig:')
 for owner in names:
  origin,originpins,path,originSources=actual_origin(owner,proof['offline_result']['path']);old=index(originpins);old_outputs=index(origin['outputs'])
  ref=manifest['responses'][owner];rsp,text=read.read(ref)
  need(rsp==path.parent/(owner+'.rsp')and old.get(key(rsp))==now.get(key(rsp))==ref['sha256'],'Actual owning RSP pinned at original compile')
  declared=[];out=[]
  for line in text.splitlines():
   if not line:continue
   low=line.lower()
   if line.startswith('"')and line.endswith('"'):
    k=key(line.strip('"'));declared.append(k);required.add(k)
   elif low.startswith(('-out:','/out:','-refout:','/refout:')):
    kind,value=line.split(':',1);out.append((kind.lstrip('-/').lower(),key(value.strip('"'))))
   elif low.startswith(prefixes):
    value=line.split(':',1)[1].strip('"');k=key(value)
    expected=old_outputs.get(k,old.get(k));actual=latest_outputs.get(k,now.get(k))
    need(expected is not None and actual==expected,'Required declared compiler reference/analyzer/additional input absent/drift')
    required.add(k)
   else:need(line.split(':',1)[0]in flags_allowed,'Unclassified owning compiler option '+line)
  rows=[r for r in originSources if r['owner']==owner]
  current=[r for r in sources if r['owner']==owner]
  need({r['path']:r['sha256']for r in rows}=={r['path']:r['sha256']for r in current},'Exact origin/current repo-relative owning source membership and bytes')
  for row in current:need(now.get(key(row['compiledSource']))==row['sha256'],'Latest source copy input pin absent/drift')
  need(Counter(declared)==Counter(key(r['compiledSource'])for r in rows),'Exact complete clean owning source membership')
  for row in rows:need(old.get(key(row['compiledSource']))==now.get(key(row['compiledSource']))==row['sha256'],'Required original/current clean source pin missing/drift')
  expected_out=outputs(origin,owner)
  need(Counter(out)==Counter([('out',key(expected_out['.dll'][0])),('refout',key(expected_out['.ref.dll'][0]))]),'Exact original output destinations')
  all_sources+=declared
  for suffix in ('dotnet.exe','sdk/8.0.318/Roslyn/bincore/csc.dll'):
   k=key('D:/Program Files/Unity/6000.5.8f1/Editor/Data/DotNetSdk/'+suffix)
   need(k in old and now.get(k)==old[k],'Required actual Unity SDK input missing/drift');required.add(k)
 need(len(all_sources)==len(set(all_sources))==len(sources),'Every current clean owning source exactly once')
 prior=off.get('priorResult');need(isinstance(prior,str),'Actual prior composite for source delta')
 _,_,previous=load_result(prior)
 previousMap={x['path']:(x['owner'],x['sha256'])for x in previous};currentMap={x['path']:(x['owner'],x['sha256'])for x in sources}
 changed={x[0]for path,x in currentMap.items()if previousMap.get(path)!=x}|{x[0]for path,x in previousMap.items()if path not in currentMap}
 closure=set(changed)
 while True:
  expanded=closure|{n for n in names if closure.intersection(deps[n])}
  if expanded==closure:break
  closure=expanded
 need(closure<=set(fresh),'Fresh owners omit actual changed-source reverse closure')
 return dict(owners=24,fresh=len(fresh),retained=len(retained),outputs=72,historicalRuns=len(results)-1,declaredRequiredInputs=len(required),inputOmissionsAllowed=False,archivedMetadataCompared=True,physicalProjectRehashed=False,externalSdkRead=False)
