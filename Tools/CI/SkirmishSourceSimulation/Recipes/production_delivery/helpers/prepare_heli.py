native=source['Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs'];managed=source['Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.ManagedDelivery.cs'];bridge=source['Assets/Game/Scripts/Systems/BuildingProductionTransportBridgeCompositionSystemHelper.cs'];B=helper['helicopter-bridge.cs.template'];proof=[]
def get(s,n,i=0):
 t,p=method(s,n,i);proof.append(p);return t
def body(t):return t[t.index('{')+1:-1]
def replace(name,text):
 global B
 m=re.search(r'^  (?:internal )?(?:bool|void|int|float3) '+name+r'\(',B,re.M)
 if not m:raise ValueError('Missing owned hook '+name)
 z=block(B,m.start());B=B[:m.start()]+text+B[z:]
extra=[]
# Native drop/hover policy, only producer TransformPoint/position/forward data access is rebound.
t=get(native,'ResolveProductionTransportDropPosition',1);t=t.replace('private static Vector3 ResolveProductionTransportDropPosition(RuntimeBuildingEntity building, RuntimeBuildingEntity.PendingProduction pending)','float3 DropPoint(RuntimeBuildingEntity building,SourcePendingProduction pending)').replace('Vector3','float3')
t=t.replace('building.Instance.transform.TransformPoint(building.ProductionSpawnLocalPositions[pending.ReservedProductionSlotIndex])','owner.World.ProducerPoint(building,new float3(building.ProductionSpawnLocalPositions[pending.ReservedProductionSlotIndex].x,building.ProductionSpawnLocalPositions[pending.ReservedProductionSlotIndex].y,building.ProductionSpawnLocalPositions[pending.ReservedProductionSlotIndex].z))')
t=re.sub(r'building.Instance.transform.position\s*\+\s*\(building.Instance.transform.forward\s*\*\s*([^()]+)\)',lambda m:'SourceForwardPoint(building,'+m.group(1)+')',t)
if '.transform.' in t:raise ValueError('Unbound producer transform source shape')
body_start=t.index('{')+1
t=t[:body_start]+'''\n            if(useLiveS073ProducerDropPose){if(building==null)throw new NotSupportedException("S073 source producer identity unavailable");return owner.World.LiveS073ProducerDropPoint(building,pending);}\n'''+t[body_start:]
replace('DropPoint','  '+t.strip());extra.append('float3 SourceForwardPoint(RuntimeBuildingEntity b,float distance){if(!owner.World.TryProducerFrame(b.Id,out var p,out var f))throw new NotSupportedException("Actual producer pose required");return p+(f*distance);}')
t=get(native,'ResolveProductionTransportHoverPosition').replace('private static Vector3 ResolveProductionTransportHoverPosition(RuntimeBuildingEntity building, RuntimeBuildingEntity.PendingProduction pending)','float3 SourceHover(RuntimeBuildingEntity building,SourcePendingProduction pending)').replace('ResolveProductionTransportDropPosition(','DropPoint(').replace('Vector3','float3');extra.append(t)
# Exact native lane bitmap/scan, mapping only the active-state view and temporary backing allocation.
t=get(native,'TryAcquireProductionTransportLane').replace('private bool TryAcquireProductionTransportLane(Context context, GameObject transportPrefab, int maxConcurrent, out int laneIndex)','bool SourceTryLane(SourceUnitPrefab transportPrefab,int maxConcurrent,out int laneIndex)').replace('Mathf.Max','math.max')
t=t.replace('EnsureLaneUsageCapacity(safeMax);\n            System.Array.Clear(_laneUsage, 0, safeMax);','if(safeMax>256)throw new NotSupportedException("Finite source transport concurrency");\n            var _laneUsage=new bool[safeMax];')
t=t.replace('context.RuntimeBuildings','active').replace('foreach (var pair in active)','foreach (var pair in active)').replace('RuntimeBuildingEntity.ActiveProductionTransport transport = pair.Value?.ActiveTransport;','Active transport = pair.Value;').replace('transport.LaneIndex','transport.Lane');extra.append(t);replace('Lane','  int Lane(SourceUnitPrefab p,int max)=>SourceTryLane(p,max,out var i)?i:-1;') if False else None
# Expression-bodied wrapper must use native method result; owned API performs no independent scan.
m=re.search(r'^  int Lane\(',B,re.M);z=block(B,m.start());B=B[:m.start()]+'  int Lane(SourceUnitPrefab p,int max){return SourceTryLane(p,max,out var i)?i:-1;}'+B[z:]
get(native,'ResolveProductionTransportLaneOffset')
kernel_path='Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs'
get(source[kernel_path],'ProductionTransportLaneOffset')
t='float3 SourceLaneOffset(int laneIndex,int maxConcurrent){if(hasLogicalBasis)return SharedProductionDeliveryKernel.ProductionTransportLaneOffsetForLogicalBasis(in logicalBasis,laneIndex,maxConcurrent);float3 forward=ReadCameraForward();ActualVector3 cameraForward=new ActualVector3(forward.x,forward.y,forward.z);ActualVector3 offset=SharedProductionDeliveryKernel.ProductionTransportLaneOffset(cameraPresent,cameraForward,laneIndex,maxConcurrent);return new float3(offset.x,offset.y,offset.z);}'
replace('SourceLaneOffset','  '+t)
extra.append('float3 SourceIngressOffset(float distance){if(hasLogicalBasis)return SharedProductionDeliveryKernel.ProductionTransportIngressOffset(in logicalBasis,distance);return cameraPresent?-math.normalize(ReadCameraRight())*distance:new float3(-distance,0f,0f);}')
# Actual native entry/exit block is the admitted Helicopter branch; native methods provide hover/lane/clearance.
f=get(managed,'TryEnsureActiveProductionTransport');a=f.index('                hoverPosition = ResolveProductionTransportHoverPosition(');z=f.index('\n            }\n\n            GameObject instance',a);entry=f[a:z]
# The current diagnostic call is an editor-only observer around the same functional
# resolver. Validate that exact branch, then project only the production behavior.
a=entry.index('                int searchStartRadius = pending.TransportClearDropSearchStartRadius;')
reset='                pending.TransportClearDropSearchStartRadius = 0;'
end=entry.index(reset,a)+len(reset)
diagnostic_branch=entry[a:end]
expected='''
                int searchStartRadius = pending.TransportClearDropSearchStartRadius;
#if UNITY_EDITOR
                var searchDiagnostics = S073NativeProductionDeliveryObserver.CreateDropSearchDiagnostics();
#else
                HelicopterDropSearchDiagnostics searchDiagnostics = null;
#endif
                bool resolvedDrop = TryResolveClearHelicopterDropPosition(
                        context,
                        building,
                        pending,
                        hoverPosition,
                        null,
                        pending.TransportClearDropSearchStartRadius,
                        searchDiagnostics,
                        out bool exhaustedSearchBudget,
                        out int nextSearchRadius,
                        out committedDropCell,
                        out committedDropPosition);
#if UNITY_EDITOR
                S073NativeProductionDeliveryObserver.ObserveHelicopterDropSearch(
                    context,
                    building,
                    pending,
                    now,
                    launchInputs,
                    randomState,
                    searchStartRadius,
                    resolvedDrop,
                    exhaustedSearchBudget,
                    nextSearchRadius,
                    committedDropCell,
                    committedDropPosition,
                    searchDiagnostics);
#endif
                if (!resolvedDrop)
                {
                    pending.TransportClearDropSearchStartRadius = exhaustedSearchBudget
                        ? nextSearchRadius
                        : 0;
                    return false;
                }

                pending.TransportClearDropSearchStartRadius = 0;'''
if re.sub(r'\s+','',diagnostic_branch)!=re.sub(r'\s+','',expected):raise ValueError('Unsupported current native helicopter search diagnostic branch')
# Drop-clear result and budget mutation remain the native resolver's exact outputs.
entry=entry[:a]+'''                if(!SourceClear(building,pending,hoverPosition,null,pending.TransportClearDropSearchStartRadius,null,out bool exhaustedSearchBudget,out int nextSearchRadius,out int2 committedDropCell,out float3 committedDropPosition)){pending.TransportClearDropSearchStartRadius=exhaustedSearchBudget?nextSearchRadius:0;return false;}
                pending.TransportClearDropSearchStartRadius = 0;'''+entry[end:]
basis_ingress = r'Vector3 horizontalOffset;\s*if\s*\(context\.HasLogicalDeliveryBasis\)\s*\{\s*float3 logicalOffset\s*=\s*SharedProductionDeliveryKernel\.ProductionTransportIngressOffset\(\s*in context\.LogicalDeliveryBasis,\s*(\d+f)\);\s*horizontalOffset\s*=\s*new Vector3\(logicalOffset\.x,\s*logicalOffset\.y,\s*logicalOffset\.z\);\s*\}\s*else\s*\{\s*horizontalOffset\s*=\s*context\.WorldCamera != null\s*\?\s*-context\.WorldCamera\.transform\.right\.normalized\s*\*\s*\1\s*:\s*new Vector3\(-\1,\s*0f,\s*0f\);\s*\}'
native_ingress_distances = re.findall(basis_ingress, f)
if native_ingress_distances != ['70f', '60f']:
 raise ValueError('Native ingress branches must retain exact AirSelf 70m and Helicopter 60m forms')
entry, ingressReplacements = re.subn(basis_ingress, lambda m:'float3 horizontalOffset=SourceIngressOffset('+m.group(1)+');', entry)
if ingressReplacements != 1: raise ValueError('Expected one admitted transport branch to be bridged from the exact native typed ingress form')
entry=entry.replace('ResolveProductionTransportHoverPosition(','SourceHover(').replace('ResolveProductionTransportLaneOffset(context,','SourceLaneOffset(').replace('context.WorldCamera != null','cameraPresent').replace('context.WorldCamera.transform.right.normalized','math.normalize(ReadCameraRight())').replace('Vector3','float3').replace('-horizontalOffset.normalized','-math.normalize(horizontalOffset)').replace('Quaternion.LookRotation((hoverPosition - entryPosition).normalized, float3.up)','PoseMath.Look(math.normalize(hoverPosition-entryPosition),new float3(0,1,0))').replace('Quaternion.LookRotation((exitPosition - hoverPosition).normalized, float3.up)','PoseMath.Look(math.normalize(exitPosition-hoverPosition),new float3(0,1,0))').replace('                hasCommittedDropPosition = true;','').replace('                touchdownPosition = hoverPosition;','').replace('                hoverRotation =','                hoverRotation =').replace('                entryRotation = hoverRotation;','')
# Both native-like and fixture paths share the same exact current hover-plus-lane
# preferred point and clear/commit operation. The fixture never computes route pose.
preferred_start='''                hoverPosition = SourceHover(building, pending);
                hoverPosition += SourceLaneOffset( laneIndex, pending.TransportMaxConcurrent);'''
if preferred_start not in entry:raise ValueError('Unsupported native preferred lane-axis source shape')
entry=entry.replace(preferred_start,'                hoverPosition = SourcePreferredPosition(building,pending,laneIndex);',1)
clear_start='''                if(!SourceClear(building,pending,hoverPosition,null,pending.TransportClearDropSearchStartRadius,null,out bool exhaustedSearchBudget,out int nextSearchRadius,out int2 committedDropCell,out float3 committedDropPosition)){pending.TransportClearDropSearchStartRadius=exhaustedSearchBudget?nextSearchRadius:0;return false;}
                pending.TransportClearDropSearchStartRadius = 0;'''
if clear_start not in entry:raise ValueError('Unsupported native clear/commit retry source shape')
entry=entry.replace(clear_start,'                if(!SourcePreferredClear(building,pending,hoverPosition,out committedDropCell,out committedDropPosition))return false;',1)
extra.append('''bool SourceEntry(RuntimeBuildingEntity building,SourcePendingProduction pending,int laneIndex,out float3 hoverPosition,out float3 entryPosition,out float3 exitPosition,out quaternion hoverRotation,out quaternion exitRotation,out int2 committedDropCell,out float3 committedDropPosition){hoverPosition=entryPosition=exitPosition=default;hoverRotation=exitRotation=default;committedDropCell=default;committedDropPosition=default;
'''+entry.replace('out int2 committedDropCell','out committedDropCell').replace('out float3 committedDropPosition','out committedDropPosition')+'\nreturn true;\n}')
extra.append('float3 SourcePreferredPosition(RuntimeBuildingEntity building,SourcePendingProduction pending,int laneIndex){float3 preferred=SourceHover(building,pending);preferred+=SourceLaneOffset(laneIndex,pending.TransportMaxConcurrent);return preferred;}')
extra.append('bool SourcePreferredClear(RuntimeBuildingEntity building,SourcePendingProduction pending,float3 preferred,out int2 cell,out float3 committed){if(!SourceClear(building,pending,preferred,null,pending.TransportClearDropSearchStartRadius,null,out bool exhaustedSearchBudget,out int nextSearchRadius,out cell,out committed)){pending.TransportClearDropSearchStartRadius=exhaustedSearchBudget?nextSearchRadius:0;return false;}pending.TransportClearDropSearchStartRadius=0;return true;}')
# Replace only old numeric entry setup. Reviewed pool/door/pose alignment remains engine/data bridge.
B=B.replace('@@ENTRY_GEOMETRY@@','if(!SourceEntry(b,p,lane,out var hover,out var entry,out var exit,out var q,out var exitQ,out var cell,out var committed))return false;')
# Native clearance, with source context callbacks mapped to the one admitted world/grid/asset descriptor.
t=get(native,'TryResolveClearHelicopterDropPosition');h='bool SourceClear(RuntimeBuildingEntity building,SourcePendingProduction pending,float3 preferredWorld,Active ignoredTransport,int startRadius,HelicopterDropSearchDiagnostics diagnostics,out bool exhaustedSearchBudget,out int nextSearchRadius,out int2 dropCell,out float3 dropPosition)'
b=body(t);a=b.index('            if (context.TransportBridgeSystem == null');z=b.index('\n\n            NativeArray<GridWalkable>',a)
if re.sub(r'\s+','',b[a:z])!=json.loads(helper['engine-binding-contract.json'])['clear_provider_binding_tokens']:raise ValueError('Unsupported grid/provider binding shape')
b=b[:a]+'''            var em=owner.World.Em;var gridEntity=owner.World.GridEntity;if(em.World==null||!em.World.IsCreated||!em.Exists(gridEntity)||!em.HasComponent<GridConfig>(gridEntity)||!em.HasComponent<DynamicBlockerComponent>(gridEntity)||!em.HasBuffer<GridWalkable>(gridEntity))return false;var grid=em.GetComponentData<GridConfig>(gridEntity);var blockerData=em.GetComponentData<DynamicBlockerComponent>(gridEntity);'''+b[z:]
static_bounds_call='ReserveStaticRenderDropBuffers(em, ref reserved, gridEntity, grid, blockerData, true, preferredCell);'
if b.count(static_bounds_call)!=1:raise ValueError('Unsupported original static drop-bounds call shape/count')
b=b.replace('context.TransportBridgeSystem.ResolveUnitFootprintForPrefab(context.TransportBridgeContext, em, pending?.Prefab)','(pending?.Prefab!=null&&em.HasComponent<UnitFootprint>(pending.Prefab.Template)?em.GetComponentData<UnitFootprint>(pending.Prefab.Template).Size:new int2(1,1))').replace('ReserveRuntimeBuildingDropBuffers(context,','ReserveRuntimeBuildingDropBuffers(').replace('ReserveActiveProductionTransportDropBuffers(context,','ReserveActiveProductionTransportDropBuffers(').replace(static_bounds_call,'if(reserveStatic!=null)reserveStatic(owner.World,preferredCell,ref reserved);').replace('TryFindClearHelicopterDropCell(','SharedHelicopterDropKernel.TryFindClearHelicopterDropCell(');a=b.index('                return SharedHelicopterDropKernel.TryFindClearHelicopterDropCell(');z=b.index(';',a)+1;expr=b[a:z].replace('return SharedHelicopterDropKernel.','bool found=SharedHelicopterDropKernel.').replace('out dropPosition','out Vector3 resolvedPosition');b=b[:a]+expr+'dropPosition=new float3(resolvedPosition.x,resolvedPosition.y,resolvedPosition.z);return found;'+b[z:];extra.append(h+'{'+b+'}')
replace('FindClear','  bool FindClear(RuntimeBuildingEntity b,SourcePendingProduction p,float3 preferred,out int2 c,out float3 pos){bool ok=SourceClear(b,p,preferred,null,p.TransportClearDropSearchStartRadius,null,out var exhausted,out var next,out c,out pos);p.TransportClearDropSearchStartRadius=ok?0:exhausted?next:0;return ok;}')
for name in ['ReserveRuntimeBuildingDropBuffers','ReserveActiveProductionTransportDropBuffers','ReserveTransportFootprint','ReserveWorldCellWithRadius','ReserveCellRect']:
 t=get(native,name).replace('private static ','private ').replace('Context context, ','').replace('            Context context,\n','').replace('RuntimeBuildingEntity.ActiveProductionTransport','Active').replace('context.RuntimeBuildings','owner.World.Buildings')
 if name=='ReserveActiveProductionTransportDropBuffers':
  t=t.replace('foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in owner.World.Buildings)','foreach (var pair in active)').replace('Active transport = pair.Value?.ActiveTransport;','Active transport = pair.Value;').replace('transport.ActiveDrop != null','transport.State.HasDrop').replace('transport.ActiveDrop.EndPosition','transport.State.DropEnd')
 if name=='ReserveTransportFootprint':
  a=t.index('            bool hasBounds = false;');z=t.index('\n\n            if (!hasBounds)',a)
  if re.sub(r'\s+','',t[a:z])!=json.loads(helper['engine-binding-contract.json'])['renderer_bounds_binding_tokens']:raise ValueError('Unsupported renderer bounds/filter binding shape')
  t=t[:a]+'            bool hasBounds=TryDataBounds(transport,out float3 boundsMin,out float3 boundsMax);'+t[z:];t=t.replace('transport.HoverPosition','transport.State.Hover').replace('new float3(bounds.min.x, bounds.min.y, bounds.min.z)','boundsMin').replace('new float3(bounds.max.x, bounds.max.y, bounds.max.z)','boundsMax')
 t=t.replace('Vector3 worldPosition','float3 worldPosition');extra.append(t)
# Renderer access is an explicitly reviewed data/pose bridge. Native policy above consumes actual bounds/fallback.
extra.append('''bool TryDataBounds(Active a,out float3 lo,out float3 hi){var t=templates[a.Prefab];bool any=false;lo=hi=default;for(int i=0;i<t.Meshes.Length;i++){var m=t.Meshes[i];if(!m.Present||!t.RendererEnabled[i])continue;var matrix=a.Pose.WorldMatrix(m.Node);float3 c=math.transform(matrix,m.Center);float3 e=math.abs(matrix.c0.xyz)*m.Extents.x+math.abs(matrix.c1.xyz)*m.Extents.y+math.abs(matrix.c2.xyz)*m.Extents.z;if(!any){lo=c-e;hi=c+e;any=true;}else{lo=math.min(lo,c-e);hi=math.max(hi,c+e);}}return any;}''')
# The old ReserveActive copy is no longer a policy owner.
replace('ReserveActive','  void ReserveActive(Active a,ref NativeBitArray r){ReserveTransportFootprint(ref r,owner.World.Grid,a,HelicopterDropActiveTransportBufferCells);}')
# Source air/runway movement gate, binding the actual freshly returned produced member.
t=get(bridge,'MoveNewestProducedUnitToCell');b=body(t);a=b.index('            bool isAirUnit');b=b[a:];extra.append('void SourceMoveNewest(Entity entity,int2 goalCell){var em=owner.World.Em;'+b+'}')
replace('MoveNewestToCell','  void MoveNewestToCell(Entity e,int2 c){if(e==Entity.Null)return;SourceMoveNewest(e,c);}')
# Publish body is native; only the existing explicit manager/boundary delegates are source data bindings.
t=get(managed,'PublishManagedDeliveryReadModel',1);b=body(t);a=b.index('            if (tryGetEntityManager');b=b[a:];b=b.replace('tryGetEntityManager == null || tryGetBoundary == null ||','').replace('!tryGetEntityManager(out EntityManager entityManager)','!SourceManager(out EntityManager entityManager)').replace('!tryGetBoundary(entityManager, out Entity boundary)','!owner.World.BoundaryRead(out Entity boundary)');extra.append('bool SourceManager(out EntityManager e){e=owner.World.Em;return e!=null;}\nvoid SourcePublishCount(int activeCount){'+b+'}')
replace('PublishDeliveryCount','  void PublishDeliveryCount(){SourcePublishCount(active.Count);}')
exec(helper['complete_heli_fragment.py'],globals())
# Native named constants used by emitted methods are linked from current source, never copied numeric rules.
constants=[]
for src in [native,managed]:
 for typ,name,value in re.findall(r'private const (int|float) (\w+) = ([^;]+);',src):
  if any(re.search(r'\b'+name+r'\b',x) for x in extra) or re.search(r'\b'+name+r'\b',B):constants.append('const '+typ+' '+name+'='+value+';')
z=B.rindex('\n }');B=B[:z]+'\n'+'\n'.join(constants+extra)+'\n'+B[z:]
active_match=re.search(r'sealed class Active \{([^}]*)\}',B)
active_fields=active_match.group(1) if active_match else ''
required_active=('float3 DropPosition;','int2 DropGoal;','bool HasCommittedDropPosition;')
if any(field not in active_fields for field in required_active) or B.count('HasCommittedDropPosition=true')<2:
 raise ValueError('Generated transport Active must own committed and per-drop state')
(P/'SourceHelicopterDelivery.cs').write_text(B);(P/'helicopter-source-spans.json').write_text(json.dumps(proof,indent=2)+'\n');print('generated heli source methods',len(proof),'current completion mapping included')
