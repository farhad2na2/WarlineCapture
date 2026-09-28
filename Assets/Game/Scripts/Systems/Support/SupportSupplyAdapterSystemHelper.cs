using Game.Components;
using Game.Runtime.Pathfinding;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
namespace Game.Runtime
{
    public static class SupportSupplyAdapterSystemHelper
    {
        public static SupportRejectionReason Plan(EntityManager em,Entity root,in SupportRequestElement request,out int2 cell,out float3 position,Entity ownCrate=default)
        {
            cell=default;position=default;
            if(request.TargetKind!=SupportTargetKind.LandingZone||request.Target!=Entity.Null)return SupportRejectionReason.InvalidTargetType;
            if(!em.HasComponent<SupportPayloadBindingsComponent>(root))return SupportRejectionReason.NotReady;
            var bindings=em.GetComponentData<SupportPayloadBindingsComponent>(root);
            if(!em.Exists(bindings.CratePrefab)||!em.HasComponent<LocalTransform>(bindings.CratePrefab)||em.HasComponent<UnitHealth>(bindings.CratePrefab)||em.HasComponent<UnitGrid>(bindings.CratePrefab)||!em.Exists(bindings.ParachutePrefab))return SupportRejectionReason.NotReady;
            if(!SupportLandingUtilitySystemHelper.TryGrid(em,out var gridEntity,out var grid))return SupportRejectionReason.NotReady;
            cell=GridUtils.WorldToCell(grid,request.Position);position=GridUtils.CellToWorldCenter(grid,cell);
            if(!SupportLandingUtilitySystemHelper.IsFree(em,root,gridEntity,grid,cell,new int2(1),default,ownCrate)||
                !new MapSurfaceSpawnGrounding().TryGroundCellCenter(em,grid,cell,ref position,out _)||!math.all(math.isfinite(position)))return SupportRejectionReason.LandingBlocked;
            var ground=SupportTargetValidationUtilitySystemHelper.ValidateGround(em,root,position);if(ground!=SupportRejectionReason.None)return ground;
            var session=em.GetComponentData<SupportSessionComponent>(root);return MaterialsEntity(em,session.FactionId)!=Entity.Null?SupportRejectionReason.None:SupportRejectionReason.NotReady;
        }
        public static Entity MaterialsEntity(EntityManager em,byte faction)
        {
            Entity found=Entity.Null;using var q=em.CreateEntityQuery(typeof(FactionTacticalMaterialsComponent));using var chunks=q.ToArchetypeChunkArray(Allocator.Temp);var entityType=em.GetEntityTypeHandle();
            foreach(var chunk in chunks)foreach(var entity in chunk.GetNativeArray(entityType))if(em.GetComponentData<FactionTacticalMaterialsComponent>(entity).FactionId==faction){if(found!=Entity.Null)return Entity.Null;found=entity;}return found;
        }
        public static Entity Prepare(EntityManager em,Entity root,Entity flight,in SupportRequestElement request,int materials)
        {
            Plan(em,root,request,out var cell,out var position);var session=em.GetComponentData<SupportSessionComponent>(root);
            var crate=em.Instantiate(em.GetComponentData<SupportPayloadBindingsComponent>(root).CratePrefab);
            em.AddComponentData(crate,new SupportSupplyCrateComponent {Root=root,Flight=flight,SessionToken=session.SessionToken,AttemptOrdinal=session.AttemptOrdinal,FactionId=session.FactionId,RemainingMaterials=materials,LandingCell=cell,LandingPosition=position});
            // This is cargo artwork with stock; it has no unit/vehicle/gameplay identity.
            em.AddBuffer<UnitTransportHiddenVisualScale>(crate);UnitTransportVisualUtility.SetPassengerVisible(em,crate,false);em.AddComponent<Disabled>(crate);return crate;
        }
        public static void DestroyUnreleased(EntityManager em,Entity flight)
        {
            if(!em.Exists(flight)||!em.HasComponent<SupportFlightCleanupComponent>(flight))return;
            var cleanup=em.GetComponentData<SupportFlightCleanupComponent>(flight);
            if(cleanup.Released==0&&em.Exists(cleanup.Payload))em.DestroyEntity(cleanup.Payload);
        }
        public static void Advance(EntityManager em,Entity entity,SupportFlightComponent flight,SupportSessionComponent session,float elapsed)
        {
            if(flight.Phase==SupportExecutionPhase.Approaching)
            {
                // Landing/population snapshots belong to acceptance and actual release,
                // not every approach frame. Fuel stays held until this final validation.
                float t=math.saturate((float)(session.SimulationSeconds-flight.StartedAt)/math.max(.001f,flight.ApproachSeconds));SupportFlightSystem.SetTransform(em,entity,math.lerp(flight.Entry,flight.Release,t),flight.Release-flight.Entry);if(t<1)return;
                var reason=Plan(em,flight.Root,flight.Request,out _,out var position,flight.Payload);
                if(reason==SupportRejectionReason.None&&(!SupportAirValidationUtilitySystemHelper.SafeRoute(em,flight.Root,session,out var route,true,flight.Request.Position)||route.Version!=flight.RouteVersion))reason=SupportRejectionReason.NoSafeAirRoute;
                if(reason==SupportRejectionReason.None&&!em.Exists(flight.Payload))reason=SupportRejectionReason.TargetGone;
                if(reason!=SupportRejectionReason.None){SupportFlightSystem.Abort(em,entity,reason);return;}
                if(!SupportFuelTransactionSystem.TryConsumeReserved(em,entity,session.FactionId)){SupportFlightSystem.Abort(em,entity,SupportRejectionReason.InsufficientFuel);return;}
                var canopy=em.Instantiate(em.GetComponentData<SupportPayloadBindingsComponent>(flight.Root).ParachutePrefab);
                var canopyTransform=em.HasComponent<LocalTransform>(canopy)?em.GetComponentData<LocalTransform>(canopy):LocalTransform.Identity;canopyTransform.Position=flight.Release+new float3(0,1.6f,0);
                if(em.HasComponent<LocalTransform>(canopy))em.SetComponentData(canopy,canopyTransform);else em.AddComponentData(canopy,canopyTransform);
                em.AddComponentData(canopy,new UnitTransportAirdropVisualCleanup());
                var crate=em.GetComponentData<SupportSupplyCrateComponent>(flight.Payload);crate.Released=1;crate.Canopy=canopy;crate.LandingPosition=position;em.SetComponentData(flight.Payload,crate);
                UnitTransportVisualUtility.SetPassengerVisible(em,flight.Payload,true);
                if(em.HasComponent<Disabled>(flight.Payload))em.RemoveComponent<Disabled>(flight.Payload);var transform=em.GetComponentData<LocalTransform>(flight.Payload);transform.Position=flight.Release;em.SetComponentData(flight.Payload,transform);
                em.AddComponentData(flight.Payload,new UnitTransportCargoDropComponent {StartPosition=flight.Release,EndPosition=position,LandingCell=crate.LandingCell,StartedAt=elapsed,DurationSeconds=4.8f,VisualEntity=canopy});
                if(em.HasComponent<UnitTransportPlaneDoorReference>(entity)){if(!em.HasComponent<UnitTransportPlaneDoorState>(entity))em.AddComponent<UnitTransportPlaneDoorState>(entity);em.AddComponentData(entity,new UnitTransportPlaneDoorOpenRequest {RemainingSeconds=.75f});}
                flight.Phase=SupportExecutionPhase.Released;flight.ReleasedAt=session.SimulationSeconds;em.SetComponentData(entity,flight);var cleanup=em.GetComponentData<SupportFlightCleanupComponent>(entity);cleanup.Released=1;em.SetComponentData(entity,cleanup);
                SupportFlightSystem.SetReceipt(em,flight,SupportExecutionPhase.Resolved,SupportRejectionReason.None,true);
            }
            else {float t=math.saturate((float)(session.SimulationSeconds-flight.ReleasedAt)/math.max(.001f,flight.ExitSeconds));SupportFlightSystem.SetTransform(em,entity,math.lerp(flight.Release,flight.Exit,t),flight.Exit-flight.Release);if(t>=1)em.DestroyEntity(entity);}
        }
        public static bool EligibleCollector(EntityManager em,Entity collector,in SupportSupplyCrateComponent crate)
        {
            if(!em.Exists(collector)||em.HasComponent<Disabled>(collector)||!em.HasComponent<UnitHealth>(collector)||em.GetComponentData<UnitHealth>(collector).Current<=0||
                !em.HasComponent<Faction>(collector)||em.GetComponentData<Faction>(collector).Id!=crate.FactionId||!em.HasComponent<UnitGrid>(collector)||!em.HasComponent<UnitMove>(collector)||!em.HasComponent<LocalTransform>(collector)||
                em.HasComponent<UnitAirMovement>(collector)||em.HasComponent<UnitTransportPassenger>(collector)||em.HasComponent<UnitTransportParachuteDropComponent>(collector)||em.HasComponent<UnitTransportCargoDropComponent>(collector)||em.HasComponent<UnitTransportAirdropSettleComponent>(collector)||em.HasComponent<CampaignMissionDormantMapUnitTag>(collector)||em.HasComponent<RuntimeBuildingCombatInfo>(collector))return false;
            if(em.HasComponent<CampaignMissionUnitRoleComponent>(collector)&&!em.GetComponentData<CampaignMissionUnitRoleComponent>(collector).SessionToken.Equals(crate.SessionToken))return false;
            return true;
        }
        public static SupportRejectionReason BeginCollection(EntityManager em,Entity root,Entity collector,Entity entity)
        {
            if(!em.Exists(entity)||!em.HasComponent<SupportSupplyCrateComponent>(entity))return SupportRejectionReason.TargetGone;
            var crate=em.GetComponentData<SupportSupplyCrateComponent>(entity);var session=em.GetComponentData<SupportSessionComponent>(root);
            if(crate.Root!=root||!crate.SessionToken.Equals(session.SessionToken)||crate.AttemptOrdinal!=session.AttemptOrdinal)return SupportRejectionReason.WrongAttempt;
            if(session.Active==0)return SupportRejectionReason.NotActive;if(crate.Landed==0||crate.RemainingMaterials<=0)return SupportRejectionReason.NotReady;
            if(!EligibleCollector(em,collector,crate))return SupportRejectionReason.InvalidTargetType;
            if(crate.Claimant!=Entity.Null)return SupportRejectionReason.AlreadyProcessed;
            if(!SupportLandingUtilitySystemHelper.TryGrid(em,out var gridEntity,out var grid))return SupportRejectionReason.NotReady;
            var start=em.GetComponentData<UnitGrid>(collector).Cell;var goal=crate.LandingCell;float best=float.MaxValue;bool found=false;
            var blocked=em.HasComponent<DynamicBlockerComponent>(gridEntity)?em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked:default;var occupied=em.HasComponent<DynamicOccupancyComponent>(gridEntity)?em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied:default;
            int2 size=em.HasComponent<UnitFootprint>(collector)?em.GetComponentData<UnitFootprint>(collector).Size:new int2(1);
            var passes=em.HasComponent<DynamicBlockerComponent>(gridEntity)?em.GetComponentData<DynamicBlockerComponent>(gridEntity).FriendlyPassFactionIds:default;
            // Keep only occupants which can affect the nine local collection candidates.
            // Read chunk storage directly rather than copy every unit into three arrays.
            using var entities=new NativeList<Entity>(16,Allocator.Temp);using var grids=new NativeList<UnitGrid>(16,Allocator.Temp);using var footprints=new NativeList<UnitFootprint>(16,Allocator.Temp);
            using(var live=em.CreateEntityQuery(typeof(UnitGrid),typeof(UnitFootprint)))
            using(var chunks=live.ToArchetypeChunkArray(Allocator.Temp))
            {
                var entityType=em.GetEntityTypeHandle();var gridType=em.GetComponentTypeHandle<UnitGrid>(true);var footprintType=em.GetComponentTypeHandle<UnitFootprint>(true);
                int2 localMin=UnitFootprintUtility.GetMinCell(crate.LandingCell,UnitFootprintUtility.ClampSize(size))-2,localMax=localMin+UnitFootprintUtility.ClampSize(size)+4;
                foreach(var chunk in chunks){var chunkEntities=chunk.GetNativeArray(entityType);var chunkGrids=chunk.GetNativeArray(ref gridType);var chunkFootprints=chunk.GetNativeArray(ref footprintType);
                    for(int i=0;i<chunk.Count;i++){int2 otherSize=UnitFootprintUtility.ClampSize(chunkFootprints[i].Size),otherMin=UnitFootprintUtility.GetMinCell(chunkGrids[i].Cell,otherSize);
                        if(!math.all(localMin<otherMin+otherSize)||!math.all(otherMin<localMax))continue;
                        entities.Add(chunkEntities[i]);grids.Add(chunkGrids[i]);footprints.Add(chunkFootprints[i]);}}
            }
            using var surfaces=em.CreateEntityQuery(typeof(MapSurfaceComponent));if(surfaces.CalculateEntityCount()!=1)return SupportRejectionReason.NotReady;var surface=surfaces.GetSingleton<MapSurfaceComponent>();
            bool vehicle=em.HasComponent<UnitMovementBehavior>(collector)&&em.GetComponentData<UnitMovementBehavior>(collector).UsesVehicleMotion!=0;
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++)
            {
                var candidate=crate.LandingCell+new int2(x,y);
                if(!UnitPathPlacementValidation.CanPlaceForPathing(grid,em.GetBuffer<GridWalkable>(gridEntity,true).AsNativeArray(),blocked,passes,occupied,entities.AsArray(),grids.AsArray(),footprints.AsArray(),default,collector,candidate,size,start,vehicle,false,crate.FactionId)||
                    !new MapSurfaceTraversalValidation().CanTraverseFootprint(surface,surface.HasSurfaceData,grid,candidate,size,vehicle))continue;
                // Prefer the centre; a closest-to-unit edge can leave path fallback outside interaction range.
                float d=math.distancesq((float2)crate.LandingCell,(float2)candidate)*1000+math.distancesq((float2)start,(float2)candidate);if(d<best){best=d;goal=candidate;found=true;}
            }
            if(!found)return SupportRejectionReason.LandingBlocked;
            if(!UnitMoveOrderRequestSystem.EnqueueAndProcessImmediateMoveOrder(em,collector,goal))return SupportRejectionReason.LandingBlocked;
            var order=new SupportCollectOrderComponent {Crate=entity,Goal=goal,ExpiresAt=session.SimulationSeconds+60};if(em.HasComponent<SupportCollectOrderComponent>(collector))em.SetComponentData(collector,order);else em.AddComponentData(collector,order);
            crate.Claimant=collector;em.SetComponentData(entity,crate);return SupportRejectionReason.None;
        }
        // A normal pathfinder fallback is useful for movement, but cannot satisfy a crate interaction outside two metres.
        public static bool ValidateCollectionPathResult(EntityManager em,Entity collector,in GridConfig grid,int2 end,bool succeeded,bool segmented)
        {
            if(!em.HasComponent<SupportCollectOrderComponent>(collector))return true;
            var order=em.GetComponentData<SupportCollectOrderComponent>(collector);
            bool valid=em.Exists(order.Crate)&&em.HasComponent<SupportSupplyCrateComponent>(order.Crate);
            SupportSupplyCrateComponent crate=valid?em.GetComponentData<SupportSupplyCrateComponent>(order.Crate):default;
            valid=valid&&EligibleCollector(em,collector,crate)&&succeeded&&(segmented||math.distancesq(GridUtils.CellToWorldCenter(grid,end).xz,crate.LandingPosition.xz)<=4);
            if(valid)return true;
            new UnitMoveOrderSystem().ClearMovementOrderComponents(em,collector);
            if(em.Exists(crate.Root)&&em.HasComponent<SupportSessionComponent>(crate.Root))
            {
                var session=em.GetComponentData<SupportSessionComponent>(crate.Root);
                if(crate.SessionToken.Equals(session.SessionToken)&&crate.AttemptOrdinal==session.AttemptOrdinal&&em.HasComponent<SupportCollectionFeedbackComponent>(crate.Root))
                    em.SetComponentData(crate.Root,new SupportCollectionFeedbackComponent {Reason=SupportRejectionReason.LandingBlocked});
            }
            return false;
        }
        // The resource owner and crate stock are written together on the main ECS thread.
        public static int Transfer(EntityManager em,Entity entity,ref SupportSupplyCrateComponent crate,Entity materialsEntity=default)
        {
            if(!EligibleCollector(em,crate.Claimant,crate)||!em.HasComponent<SupportCollectOrderComponent>(crate.Claimant)||em.GetComponentData<SupportCollectOrderComponent>(crate.Claimant).Crate!=entity||
                math.distancesq(em.GetComponentData<LocalTransform>(crate.Claimant).Position.xz,crate.LandingPosition.xz)>4)return 0;
            var storage=materialsEntity==Entity.Null?MaterialsEntity(em,crate.FactionId):materialsEntity;if(storage==Entity.Null)return 0;var materials=em.GetComponentData<FactionTacticalMaterialsComponent>(storage);
            int accepted=math.min(crate.RemainingMaterials,math.max(0,materials.Capacity-materials.Current));if(accepted<=0)return 0;
            if(FactionTacticalMaterialsUtilitySystemHelper.TryGrant(ref materials,accepted,FactionTacticalMaterialsSourceKind.Reward)!=FactionTacticalMaterialsMutationResult.Applied)return 0;
            crate.RemainingMaterials-=accepted;em.SetComponentData(storage,materials);em.SetComponentData(entity,crate);return accepted;
        }
    }
}
