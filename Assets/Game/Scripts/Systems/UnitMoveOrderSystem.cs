using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Debug = UnityEngine.Debug;

public sealed class UnitMoveOrderSystem
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
        Debug.Log(
            $"[SelectionClick] unitMoveOrderGrouped caller={ResolveCaller()} entity={DescribeMoveEntity(entityManager, entity)} " +
            $"goal={goal} issuePathNow={issueGroundPathNow} retry={useGroundPathRetryCooldown} resumeFrame={resumeFrame} frame={currentFrame}");

        result.StructuralRemoves += RemoveComponentIfPresent<EngageTarget>(entityManager, entity) ? 1 : 0;
        result.StructuralRemoves += RemoveComponentIfPresent<UnitPathFollow>(entityManager, entity) ? 1 : 0;
        result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRange>(entityManager, entity) ? 1 : 0;
        result.StructuralRemoves += RemoveComponentIfPresent<UnitLongDistanceMove>(entityManager, entity) ? 1 : 0;
        result.StructuralRemoves += RemoveComponentIfPresent<AutoWanderMoveTag>(entityManager, entity) ? 1 : 0;

        if (!entityManager.HasComponent<ManualMoveGroupMemberTag>(entity))
        {
            entityManager.AddComponent<ManualMoveGroupMemberTag>(entity);
            result.StructuralAdds++;
        }

        SetOrAdd(entityManager, entity, new UnitTarget { Cell = goal }, ref result);

        if (!entityManager.HasComponent<UnitAirMovement>(entity))
        {
            if (issueGroundPathNow)
            {
                result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRetryCooldown>(entityManager, entity) ? 1 : 0;
                SetOrAdd(entityManager, entity, new UnitPathRequest { Goal = goal }, ref result);
                result.PathRequests++;
            }
            else if (useGroundPathRetryCooldown)
            {
                result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRequest>(entityManager, entity) ? 1 : 0;
                SetOrAdd(entityManager, entity, new UnitPathRetryCooldown { ResumeFrame = resumeFrame }, ref result);
                result.StaggeredPathRequests++;
                result.MaxStaggerDelayFrames = math.max(0, resumeFrame - currentFrame);
            }
        }
        else
        {
            result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRequest>(entityManager, entity) ? 1 : 0;
            result.StructuralRemoves += RemoveComponentIfPresent<UnitPathRetryCooldown>(entityManager, entity) ? 1 : 0;
            result.AirUnits++;
        }

        if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
        {
            entityManager.AddComponent<ManualMoveOrderTag>(entity);
            result.StructuralAdds++;
        }

        return result;
    }

    public void IssueImmediateMoveCommand(EntityManager entityManager, Entity entity, int2 goal)
    {
        Debug.Log($"[SelectionClick] unitMoveOrderImmediate caller={ResolveCaller()} entity={DescribeMoveEntity(entityManager, entity)} goal={goal}");
        RemoveComponentIfPresent<EngageTarget>(entityManager, entity);
        RemoveComponentIfPresent<UnitPathFollow>(entityManager, entity);
        RemoveComponentIfPresent<UnitPathRange>(entityManager, entity);
        RemoveComponentIfPresent<AutoWanderMoveTag>(entityManager, entity);

        SetOrAdd(entityManager, entity, new UnitTarget { Cell = goal });

        if (!entityManager.HasComponent<UnitAirMovement>(entity))
            SetOrAdd(entityManager, entity, new UnitPathRequest { Goal = goal });
        else
            RemoveComponentIfPresent<UnitPathRequest>(entityManager, entity);

        if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
            entityManager.AddComponent<ManualMoveOrderTag>(entity);
    }

    public void IssueTargetOnlyMoveCommand(EntityManager entityManager, Entity entity, int2 goal)
    {
        Debug.Log($"[SelectionClick] unitMoveOrderTargetOnly caller={ResolveCaller()} entity={DescribeMoveEntity(entityManager, entity)} goal={goal}");
        SetOrAdd(entityManager, entity, new UnitTarget { Cell = goal });
        if (!entityManager.HasComponent<ManualMoveOrderTag>(entity))
            entityManager.AddComponent<ManualMoveOrderTag>(entity);
    }

    public void ClearMovementOrderComponents(EntityManager entityManager, Entity entity)
    {
        RemoveComponentIfPresent<UnitTarget>(entityManager, entity);
        RemoveComponentIfPresent<UnitPathRequest>(entityManager, entity);
        RemoveComponentIfPresent<UnitPathFollow>(entityManager, entity);
        RemoveComponentIfPresent<UnitPathRange>(entityManager, entity);
        RemoveComponentIfPresent<ManualMoveOrderTag>(entityManager, entity);
        RemoveComponentIfPresent<AutoWanderMoveTag>(entityManager, entity);
        RemoveComponentIfPresent<EngageTarget>(entityManager, entity);
    }

    public bool RemoveComponentIfPresent<T>(EntityManager entityManager, Entity entity)
        where T : unmanaged, IComponentData
    {
        if (!entityManager.Exists(entity) || !entityManager.HasComponent<T>(entity))
            return false;

        entityManager.RemoveComponent<T>(entity);
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
        int slotIndex)
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
                factionId))
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
                        factionId))
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
        byte factionId)
    {
        int2 size = UnitFootprintUtility.ClampSize(footprintSize);
        int2 min = UnitFootprintUtility.GetMinCell(cell, size);
        int2 max = min + size;
        int2 paddedMin = min - new int2(padding, padding);
        int2 paddedMax = max + new int2(padding, padding);

        if (paddedMin.x < 0 || paddedMin.y < 0 || paddedMax.x > grid.Width || paddedMax.y > grid.Height)
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

    public HashSet<int> BuildSelectedCurrentFootprintCells(EntityManager entityManager, in GridConfig grid, NativeArray<Entity> entities)
    {
        var cells = new HashSet<int>();
        if (entities.Length == 0)
            return cells;

        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (!entityManager.HasComponent<UnitGrid>(entity))
                continue;

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

        return cells;
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

    private static void SetOrAdd<T>(EntityManager entityManager, Entity entity, T value)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
            entityManager.SetComponentData(entity, value);
        else
            entityManager.AddComponentData(entity, value);
    }

    private static void SetOrAdd<T>(EntityManager entityManager, Entity entity, T value, ref MoveOrderCommandResult result)
        where T : unmanaged, IComponentData
    {
        if (entityManager.HasComponent<T>(entity))
            entityManager.SetComponentData(entity, value);
        else
        {
            entityManager.AddComponentData(entity, value);
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
        bool selected = em.HasComponent<SelectedUnitTag>(entity);
        string grid = em.HasComponent<UnitGrid>(entity) ? em.GetComponentData<UnitGrid>(entity).Cell.ToString() : "none";
        string target = em.HasComponent<UnitTarget>(entity) ? em.GetComponentData<UnitTarget>(entity).Cell.ToString() : "none";
        string path = em.HasComponent<UnitPathRequest>(entity) ? em.GetComponentData<UnitPathRequest>(entity).Goal.ToString() : "none";
        bool follow = em.HasComponent<UnitPathFollow>(entity);
        bool manual = em.HasComponent<ManualMoveOrderTag>(entity);
        bool disabled = em.HasComponent<Disabled>(entity);
        return $"{entity}/{source}/faction={faction}/selected={selected}/grid={grid}/target={target}/pathRequest={path}/pathFollow={follow}/manual={manual}/disabled={disabled}";
    }
}
