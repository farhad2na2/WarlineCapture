using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AIProductionSystem))]
public partial struct AISquadSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AISquadPlan>();
        state.RequireForUpdate<Faction>();
        state.RequireForUpdate<UnitGrid>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!InitialUnitsRuntimeState.PlayRequested)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        NativeArray<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true).ToNativeArray(Allocator.Temp)
            : default;

        EntityManager em = state.EntityManager;
        EntityQuery planQuery = em.CreateEntityQuery(ComponentType.ReadWrite<AISquadPlan>());
        using NativeArray<Entity> planEntities = planQuery.ToEntityArray(Allocator.Temp);
        planQuery.Dispose();

        EntityQuery unitQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<Faction>(),
            ComponentType.ReadOnly<UnitGrid>(),
            ComponentType.ReadOnly<UnitHealth>(),
            ComponentType.ReadOnly<AIControlledTag>());
        using NativeArray<Entity> unitEntities = unitQuery.ToEntityArray(Allocator.Temp);
        unitQuery.Dispose();

        for (int i = 0; i < planEntities.Length; i++)
        {
            Entity planEntity = planEntities[i];
            AISquadPlan plan = em.GetComponentData<AISquadPlan>(planEntity);
            if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                continue;

            int activeSquads = CountActiveSquads(em, plan.FactionId);
            int maxActiveSquads = math.max(1, plan.MaxActiveSquads);
            if (activeSquads >= maxActiveSquads)
            {
                LogCompleteIfNeeded(ref plan, now, activeSquads);
                em.SetComponentData(planEntity, plan);
                continue;
            }

            int maxUnits = math.max(1, plan.MaxUnits);
            int minUnits = math.clamp(math.max(1, plan.MinUnits), 1, maxUnits);
            using NativeList<Entity> members = new(maxUnits, Allocator.Temp);
            int2 cellSum = int2.zero;

            for (int unitIndex = 0; unitIndex < unitEntities.Length && members.Length < maxUnits; unitIndex++)
            {
                Entity unit = unitEntities[unitIndex];
                if (!em.Exists(unit) ||
                    em.HasComponent<AISquadMember>(unit) ||
                    em.HasComponent<StaticGridBlocker>(unit) ||
                    em.HasComponent<EngageTarget>(unit) ||
                    em.HasComponent<UnitPathRequest>(unit))
                {
                    continue;
                }

                if (em.GetComponentData<Faction>(unit).Id != plan.FactionId)
                    continue;

                UnitHealth health = em.GetComponentData<UnitHealth>(unit);
                if (health.Current <= 0)
                    continue;

                UnitGrid grid = em.GetComponentData<UnitGrid>(unit);
                members.Add(unit);
                cellSum += grid.Cell;
            }

            if (members.Length < minUnits)
            {
                if (now - plan.LastLogTime >= LogIntervalSeconds)
                {
                    plan.LastLogTime = now;
                    AILog.Log($"[AISquad] faction={plan.FactionId} result=Waiting units={members.Length} minUnits={minUnits}");
                }
                em.SetComponentData(planEntity, plan);
                continue;
            }

            Entity squadEntity = em.CreateEntity(typeof(AISquad));
            DynamicBuffer<AISquadUnit> squadUnits = em.AddBuffer<AISquadUnit>(squadEntity);
            int squadId = plan.NextSquadId <= 0 ? 1 : plan.NextSquadId;
            byte targetFactionId = plan.FactionId == 0 ? (byte)1 : (byte)0;
            int2 rallyCell = cellSum / members.Length;
            int2 targetCell = ResolveInitialTargetCell(em, targetFactionId, rallyCell);
            em.SetComponentData(squadEntity, new AISquad
            {
                SquadId = squadId,
                FactionId = plan.FactionId,
                Purpose = (byte)AISquadPurpose.Attack,
                TargetFactionId = targetFactionId,
                TargetKind = (byte)AITargetKind.None,
                TargetEntity = Entity.Null,
                RallyCell = rallyCell,
                TargetCell = targetCell,
                TargetScore = 0,
                MinUnits = minUnits,
                MaxUnits = maxUnits,
                LastOrderTime = -999f,
                LastLogTime = now
            });

            for (int memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                Entity unit = members[memberIndex];
                squadUnits.Add(new AISquadUnit { Unit = unit });
            }

            for (int memberIndex = 0; memberIndex < members.Length; memberIndex++)
            {
                Entity unit = members[memberIndex];
                em.AddComponentData(unit, new AISquadMember
                {
                    Squad = squadEntity,
                    SquadId = squadId
                });
            }

            plan.NextSquadId = squadId + 1;
            plan.LastLogTime = now;
            em.SetComponentData(planEntity, plan);
            AILog.Log($"[AISquad] faction={plan.FactionId} squad={squadId} purpose=Attack units={members.Length} targetFaction={targetFactionId} targetCell={targetCell}");
        }

        if (controls.IsCreated)
            controls.Dispose();
    }

    private static int CountActiveSquads(EntityManager em, byte factionId)
    {
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<AISquad>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        int count = 0;
        for (int i = 0; i < entities.Length; i++)
        {
            AISquad squad = em.GetComponentData<AISquad>(entities[i]);
            if (squad.FactionId == factionId)
                count++;
        }

        return count;
    }

    private static int2 ResolveInitialTargetCell(EntityManager em, byte targetFactionId, int2 fallbackCell)
    {
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<Faction>(), ComponentType.ReadOnly<UnitGrid>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        int bestDistance = int.MaxValue;
        int2 bestCell = fallbackCell;
        bool found = false;
        for (int i = 0; i < entities.Length; i++)
        {
            Entity entity = entities[i];
            if (em.GetComponentData<Faction>(entity).Id != targetFactionId)
                continue;

            int2 cell = em.GetComponentData<UnitGrid>(entity).Cell;
            int distance = math.abs(cell.x - fallbackCell.x) + math.abs(cell.y - fallbackCell.y);
            if (found && distance >= bestDistance)
                continue;

            found = true;
            bestDistance = distance;
            bestCell = cell;
        }

        return bestCell;
    }

    private static bool IsFactionAIControlled(byte factionId, bool hasControls, NativeArray<FactionControlEntry> controls)
    {
        if (!hasControls)
            return factionId != 0;

        for (int i = 0; i < controls.Length; i++)
        {
            FactionControlEntry control = controls[i];
            if (control.FactionId == factionId)
                return control.AIControlled != 0;
        }

        return factionId != 0;
    }

    private static void LogCompleteIfNeeded(ref AISquadPlan plan, float now, int activeSquads)
    {
        if (now - plan.LastLogTime < LogIntervalSeconds)
            return;

        plan.LastLogTime = now;
        AILog.Log($"[AISquad] faction={plan.FactionId} result=Complete activeSquads={activeSquads}");
    }
}
