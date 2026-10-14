using Unity.Collections;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Game.Components;

namespace Game.Runtime
{
    [BurstCompile]
    [UpdateAfter(typeof(DynamicBlockerInitSystem))]
    [UpdateBefore(typeof(UnitPathfindingSystem))]
    public partial struct StaticGridBlockerUpdateSystem : ISystem
    {
        private EntityQuery _gridQuery;

        private static void ApplyDelta(ref NativeArray<int> counts, ref NativeBitArray blocked, int cellIndex, int delta)
        {
            if ((uint)cellIndex >= (uint)counts.Length)
                return;

            int before = counts[cellIndex];
            int after = before + delta;
            counts[cellIndex] = after;

            if (before <= 0 && after > 0)
                blocked.Set(cellIndex, true);
            else if (before > 0 && after <= 0)
                blocked.Set(cellIndex, false);
        }

        private static void ApplyRectDelta(
            in GridConfig grid,
            ref NativeArray<int> counts,
            ref NativeBitArray blocked,
            int2 min,
            int2 max,
            int delta,
            in StaticGridBlockerSurfaceOpeningComponent opening)
        {
            int2 clampedMin = new int2(math.clamp(min.x, 0, grid.Width), math.clamp(min.y, 0, grid.Height));
            int2 clampedMax = new int2(math.clamp(max.x, 0, grid.Width), math.clamp(max.y, 0, grid.Height));
            bool hasOpening = opening.IsValid;
            for (int y = clampedMin.y; y < clampedMax.y; y++)
            {
                int row = y * grid.Width;
                for (int x = clampedMin.x; x < clampedMax.x; x++)
                    if (!hasOpening || !opening.ContainsFullCell(grid, x, y))
                        ApplyDelta(ref counts, ref blocked, row + x, delta);
            }
        }

        private static void ApplyFriendlyPassRect(
            in GridConfig grid,
            ref NativeArray<byte> friendlyPassFactionIds,
            int2 min,
            int2 max,
            byte factionId)
        {
            if (!friendlyPassFactionIds.IsCreated)
                return;

            int2 clampedMin = new int2(math.clamp(min.x, 0, grid.Width), math.clamp(min.y, 0, grid.Height));
            int2 clampedMax = new int2(math.clamp(max.x, 0, grid.Width), math.clamp(max.y, 0, grid.Height));
            for (int y = clampedMin.y; y < clampedMax.y; y++)
            {
                int row = y * grid.Width;
                for (int x = clampedMin.x; x < clampedMax.x; x++)
                    friendlyPassFactionIds[row + x] = factionId;
            }
        }

        private static void ComputeBounds(int2 origin, in GridBlockerSize size, out int2 min, out int2 max)
        {
            min = origin;
            int2 s = size.Size;
            if (s.x < 1) s.x = 1;
            if (s.y < 1) s.y = 1;
            max = min + s;
        }

        private static byte GetFriendlyPassFactionId(ComponentLookup<FriendlyPassGridBlocker> friendlyPassLookup, Entity entity)
        {
            return friendlyPassLookup.HasComponent(entity)
                ? friendlyPassLookup[entity].AllowedFactionId
                : byte.MaxValue;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _gridQuery = state.GetEntityQuery(ComponentType.ReadOnly<GridConfig>());
            state.RequireForUpdate(_gridQuery);
            state.RequireForUpdate<DynamicBlockerComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            Entity gridEntity = _gridQuery.GetSingletonEntity();
            var grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
            var blockerDataRw = SystemAPI.GetComponentRW<DynamicBlockerComponent>(gridEntity);

            var counts = blockerDataRw.ValueRW.Counts;
            var blocked = blockerDataRw.ValueRW.Blocked;
            var friendlyPassFactionIds = blockerDataRw.ValueRW.FriendlyPassFactionIds;
            var friendlyPassLookup = SystemAPI.GetComponentLookup<FriendlyPassGridBlocker>(true);
            var authoredBuildingLookup = SystemAPI.GetComponentLookup<OperationMapBuildingComponent>(true);
            var buildingInfoLookup = SystemAPI.GetComponentLookup<RuntimeBuildingCombatInfo>(true);
            var openingLookup = SystemAPI.GetComponentLookup<StaticGridBlockerSurfaceOpeningComponent>(true);
            var prevOpeningLookup = SystemAPI.GetComponentLookup<StaticGridBlockerPrevSurfaceOpeningComponent>(true);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (unitGrid, size, entity) in
                     SystemAPI.Query<RefRO<UnitGrid>, RefRO<GridBlockerSize>>()
                         .WithAll<StaticGridBlocker>()
                         .WithNone<StaticBlockerPrevBounds>()
                         .WithEntityAccess())
            {
                // Authored map buildings use UnitGrid for their selection centre.
                // Their authoritative foundation origin is stored separately.
                int2 origin = authoredBuildingLookup.HasComponent(entity) && buildingInfoLookup.HasComponent(entity)
                    ? buildingInfoLookup[entity].OriginCell : unitGrid.ValueRO.Cell;
                ComputeBounds(origin, size.ValueRO, out var min, out var max);
                byte friendlyPassFactionId = GetFriendlyPassFactionId(friendlyPassLookup, entity);
                var opening = openingLookup.HasComponent(entity) ? openingLookup[entity] : default;
                ApplyRectDelta(grid, ref counts, ref blocked, min, max, +1, opening);
                if (openingLookup.HasComponent(entity))
                    ecb.AddComponent(entity, new StaticGridBlockerPrevSurfaceOpeningComponent { Value = opening });
                if (friendlyPassFactionId != byte.MaxValue)
                    ApplyFriendlyPassRect(grid, ref friendlyPassFactionIds, min, max, friendlyPassFactionId);
                ecb.AddComponent(entity, new StaticBlockerPrevBounds
                {
                    Min = min,
                    Max = max,
                    FriendlyPassFactionId = friendlyPassFactionId
                });
            }

            foreach (var (unitGrid, size, prev, entity) in
                     SystemAPI.Query<RefRO<UnitGrid>, RefRO<GridBlockerSize>, RefRW<StaticBlockerPrevBounds>>()
                         .WithAll<StaticGridBlocker>()
                         .WithEntityAccess())
            {
                int2 origin = authoredBuildingLookup.HasComponent(entity) && buildingInfoLookup.HasComponent(entity)
                    ? buildingInfoLookup[entity].OriginCell : unitGrid.ValueRO.Cell;
                ComputeBounds(origin, size.ValueRO, out var min, out var max);
                int2 oldMin = prev.ValueRO.Min;
                int2 oldMax = prev.ValueRO.Max;
                byte oldFriendlyPassFactionId = prev.ValueRO.FriendlyPassFactionId;
                byte friendlyPassFactionId = GetFriendlyPassFactionId(friendlyPassLookup, entity);
                bool boundsChanged = min.x != oldMin.x || min.y != oldMin.y || max.x != oldMax.x || max.y != oldMax.y;
                var oldOpening = prevOpeningLookup.HasComponent(entity) ? prevOpeningLookup[entity].Value : default;
                var opening = openingLookup.HasComponent(entity) ? openingLookup[entity] : default;
                bool openingChanged = !opening.SameGeometry(oldOpening);
                bool friendlyPassChanged = oldFriendlyPassFactionId != friendlyPassFactionId;
                if (!boundsChanged && !friendlyPassChanged && !openingChanged)
                    continue;

                if (boundsChanged || openingChanged)
                {
                    ApplyRectDelta(grid, ref counts, ref blocked, oldMin, oldMax, -1, oldOpening);
                    ApplyRectDelta(grid, ref counts, ref blocked, min, max, +1, opening);
                    if (openingLookup.HasComponent(entity))
                    {
                        var previous = new StaticGridBlockerPrevSurfaceOpeningComponent { Value = opening };
                        if (prevOpeningLookup.HasComponent(entity)) ecb.SetComponent(entity, previous);
                        else ecb.AddComponent(entity, previous);
                    }
                    else if (prevOpeningLookup.HasComponent(entity))
                        ecb.RemoveComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(entity);
                }

                if (oldFriendlyPassFactionId != byte.MaxValue)
                    ApplyFriendlyPassRect(grid, ref friendlyPassFactionIds, oldMin, oldMax, byte.MaxValue);
                if (friendlyPassFactionId != byte.MaxValue)
                    ApplyFriendlyPassRect(grid, ref friendlyPassFactionIds, min, max, friendlyPassFactionId);

                prev.ValueRW.Min = min;
                prev.ValueRW.Max = max;
                prev.ValueRW.FriendlyPassFactionId = friendlyPassFactionId;
            }

            foreach (var (prev, entity) in
                     SystemAPI.Query<RefRO<StaticBlockerPrevBounds>>()
                         .WithNone<StaticGridBlocker>()
                         .WithEntityAccess())
            {
                var oldOpening = prevOpeningLookup.HasComponent(entity) ? prevOpeningLookup[entity].Value : default;
                ApplyRectDelta(grid, ref counts, ref blocked, prev.ValueRO.Min, prev.ValueRO.Max, -1, oldOpening);
                if (prevOpeningLookup.HasComponent(entity))
                    ecb.RemoveComponent<StaticGridBlockerPrevSurfaceOpeningComponent>(entity);
                if (prev.ValueRO.FriendlyPassFactionId != byte.MaxValue)
                    ApplyFriendlyPassRect(grid, ref friendlyPassFactionIds, prev.ValueRO.Min, prev.ValueRO.Max, byte.MaxValue);

                ecb.RemoveComponent<StaticBlockerPrevBounds>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}
