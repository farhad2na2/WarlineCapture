native=source['Assets/Game/Scripts/Systems/BuildingProductionQueueCompositionSystemHelper.cs'];defs=source['Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs'];B=helper['queue-bridge.cs.template'];proof=[]
t,p=method(native,'TryQueuePlayerUnitFromBuilding');proof.append(p);body=t[t.index('{')+1:-1]
body=body.replace('RuntimeBuildingEntity.PendingProduction','SourcePendingProduction').replace('Production queuedProduction = AcquirePendingProduction();','SourcePendingProduction queuedProduction = SourceAcquirePending();')
for name in ['PruneProducedUnits','ResolveProductionTransportSettings','InitializePendingProduction','ResolveProductionDurationSeconds','RebuildPendingProductionTimeline','IsHelicopterUnitPrefab']:
 body=re.sub(r'\b'+name+r'\(', 'Production.'+name+'(',body)
body=body.replace('ProductionTransportSettings transportSettings','BuildingProductionQueueCompositionSystemHelper.ProductionTransportSettings transportSettings')
body=body.replace('TryReserveProductionSlot(', 'BuildingProductionQueueCompositionSystemHelper.TryReserveProductionSlot(').replace('BuildingDefinitionPrefabSystemHelper.GetProductionQuantity(', 'NativeProductionQuantity(')
if body.count('return true;')!=1:raise ValueError('Native queue success shape changed')
body=body.replace('return true;','pending=queuedProduction;return true;')
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
end=B.rindex('\n }');B=B[:end]+'\n'+sig+body+'\n}\n'+q+'\n'+'\n'.join(pool)+'\n'+B[end:]
(P/'SourceProductionDeliveryOwner.cs').write_text(B);(P/'queue-source-spans.json').write_text(json.dumps(proof,indent=2)+'\n');print('generated native queue/quantity/pool methods',len(proof))
