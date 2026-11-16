"""Current generic-ground native capability bindings; unsupported active transport is explicit."""
from pathlib import Path
import hashlib,json,sys
sys.path.insert(0,str(Path(__file__).resolve().parents[2]))
from skirmish_source_syntax import method_span
INPUTS=['Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityService.cs','Assets/Game/Scripts/Runtime/Skirmish/SkirmishEnemyCapabilityCommands.cs','Assets/Game/Scripts/Runtime/Skirmish/VehicleFieldRepairService.cs']
def generate(repo,output):
 repo=Path(repo);output=Path(output);output.mkdir(parents=True,exist_ok=True);pins={};raws={};proof=[]
 for rel in INPUTS:
  p=repo/rel;raw=p.read_bytes();raws[p]=raw;pins[str(p)]=hashlib.sha256(raw).hexdigest()
  text=raw.decode('utf-8').replace('\r\n','\n')
  if rel.endswith('VehicleFieldRepairService.cs'):
   anchor='where T:unmanaged,IComponentData'
   if text.count(anchor)!=1:raise ValueError('Unique native repair Set storage header required')
   text=text.replace(anchor,'where T:struct,IComponentData',1)
   proof.append({'binding':'Only unmanaged-to-struct generic Set header; all original repair bodies retained over finite managed storage.'})
  elif rel.endswith('SkirmishEnemyCapabilityService.cs'):
   sig='internal static bool StepTransport(EntityManager em,Entity session,OwnActor carrier,ref SkirmishEnemyCapabilityOperationV1 op,float now,SkirmishResolvedSetup setup,SkirmishEnemyPerceptionBuilder.Snapshot facts)'
   if text.count(sig)!=1:raise ValueError('Unique current StepTransport signature required')
   a,b=method_span(text,sig);old=text[a:b];transport=old
   binding=';var commands=new TransportBoardingCommandSystem();'
   if transport.count(binding)!=1:raise ValueError('Unique native local transport command value required')
   transport=transport.replace(binding,';',1)
   board='var result=commands.TryIssueOwnedBoardOrderToPassenger(em,session,2,carrier.Entity,passenger,new UnitTransportAirPickupSystem(),new UnitMoveOrderSystem());\n                    if(result.Accepted)Accepted(ref op,now);else Reject(ref op,(ushort)result.ReasonCode,now);\n                    return true;'
   if transport.count(board)!=1:raise ValueError('Native boarding command frontier changed')
   transport=transport.replace(board,'throw new System.NotSupportedException("Original owned boarding command/air pickup owner not admitted");',1)
   lo=transport.index('                bool accepted;TacticalCommandReasonCode reason;');hi=transport.index('                return true;',lo)+len('                return true;')
   dispatch=transport[lo:hi]
   if 'TryIssueOwnedDeploy'not in dispatch or 'TryIssueOwnedDisembark'not in dispatch:raise ValueError('Native deploy/disembark command frontier changed')
   # Preserve the original source plane query/cell guard before command consumption.
   plane=dispatch[dispatch.index('                    if(grid.CalculateEntityCount()!=1)return false;'):dispatch.index('                    accepted=TransportBoardingCommandSystem.TryIssueOwnedDeploy')]
   transport=transport[:lo]+'                if(carrier.Role==SkirmishRoleKind.TransportPlane){'+plane+'throw new System.NotSupportedException("Original owned deploy/capacity owner not admitted");}throw new System.NotSupportedException("Original owned disembark/capacity owner not admitted");'+transport[hi:]
   returning='bool accepted=TransportBoardingCommandSystem.TryIssueOwnedDisembark(em,session,2,carrier.Entity,new UnitTransportCapacitySystem(),new UnitMoveOrderSystem(),grid,out var reason);\n                    if(accepted)Accepted(ref op,now);else Reject(ref op,(ushort)reason,now);return true;'
   if transport.count(returning)!=1:raise ValueError('Native return-home cargo command frontier changed')
   transport=transport.replace(returning,'throw new System.NotSupportedException("Original return-home disembark/capacity owner not admitted");',1)
   text=text[:a]+transport+text[b:]
   sig2='static bool Return(EntityManager em,Entity session,OwnActor actor,ref SkirmishEnemyCapabilityOperationV1 op,float now)'
   if text.count(sig2)!=1:raise ValueError('Unique current return command signature required')
   ra,rb=method_span(text,sig2);native_return=text[ra:rb]
   if native_return.count('UnitReturnToBaseCommandService.TryIssueOwned(em,session,2,actor.Entity)')!=1:raise ValueError('Unique current owned return command required')
   proof.append({'source_method':sig2,'source_body_sha256':hashlib.sha256(native_return.encode()).hexdigest(),'binding':'Complete current native Return body unchanged; observed native UnitTarget owns home evidence, not assumed queue acceptance.'})
   proof.append({'source_method':sig,'source_body_sha256':hashlib.sha256(old.encode()).hexdigest(),'binding':'Full current transport survey/state/casualty/ready-Hold/query/timeout/no-work branches retained; only owned boarding/deploy/disembark invocation frontiers throw. No false feasibility or transport success.'})
  else:
   marker='    public partial struct TransportBoardingCommandSystem'
   if text.count(marker)!=1:raise ValueError('Unique current transport command partial boundary required')
   text=text[:text.index(marker)]+'}\n'
   start='            requestId=last+1;'
   if text.count(start)!=1:raise ValueError('Unique current scan command frontier required')
   aa=text.index(start);text=text[:aa]+'            throw new System.NotSupportedException("Original active scan command queue owner is not admitted in this incremental host");\n        }\n    }\n}\n'

   proof.append({'binding':'Exact current SkirmishEnemyCapabilityCommands.TryScan class retained; unreferenced transport partial omitted because its source capacity/command owner is not admitted.'})
  with (output/Path(rel).name).open('w',encoding='utf-8',newline='')as f:f.write(text)
 for p,raw in raws.items():
  if p.read_bytes()!=raw:raise ValueError('Current native service drift')
 result={'source_inputs':pins,'bindings':proof};(output/'source-extraction.json').write_text(json.dumps(result,indent=2));return result
