# Executed inside prepare_heli's current-source generation scope; no standalone game methods.
# Static snapshot state is one explicit helicopter mode. Preserve its original consumed source branches.
B=B.replace('internal SourcePendingProduction Drop;internal float3 DropPosition;internal bool FixtureNoPresentation;','internal SourcePendingProduction Drop;internal float3 DropPosition;internal int2 DropGoal;internal bool HasCommittedDropPosition;internal bool FixtureNoPresentation;').replace('CommittedPosition=committed});','CommittedPosition=committed,HasCommittedDropPosition=true});')
t=get(managed,'TryResolveCommittedHelicopterDropPosition');b=body(t);b=b.replace('transport.CommittedDropCell','a.CommittedCell').replace('transport.CommittedDropPosition','a.CommittedPosition').replace('transport.HasCommittedDropPosition','a.HasCommittedDropPosition').replace('transport.DeliveredUnitCount','Delivered(a)').replace('Vector3','float3')
b=b.replace('context.TransportBridgeContext.TryGetEntityManager == null ||','').replace('!context.TransportBridgeContext.TryGetEntityManager(out EntityManager entityManager)','!SourceManager(out EntityManager entityManager)').replace('context.TransportBridgeContext.TryGetGridData == null ||','');b=re.sub(r'!context.TransportBridgeContext.TryGetGridData\(\s*out _,\s*out GridConfig grid,\s*out _,\s*out _\)','!SourceGrid(out GridConfig grid)',b)
committed_policy=re.sub(r'\s+','',b)
extra.append('bool SourceGrid(out GridConfig grid){var w=owner.World;if(w.Em.World==null||!w.Em.World.IsCreated||!w.Em.Exists(w.GridEntity)||!w.Em.HasComponent<GridConfig>(w.GridEntity)){grid=default;return false;}grid=w.Em.GetComponentData<GridConfig>(w.GridEntity);return true;}\nbool SourceCommitted(Active a,out int2 dropCell,out float3 dropPosition){'+b+'}')
# Emit source pending-selection/departure tail; rendering/self-arrival branches are outside admitted mode.
t=get(native,'UpdateDeliveryPhase');b=body(t);b=b[b.index('            if (transport.ActiveDrop != null)'):]
b=b.replace('transport.ActiveDrop != null','HasDrop(a)').replace('UpdateActiveTransportDrop(context, building, transport, now, ref randomState)','SourceCompleteDrop(a,now,ref randomState)').replace('RuntimeBuildingEntity.PendingProduction','SourcePendingProduction').replace('context.ProductionSystem','owner.Production').replace('building.PendingProductions','a.Building.PendingProductions').replace('transport.Prefab','a.Prefab').replace('transport.NextDropReadyAt','NextDropReadyAt(a)').replace('transport.HoldForNextReadySeconds','HoldSeconds(a)').replace('transport.HoverEnteredAt','HoverEnteredAt(a)').replace('transport.PhaseStartedAt','a.State.PhaseStartedAt').replace('transport.Phase','Phase(a)').replace('StartActiveTransportDrop(context, building, transport, readyPending, now)','SourceStartDrop(a,readyPending,now)')
b=b.replace('Phase(a) = 2;\n                a.State.PhaseStartedAt = now;','SetDeparture(a,now);')
if 'a.State.Phase = 2' in b or 'NextDropReadyAt(a) = ' in b or 'Phase(a) = ' in b:raise ValueError('Unsupported transport delivery timing assignment shape')
replace('Deliver','  void Deliver(Active a,float now,ref uint randomState){AdvanceActivePhase(a,now);'+b+'}')
# Extract the exact helicopter committed-position retry/anchor assignment block of the native drop starter.
t=get(native,'StartActiveTransportDrop');a=t.index('            if (transport.Mode == ProductionTransportMode.Helicopter)');z=block(t,a);branch=body(t[a:z])
branch=branch.replace('transport.HasCommittedDropPosition','a.HasCommittedDropPosition').replace('TryResolveCommittedHelicopterDropPosition(\n                        context,\n                        transport,','SourceCommitted(\n                        a,').replace('out Vector3 committedDropPosition','out float3 committedDropPosition').replace('transport.NextDropReadyAt','a.State.NextDropReadyAt').replace('ResolveTransportVisualCenterWorld(transport)','a.Pose.BoundsCenter(out _)').replace('Vector3','float3')
extra.append('void SourceStartDrop(Active a,SourcePendingProduction pending,float now){if(a.FixtureNoPresentation){SourceStartFixtureDrop(a,pending,now);return;}float3 dropStartPosition=a.State.Hover,dropEndPosition=DropPoint(a.Building,pending);int2 finalGoalCell=GridUtils.WorldToCell(owner.World.Grid,dropEndPosition);int2 committedDropCell=default;float3 committedDropPosition=default;'+branch.replace('out int2 committedDropCell','out committedDropCell').replace('out float3 committedDropPosition','out committedDropPosition')+'SharedProductionDeliveryKernel.BeginHelicopterDrop(ref a.State,now,dropStartPosition,dropEndPosition);a.Drop=pending;a.DropPosition=dropEndPosition;a.DropGoal=finalGoalCell;owner.RecordDeliveryEvent("helicopter_drop_started",now,a.Building.Id,pending.ProductionIndex,pending.Prefab.name,finalGoalCell,dropEndPosition,false,Entity.Null,true,pending.ReceiptId);}')
extra.append('void SourceStartFixtureDrop(Active a,SourcePendingProduction pending,float now){int2 cell=default;float3 position=default;if(!a.HasCommittedDropPosition||!SourceCommitted(a,out cell,out position)){a.FixturePhase.NextDropReadyAt=now+HelicopterDropBlockedRetryBaseSeconds;return;}SharedProductionDeliveryKernel.BeginHelicopterDropTiming(ref a.FixturePhase,ref a.FixtureDropTiming,ref a.FixtureHasDrop,now);a.Drop=pending;a.DropPosition=position;a.DropGoal=cell;owner.RecordDeliveryEvent("helicopter_drop_started",now,a.Building.Id,pending.ProductionIndex,pending.Prefab.name,cell,position,false,Entity.Null,true,pending.ReceiptId);}')
# Bind native visual timing to the shared data-only timing seam. The native
# duration remains 3s for planes and 2s for helicopters; the shared helicopter
# owner must pass that 2s value through BeginDropTiming and AdvanceDrop must use
# the common clamped progress policy. Do not accept a copied duration formula.
kernel=source['Assets/Game/Scripts/Simulation/Mathematics/SharedProductionDeliveryKernel.cs']
def require_drop_timing_contract(native_start,kernel_source):
 durations=re.findall(r'drop\.Duration\s*=\s*transport\.Mode\s*==\s*ProductionTransportMode\.Plane\s*\?\s*([^:;]+)\s*:\s*([^;]+);',native_start)
 if len(durations)!=1 or tuple(x.strip() for x in durations[0])!=('3f','2f'):
  raise ValueError('Unsupported native plane/helicopter drop durations')
 compact=re.sub(r'\s+','',kernel_source)
 required=(
  'publicstaticDropTimingBeginDropTiming(floatnow,floatduration)=>newDropTiming(now,duration);',
  'publicstaticfloatDropProgress(inDropTimingtiming,floatnow)=>math.clamp((now-timing.StartedAt)/math.max(.01f,timing.Duration),0f,1f);',
  'staticDropTimingBeginHelicopterDropTimingCore(intphase,boolhasDrop,floatnextDropReadyAt,floatnow){if(phase!=1||hasDrop||now<nextDropReadyAt)thrownewInvalidOperationException("Sourcedropadmission/order");returnBeginDropTiming(now,2f);}',
  'publicstaticvoidBeginHelicopterDropTiming(refTransportt,floatnow){vartiming=BeginHelicopterDropTimingCore(t.Phase,t.HasDrop,t.NextDropReadyAt,now);t.HasDrop=true;t.DropStartedAt=timing.StartedAt;t.DropDuration=timing.Duration;}',
  'publicstaticfloatAdvanceDropTiming(refTransportt,floatnow)=>AdvanceDropTiming(newDropTiming(t.DropStartedAt,t.DropDuration),t.HasDrop,now);',
  'publicstaticboolAdvanceDrop(refTransportt,floatnow){floata=AdvanceDropTiming(reft,now);t.DropPosition=Lerp(t.DropStart,t.DropEnd,a);returna>=1f;}')
 if any(fragment not in compact for fragment in required):
  raise ValueError('Native/shared helicopter drop timing seam mismatch')

require_drop_timing_contract(t,kernel)
# Focused source-shape negatives guard the exact duration binding, shared
# denominator floor, and common begin/progress call chain at recipe generation.
negative_cases=(
 (t.replace('drop.Duration = transport.Mode == ProductionTransportMode.Plane ? 3f : 2f;',
            'drop.Duration = transport.Mode == ProductionTransportMode.Plane ? 3f : 2.5f;',1),kernel),
 (t,kernel.replace('math.max(.01f,timing.Duration)','math.max(.02f,timing.Duration)',1)),
 (t,kernel.replace('BeginDropTiming(now,2f)','BeginDropTiming(now,3f)',1)),
)
for negative_native,negative_kernel in negative_cases:
 try:require_drop_timing_contract(negative_native,negative_kernel)
 except ValueError:pass
 else:raise ValueError('Drop timing contract negative control was not rejected')
# Exact native helicopter completed-drop branch; instance identity is the actual source spawn result.
t=get(native,'UpdateActiveTransportDrop');a=t.index('            else if (transport.Mode == ProductionTransportMode.Helicopter)');z=block(t,a);b=body(t[a:z]);b=b.replace('ResolveProductionGroundGoalCell(context, drop.EndPosition)','GridUtils.WorldToCell(owner.World.Grid,dropEndPosition)');b=re.sub(r'TrySpawnPlayerUnitNearBuilding\(context, building, production.ProductionIndex, production.ReservedProductionSlotIndex, drop.EndPosition, startCell, ref randomState, production\)','owner.World.TrySpawn(a.Building,production,dropEndPosition,startCell,ref randomState,out Entity member)',b);b=b.replace('MoveNewestProducedUnitToCell(context, building, drop.FinalGoalCell)','MoveNewestToCell(member,a.DropGoal)')
a=t.index('            bool removedProduction = false;');z=t.index('            ReturnTransportDropState(drop);',a);tail=t[a:z].replace('context.ProductionSystem','owner.Production').replace('building.PendingProductions','a.Building.PendingProductions').replace('owner.Production.ReleasePendingProduction(production)','owner.SourceReleasePending(production)')
# FinishDrop performs the native successful helicopter counter plus final clear/due assignment.
if not re.search(r'if \(spawned && transport.Mode == ProductionTransportMode.Helicopter\)\s*transport.DeliveredUnitCount\+\+;',t):raise ValueError('Unsupported native successful-drop counter shape')
extra.append('void SourceCompleteDrop(Active a,float now,ref uint randomState){bool complete=a.FixtureNoPresentation?SharedProductionDeliveryKernel.AdvanceDropTiming(a.FixtureDropTiming,a.FixtureHasDrop,now)>=1f:SharedProductionDeliveryKernel.AdvanceDrop(ref a.State,now);if(!complete)return;float3 dropEndPosition=a.FixtureNoPresentation?a.DropPosition:a.State.DropEnd;var production=a.Drop;bool spawned=false;'+b+'if(spawned)owner.DeliveredMembers++;owner.RecordDeliveryEvent(spawned?"unit_spawned_and_placed":"unit_spawn_failed",now,a.Building.Id,production.ProductionIndex,production.Prefab.name,startCell,dropEndPosition,spawned,spawned?member:Entity.Null,true,production.ReceiptId);'+tail+'if(a.FixtureNoPresentation)SharedProductionDeliveryKernel.FinishDrop(ref a.FixturePhase,ref a.FixtureHasDrop,ref a.FixtureDelivered,now,spawned);else SharedProductionDeliveryKernel.FinishDrop(ref a.State,now,spawned);a.Drop=null;}')
if 'Delivered(a)switch{1=>newint2(1,0),2=>newint2(-1,0),3=>newint2(0,1)' not in committed_policy or 'dropCell+=offset;' not in committed_policy or 'a.DropPosition=position' not in extra[-2] or 'a.FixtureNoPresentation?a.DropPosition:a.State.DropEnd' not in extra[-1]:raise ValueError('Fixture must preserve native later-drop offset and spawn at its per-drop resolved endpoint')
