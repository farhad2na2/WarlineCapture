native=source['Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs'];defs=source['Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs'];B=helper['queue-bridge.cs.template'];proof=[]
t,p=method(native,'TryQueuePlayerUnitFromBuilding');proof.append(p)
sig=''' bool NativeQueue(BuildingProductionQueueCompositionSystemHelper.QueueContext context,RuntimeBuildingEntity building,int productionIndex,SourceUnitPrefab spawnUnitPrefab,EntityManager entityManager,float now,out SourcePendingProduction pending){pending=null;
'''
# Retain adapter identity/public-catalog/camera transport admissions, then invoke current original queue policy.
B=B.replace('@@ORIGINAL_QUEUE@@','   return NativeQueue(QueueContext,building,productionIndex,recipe.Unit,World.Em,now,out pending);')
q,p=method(defs,'GetProductionQuantity');proof.append(p);q=q.replace('public static int GetProductionQuantity(BuildingDefinition definition','private static int NativeProductionQuantity(RuntimeDefinition definition').replace('Mathf.Max','math.max')
pool=[]
for name,replacement in [('AcquirePendingProduction','SourceAcquirePending'),('ReleasePendingProduction','SourceReleasePending'),('RemovePendingAt','SourceRemovePendingAt')]:
 t,p=method(native,name);proof.append(p);t=re.sub(r'\bProduction\b','SourcePendingProduction',t).replace(name,replacement,1).replace('IPendingProduction','BuildingProductionQueueCompositionSystemHelper.IPendingProduction').replace('_pendingProductionPool','sourcePendingPool').replace('ReleasePendingProduction(','SourceReleasePending(')
 if name=='ReleasePendingProduction':t=t.replace('internal void','internal void',1)
 pool.append(t)
B=B.replace('internal SourceHelicopterDelivery Helicopters;','internal SourceHelicopterDelivery Helicopters;readonly Stack<SourcePendingProduction> sourcePendingPool=new Stack<SourcePendingProduction>();int _createdCount;')
old='public bool RemoveAt(RuntimeBuildingEntity b,int index){if(b.PendingProductions==null||index<0||index>=b.PendingProductions.Count)return false;b.PendingProductions.RemoveAt(index);return true;}'
if old not in B:raise ValueError('Owned view RemoveAt binding shape changed')
B=B.replace(old,'public bool RemoveAt(RuntimeBuildingEntity b,int index)=>owner.SourceRemovePendingAt(b.PendingProductions,index);')
body='''
  if(building==null||spawnUnitPrefab==null)return false;
  building.PendingProductions??=new List<SourcePendingProduction>();
  building.ProducedUnits??=new List<Entity>();
  bool accepted=Production.TryQueuePlayerUnitFromBuildingShared<SourcePendingProduction>(
   context,building,productionIndex,spawnUnitPrefab,entityManager,now,building.PendingProductions,
   SourceAcquirePending,SourceReleasePending,(owner,index)=>NativeProductionQuantity(owner.Definition,index),out pending);
  if(accepted)RecordDeliveryEvent("queue_accepted",now,building.Id,productionIndex,spawnUnitPrefab.name,new int2(-1,-1),default,false,Entity.Null,false,pending.ReceiptId);
  return accepted;
 }
'''
end=B.rindex('\n }');B=B[:end]+'\n'+sig+body+q+'\n'+'\n'.join(pool)+'\n'+B[end:]
(P/'SourceProductionDeliveryOwner.cs').write_text(B);(P/'queue-source-spans.json').write_text(json.dumps(proof,indent=2)+'\n');print('generated native queue/quantity/pool methods',len(proof))
