# Finite current-native lifetime projection; no runtime or asset writes.
from pathlib import Path
import argparse,hashlib,json,re
SYSTEMS='Assets/Game/Scripts/Systems/'
SOURCE_INPUTS=('BuildingCombatUtilitySystemHelper.cs', 'BuildingPlacementRuntimeTickCompositionSystemHelper.cs', 'BuildingRuntimeTickCompositionSystemHelper.cs', 'BuildingGameplayCompositionSystemHelper.cs', 'BuildingBarrierUtilitySystemHelper.cs')
HELPER_PINS={'utility-method-contract.json':'bca95245d1bcedb408fb31de5c571c70d089346ee994bcaa6c2551c3ae7fbe7c','utility-header.txt':'527030fd0dad8511ba0872954caff5e4c5784f3734350a2a196a3c33c03b6578','owner-adapter.cs.template':'85ae2b7a8a05bd7023b8ec6b79471a81696e182b799ae3be544cda6412d92561'}
NAMES={'TryResolveBaseBreachedAudioEvent','TryEmitBaseBreachedAudio','IsPlayerOwnedBaseBarrier','ResolveRuntimeCombatState','DestroyBlockerEntity','UpdateDestroyedBuildings','CollectDestroyedCleanupIds','AddDestroyedCleanupId','SyncDestroyedRuntimeBuildingCombatEntities','SyncDestroyedRuntimeBuildingCombatEntity','BeginDestroyedBuildingState','TryResolveBuildingWorldPosition','DestroyRuntimeBuildingBlockerEntity','FinalizeDestroyedBuilding','DestroyRuntimeBuildingEntities','DestroyEntity','DestroyRuntimeBuildingObject'}
PATTERN=r'^        (?:public|private|internal)\s+(?:static\s+)?[^\n]+?\s+(\w+)(?:<[^\n>]+>)?\s*\('
def sha(b):return hashlib.sha256(b).hexdigest()
def read_current(path):
 if path.stat().st_size>2*1024*1024:raise ValueError('Native/helper source exceeds finite2MiB bound: '+str(path))
 raw=path.read_bytes()
 if len(raw)>2*1024*1024:raise ValueError('Native/helper source grew over bound')
 if b'\r' in raw.replace(b'\r\n',b''):raise ValueError('Unsupported bare CR: '+str(path))
 norm=raw.replace(b'\r\n',b'\n')
 return norm.decode(),{'path':str(path),'raw_sha256':sha(raw),'lf_sha256':sha(norm),'representation':'CRLF_TO_LF_IN_MEMORY' if raw!=norm else 'LF_IDENTITY'}
def read_pinned(path,pin):
 text,p=read_current(path)
 if p['lf_sha256']!=pin:raise ValueError('Unreviewed helper drift: '+str(path))
 return text,p

def block(s,start):
 at=s.index('{',start);depth=1;i=at+1;state='code'
 while depth:
  if i>=len(s):raise ValueError('Unclosed source block')
  c=s[i];n=s[i:i+2]
  if state=='line':
   if c=='\n':state='code'
  elif state=='comment':
   if n=='*/':state='code';i+=1
  elif state in ('string','char'):
   if c=='\\':i+=1
   elif c==('"' if state=='string' else "'"):state='code'
  elif n=='//':state='line';i+=1
  elif n=='/*':state='comment';i+=1
  elif c=='"':state='string'
  elif c=="'":state='char'
  elif c=='{':depth+=1
  elif c=='}':depth-=1
  i+=1
 return i
def tok(s):return re.sub(r'\s+','',s)
def require(a,b,label):
 if tok(a)!=tok(b):raise ValueError('Unreviewed native binding: '+label)
def unchanged(proofs):
 for p in proofs:
  if sha(Path(p['path']).read_bytes())!=p['raw_sha256']:raise ValueError('Input changed during generation: '+p['path'])
def assemble(src,h):
 s=src['BuildingCombatUtilitySystemHelper.cs'];a=s.index('{',s.index('public sealed class BuildingCombatUtilitySystemHelper'))+1;end=block(s,s.index('        public bool TryMarkDestroyed('))
 spans=[];bodies=[];names=[];headers=[{'name':'TryMarkDestroyed','signature_tokens':tok(s[s.index('        public bool TryMarkDestroyed('):s.index('{',s.index('        public bool TryMarkDestroyed('))])}]
 def span(name,start,end):return {'name':name,'start_line':s.count('\n',0,start)+1,'end_line':s.count('\n',0,end)+1,'lf_sha256':sha(s[start:end].encode())}
 spans.append(span('declarations_and_TryMarkDestroyed',a,end))
 for m in re.finditer(PATTERN,s,re.M):
  if m.group(1) not in NAMES:continue
  z=block(s,m.start());names.append(m.group(1));bodies.append(s[m.start():z]);spans.append(span(m.group(1),m.start(),z));headers.append({'name':m.group(1),'signature_tokens':tok(s[m.start():s.index('{',m.start())])})
 if not NAMES<=set(names) or len(names)!=20:raise ValueError('Expected20selected methods plus prefix TryMarkDestroyed')
 if headers!=json.loads(h['utility-method-contract.json'])['method_headers']:raise ValueError('Unsupported native utility member/signature shape')
 utility=h['utility-header.txt']+s[a:end]+'\n\n'+'\n\n'.join(bodies)+'\n}}\n'
 utility=utility.replace('BuildingDestroyedVisualPresentationSystemHelper','SourceDestroyedVisualBoundary')
 tick=src['BuildingPlacementRuntimeTickCompositionSystemHelper.cs'];interval=re.search(r'private const double DestroyedCleanupIntervalSeconds\s*=\s*([^;]+);',tick).group(1).strip()
 at=tick.index('if (!GameplayRuntimeUpdateDebugFlags.DisableBuildingDestroyedRuntime');managed=tick[at:block(tick,at)]
 require(managed,"""if (!GameplayRuntimeUpdateDebugFlags.DisableBuildingDestroyedRuntime && now >= _nextDestroyedCleanupAt) {
 _nextDestroyedCleanupAt = now + DestroyedCleanupIntervalSeconds;
 using (SyncDestroyedRuntimeBuildingCombatEntitiesMarker.Auto()) { context.SyncDestroyedRuntimeBuildingCombatEntities?.Invoke(); }
 using (UpdateDestroyedBuildingsMarker.Auto()) { context.UpdateDestroyedBuildings?.Invoke(); }}""",'managed guard/order')
 life=src['BuildingGameplayCompositionSystemHelper.cs'];lifetime=re.search(r'private const float DestroyedBuildingLifetimeSeconds\s*=\s*([^;]+);',life).group(1).strip()
 if 'DestroyedBuildingLifetimeSeconds);' not in life:raise ValueError('Native lifetime constant not bound')
 runtime=src['BuildingRuntimeTickCompositionSystemHelper.cs'];cb=runtime[runtime.index('() => source.BuildingCombatUtilitySystemHelper.SyncDestroyedRuntimeBuildingCombatEntities('):runtime.index('() => source.BuildingBarrierUtilitySystemHelper.UpdateRoadBarrierDoors(')]
 require(cb,"""() => source.BuildingCombatUtilitySystemHelper.SyncDestroyedRuntimeBuildingCombatEntities(combatContext,UnityEngine.Time.time,destroyedBuildingLifetimeSeconds), () => source.BuildingCombatUtilitySystemHelper.UpdateDestroyedBuildings(combatContext,UnityEngine.Time.time),""",'scaled callbacks')
 barrier=src['BuildingBarrierUtilitySystemHelper.cs'];at=barrier.index('public void RememberOpenBaseBreach(');at=barrier.index('if (',at);predicate=barrier[at:block(barrier,at)]
 require(predicate,"""if (building?.Definition == null || !building.HasOwnerFaction || (!building.Definition.IsWall && !IsWallGateDefinition(context,building.Definition))) { return; }""",'ordinary breach guard')
 guard=tok(predicate).replace('IsWallGateDefinition(context,building.Definition)','BuildingBarrierUtilitySystemHelper.IsWallGateDefinition(building.Definition)').replace('{return;}','return;')
 owner=h['owner-adapter.cs.template'].replace('@@INTERVAL@@',interval).replace('@@LIFETIME@@',lifetime).replace('@@BREACH_GUARD@@',guard)
 if '@@' in owner:raise ValueError('Unbound owner token')
 return {'SourceBuildingCombatUtility.cs':utility,'SourceBuildingLifetimeOwner.cs':owner},{'original_utility_methods':20,'additional_prefix_method':'TryMarkDestroyed','native_spans':spans,'native_method_headers':headers,'native_source_policy':'FRESH_CURRENT_BYTES_NO_HISTORICAL_WHOLE_FILE_HASH_GUARD','managed_block_lf_sha256':sha(managed.encode()),'runtime_callbacks_lf_sha256':sha(cb.encode()),'breach_guard_lf_sha256':sha(predicate.encode()),'interval_literal':interval,'lifetime_literal':lifetime,
 'bindings':['owner/type rename','Unity object/transform aliases to existing numeric wrapper','visual helper type -> explicit existing callback boundary','strip native profiler scopes; verify original guard/order','native realtime due -> explicit realtimeSeconds; native scaled Time.time -> scaledSeconds','original ordinary breach first guard; unsupported wall/gate rejects'],
 'scope':['same canonical registry','source S002 cityCount0/homehousehold admission unchanged','no visual parity','no refund/cancel/secondregistry','finite clocks rejectNaN; original due guard represented by DeMorgan equivalent','no mission/runtime proof from generation']}
def generate(repo:Path,output:Path):
 repo=Path(repo);output=Path(output);base=Path(__file__).resolve().parent;src={};h={};proofs=[]
 for n in SOURCE_INPUTS:src[n],p=read_current(repo/SYSTEMS/n);proofs.append(p)
 for n,pin in HELPER_PINS.items():h[n],p=read_pinned(base/'helpers'/n,pin);proofs.append(p)
 code,details=assemble(src,h)
 if any((output/n).exists() for n in code):raise FileExistsError('Output already exists')
 unchanged(proofs);output.mkdir(parents=True,exist_ok=True)
 for n,s in code.items():
  with (output/n).open('xb') as f:f.write(s.encode())
 unchanged(proofs)
 return {'schema_version':1,'generator':'CURRENT_NATIVE_BUILDING_LIFETIME_FINITE_PROJECTION','inputs':proofs,'outputs':{n:sha(s.encode()) for n,s in code.items()},**details}
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True,type=Path);p.add_argument('--output',required=True,type=Path);a=p.parse_args();print(json.dumps(generate(a.repo,a.output),indent=2))
