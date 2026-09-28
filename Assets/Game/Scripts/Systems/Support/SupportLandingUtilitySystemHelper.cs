using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
namespace Game.Runtime
{
    public static class SupportLandingUtilitySystemHelper
    {
        public static bool TryGrid(EntityManager em,out Entity entity,out GridConfig grid)
        {
            using var q=em.CreateEntityQuery(typeof(GridConfig),typeof(GridWalkable));entity=Entity.Null;grid=default;
            if(q.CalculateEntityCount()!=1)return false;entity=q.GetSingletonEntity();grid=em.GetComponentData<GridConfig>(entity);return true;
        }
        public static bool IsFree(EntityManager em,Entity root,Entity gridEntity,in GridConfig grid,int2 cell,int2 footprint,Entity ownPassenger=default,Entity ownCrate=default)
        {
            var blocked=em.HasComponent<DynamicBlockerComponent>(gridEntity)?em.GetComponentData<DynamicBlockerComponent>(gridEntity).Blocked:default;
            var occupied=em.HasComponent<DynamicOccupancyComponent>(gridEntity)?em.GetComponentData<DynamicOccupancyComponent>(gridEntity).Occupied:default;
            if(!UnitTransportAirdropSystem.IsValidLandingCell(grid,em.GetBuffer<GridWalkable>(gridEntity,true).AsNativeArray(),blocked,occupied,cell,footprint))return false;
            var session=em.GetComponentData<SupportSessionComponent>(root);
            using var claims=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<SupportPassengerOwnerComponent>(),ComponentType.ReadOnly<UnitFootprint>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var claimChunks=claims.ToArchetypeChunkArray(Allocator.Temp);var entityType=em.GetEntityTypeHandle();
            int2 min=UnitFootprintUtility.GetMinCell(cell,UnitFootprintUtility.ClampSize(footprint));int2 max=min+UnitFootprintUtility.ClampSize(footprint);
            foreach(var chunk in claimChunks)foreach(var entity in chunk.GetNativeArray(entityType))
            {
                if(entity==ownPassenger)continue;var claim=em.GetComponentData<SupportPassengerOwnerComponent>(entity);
                if(claim.Landed!=0||!claim.SessionToken.Equals(session.SessionToken)||claim.AttemptOrdinal!=session.AttemptOrdinal)continue;
                int2 size=UnitFootprintUtility.ClampSize(em.GetComponentData<UnitFootprint>(entity).Size);
                int2 otherMin=UnitFootprintUtility.GetMinCell(claim.LandingCell,size);
                if(math.all(min<otherMin+size)&&math.all(otherMin<max))return false;
            }
            using var supplies=em.CreateEntityQuery(new EntityQueryDesc {All=new[]{ComponentType.ReadOnly<SupportSupplyCrateComponent>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var supplyChunks=supplies.ToArchetypeChunkArray(Allocator.Temp);
            foreach(var chunk in supplyChunks)foreach(var entity in chunk.GetNativeArray(entityType)){if(entity==ownCrate)continue;var claim=em.GetComponentData<SupportSupplyCrateComponent>(entity);if(!claim.SessionToken.Equals(session.SessionToken)||claim.AttemptOrdinal!=session.AttemptOrdinal)continue;int2 otherMin=claim.LandingCell;if(math.all(min<otherMin+1)&&math.all(otherMin<max))return false;}
            return true;
        }
        public static bool CountPopulation(EntityManager em,byte faction,out int total)
        {
            total=0;
            using var units=em.CreateEntityQuery(new EntityQueryDesc {
                All=new[]{ComponentType.ReadOnly<UnitHealth>(),ComponentType.ReadOnly<UnitMovementBehavior>(),ComponentType.ReadOnly<Faction>()},
                None=new[]{ComponentType.ReadOnly<UnitAirMovement>(),ComponentType.ReadOnly<RuntimeBuildingCombatInfo>(),ComponentType.ReadOnly<CampaignMissionDormantMapUnitTag>()},Options=EntityQueryOptions.IncludeDisabledEntities});
            using var unitChunks=units.ToArchetypeChunkArray(Allocator.Temp);
            var factionType=em.GetComponentTypeHandle<Faction>(true);var healthType=em.GetComponentTypeHandle<UnitHealth>(true);var movementType=em.GetComponentTypeHandle<UnitMovementBehavior>(true);
            foreach(var chunk in unitChunks){var factions=chunk.GetNativeArray(ref factionType);var health=chunk.GetNativeArray(ref healthType);var movement=chunk.GetNativeArray(ref movementType);for(int i=0;i<chunk.Count;i++)if(factions[i].Id==faction&&health[i].Current>0&&movement[i].UsesVehicleMotion==0)total++;}
            using var summaries=em.CreateEntityQuery(typeof(BuildingRuntimeUnitProductionSummary));
            using var summaryChunks=summaries.ToArchetypeChunkArray(Allocator.Temp);var entityType=em.GetEntityTypeHandle();
            foreach(var chunk in summaryChunks)foreach(var root in chunk.GetNativeArray(entityType))foreach(var queued in em.GetBuffer<BuildingRuntimeUnitProductionSummary>(root,true))
            {
                if(queued.FactionId!=faction||queued.QueuedCount<=0)continue;bool classified=false,infantry=false;
                if(em.HasBuffer<BuildingConfiguredUnitReadModel>(root))foreach(var configured in em.GetBuffer<BuildingConfiguredUnitReadModel>(root,true))
                    if(configured.UnitId.Equals(queued.UnitId)){classified=true;infantry=configured.IsVehicle==0;break;}
                if(!classified)return false;
                if(infantry)total=(int)System.Math.Min(int.MaxValue,(long)total+queued.QueuedCount);
            }
            return true;
        }
        public static SupportRejectionReason PlanParatroopers(EntityManager em,Entity root,in SupportRequestElement request,NativeList<int2> cells,NativeList<float3> positions)
        {
            if(request.TargetKind!=SupportTargetKind.LandingZone||request.Target!=Entity.Null)return SupportRejectionReason.InvalidTargetType;
            if(!em.HasComponent<SupportPayloadBindingsComponent>(root))return SupportRejectionReason.NotReady;
            var payload=em.GetComponentData<SupportPayloadBindingsComponent>(root);var policy=em.GetComponentData<SupportMissionPolicyComponent>(root);
            if(policy.PopulationCeiling<=0||payload.InfantryCount<=0||payload.InfantryCount>32||payload.InfantryPrefab==Entity.Null||!em.Exists(payload.InfantryPrefab)||
               !em.HasComponent<Faction>(payload.InfantryPrefab)||!em.HasComponent<UnitGrid>(payload.InfantryPrefab)||!em.HasComponent<Unity.Transforms.LocalTransform>(payload.InfantryPrefab)||
               !em.HasComponent<UnitHealth>(payload.InfantryPrefab)||em.GetComponentData<UnitHealth>(payload.InfantryPrefab).Current<=0||!em.HasComponent<UnitFootprint>(payload.InfantryPrefab)||!em.HasComponent<UnitMovementBehavior>(payload.InfantryPrefab)||
               em.GetComponentData<UnitMovementBehavior>(payload.InfantryPrefab).UsesVehicleMotion!=0||em.HasComponent<UnitAirMovement>(payload.InfantryPrefab)||
               payload.ParachutePrefab==Entity.Null||!em.Exists(payload.ParachutePrefab))return SupportRejectionReason.NotReady;
            var session=em.GetComponentData<SupportSessionComponent>(root);
            if(!CountPopulation(em,session.FactionId,out int population))return SupportRejectionReason.NotReady;
            if((long)population+payload.InfantryCount>policy.PopulationCeiling)return SupportRejectionReason.PopulationFull;
            if(!TryGrid(em,out var gridEntity,out var grid))return SupportRejectionReason.NotReady;
            using(var surface=em.CreateEntityQuery(typeof(MapSurfaceComponent)))if(surface.CalculateEntityCount()!=1)return SupportRejectionReason.NotReady;
            int2 center=GridUtils.WorldToCell(grid,request.Position),footprint=em.GetComponentData<UnitFootprint>(payload.InfantryPrefab).Size;
            for(int radius=0;radius<=14&&cells.Length<payload.InfantryCount;radius++)
                for(int y=-radius;y<=radius&&cells.Length<payload.InfantryCount;y++)for(int x=-radius;x<=radius&&cells.Length<payload.InfantryCount;x++)
                {
                    if(radius>0&&math.abs(x)!=radius&&math.abs(y)!=radius)continue;int2 cell=center+new int2(x,y);
                    if(!IsFree(em,root,gridEntity,grid,cell,footprint))continue;
                    int2 min=UnitFootprintUtility.GetMinCell(cell,footprint);bool overlaps=false;
                    foreach(var selected in cells){int2 other=UnitFootprintUtility.GetMinCell(selected,footprint);if(math.all(min<other+footprint)&&math.all(other<min+footprint)){overlaps=true;break;}}
                    if(overlaps)continue;float3 position=GridUtils.CellToWorldCenter(grid,cell);
                    if(!new MapSurfaceSpawnGrounding().TryGroundCellCenter(em,grid,cell,ref position,out _)||!math.all(math.isfinite(position))||
                       SupportTargetValidationUtilitySystemHelper.ValidateGround(em,root,position)!=SupportRejectionReason.None)continue;
                    cells.Add(cell);positions.Add(position);
                }
            return cells.Length==payload.InfantryCount?SupportRejectionReason.None:SupportRejectionReason.LandingBlocked;
        }
    }
}
