"""CURRENT opening actor, ledger and session source projection for generated S002."""
from pathlib import Path
import argparse,hashlib,json,re
def mask(text):
 out=list(text);i=0;state='code'
 while i<len(text):
  c=text[i];n=text[i+1]if i+1<len(text)else''
  if state=='code':
   if c=='/'and n in['/','*']:state='line'if n=='/'else'block';out[i]=out[i+1]=' ';i+=2;continue
   if c=='@'and n=='"':state='verbatim';out[i]=out[i+1]=' ';i+=2;continue
   if c in['"',"'"]:state='string'if c=='"'else'char';out[i]=' ';i+=1;continue
  elif state=='line':
   if c=='\n':state='code'
   else:out[i]=' '
  elif state=='block':
   if c=='*'and n=='/':out[i]=out[i+1]=' ';i+=2;state='code';continue
   if c!='\n':out[i]=' '
  elif state=='verbatim':
   if c=='"'and n=='"':out[i]=out[i+1]=' ';i+=2;continue
   if c=='"':state='code'
   if c!='\n':out[i]=' '
  else:
   if c=='\\':out[i]=' ';i+=1
   elif c==('"'if state=='string'else"'"):state='code'
   if i<len(text)and text[i]!='\n':out[i]=' '
  i+=1
 if state not in['code','line']:raise ValueError('Unclosed native C# lexical value')
 return ''.join(out)


def member(text,name,signature=None,expression=False):
 code=mask(text);pattern=r'\b(?:public|private|internal|protected)\s+(?:(?:static|readonly|unsafe|virtual|override|async|sealed|partial)\s+)*[^\n{};=]+?\b'+re.escape(name)+r'(?:<[^{};()]*>)?\s*\('
 matches=list(re.finditer(pattern,code))
 if signature:matches=[x for x in matches if signature in x.group()]
 if len(matches)!=1:raise ValueError('Unique native member required: '+name+' count='+str(len(matches)))
 start=matches[0].start();cursor=matches[0].end();depth=1
 while depth:
  if cursor>=len(code):raise ValueError('Unclosed source signature')
  if code[cursor]=='(':depth+=1
  elif code[cursor]==')':depth-=1
  cursor+=1
 rest=code[cursor:];arrow=rest.find('=>');brace=rest.find('{')
 if arrow>=0 and(brace<0 or arrow<brace):
  if expression:
   semi=rest.find(';',arrow)
   if semi<0 or any(x in rest[arrow+2:semi]for x in '{}'):raise ValueError('Unsupported finite expression shape: '+name)
   if name!='CanInspectUnavailable' or not rest[arrow+2:semi].strip().startswith('failure is'):raise ValueError('Unadmitted expression member: '+name)
   return text[start:cursor+semi+1]
  if brace<0 or not re.fullmatch(r'\s*[\w.]+\s+switch\s*',rest[arrow+2:brace]):raise ValueError('Unsupported scalar expression-bodied native method: '+name)
 if brace<0:raise ValueError('Native block body required: '+name)
 brace+=cursor;end=brace+1;depth=1
 while depth:
  if end>=len(code):raise ValueError('Unclosed native body: '+name)
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 if arrow>=0 and arrow+cursor<brace:
  while end<len(code)and code[end].isspace():end+=1
  if end>=len(code)or code[end]!=';':raise ValueError('Expected switch expression terminator')
  end+=1
 return text[start:end]
def declaration(text,kind,name):
 code=mask(text);matches=list(re.finditer(r'\bpublic\s+'+re.escape(kind)+r'\s+'+re.escape(name)+r'\b',code))
 if len(matches)!=1:raise ValueError('Unique source declaration required: '+name)
 start=matches[0].start();brace=code.index('{',matches[0].end());end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]


PATHS={'spawn':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs','random':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishTypedSpawnConstraint.cs','roles':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishSessionInitializationSystem.cs','research':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishResearchService.cs','launch':'Assets/Game/Scripts/Composition/Skirmish/SkirmishExpandedLaunchProjection.cs','enemy':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyPlannerService.cs','objective':'Assets/Game/Scripts/Runtime/Skirmish/SkirmishOriginalObjectiveSpawnHooks.cs'}
def once(text,before,after):
 if text.count(before)!=1:raise ValueError('Explicit current opening binding changed: '+before[:100])
 return text.replace(before,after,1)
def editor_projection(text):
 result=[];depth=0
 for line in text.splitlines(True):
  token=line.strip()
  if token=='#if UNITY_EDITOR':depth+=1;continue
  if depth:
   if token.startswith('#if'):raise ValueError('Nested Editor projection needs explicit review')
   if token=='#else':raise ValueError('Editor else branch needs explicit review')
   if token=='#endif':depth-=1
   continue
  result.append(line)
 if depth:raise ValueError('Unclosed Editor-only opening block')
 return ''.join(result)
def block(text,marker):
 code=mask(text)
 if code.count(marker)!=1:raise ValueError('Unique finite opening branch required: '+marker)
 start=code.index(marker);brace=code.index('{',start);end=brace+1;depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]
def initializer(text,name):
 code=mask(text);matches=list(re.finditer(r'new\s+'+re.escape(name)+r'\s*{',code))
 if len(matches)!=1:raise ValueError('Unique native source initializer required: '+name)
 start=matches[0].start();end=matches[0].end();depth=1
 while depth:
  if code[end]=='{':depth+=1
  elif code[end]=='}':depth-=1
  end+=1
 return text[start:end]
def write_lf(path,text):
 with path.open('w',encoding='utf-8',newline='')as f:f.write(text)
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);here=Path(__file__).resolve().parent;paths={k:repo/v for k,v in PATHS.items()};raw={k:v.read_bytes()for k,v in paths.items()};text={k:v.decode('utf-8').replace('\r\n','\n')for k,v in raw.items()};template_paths=list((here/'templates').glob('*.txt'));templates={x.name:x.read_bytes()for x in template_paths};proof=[]
 def selected(group,name,signature=None):
  value=member(text[group],name,signature);proof.append({'source':PATHS[group],'member':name,'signature':signature,'source_member_sha256':hashlib.sha256(value.encode()).hexdigest()});return value
 create=editor_projection(selected('spawn','CreateForceMember','internal static'));place=editor_projection(selected('spawn','TryPlaceTypedOnMap'));lookup=selected('spawn','TryResolvePrefabEntity');setadd=selected('spawn','SetOrAdd');random=selected('random','PlacementRandom')
 create=once(create,'SkirmishResolvedSetup setup,SkirmishTypedSpawnConstraint typedPlacement=null, System.Action<Entity> afterInstantiateForValidation = null)','SkirmishResolvedSetup setup)');create=once(create,'if (!fromPrefab && Application.isPlaying)','if (!fromPrefab)');birth=templates['BirthDiagnosticRegion.cs.txt'].decode();create=once(create,birth,'');create=once(create,'                birth.StampFresh(em, owned, actualPlacementGrid);\n','')
 create=once(create,'                if(typedPlacement!=null&&!typedPlacement.TryRequestedWorld(force,member,out position))\n                {em.DestroyEntity(owned);return Entity.Null;}\n','');create=once(create,'TryPlaceTypedOnMap(em, owned, position, setup?.Seed ?? 0,force,typedPlacement, placementGrid,','TryPlaceTypedOnMap(em, owned, position, setup?.Seed ?? 0, placementGrid,')
 convoy=block(create,'if (force.RoleKind == SkirmishRoleKind.ConvoyLogic &&');create=once(create,convoy,'')
 # Native TryPlan makes BA's plan empty; TryTag has no non-loop writes for that exact domain.
 plan=selected('objective','TryPlan');tag=selected('objective','TryTagActualOpeningMember');plan_code=re.sub(r'\s+','',mask(plan));expected='if(setup.ObjectiveKind!=SkirmishObjectiveKind.Breakthrough&&setup.ObjectiveKind!=SkirmishObjectiveKind.ConvoyEscort)returntrue;'
 before_for=plan_code.split('if(setup.Forces==null)',1)[0]
 body_prefix=before_for.split('{',1)[1];expected_prefix='bindings=Array.Empty<SkirmishOriginalObjectiveBinding>();error=null;if(setup==null){error=;returnfalse;}'+expected
 if body_prefix!=expected_prefix:raise ValueError('Current objective BA no-op plan branch changed')
 loop=block(tag,'foreach(var binding in plan)');outside=tag.replace(loop,'');outside_code=re.sub(r'\s+','',mask(outside).split('{',1)[1]);expected_out='if(!TryPlan(setup,outvarplan,outerror))returnfalse;error=null;returntrue;}'
 if outside_code!=expected_out:raise ValueError('Current empty-plan opening hook gained consumed work')
 objective=block(create,'if (fromPrefab && setup != null &&');create=once(create,objective,'')
 create=create.replace('SkirmishVisualSpawnService.TryResolveUnitSpawnWorld','GeneratedSpawnLayoutValues.TryResolveUnitSpawnWorld').replace('SkirmishVisualSpawnService.ResolveUnitFallbackWorld','GeneratedSpawnLayoutValues.ResolveUnitFallbackWorld');brace=create.index('{');guard='\n if(setup==null||setup.CatalogId!="S002"||setup.ObjectiveKind!=SkirmishObjectiveKind.BaseAssault||setup.EnemyPolicy!=null||SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)||force.RoleKind==SkirmishRoleKind.ConvoyLogic)throw new System.NotSupportedException("Source S002 BA non-typed actor profile required");\n';create=create[:brace+1]+guard+create[brace+1:]
 place=once(place,'int missionSeed,SkirmishResolvedForceEntry force,SkirmishTypedSpawnConstraint typedPlacement, Entity selectedGrid','int missionSeed, Entity selectedGrid');place=once(place,'            if(typedPlacement!=null&&grids.CalculateEntityCount()!=1)return false;\n','');place=once(place,'                if(typedPlacement!=null&&!SkirmishTypedSpawnConstraint.HasCompleteAllocator(em,gridEntity,grid))return false;\n','');place=once(place,'                    NativeBitArray constrained=default;\n                    if(typedPlacement!=null&&!typedPlacement.TryMask(force,actualOccupied,out constrained))return false;\n                    var occupied=typedPlacement==null?actualOccupied:constrained;','                    var occupied=actualOccupied;');place=once(place,'finally { reserved.Dispose();if(constrained.IsCreated)constrained.Dispose(); }','finally { reserved.Dispose(); }');place=place.replace('InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell','SpawnCellUtility.TryFind').replace('SkirmishTypedSpawnConstraint.PlacementRandom','PlacementRandom');setadd=once(setadd,'where T : unmanaged, IComponentData','where T : struct, IComponentData')
 if 'typedPlacement'in create+place:raise ValueError('Unbound typed opening read outside S002 profile')
 actor='using System;using System.Collections.Generic;using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;using Unity.Transforms;\nnamespace Game.Runtime{internal static class GeneratedOpeningActorSource{\n'+'\n'.join([create,place,lookup,setadd,random])+'\n}}\n'
 spawn=selected('spawn','TrySpawnLedgers');anchor='            bool startingSetBound = opening.StartingSetBound;'
 if spawn.count(anchor)!=1:raise ValueError('Current shared bootstrap completion boundary changed')
 tail=spawn[spawn.index(anchor):];tail=once(tail,'Application.isPlaying && !SkirmishStartingSupplyService.Initialize','!SkirmishStartingSupplyService.Initialize');tail=tail[:tail.rfind('}')];ledger='using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Entities;\nnamespace Game.Runtime{internal static class GeneratedOpeningLedgerCompletion{internal static bool Complete(EntityManager em,Entity session,SkirmishResolvedSetup setup,Game.Simulation.SkirmishMissionBootstrapKernel opening,out SkirmishReasonCode reason,out byte visualPending){reason=SkirmishReasonCode.None;visualPending=0;var sessionId=em.GetComponentData<SkirmishExpandedSessionComponent>(session).SessionId;if(!em.HasComponent<SkirmishSharedBuildingsReady>(session))throw new System.InvalidOperationException("Actual starting buildings must precede source opening ledger finalization");\n'+tail+'\n}\nprivate static void CreateStructure(params object[] args)=>throw new System.NotSupportedException("Complete source starting-building pipeline required; no logical fallback structures");}}\n'
 roles=selected('roles','ProjectRoles').replace('private static void ProjectRoles','internal static void ProjectRoles',1).replace('SkirmishResearchService.EnsureSession','GeneratedResearchInitialSource.EnsureSession');research=selected('research','EnsureSession');roles='using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Entities;\nnamespace Game.Runtime{internal static class GeneratedSessionRoles{\n'+roles+'\n}internal static class GeneratedResearchInitialSource{\n'+research+'\n}}\n'
 launch=selected('launch','TryQueue');constructor=templates['GeneratedS002SessionConstruction.cs.txt'].decode()
 creation_start=launch.index('entity = em.CreateEntity(');creation_end=launch.index('\n            }',creation_start);creation=launch[creation_start:creation_end]
 start=constructor.index('            Entity entity=em.CreateEntity(');end=constructor.index('            SkirmishMissionDispatch.TryGetLegacyIndex',start)
 constructor=constructor[:start]+'            Entity entity;\n            '+creation+'\n'+constructor[end:]
 initial=selected('roles','TryInitialize');phase_anchor='            session.InitializationComplete = 1;'
 if initial.count(phase_anchor)!=1:raise ValueError('Current session initialization completion boundary changed')
 phase=initial[initial.index(phase_anchor):initial.rfind('}')].strip()
 constructor=once(constructor,'session.InitializationComplete=1;session.Phase=SkirmishSessionPhase.Spawning;em.SetComponentData(entity,session);',phase)

 def payload_bound(value):
  fields={'SessionId':'attempt','Seed':'setup.Seed','CatalogId':'setup.CatalogId','DefinitionId':'setup.DefinitionId','ContentVersion':'setup.ContentVersion','SizeId':'setup.SizeId','DifficultyId':'setup.DifficultyId','IsCustom':'false'}
  for field in set(re.findall(r'payload\.(\w+)',value)):
   if field not in fields:raise ValueError('New launch payload initializer input needs explicit setup binding: '+field)
   value=value.replace('payload.'+field,fields[field])
  return value
 for name in ['SkirmishMatchState','SkirmishExpandedSessionComponent','SkirmishResolvedSetupComponent']:
  constructor=once(constructor,initializer(constructor,name),payload_bound(initializer(launch,name)))
 old_record=initializer(constructor,'SkirmishResolvedSetupRecord');native_record=initializer(launch,'SkirmishResolvedSetupRecord')
 if re.sub(r'\s+','',old_record)!=re.sub(r'\s+','',native_record):constructor=once(constructor,old_record,native_record)
 # Original fresh strategy-owner writes, from CURRENT launch caller.
 start=launch.index('            strategyOwners.Clear();');end=launch.index('\n\n',start);strategy=launch[start:end].replace('strategyOwners','owners');a=constructor.index('            var owners=');b=constructor.index('\n            em.AddComponentData(entity,new SkirmishResolvedSetupComponent',a)if '\n            em.AddComponentData(entity,new SkirmishResolvedSetupComponent'in constructor else constructor.index('\n            em.AddComponentData(entity,new SkirmishResolvedSetupComponent',a)
 constructor=constructor[:a]+'            var owners=em.AddBuffer<ExternalFactionStrategy>(entity);\n'+strategy+constructor[b:]
 enemy=selected('enemy','EnsureSession');anchor='            if (setup?.EnemyPolicy == null) return true;'
 if enemy.count(anchor)!=1:raise ValueError('Current null enemy policy early return changed')
 enemy='private static bool EnsureEnemySessionSource(EntityManager em,Entity session,SkirmishResolvedSetup setup) => SkirmishEnemyPlannerService.EnsureSession(em,session,setup);'
 constructor=once(constructor,'            GeneratedSessionRoles.ProjectRoles(em,entity,setup);','            GeneratedSessionRoles.ProjectRoles(em,entity,setup);\n            if(!EnsureEnemySessionSource(em,entity,setup))throw new NotSupportedException("Current source enemy initialization rejects setup");')
 pos=constructor.rfind('    }');constructor=constructor[:pos]+enemy+'\n'+constructor[pos:]
 typed_create=editor_projection(selected('spawn','CreateForceMember','internal static'))
 typed_create=once(typed_create,'if (!fromPrefab && Application.isPlaying)','if (!fromPrefab)')
 typed_create=typed_create.replace('RuntimeActorBirthContext','SourceOpeningBirthContext').replace('SkirmishVisualSpawnService.TryResolveUnitSpawnWorld','GeneratedSpawnLayoutValues.TryResolveUnitSpawnWorld').replace('SkirmishVisualSpawnService.ResolveUnitFallbackWorld','GeneratedSpawnLayoutValues.ResolveUnitFallbackWorld')
 brace=typed_create.index('{');typed_guard='\n if(setup==null||!SkirmishMissionLogicalBindingCompiler.RequiresBinding(setup)||typedPlacement==null)throw new NotSupportedException("Actual original launch-owned typed constraint required");SourceGroundMissionFamily.RequireCatalogSetup(setup);\n';typed_create=typed_create[:brace+1]+typed_guard+typed_create[brace+1:]
 typed_place=editor_projection(selected('spawn','TryPlaceTypedOnMap')).replace('InitialUnitsSpawnSystem.TryFindInitialUnitSpawnCell','SpawnCellUtility.TryFind')
 typed_actor='using System;using System.Collections.Generic;using Game.Components;using Game.Configs;using Game.Skirmish.Contracts;using Unity.Entities;using Unity.Collections;using Unity.Mathematics;using Unity.Transforms;\nnamespace Game.Runtime{internal static class GeneratedTypedOpeningActorSource{\n'+'\n'.join([typed_create,typed_place,lookup,setadd])+'\n}}\n'
 files={'GeneratedTypedOpeningActorSource.cs':typed_actor,'GeneratedOpeningActorSource.cs':actor,'GeneratedOpeningLedgerCompletion.cs':ledger,'GeneratedS002SessionConstruction.cs':constructor,'GeneratedSessionRoles.cs':roles}
 def unchanged():
  if any(paths[k].read_bytes()!=v for k,v in raw.items()):raise ValueError('Current opening source byte drift')
  if any((here/'templates'/k).read_bytes()!=v for k,v in templates.items()):raise ValueError('Owned opening template drift')
 unchanged()
 for name,value in files.items():write_lf(output/name,value)
 result={'kind':'CurrentSourceOpeningSessionRecipe','source_inputs':{str(paths[k]):hashlib.sha256(v).hexdigest()for k,v in raw.items()},'members':proof,'outputs':{k:hashlib.sha256(v.encode()).hexdigest()for k,v in files.items()},'required_current_links':['Assets/Game/Scripts/Configs/Skirmish/SkirmishGenericAirCombinedEnemyRollout.cs','Assets/Game/Scripts/Simulation/SkirmishMissionBootstrapKernel.cs'],'bindings':['CURRENT complete actor lookup/component writes, real Instantiate/placement/stable ID/RNG; S002 BA/null-policy/no typed/convoy guard','Only explicit native diagnostic capture/stamp region omitted; no native record/certificate source authority. Current source scene detach retained','Current original objective BA empty-plan no-op proven before omitting unconsumed objective specialization, no trailing gameplay truncated','CURRENT complete placement method null-typed branch and existing SpawnCellUtility alias; no geometry defaults/actor template substitution','CURRENT shared opening ledger completion tail, play-mode supply initialization explicitly active; genuine shared-buildings readiness required','CURRENT ProjectRoles/Research EnsureSession; CURRENT native launch field initializers +fresh strategy owner writes, CURRENT full native enemy initialization forwarded without reset, preserving S002 null return and generic-ground planner state','Existing fresh logical constructor context/admission is an owned adapter, not whole native UI launch/ContentAccess readiness','Exact raw byte pins/recheck; unknown prefix/region/initializer/typed reads reject; no old body fallback']};write_lf(output/'source-extraction.json',json.dumps(result,indent=2));unchanged();return result
if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--repo',required=True);p.add_argument('--output',required=True);a=p.parse_args();generate(a.repo,a.output)
