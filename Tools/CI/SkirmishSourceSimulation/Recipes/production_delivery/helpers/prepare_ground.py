spawn=source['Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs'];defs=source['Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs'];B=helper['ground-bridge.cs.template'];proof=[]
def get(s,n,i=0):
 t,p=method(s,n,i);proof.append(p);return t
def body(t):return t[t.index('{')+1:-1]
def project(t):return re.sub(r'\bContext\b','BuildingProductionQueueCompositionSystemHelper.QueueContext',t)
start=B.index('  void CleanupRecent()');prefix=B[:start]
owned='''
 internal float3 ProducerPoint(RuntimeBuildingEntity b,float3 local){if(ProducerRootFrames!=null&&ProducerRootFrames.TryGetValue(b.Id,out var root))return root.Point(local);if(!ProducerPoses.TryGetValue(b.Id,out var pose))throw new NotSupportedException("Actual source producer transform unknown");return pose.Point(0,local);}
 EntityQuery SourceLiveFootprintQuery()=>Em.CreateEntityQuery(typeof(UnitGrid),typeof(UnitFootprint));
 static bool UnsupportedBranch(string n)=>throw new NotSupportedException("Source GroundS002 delivery branch not admitted: "+n);
 void CleanupRecent(){if(TryGetRecentSpawnReservationBuffer(SpawnContext,Em,false,out var r))CleanupBoundaryRecentSpawnReservations(r,Now);}
 internal bool OverlapsRecent(int2 c,int2 s)=>OverlapsRecentSpawnReservation(SpawnContext,Em,c,s);
 internal bool OverlapsExisting(int2 c,int2 s)=>OverlapsExistingUnitFootprint(SpawnContext,Em,c,s);
 internal FixedString64Bytes SourceKey(RuntimeBuildingEntity b,SourcePendingProduction p)=>TryGetProductionSourceKey(b.Definition,p.ProductionIndex,out var k)?k:default;
 internal bool TryResolve(RuntimeBuildingEntity b,SourcePendingProduction p,float3? overridePosition,int2? overrideCell,ref uint rng,out int2 c,out float3 pos,out int2 fp,out bool air,out int slot){if(p==null)throw new ArgumentNullException(nameof(p));if(overridePosition.HasValue!=overrideCell.HasValue)throw new ArgumentException("Paired source overrides required");return TryResolveSpawnPlacement(SpawnContext,b,SourceKey(b,p),p.Prefab.Template,p.ReservedProductionSlotIndex,overridePosition.HasValue?(Vector3?)new Vector3(overridePosition.Value.x,overridePosition.Value.y,overridePosition.Value.z):null,overrideCell,Em,GridEntity,Grid,Em.GetComponentData<DynamicBlockerComponent>(GridEntity),ref rng,out c,out pos,out fp,out air,out _,out slot);}
'''
out=[prefix,owned]
rectSource=source['Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs'];rect=get(rectSource,'ReserveCellRect');rectBody=body(rect);out.append('internal void ReserveRect(ref NativeBitArray reserved,int minX,int minY,int maxX,int maxY){var grid=Grid;'+rectBody+'}\n')
for n in ['TryGetProductionSourceKey','GetProductionPrefab','GetSpawnableLookupKey']:
 t=get(defs,n).replace('BuildingDefinition definition','RuntimeDefinition definition').replace('GameObject','SourceUnitPrefab').replace('public static','private static',1).replace('NormalizeSpawnableKey(', 'SpawnableKeyText.Normalize(');out.append(t+'\n')
t=project(get(spawn,'TryResolveSpawnPlacement'))
for name,label in [('TryResolveHelicopterSpawnForFaction','faction helicopter helipad resolver'),('TryResolveProductionSlotAtCell','override aircraft slot resolver'),('SkirmishPopulationPolicy.ReserveDisconnectedSpawnCells','disconnected ground-infantry spawn predecessor')]:
 match=re.search(r'\b'+name+r'\s*\(',t)
 if match:
  a=t.index('(',match.start());d=1;i=a+1
  while d:
   if t[i]=='(':d+=1
   elif t[i]==')':d-=1
   i+=1
  t=t[:match.start()]+'UnsupportedBranch("'+label+'")'+t[i:]
t=t.replace('(float3)building.Instance.transform.TransformPoint(productionSpawnLocalPosition)','ProducerPoint(building,new float3(productionSpawnLocalPosition.x,productionSpawnLocalPosition.y,productionSpawnLocalPosition.z))')
out.append(t+'\n')
for n in ['TryGetLegacyProductionSpawnLocalPosition','ReserveRecentSpawnBuffers','ReserveRecentSpawnBuffer','AddRecentSpawnReservation','OverlapsRecentSpawnReservation','TryGetRecentSpawnReservationBuffer','CleanupBoundaryRecentSpawnReservations','TryFindStrictSpawnCell','TryFindStrictSpawnCellAdjacentToBuilding','TryReserveSpawnCandidate','OverlapsExistingUnitFootprint','ReserveDynamicOccupancy','ReserveBuildingBuffer','IsHelicopterSourceKey']:
 t=project(get(spawn,n)).replace('UnityEngine.Time.time','Now').replace('NormalizeSpawnableKey(', 'SpawnableKeyText.Normalize(')
 if n=='OverlapsExistingUnitFootprint':t=t.replace('context.LiveUnitFootprintQuery','SourceLiveFootprintQuery()')
 t=t.replace('(float3)building.Instance.transform.TransformPoint(productionSpawnLocalPosition)','ProducerPoint(building,new float3(productionSpawnLocalPosition.x,productionSpawnLocalPosition.y,productionSpawnLocalPosition.z))')
 out.append(t+'\n')
for i in range(len(out)):
 for n in ['TryGetProductionSpawnPointFromReadModel','TryGetAvailableProductionSpawnPointFromReadModel','HasAnyProductionSpawnPointInReadModel']:
  out[i]=re.sub(r'(?<!\.)\b'+n+r'\(', 'SourceProductionSpawnReadModels.'+n+'(',out[i])
b=body(get(spawn,'TrySpawnPlayerUnitNearBuilding'));a=b.index('            if (!context.SpawnPrefabSystem.TryGetSpawnUnitPrefabEntity(');z=b.index('\n\n            if (!TryResolveSpawnPlacement(',a)
if re.sub(r'\s+','',b[a:z])!=json.loads(helper['engine-binding-contract.json'])['spawn_prefab_binding_tokens']:raise ValueError('Unsupported prefab resolver binding shape')
b=b[:a]+'''            Entity prefabEntity = pending.Prefab.Template;
            if(!em.Exists(prefabEntity)||!em.HasComponent<Prefab>(prefabEntity))throw new NotSupportedException("Actual source prefab template required");'''+b[z:]
b=b.replace('GetProductionSourceKey(context, building.Definition, productionIndex)','(TryGetProductionSourceKey(building.Definition,productionIndex,out var sourceKey)?sourceKey:default)')
a=b.index('            var birth = ');z=b.index('            Entity instance = em.Instantiate(prefabEntity);',a)
if re.sub(r'\s+','',b[a:z])!=json.loads(helper['engine-binding-contract.json'])['birth_capture_binding_tokens']:raise ValueError('Unsupported no-provider birth binding shape')
b=b[:a]+'            RuntimeActorBirthContext.RequireNoRecordProfile(em,prefabEntity);\n'+b[z:]
b=b.replace('            RuntimeActorBirthContext.RequireNoRecordProfile(em,prefabEntity);\n','');at=b.index('\n\n            if (!TryResolveSpawnPlacement(');b=b[:at]+'\n            RuntimeActorBirthContext.RequireNoRecordProfile(em,prefabEntity);'+b[at:]
b=b.replace('Entity instance = em.Instantiate(prefabEntity);','instance = em.Instantiate(prefabEntity);').replace('birth.StampFresh(em, instance, gridEntity);','RuntimeActorBirthContext.RequireNoRecordProfile(em,instance);').replace('InitializeSpawnedUnit(', 'SharedProductionActorInitializer.Initialize(').replace('SetOrAddComponent(em, instance, new UnitSourcePrefabKey { Value = spawnUnitSourceKey });','SetOrAddSourceKey(em,instance,new UnitSourcePrefabKey { Value = spawnUnitSourceKey });')
for n in ['PublishProducedUnitReadModel','PublishProductionSpawnRequest']:b=b.replace(n+'(','SourceProductionSpawnReadModels.'+n+'(')
if b.count('            return true;')!=1:raise ValueError('Unexpected original spawn success boundary')
b=b.replace('            return true;','            RecordSuccessfulDelivery(building,pending,instance);\n            return true;')
sig=''' internal bool TrySpawn(RuntimeBuildingEntity building,SourcePendingProduction pending,float3? overridePosition,int2? overrideCell,ref uint randomState,out Entity instance){instance=Entity.Null;if(pending==null)throw new ArgumentNullException(nameof(pending));var context=SpawnContext;var em=Em;var gridEntity=GridEntity;var grid=Grid;var blockerData=Em.GetComponentData<DynamicBlockerComponent>(GridEntity);int productionIndex=pending.ProductionIndex;int reservedProductionSlotIndex=pending.ReservedProductionSlotIndex;Vector3? overrideWorldPosition=overridePosition.HasValue?(Vector3?)new Vector3(overridePosition.Value.x,overridePosition.Value.y,overridePosition.Value.z):null;
'''
out.append(sig+b+'\n}\n');out.append(' static void SetOrAddSourceKey(EntityManager em,Entity e,UnitSourcePrefabKey v){if(em.HasComponent<UnitSourcePrefabKey>(e))em.SetComponentData(e,v);else em.AddComponentData(e,v);}\n')
result='\n'.join(out)+'\n }\n}\n';O=P;O.mkdir(exist_ok=True);(O/'SourceProductionGroundWorld.cs').write_text(result);(P/'ground-source-spans.json').write_text(json.dumps(proof,indent=2)+'\n');print('generated ground source methods',len(proof))
