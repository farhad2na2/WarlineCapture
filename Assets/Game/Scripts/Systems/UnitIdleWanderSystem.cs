using Unity.Entities;
using Unity.Mathematics;

[UpdateAfter(typeof(UnitMoveVisualStateSystem))]
[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct UnitIdleWanderSystem : ISystem
{
    private const int MaxCandidateAttempts = 18;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<GridWalkable>();
        state.RequireForUpdate<DynamicBlockerComponent>();
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<UnitIdleWanderComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        GridConfig grid = SystemAPI.GetSingleton<GridConfig>();
        var walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray();
        var blocked = SystemAPI.GetComponent<DynamicBlockerComponent>(gridEntity).Blocked;
        var friendlyPassFactionIds = SystemAPI.GetComponent<DynamicBlockerComponent>(gridEntity).FriendlyPassFactionIds;
        var occupied = SystemAPI.GetComponent<DynamicOccupancyComponent>(gridEntity).Occupied;
        var factionLookup = SystemAPI.GetComponentLookup<Faction>(true);
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (unitGrid, footprint, movementBehavior, moveVisual, animationSettings, idleWanderState, health, entity) in SystemAPI
                 .Query<RefRO<UnitGrid>, RefRO<UnitFootprint>, RefRO<UnitMovementBehavior>, RefRO<UnitMoveVisualComponent>, RefRO<UnitAnimationSettings>, RefRW<UnitIdleWanderComponent>, RefRO<UnitHealth>>()
                 .WithNone<StaticGridBlocker>()
                 .WithNone<EngageTarget>()
                 .WithNone<UnitPathFollow>()
                 .WithNone<UnitPathRequest>()
                 .WithNone<ManualMoveOrderTag>()
                 .WithNone<UnitAirMovement>()
                 .WithNone<UnitDeathAnimationComponent>()
                 .WithNone<SelectedUnitTag>()
                 .WithEntityAccess())
        {
            ref var wanderState = ref idleWanderState.ValueRW;
            wanderState.RetrySeconds = math.max(0f, wanderState.RetrySeconds - dt);
            EnsureIdleDelay(ref wanderState, animationSettings.ValueRO);

            if (health.ValueRO.Current <= 0)
                continue;
            // RTS-controlled units should idle in place. Automatic walking creates
            // background path requests for large armies even when the player is idle.
            if (factionLookup.HasComponent(entity))
                continue;
            if (movementBehavior.ValueRO.AllowIdleWander == 0)
                continue;
            if (moveVisual.ValueRO.IsMoving != 0)
                continue;
            if (moveVisual.ValueRO.StillSeconds < wanderState.CurrentIdleDelaySeconds)
                continue;
            if (wanderState.RetrySeconds > 0f)
                continue;

            if (!TryFindIdleWanderGoal(
                    grid,
                    walkable,
                    blocked,
                    friendlyPassFactionIds,
                    occupied,
                    unitGrid.ValueRO.Cell,
                    footprint.ValueRO.Size,
                    factionLookup.HasComponent(entity) ? factionLookup[entity].Id : (byte)0,
                    animationSettings.ValueRO,
                    ref wanderState.RandomState,
                    out int2 goal))
            {
                wanderState.RetrySeconds = 1f;
                continue;
            }

            if (em.HasComponent<UnitTarget>(entity))
                ecb.SetComponent(entity, new UnitTarget { Cell = goal });
            else
                ecb.AddComponent(entity, new UnitTarget { Cell = goal });

            if (em.HasComponent<UnitPathRequest>(entity))
                ecb.SetComponent(entity, new UnitPathRequest { Goal = goal });
            else
                ecb.AddComponent(entity, new UnitPathRequest { Goal = goal });

            if (!em.HasComponent<AutoWanderMoveTag>(entity))
                ecb.AddComponent<AutoWanderMoveTag>(entity);

            wanderState.RetrySeconds = 0.75f;
            wanderState.CurrentIdleDelaySeconds = NextIdleDelaySeconds(ref wanderState.RandomState, animationSettings.ValueRO);
        }

        ecb.Playback(em);
        ecb.Dispose();
    }

    private static bool TryFindIdleWanderGoal(
        GridConfig grid,
        Unity.Collections.NativeArray<GridWalkable> walkable,
        Unity.Collections.NativeBitArray blocked,
        Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        Unity.Collections.NativeBitArray occupied,
        int2 originCell,
        int2 footprintSize,
        byte factionId,
        UnitAnimationSettings settings,
        ref uint randomState,
        out int2 goal)
    {
        goal = originCell;
        if (settings.IdleWanderDistanceMax <= 0f || grid.CellSize <= 0f)
            return false;

        randomState = math.max(1u, randomState);
        var random = new Unity.Mathematics.Random(randomState);
        float minDistance = math.max(0f, settings.IdleWanderDistanceMin);
        float maxDistance = math.max(minDistance, settings.IdleWanderDistanceMax);
        float3 originWorld = GridUtils.CellToWorldCenter(grid, originCell);

        for (int attempt = 0; attempt < MaxCandidateAttempts; attempt++)
        {
            float distance = random.NextFloat(minDistance, maxDistance);
            float angle = random.NextFloat(0f, math.PI * 2f);
            float3 offset = new float3(math.cos(angle) * distance, 0f, math.sin(angle) * distance);
            int2 candidate = GridUtils.WorldToCell(grid, originWorld + offset);
            if (candidate.Equals(originCell))
                continue;

            if (!IsValidIdleWanderCell(grid, walkable, blocked, friendlyPassFactionIds, occupied, originCell, footprintSize, candidate, factionId))
                continue;

            goal = candidate;
            randomState = random.state;
            return true;
        }

        randomState = random.state;
        return false;
    }

    private static bool IsValidIdleWanderCell(
        GridConfig grid,
        Unity.Collections.NativeArray<GridWalkable> walkable,
        Unity.Collections.NativeBitArray blocked,
        Unity.Collections.NativeArray<byte> friendlyPassFactionIds,
        Unity.Collections.NativeBitArray occupied,
        int2 originCell,
        int2 footprintSize,
        int2 candidate,
        byte factionId)
    {
        return UnitFootprintUtility.CanPlace(grid, walkable, blocked, friendlyPassFactionIds, occupied, candidate, footprintSize, originCell, factionId);
    }

    private static void EnsureIdleDelay(ref UnitIdleWanderComponent state, UnitAnimationSettings settings)
    {
        if (state.CurrentIdleDelaySeconds > 0f)
            return;

        state.CurrentIdleDelaySeconds = NextIdleDelaySeconds(ref state.RandomState, settings);
    }

    private static float NextIdleDelaySeconds(ref uint randomState, UnitAnimationSettings settings)
    {
        randomState = math.max(1u, randomState);
        var random = new Unity.Mathematics.Random(randomState);
        float minDelay = math.max(0f, settings.IdleDelayMinSeconds);
        float maxDelay = math.max(minDelay, settings.IdleDelayMaxSeconds);
        float delay = random.NextFloat(minDelay, maxDelay);

        randomState = random.state;
        return math.max(0f, delay);
    }
}
