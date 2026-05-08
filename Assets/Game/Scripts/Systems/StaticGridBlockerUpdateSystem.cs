using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(DynamicBlockerInitSystem))]
[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct StaticGridBlockerUpdateSystem : ISystem
{
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
        int delta)
    {
        int2 clampedMin = new int2(math.clamp(min.x, 0, grid.Width), math.clamp(min.y, 0, grid.Height));
        int2 clampedMax = new int2(math.clamp(max.x, 0, grid.Width), math.clamp(max.y, 0, grid.Height));
        for (int y = clampedMin.y; y < clampedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = clampedMin.x; x < clampedMax.x; x++)
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

    private static void ComputeBounds(in UnitGrid unitGrid, in GridBlockerSize size, out int2 min, out int2 max)
    {
        min = unitGrid.Cell;
        int2 s = size.Size;
        if (s.x < 1) s.x = 1;
        if (s.y < 1) s.y = 1;
        max = min + s;
    }

    private static byte GetFriendlyPassFactionId(EntityManager em, Entity entity)
    {
        return em.HasComponent<FriendlyPassGridBlocker>(entity)
            ? em.GetComponentData<FriendlyPassGridBlocker>(entity).AllowedFactionId
            : byte.MaxValue;
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<DynamicBlockerData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var grid = SystemAPI.GetSingleton<GridConfig>();
        var gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        var blockerDataRw = SystemAPI.GetComponentRW<DynamicBlockerData>(gridEntity);

        var counts = blockerDataRw.ValueRW.Counts;
        var blocked = blockerDataRw.ValueRW.Blocked;
        var friendlyPassFactionIds = blockerDataRw.ValueRW.FriendlyPassFactionIds;

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (unitGrid, size, entity) in
                 SystemAPI.Query<RefRO<UnitGrid>, RefRO<GridBlockerSize>>()
                     .WithAll<StaticGridBlocker>()
                     .WithNone<StaticBlockerPrevBounds>()
                     .WithEntityAccess())
        {
            ComputeBounds(unitGrid.ValueRO, size.ValueRO, out var min, out var max);
            byte friendlyPassFactionId = GetFriendlyPassFactionId(state.EntityManager, entity);
            ApplyRectDelta(grid, ref counts, ref blocked, min, max, +1);
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
            ComputeBounds(unitGrid.ValueRO, size.ValueRO, out var min, out var max);
            int2 oldMin = prev.ValueRO.Min;
            int2 oldMax = prev.ValueRO.Max;
            byte oldFriendlyPassFactionId = prev.ValueRO.FriendlyPassFactionId;
            byte friendlyPassFactionId = GetFriendlyPassFactionId(state.EntityManager, entity);
            bool boundsChanged = min.x != oldMin.x || min.y != oldMin.y || max.x != oldMax.x || max.y != oldMax.y;
            bool friendlyPassChanged = oldFriendlyPassFactionId != friendlyPassFactionId;
            if (!boundsChanged && !friendlyPassChanged)
                continue;

            if (boundsChanged)
            {
                ApplyRectDelta(grid, ref counts, ref blocked, oldMin, oldMax, -1);
                ApplyRectDelta(grid, ref counts, ref blocked, min, max, +1);
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
            ApplyRectDelta(grid, ref counts, ref blocked, prev.ValueRO.Min, prev.ValueRO.Max, -1);
            if (prev.ValueRO.FriendlyPassFactionId != byte.MaxValue)
                ApplyFriendlyPassRect(grid, ref friendlyPassFactionIds, prev.ValueRO.Min, prev.ValueRO.Max, byte.MaxValue);

            ecb.RemoveComponent<StaticBlockerPrevBounds>(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
