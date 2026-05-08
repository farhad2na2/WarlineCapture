using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AIBuildPlannerSystem))]
public partial struct AIProductionSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AIProductionPlan>();
        state.RequireForUpdate<FactionEconomy>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!InitialUnitsRuntimeState.PlayRequested)
            return;

        BuildingPlacementSystem buildingPlacement = BuildingPlacementSystem.Instance;
        if (buildingPlacement == null)
            return;

        double elapsedTime = SystemAPI.Time.ElapsedTime;
        float now = elapsedTime > float.MaxValue ? float.MaxValue : (float)elapsedTime;
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        NativeArray<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true).ToNativeArray(Allocator.Temp)
            : default;

        EntityManager em = state.EntityManager;
        EntityQuery planQuery = em.CreateEntityQuery(ComponentType.ReadWrite<AIProductionPlan>(), ComponentType.ReadOnly<AIProductionPlanEntry>());
        using NativeArray<Entity> planEntities = planQuery.ToEntityArray(Allocator.Temp);
        planQuery.Dispose();

        for (int i = 0; i < planEntities.Length; i++)
        {
            Entity planEntity = planEntities[i];
            AIProductionPlan plan = em.GetComponentData<AIProductionPlan>(planEntity);
            if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                continue;

            float interval = Mathf.Max(0.1f, plan.UnitProductionIntervalSeconds);
            if (now - plan.LastProductionTime < interval)
                continue;

            DynamicBuffer<AIProductionPlanEntry> entries = em.GetBuffer<AIProductionPlanEntry>(planEntity);
            if (entries.Length == 0)
            {
                LogNoPlanIfNeeded(ref plan, now);
                em.SetComponentData(planEntity, plan);
                continue;
            }

            if (!TryFindEconomyEntity(em, plan.FactionId, out Entity economyEntity, out FactionEconomy economy))
                continue;

            bool handledDecision = false;
            int attempts = math.max(1, entries.Length);
            int maxQueuedUnits = math.max(1, plan.MaxQueuedUnits);
            int targetProducedUnits = math.max(1, plan.TargetProducedUnits);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int entryIndex = PositiveModulo(plan.NextUnitIndex + attempt, entries.Length);
                string unitId = entries[entryIndex].UnitId.ToString();
                if (string.IsNullOrWhiteSpace(unitId))
                    continue;

                int producedCount = buildingPlacement.CountRuntimeProducedUnitsForFaction(plan.FactionId, unitId);
                int queuedCount = buildingPlacement.CountPendingProductionsForFaction(plan.FactionId, unitId);
                if (producedCount + queuedCount >= targetProducedUnits || queuedCount >= maxQueuedUnits)
                    continue;

                handledDecision = true;
                if (!buildingPlacement.TryGetConfiguredUnit(unitId, out BuildingPlacementSystem.ConfiguredUnitEntry unit) ||
                    unit.Prefab == null ||
                    !unit.CanRequest)
                {
                    plan.NextUnitIndex = entryIndex + 1;
                    plan.LastProductionTime = now;
                    AILog.Log($"[AIProduction] faction={plan.FactionId} unit={unitId} result=MissingConfig");
                    break;
                }

                int cost = Mathf.Max(0, unit.Price);
                if (economy.Money < cost)
                {
                    plan.LastProductionTime = now;
                    AILog.Log($"[AIProduction] faction={plan.FactionId} unit={unit.DisplayName} cost={cost} result=InsufficientFunds money={economy.Money}");
                    break;
                }

                bool queued = buildingPlacement.TryQueueFactionUnitProduction(plan.FactionId, unitId, out BuildingPlacementSystem.FactionUnitProductionResult result);
                plan.LastProductionTime = now;
                if (!queued)
                {
                    AILog.Log($"[AIProduction] faction={plan.FactionId} producer={result.ProducerDisplayName} unit={result.UnitDisplayName} cost={result.Cost} queue={result.QueueCount} result={result.Code}");
                    break;
                }

                economy.Money = Mathf.Max(0, economy.Money - cost);
                em.SetComponentData(economyEntity, economy);
                plan.NextUnitIndex = entryIndex + 1;
                AILog.Log($"[AIProduction] faction={plan.FactionId} producer={result.ProducerDisplayName} unit={result.UnitDisplayName} cost={cost} queue={result.QueueCount} result=Queued");
                break;
            }

            if (!handledDecision && now - plan.LastLogTime >= LogIntervalSeconds)
            {
                plan.LastLogTime = now;
                AILog.Log($"[AIProduction] faction={plan.FactionId} result=Complete");
            }

            em.SetComponentData(planEntity, plan);
        }

        if (controls.IsCreated)
            controls.Dispose();
    }

    private static bool TryFindEconomyEntity(EntityManager em, byte factionId, out Entity entity, out FactionEconomy economy)
    {
        EntityQuery query = em.CreateEntityQuery(ComponentType.ReadOnly<FactionEconomy>());
        using NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
        query.Dispose();

        for (int i = 0; i < entities.Length; i++)
        {
            FactionEconomy candidate = em.GetComponentData<FactionEconomy>(entities[i]);
            if (candidate.FactionId != factionId)
                continue;

            entity = entities[i];
            economy = candidate;
            return true;
        }

        entity = Entity.Null;
        economy = default;
        return false;
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

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
            return 0;

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static void LogNoPlanIfNeeded(ref AIProductionPlan plan, float now)
    {
        if (now - plan.LastLogTime < LogIntervalSeconds)
            return;

        plan.LastLogTime = now;
        AILog.Log($"[AIProduction] faction={plan.FactionId} result=NoPlan");
    }
}
