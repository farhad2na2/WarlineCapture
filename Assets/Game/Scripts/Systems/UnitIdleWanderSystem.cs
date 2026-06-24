using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
[UpdateBefore(typeof(UnitPathfindingSystem))]
public partial struct UnitIdleWanderSystem : ISystem
{
    private const int MaxCandidateAttempts = 18;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GridConfig>();
        state.RequireForUpdate<GridWalkable>();
        state.RequireForUpdate<DynamicBlockerComponent>();
        state.RequireForUpdate<DynamicOccupancyComponent>();
        state.RequireForUpdate<UnitIdleWanderComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Entity gridEntity = SystemAPI.GetSingletonEntity<GridConfig>();
        GridConfig grid = SystemAPI.GetComponent<GridConfig>(gridEntity);
        DynamicBlockerComponent blocker = SystemAPI.GetComponent<DynamicBlockerComponent>(gridEntity);
        DynamicOccupancyComponent occupancy = SystemAPI.GetComponent<DynamicOccupancyComponent>(gridEntity);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new IdleWanderJob
        {
            Grid = grid,
            Walkable = SystemAPI.GetBuffer<GridWalkable>(gridEntity).AsNativeArray(),
            Blocked = blocker.Blocked,
            FriendlyPassFactionIds = blocker.FriendlyPassFactionIds,
            Occupied = occupancy.Occupied,
            TargetLookup = SystemAPI.GetComponentLookup<UnitTarget>(true),
            AutoWanderLookup = SystemAPI.GetComponentLookup<AutoWanderMoveTag>(true),
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb.AsParallelWriter()
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    [WithNone(
        typeof(StaticGridBlocker),
        typeof(EngageTarget),
        typeof(UnitPathFollow),
        typeof(UnitPathRequest),
        typeof(ManualMoveOrderTag),
        typeof(UnitAirMovement),
        typeof(UnitDeathAnimationComponent),
        typeof(SelectedUnitTag),
        typeof(Faction))]
    private partial struct IdleWanderJob : IJobEntity
    {
        [ReadOnly] public GridConfig Grid;
        [ReadOnly] public NativeArray<GridWalkable> Walkable;
        [ReadOnly] public NativeBitArray Blocked;
        [ReadOnly] public NativeArray<byte> FriendlyPassFactionIds;
        [ReadOnly] public NativeBitArray Occupied;
        [ReadOnly] public ComponentLookup<UnitTarget> TargetLookup;
        [ReadOnly] public ComponentLookup<AutoWanderMoveTag> AutoWanderLookup;
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter Ecb;

        private void Execute(
            [EntityIndexInQuery] int sortKey,
            Entity entity,
            in UnitGrid unitGrid,
            in UnitFootprint footprint,
            in UnitMovementBehavior movementBehavior,
            in UnitMoveVisualComponent moveVisual,
            in UnitAnimationSettings animationSettings,
            ref UnitIdleWanderComponent idleWanderState,
            in UnitHealth health)
        {
            idleWanderState.RetrySeconds = math.max(0f, idleWanderState.RetrySeconds - DeltaTime);
            EnsureIdleDelay(ref idleWanderState, animationSettings);

            if (health.Current <= 0)
                return;
            if (movementBehavior.AllowIdleWander == 0)
                return;
            if (moveVisual.IsMoving != 0)
                return;
            if (moveVisual.StillSeconds < idleWanderState.CurrentIdleDelaySeconds)
                return;
            if (idleWanderState.RetrySeconds > 0f)
                return;

            if (!TryFindIdleWanderGoal(
                    Grid,
                    Walkable,
                    Blocked,
                    FriendlyPassFactionIds,
                    Occupied,
                    unitGrid.Cell,
                    footprint.Size,
                    0,
                    animationSettings,
                    ref idleWanderState.RandomState,
                    out int2 goal))
            {
                idleWanderState.RetrySeconds = 1f;
                return;
            }

            if (TargetLookup.HasComponent(entity))
                Ecb.SetComponent(sortKey, entity, new UnitTarget { Cell = goal });
            else
                Ecb.AddComponent(sortKey, entity, new UnitTarget { Cell = goal });

            Ecb.AddComponent(sortKey, entity, new UnitPathRequest { Goal = goal });

            if (!AutoWanderLookup.HasComponent(entity))
                Ecb.AddComponent<AutoWanderMoveTag>(sortKey, entity);

            idleWanderState.RetrySeconds = 0.75f;
            idleWanderState.CurrentIdleDelaySeconds = NextIdleDelaySeconds(ref idleWanderState.RandomState, animationSettings);
        }
    }

    private static bool TryFindIdleWanderGoal(
        GridConfig grid,
        NativeArray<GridWalkable> walkable,
        NativeBitArray blocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
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
        NativeArray<GridWalkable> walkable,
        NativeBitArray blocked,
        NativeArray<byte> friendlyPassFactionIds,
        NativeBitArray occupied,
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
