from pathlib import Path
import re,json,hashlib,collections
PHASES=[('combat_ensure',['CombatDamageObservationBootstrapSystem','UnitAttackStateEnsureSystem','UnitAttackTraceStateEnsureSystem']),('roster',['SkirmishRosterProjectionSystem']),('blockers',['RuntimeGridDeduplicationSystem','DynamicBlockerInitSystem','StaticGridBlockerUpdateSystem']),('fabrication',['MaterialFabricationSystem']),('queue',['UnitMoveOrderRequestSystem']),('fog',['SkirmishSharedFogUpdateSystem']),('validate',['EngageTargetValidateSystem']),('occupancy',['DynamicOccupancyRebuildSystem']),('attack_resume',['AttackMoveSystem']),('acquire',['UnitEngagementSystem']),('engage_sync',['EngageTargetSyncSystem']),('manual_retry',['UnitManualMoveRetrySystem']),('path',['UnitPathfindingSystem']),('chase',['UnitEngagedMovementSystem']),('movement',['UnitGridMovementSystem']),('fuel',['VehicleFuelConsumptionSystem']),('attack',['UnitAttackSystem']),('defense',['BuildingDefenseAttackSystem']),('capacity',['SkirmishCapacityLifecycleSystem']),('groups',['SkirmishArmyGroupSystem']),('research',['SkirmishResearchSystem']),('death',['UnitDeathSystem']),('vehicle_wreck',['VehicleWreckCleanupSystem']),('objective_facts',['SkirmishObjectiveFactProjectionSystem','SkirmishBaseAssaultObjectiveSystem','SkirmishNativeObjectiveTickSystem']),('enemy',['SkirmishEnemyStrategySystem']),('objective_outcome',['SkirmishOutcomeSystem'])]
def generate(repo,output):
 R=Path(repo);P=Path(output);P.mkdir(parents=True,exist_ok=True);root=R/"Assets/Game/Scripts";scanpins={}
 index=collections.defaultdict(list)
 for file in root.rglob('*.cs'):
  raw=file.read_bytes();scanpins[str(file.resolve())]=hashlib.sha256(raw).hexdigest();text=raw.decode('utf-8').replace('\r\n','\n')
  for m in re.finditer(r'\b(?:partial\s+)?(?:struct|class)\s+(\w+)',text):index[m.group(1)].append((file,text,m.start()))
 def pick(name):
  vals=index.get(name,[]);named=[x for x in vals if x[0].name==name+'.cs'];vals=named or vals
  if not vals:return None
  # Partial files without attributes are not a substitute for the actual annotated declaration.
  annotated=[x for x in vals if re.search(r'\[Update(?:Before|After|InGroup)',x[1][:x[2]].split('namespace')[-1])];vals=annotated or vals
  if len(vals)>1:raise RuntimeError('Ambiguous source ordering type '+name+':'+str([str(x[0])for x in vals]))
  return vals[0]
 queue=[x for _,names in PHASES for x in names];records={};pins={}
 while queue:
  name=queue.pop(0)
  if name in records:continue
  if len(records)>160:raise RuntimeError('Finite owner metadata closure exceeded')
  found=pick(name);attrs=[];group='SimulationSystemGroup';first=last=False;source=None
  if found:
   path,text,start=found;source=str(path);pins[source]=scanpins[str(path.resolve())];prefix=text[:start];brace=max(prefix.rfind('{'),prefix.rfind('}'));header=prefix[brace+1:]
   for m in re.finditer(r'\[\s*(UpdateBefore|UpdateAfter|UpdateInGroup)\s*\(\s*typeof\s*\(\s*([\w.]+)\s*\)([^\]]*)\]\s*',header,re.S):
    kind,target,rest=m.groups();target=target.split('.')[-1]
    if kind=='UpdateInGroup':group=target;first=bool(re.search(r'OrderFirst\s*=\s*true',rest));last=bool(re.search(r'OrderLast\s*=\s*true',rest))
    else:attrs.append((kind,target));queue.append(target)
   queue.append(group)
  records[name]={'source':source,'edges':attrs,'group':group,'order_first':first,'order_last':last,'external_unowned':found is None}
 for filename,expected in scanpins.items():
  if hashlib.sha256(Path(filename).read_bytes()).hexdigest()!=expected:raise RuntimeError('Source changed during metadata generation: '+filename)
 # Emit metadata into a unique namespace; typeof references point only to this finite metadata closure.
 lines=['// Generated from current source headers; body-free metadata for admission only.','namespace Game.Runtime.NativeOrderMetadata {']
 for name,x in sorted(records.items()):
  for kind,target in x['edges']:lines.append('[Unity.Entities.'+kind+'(typeof('+target+'))]')
  if x['group'] in records and not name.endswith('SystemGroup'):lines.append('[Unity.Entities.UpdateInGroup(typeof('+x['group']+'),OrderFirst='+str(x['order_first']).lower()+',OrderLast='+str(x['order_last']).lower()+')]')
  lines.append('internal struct '+name+' {}')
 lines.append('}')
 with (P/'SourceOwnerOrderMetadata.cs').open('w',encoding='utf-8',newline='\n')as stream:stream.write('\n'.join(lines)+'\n')
 (P/'source-owner-order-inputs.json').write_text(json.dumps({'kind':'FiniteCurrentSourceOwnerOrderingProjection','phase_types':PHASES,'records':records,'source_pins':pins,'scope':'Compiled attribute Type arguments are reflected at runtime. Body-free marker namespace is metadata only, never a replacement system.'},indent=2)+'\n')
 # Fast build-time diagnostic mirrors the runtime reachability check.
 plan=[x for _,names in PHASES for x in names];pos={x:i for i,x in enumerate(plan)};edges=collections.defaultdict(set)
 for name,x in records.items():
  for kind,target in x['edges']:edges[target if kind=='UpdateAfter'else name].add(name if kind=='UpdateAfter'else target)
 for a in plan:
  for b in plan:
   if a==b:continue
   xa,xb=records[a],records[b]
   if xa['group']==xb['group'] and (xb['order_last'] and not xa['order_last'] or xa['order_first'] and not xb['order_first']):edges[a].add(b)
   if xa['group']=='InitializationSystemGroup' and xb['group']=='SimulationSystemGroup':edges[a].add(b)
 violations=[]
 for start in plan:
  todo=list(edges[start]);seen=set()
  while todo:
   node=todo.pop()
   if node in seen:continue
   seen.add(node);todo+=list(edges[node])
   if node in pos and pos[start]>=pos[node]:violations.append([start,node])
 if violations:raise RuntimeError('Current source ordering contradicts admitted linearization: '+str(violations))
 return {'kind':'FiniteCurrentSourceHeaderAdmission','source_pins':scanpins,'referenced_header_pins':pins,'phase_types':PHASES,'metadata_types':len(records),'current_plan_violations':violations,'scope':'Declared metadata and selected valid linearization only; no native observed total order claim.'}
