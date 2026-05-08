using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[UpdateAfter(typeof(AIFactionControlSystem))]
public partial struct AIBuildPlannerSystem : ISystem
{
    private const float LogIntervalSeconds = 10f;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<AIBuildPlan>();
        state.RequireForUpdate<FactionEconomy>();
        state.RequireForUpdate<GridConfig>();
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
        GridConfig grid = SystemAPI.GetSingleton<GridConfig>();
        bool hasControls = SystemAPI.HasSingleton<FactionControlConfigTag>();
        NativeArray<FactionControlEntry> controls = hasControls
            ? SystemAPI.GetSingletonBuffer<FactionControlEntry>(true).ToNativeArray(Allocator.Temp)
            : default;

        EntityManager em = state.EntityManager;
        EntityQuery planQuery = em.CreateEntityQuery(ComponentType.ReadWrite<AIBuildPlan>(), ComponentType.ReadOnly<AIBuildPlanEntry>());
        using NativeArray<Entity> planEntities = planQuery.ToEntityArray(Allocator.Temp);
        planQuery.Dispose();

        for (int i = 0; i < planEntities.Length; i++)
        {
            Entity planEntity = planEntities[i];
            AIBuildPlan plan = em.GetComponentData<AIBuildPlan>(planEntity);
            if (plan.Enabled == 0 || !IsFactionAIControlled(plan.FactionId, hasControls, controls))
                continue;

            float interval = Mathf.Max(0.1f, plan.BuildIntervalSeconds);
            if (now - plan.LastBuildTime < interval)
                continue;

            DynamicBuffer<AIBuildPlanEntry> entries = em.GetBuffer<AIBuildPlanEntry>(planEntity);
            if (entries.Length == 0)
            {
                LogNoPlanIfNeeded(ref plan, now);
                em.SetComponentData(planEntity, plan);
                continue;
            }

            if (!TryFindEconomyEntity(em, plan.FactionId, out Entity economyEntity, out FactionEconomy economy))
                continue;

            if (plan.BaseCenterCell.x <= 0 && plan.BaseCenterCell.y <= 0)
                plan.BaseCenterCell = ResolveDefaultBaseCenter(plan.FactionId, grid);

            bool handledDecision = false;
            int attempts = math.max(1, entries.Length);
            for (int attempt = 0; attempt < attempts; attempt++)
            {
                int entryIndex = PositiveModulo(plan.NextBuildIndex + attempt, entries.Length);
                string buildingId = entries[entryIndex].BuildingId.ToString();
                if (string.IsNullOrWhiteSpace(buildingId))
                    continue;

                if (buildingPlacement.CountRuntimeBuildingsForFaction(plan.FactionId, buildingId) > 0)
                    continue;

                handledDecision = true;
                if (!buildingPlacement.TryGetConfiguredSpawnable(buildingId, out BuildingPlacementSystem.ConfiguredSpawnableEntry spawnable) ||
                    spawnable.Prefab == null ||
                    !spawnable.CanRequest)
                {
                    plan.NextBuildIndex = entryIndex + 1;
                    plan.LastBuildTime = now;
                    AILog.Log($"[AIBuild] faction={plan.FactionId} building={buildingId} result=MissingConfig");
                    break;
                }

                int cost = Mathf.Max(0, spawnable.Price);
                if (economy.Money < cost)
                {
                    plan.LastBuildTime = now;
                    AILog.Log($"[AIBuild] faction={plan.FactionId} building={spawnable.DisplayName} cost={cost} result=InsufficientFunds money={economy.Money}");
                    break;
                }

                Vector2Int preferredOrigin = ResolvePreferredOrigin(plan.BaseCenterCell, entryIndex);
                bool placed = buildingPlacement.TrySpawnRuntimeBuilding(
                    spawnable.Prefab,
                    preferredOrigin,
                    out _,
                    out Vector2Int actualOrigin,
                    out _,
                    spawnable.DisplayName,
                    spawnable.Description,
                    null,
                    500,
                    false,
                    plan.FactionId);

                plan.LastBuildTime = now;
                if (!placed)
                {
                    AILog.Log($"[AIBuild] faction={plan.FactionId} building={spawnable.DisplayName} cell={new int2(preferredOrigin.x, preferredOrigin.y)} cost={cost} result=Blocked");
                    break;
                }

                economy.Money = Mathf.Max(0, economy.Money - cost);
                em.SetComponentData(economyEntity, economy);
                plan.NextBuildIndex = entryIndex + 1;
                AILog.Log($"[AIBuild] faction={plan.FactionId} building={spawnable.DisplayName} cell={new int2(actualOrigin.x, actualOrigin.y)} cost={cost} result=Placed");
                break;
            }

            if (!handledDecision && now - plan.LastLogTime >= LogIntervalSeconds)
            {
                plan.LastLogTime = now;
                AILog.Log($"[AIBuild] faction={plan.FactionId} result=Complete ownedBuildings={buildingPlacement.CountRuntimeBuildingsForFaction(plan.FactionId)}");
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

    private static int2 ResolveDefaultBaseCenter(byte factionId, GridConfig grid)
    {
        int x = factionId == 0 ? grid.Width / 4 : (grid.Width * 3) / 4;
        int y = factionId == 0 ? grid.Height / 2 : grid.Height / 2;
        return new int2(math.max(0, x), math.max(0, y));
    }

    private static Vector2Int ResolvePreferredOrigin(int2 baseCenterCell, int entryIndex)
    {
        int ring = entryIndex / 5;
        int spacing = 14 + ring * 8;
        int2 offset = PositiveModulo(entryIndex, 5) switch
        {
            0 => new int2(0, 0),
            1 => new int2(spacing, 0),
            2 => new int2(-spacing, 0),
            3 => new int2(0, spacing),
            _ => new int2(0, -spacing)
        };

        int2 origin = baseCenterCell + offset;
        return new Vector2Int(origin.x, origin.y);
    }

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
            return 0;

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static void LogNoPlanIfNeeded(ref AIBuildPlan plan, float now)
    {
        if (now - plan.LastLogTime < LogIntervalSeconds)
            return;

        plan.LastLogTime = now;
        AILog.Log($"[AIBuild] faction={plan.FactionId} result=NoPlan");
    }
}
