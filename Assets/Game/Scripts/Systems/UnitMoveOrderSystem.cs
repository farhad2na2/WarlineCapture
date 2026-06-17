using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

[DisableAutoCreation]
public partial struct UnitMoveOrderSystem : ISystem
{
    private const int ManualMoveGoalSearchRadiusInfantry = 12;
    private const int ManualMoveGoalSearchRadiusVehicle = 20;
    private const int ManualMoveGoalPaddingInfantry = 1;
    private const int ManualMoveGoalPaddingVehicle = 0;

    public struct MoveOrderCommandResult
    {
        public bool Issued;
        public int StructuralAdds;
        public int StructuralRemoves;
        public int PathRequests;
        public int StaggeredPathRequests;
        public int MaxStaggerDelayFrames;
        public int AirUnits;
    }

    public void OnCreate(ref SystemState state)
    {
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state)
    {
    }

    public MoveOrderCommandResult IssueGroupedManualMoveOrder(
        EntityManager entityManager,
        Entity entity,
        int2 goal,
        bool issueGroundPathNow,
        bool useGroundPathRetryCooldown,
        int resumeFrame,
        int currentFrame)
    {
        MoveOrderCommandResult result = new() { Issued = true };
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
        {
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                $"unitMoveOrderGrouped caller={ResolveCaller()} entityBefore={DescribeMoveEntity(entityManager, entity)} " +
                $"goal={goal} issuePathNow={issueGroundPathNow} retry={useGroundPathRetryCooldown} resumeFrame={resumeFrame} frame={currentFrame}");
        }

        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            result.StructuralRemoves += RemoveComponentIfPresent<EngageTarget>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitPathFollow>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRange>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRetryCooldown>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitLongDistanceMove>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<AutoWanderMoveTag>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<HoldPositionOrderTag>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<BaseBreachOrder>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitTransportBoardingTarget>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitTransportDeployOrder>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitTransportRopeDisembarkRequest>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitTransportAirdropRequest>(entityManager, ecb, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitResourceHaulOrder>(entityManager, ecb, entity) ? 1 : 0;

            if (!entityManager.HasComponent<ManualMoveGroupMemberTag>(entity))
            {
                ecb.AddComponent<ManualMoveGroupMemberTag>(entity);
                result.StructuralAdds++;
            }

            SetOrAdd(entityManager, ecb, entity, new UnitTarget { Cell = goal }, ref result);

            if (!entityManager.HasComponent<UnitAirMovement>(entity))
            {
                if (issueGroundPathNow)
                {
                    SetOrAdd(entityManager, ecb, entity, new UnitPathRequest { Goal = goal }, ref result);
                    result.PathRequests++;
                }
                else if (useGroundPathRetryCooldown)
                {
                    result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRequest>(entityManager, ecb, entity) ? 1 : 0;
                    ecb.AddComponent(entity, new UnitPathRetryCooldown { ResumeFrame = resumeFrame });
                    result.StructuralAdds++;
                    result.StaggeredPathRequests++;
                    result.MaxStaggerDelayFrames = math.max(0, resumeFrame - currentFrame);
                }
            }
            else
            {
                result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRequest>(entityManager, ecb, entity) ? 1 : 0;
                result.AirUnits++;
            }

            if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
            {
                ecb.AddComponent<ManualMoveOrderTag>(entity);
                result.StructuralAdds++;
            }

            ecb.Playback(entityManager);

            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            {
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace(
                    $"unitMoveOrderGroupedApplied entityAfter={DescribeMoveEntity(entityManager, entity)} goal={goal} " +
                    $"pathRequests={result.PathRequests} staggered={result.StaggeredPathRequests} air={result.AirUnits} " +
                    $"adds={result.StructuralAdds} removes={result.StructuralRemoves}");
            }
        }
        finally
        {
            ecb.Dispose();
        }

        return result;
    }

    public void IssueImmediateMoveCommand(EntityManager entityManager, Entity entity, int2 goal)
    {
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"unitMoveOrderImmediate caller={ResolveCaller()} entityBefore={DescribeMoveEntity(entityManager, entity)} goal={goal}");
        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            RemoveComponentIfPresent<EngageTarget>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitPathFollow>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitPathRange>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitPathRetryCooldown>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitLongDistanceMove>(entityManager, ecb, entity);
            RemoveComponentIfPresent<AutoWanderMoveTag>(entityManager, ecb, entity);
            RemoveComponentIfPresent<HoldPositionOrderTag>(entityManager, ecb, entity);
            RemoveComponentIfPresent<BaseBreachOrder>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportBoardingTarget>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportDeployOrder>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportRopeDisembarkRequest>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportAirdropRequest>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitResourceHaulOrder>(entityManager, ecb, entity);

            SetOrAdd(entityManager, ecb, entity, new UnitTarget { Cell = goal });

            if (!entityManager.HasComponent<UnitAirMovement>(entity))
                SetOrAdd(entityManager, ecb, entity, new UnitPathRequest { Goal = goal });
            else
                RemoveComponentIfPresent<UnitPathRequest>(entityManager, ecb, entity);

            if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
                ecb.AddComponent<ManualMoveOrderTag>(entity);

            ecb.Playback(entityManager);
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"unitMoveOrderImmediateApplied entityAfter={DescribeMoveEntity(entityManager, entity)} goal={goal}");
        }
        finally
        {
            ecb.Dispose();
        }
    }

    public void IssueTargetOnlyMoveCommand(EntityManager entityManager, Entity entity, int2 goal)
    {
        if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
            SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"unitMoveOrderTargetOnly caller={ResolveCaller()} entityBefore={DescribeMoveEntity(entityManager, entity)} goal={goal}");
        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            SetOrAdd(entityManager, ecb, entity, new UnitTarget { Cell = goal });
            RemoveComponentIfPresent<HoldPositionOrderTag>(entityManager, ecb, entity);
            RemoveComponentIfPresent<BaseBreachOrder>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportBoardingTarget>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportDeployOrder>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportRopeDisembarkRequest>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitTransportAirdropRequest>(entityManager, ecb, entity);
            RemoveComponentIfPresent<UnitResourceHaulOrder>(entityManager, ecb, entity);
            if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
                ecb.AddComponent<ManualMoveOrderTag>(entity);

            ecb.Playback(entityManager);
            if (SelectionRuntimeDiagnosticsSystem.EnableMoveCommandTrace)
                SelectionRuntimeDiagnosticsSystem.LogMoveCommandTrace($"unitMoveOrderTargetOnlyApplied entityAfter={DescribeMoveEntity(entityManager, entity)} goal={goal}");
        }
        finally
        {
            ecb.Dispose();
        }
    }

    public void ClearMovementOrderComponents(EntityManager entityManager, Entity entity)
    {
        EntityCommandBuffer ecb = new(Allocator.Temp);
        try
        {
            ClearMovementOrderComponents(entityManager, ecb, entity);
            ecb.Playback(entityManager);
        }
        finally
        {
            ecb.Dispose();
        }
    }

    public void ClearMovementOrderComponents(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity)
    {
        RemoveComponentIfPresent<UnitTarget>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitPathRequest>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitPathFollow>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitPathRange>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitPathRetryCooldown>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitLongDistanceMove>(entityManager, ecb, entity);
        RemoveComponentIfPresent<ManualMoveOrderTag>(entityManager, ecb, entity);
        RemoveComponentIfPresent<ManualMoveGroupMemberTag>(entityManager, ecb, entity);
        RemoveComponentIfPresent<AutoWanderMoveTag>(entityManager, ecb, entity);
        RemoveComponentIfPresent<HoldPositionOrderTag>(entityManager, ecb, entity);
        RemoveComponentIfPresent<EngageTarget>(entityManager, ecb, entity);
        RemoveComponentIfPresent<BaseBreachOrder>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitTransportBoardingTarget>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitTransportDeployOrder>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitTransportRopeDisembarkRequest>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitTransportAirdropRequest>(entityManager, ecb, entity);
        RemoveComponentIfPresent<UnitResourceHaulOrder>(entityManager, ecb, entity);
    }

    public bool RemoveComponentIfPresent<T>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<T>(entity))
            return false;

        ecb.RemoveComponent<T>(entity);
        return true;
    }

    public int2 FindManualMoveGoal(
        EntityManager entityManager,
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        HashSet<int> reservedGoalCells,
        HashSet<int> selectedCurrentCells,
        Entity entity,
        int2 desiredGoal,
        int slotIndex,
        MapSurfacePathfindingSnapshot.Context surfaceContext = default)
    {
        int2 footprintSize = entityManager.HasComponent<UnitFootprint>(entity)
            ? entityManager.GetComponentData<UnitFootprint>(entity).Size
            : new int2(1, 1);
        UnitMovementBehavior movementBehavior = entityManager.HasComponent<UnitMovementBehavior>(entity)
            ? entityManager.GetComponentData<UnitMovementBehavior>(entity)
            : default;
        bool isVehicle = UnitVehicleMovementUtility.IsVehicle(new UnitFootprint { Size = footprintSize }, movementBehavior);
        byte factionId = entityManager.HasComponent<Faction>(entity) ? entityManager.GetComponentData<Faction>(entity).Id : (byte)0;
        int goalPadding = isVehicle ? ManualMoveGoalPaddingVehicle : ManualMoveGoalPaddingInfantry;
        int2 slotAnchor = desiredGoal + GetManualMoveFormationOffset(slotIndex, footprintSize, goalPadding);

        if (CanReserveManualMoveGoal(
                grid,
                walkable,
                blocked,
                friendlyPassFactionIds,
                occupied,
                reservedGoalCells,
                selectedCurrentCells,
                slotAnchor,
                footprintSize,
                goalPadding,
                factionId,
                surfaceContext,
                isVehicle))
        {
            ReserveManualMoveGoalFootprint(grid, reservedGoalCells, slotAnchor, footprintSize, goalPadding);
            return slotAnchor;
        }

        int maxRadius = isVehicle ? ManualMoveGoalSearchRadiusVehicle : ManualMoveGoalSearchRadiusInfantry;
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            int ringLen = math.max(1, 8 * radius);
            for (int step = 0; step < ringLen; step++)
            {
                int2 candidate = SquareRingOffset(radius, step) + slotAnchor;
                if (!CanReserveManualMoveGoal(
                        grid,
                        walkable,
                        blocked,
                        friendlyPassFactionIds,
                        occupied,
                        reservedGoalCells,
                        selectedCurrentCells,
                        candidate,
                        footprintSize,
                        goalPadding,
                        factionId,
                        surfaceContext,
                        isVehicle))
                {
                    continue;
                }

                ReserveManualMoveGoalFootprint(grid, reservedGoalCells, candidate, footprintSize, goalPadding);
                return candidate;
            }
        }

        return slotAnchor;
    }

    public int2 GetManualMoveFormationOffset(int slotIndex, int2 footprintSize, int padding)
    {
        if (slotIndex <= 0)
            return int2.zero;

        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int stride = math.max(1, math.max(size.x, size.y) + (padding * 2));
        int ringIndex = slotIndex - 1;
        int radius = 1;
        int accumulated = 0;
        while (true)
        {
            int ringLen = math.max(1, 8 * radius);
            if (ringIndex < accumulated + ringLen)
            {
                int step = ringIndex - accumulated;
                return SquareRingOffset(radius, step) * stride;
            }

            accumulated += ringLen;
            radius++;
        }
    }

    public bool CanReserveManualMoveGoal(
        in GridConfig grid,
        in NativeArray<GridWalkable> walkable,
        in NativeBitArray blocked,
        in NativeArray<byte> friendlyPassFactionIds,
        in NativeBitArray occupied,
        HashSet<int> reservedGoalCells,
        HashSet<int> selectedCurrentCells,
        int2 cell,
        int2 footprintSize,
        int padding,
        byte factionId,
        MapSurfacePathfindingSnapshot.Context surfaceContext = default,
        bool isVehicle = false)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);

        if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
            return false;

        if (!CanUseSurfaceFootprint(surfaceContext, grid, cell, size, isVehicle))
            return false;

        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
            {
                int idx = row + x;
                bool insideActualFootprint = x >= min.x && x < max.x && y >= min.y && y < max.y;
                if (insideActualFootprint)
                {
                    if (walkable[idx].Value == 0)
                        return false;
                    if (blocked.IsCreated && blocked.IsSet(idx) &&
                        (!friendlyPassFactionIds.IsCreated || (uint)idx >= (uint)friendlyPassFactionIds.Length || friendlyPassFactionIds[idx] != factionId))
                    {
                        return false;
                    }
                }

                if (occupied.IsCreated && occupied.IsSet(idx) && !selectedCurrentCells.Contains(idx))
                    return false;
                if (reservedGoalCells.Contains(idx))
                    return false;
            }
        }

        return true;
    }

    private static bool CanUseSurfaceFootprint(
        MapSurfacePathfindingSnapshot.Context surfaceContext,
        in GridConfig grid,
        int2 cell,
        int2 footprintSize,
        bool isVehicle)
    {
        if (surfaceContext.HasSurfaceData == 0)
            return true;

        MapSurfaceTraversalValidation validationSystem = new();
        return validationSystem.CanTraverseFootprint(
            surfaceContext.Surface,
            surfaceContext.HasSurfaceData,
            grid,
            cell,
            footprintSize,
            isVehicle);
    }

    public HashSet<int> BuildSelectedCurrentFootprintCells(EntityManager entityManager, in GridConfig grid, NativeArray<Entity> entities)
    {
        var cells = new HashSet<int>();
        if (entities.Length == 0)
            return cells;

        for (int i = 0; i < entities.Length; i++)
            AddSelectedCurrentFootprintCells(entityManager, grid, entities[i], cells);

        return cells;
    }

    public HashSet<int> BuildSelectedCurrentFootprintCells(EntityManager entityManager, in GridConfig grid, IReadOnlyList<Entity> entities)
    {
        var cells = new HashSet<int>();
        if (entities == null || entities.Count == 0)
            return cells;

        for (int i = 0; i < entities.Count; i++)
            AddSelectedCurrentFootprintCells(entityManager, grid, entities[i], cells);

        return cells;
    }

    private static void AddSelectedCurrentFootprintCells(
        EntityManager entityManager,
        in GridConfig grid,
        Entity entity,
        HashSet<int> cells)
    {
        if (!entityManager.HasComponent<UnitGrid>(entity))
            return;

        int2 unitCell = entityManager.GetComponentData<UnitGrid>(entity).Cell;
        int2 unitSize = entityManager.HasComponent<UnitFootprint>(entity)
            ? entityManager.GetComponentData<UnitFootprint>(entity).Size
            : new int2(1, 1);
        int2 min = UnitFootprintUtility.GetMinCell(unitCell, UnitFootprintUtility.ClampSize(unitSize));
        int2 max = min + UnitFootprintUtility.ClampSize(unitSize);

        for (int y = min.y; y < max.y; y++)
        {
            if (y < 0 || y >= grid.Height)
                continue;

            int row = y * grid.Width;
            for (int x = min.x; x < max.x; x++)
            {
                if (x < 0 || x >= grid.Width)
                    continue;

                cells.Add(row + x);
            }
        }
    }

    public void ReserveManualMoveGoalFootprint(
        in GridConfig grid,
        HashSet<int> reservedGoalCells,
        int2 cell,
        int2 footprintSize,
        int padding)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);
        for (int y = paddedMin.y; y < paddedMax.y; y++)
        {
            int row = y * grid.Width;
            for (int x = paddedMin.x; x < paddedMax.x; x++)
                reservedGoalCells.Add(row + x);
        }
    }

    public int2 SquareRingOffset(int radius, int step)
    {
        int topLen = (2 * radius) + 1;
        if (step < topLen)
            return new int2(-radius + step, radius);

        step -= topLen;
        int rightLen = 2 * radius;
        if (step < rightLen)
            return new int2(radius, (radius - 1) - step);

        step -= rightLen;
        int bottomLen = 2 * radius;
        if (step < bottomLen)
            return new int2((radius - 1) - step, -radius);

        step -= bottomLen;
        return new int2(-radius, (-radius + 1) + step);
    }

    private static void SetOrAdd<T>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
            ecb.SetComponent(entity, value);
        else
            ecb.AddComponent(entity, value);
    }

    private static void SetOrAdd<T>(EntityManager entityManager, EntityCommandBuffer ecb, Entity entity, T value, ref MoveOrderCommandResult result)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
            ecb.SetComponent(entity, value);
        else
        {
            ecb.AddComponent(entity, value);
            result.StructuralAdds++;
        }
    }

    private static string ResolveCaller()
    {
        StackTrace stack = new(2, false);
        int frameCount = math.min(6, stack.FrameCount);
        for (int i = 0; i < frameCount; i++)
        {
            System.Reflection.MethodBase method = stack.GetFrame(i)?.GetMethod();
            if (method == null)
                continue;

            string type = method.DeclaringType?.Name ?? "unknown";
            if (type == nameof(UnitMoveOrderSystem))
                continue;

            return $"{type}.{method.Name}";
        }

        return "unknown";
    }

    private static string DescribeMoveEntity(EntityManager em, Entity entity)
    {
        if (entity == Entity.Null || !em.Exists(entity))
            return "null";

        string source = em.HasComponent<UnitSourcePrefabKey>(entity)
            ? em.GetComponentData<UnitSourcePrefabKey>(entity).Value.ToString()
            : em.GetName(entity);
        byte faction = em.HasComponent<Faction>(entity) ? em.GetComponentData<Faction>(entity).Id : (byte)0;
        int2 footprintSize = em.HasComponent<UnitFootprint>(entity) ? em.GetComponentData<UnitFootprint>(entity).Size : new int2(1, 1);
        UnitMovementBehavior movementBehavior = em.HasComponent<UnitMovementBehavior>(entity)
            ? em.GetComponentData<UnitMovementBehavior>(entity)
            : default;
        bool isVehicle = UnitVehicleMovementUtility.IsVehicle(new UnitFootprint { Size = footprintSize }, movementBehavior);
        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        string grid = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell.ToString() : "none";
        string target = em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none";
        string path = em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none";
        bool follow = em.HasComponent<UnitPathFollow>(entity);
        bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
        bool longMove = em.HasComponent<UnitLongDistanceMove>(entity);
        bool disabled = em.HasComponent<Disabled>(entity);
        return $"{entity}/{source}/faction={faction}/selected={selected}/vehicle={isVehicle}/footprint={footprintSize}/grid={grid}/target={target}/pathRequest={path}/pathFollow={follow}/manual={manual}/longMove={longMove}/disabled={disabled}";
    }
}
