using Game.Components;
using Game.Runtime;
#if UNITY_INCLUDE_TESTS && UNITY_EDITOR
using System;
using System.Text;
using Unity.Entities;

internal static class RuntimeGameplayStateTestHelper
{
    public static void SetPlayRequested(EntityManager entityManager, bool playRequested)
    {
        InitialUnitsRuntimeState.PlayRequested = playRequested;
        InitialUnitsRuntimeState.SimulationActive = playRequested;
        Entity entity = GetOrCreateRuntimeStateEntity(entityManager);
        RuntimeGameplayStateComponent state = entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity);
        state.PlayRequested = playRequested ? (byte)1 : (byte)0;
        state.SimulationActive = playRequested ? (byte)1 : (byte)0;
        entityManager.SetComponentData(entity, state);
    }

    public static void SetSimulationActive(EntityManager entityManager, bool simulationActive)
    {
        InitialUnitsRuntimeState.SimulationActive = simulationActive;
        Entity entity = GetOrCreateRuntimeStateEntity(entityManager);
        RuntimeGameplayStateComponent state = entityManager.GetComponentData<RuntimeGameplayStateComponent>(entity);
        state.SimulationActive = simulationActive ? (byte)1 : (byte)0;
        entityManager.SetComponentData(entity, state);
    }

    public static void SetBuildingPlacement(EntityManager entityManager, Action tickBuildingRuntime)
    {
        GetOrCreateBuildingRuntimeStateEntity(entityManager);
        TickBuildingRuntime(tickBuildingRuntime);
    }

    public static void PublishBuildingRuntimeState(EntityManager entityManager, Action tickBuildingRuntime)
    {
        GetOrCreateBuildingRuntimeStateEntity(entityManager);
        TickBuildingRuntime(tickBuildingRuntime);
    }

    private static void TickBuildingRuntime(Action tickBuildingRuntime)
    {
        tickBuildingRuntime?.Invoke();
    }

    public static int CountRuntimeBuildingsForFaction(EntityManager entityManager, byte factionId, string buildingId)
    {
        Entity entity = GetOrCreateBuildingRuntimeStateEntity(entityManager);
        if (!entityManager.HasBuffer<BuildingRuntimeOwnedBuildingSummary>(entity))
            return 0;

        string normalized = NormalizeKey(buildingId);
        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> summaries =
            entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(entity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeOwnedBuildingSummary summary = summaries[i];
            if (summary.FactionId != factionId)
                continue;
            if (NormalizeKey(summary.BuildingId.ToString()) != normalized)
                continue;
            return summary.Count;
        }

        return 0;
    }

    public static string DescribeOwnedBuildingSummaries(EntityManager entityManager)
    {
        Entity entity = GetOrCreateBuildingRuntimeStateEntity(entityManager);
        if (!entityManager.HasBuffer<BuildingRuntimeOwnedBuildingSummary>(entity))
            return "<no BuildingRuntimeOwnedBuildingSummary buffer>";

        DynamicBuffer<BuildingRuntimeOwnedBuildingSummary> summaries =
            entityManager.GetBuffer<BuildingRuntimeOwnedBuildingSummary>(entity, true);
        if (summaries.Length == 0)
            return "<empty BuildingRuntimeOwnedBuildingSummary buffer>";

        StringBuilder builder = new();
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeOwnedBuildingSummary summary = summaries[i];
            if (builder.Length > 0)
                builder.Append(", ");
            builder.Append("faction=");
            builder.Append(summary.FactionId);
            builder.Append(" id=");
            builder.Append(summary.BuildingId.ToString());
            builder.Append(" count=");
            builder.Append(summary.Count);
        }

        return builder.ToString();
    }

    public static int CountPendingProductionsForFaction(EntityManager entityManager, byte factionId, string unitId)
    {
        Entity entity = GetOrCreateBuildingRuntimeStateEntity(entityManager);
        if (!entityManager.HasBuffer<BuildingRuntimeUnitProductionSummary>(entity))
            return 0;

        string normalized = NormalizeKey(unitId);
        DynamicBuffer<BuildingRuntimeUnitProductionSummary> summaries =
            entityManager.GetBuffer<BuildingRuntimeUnitProductionSummary>(entity, true);
        for (int i = 0; i < summaries.Length; i++)
        {
            BuildingRuntimeUnitProductionSummary summary = summaries[i];
            if (summary.FactionId != factionId)
                continue;
            if (NormalizeKey(summary.UnitId.ToString()) != normalized)
                continue;
            return summary.QueuedCount;
        }

        return 0;
    }

    public static string DescribeUnitProductionBoundary(EntityManager entityManager)
    {
        Entity entity = GetOrCreateBuildingRuntimeStateEntity(entityManager);
        StringBuilder builder = new();
        AppendBuffer(builder, "configuredUnits", entityManager.GetBuffer<BuildingConfiguredUnitReadModel>(entity, true), item =>
            $"{item.UnitId.ToString()}:{item.DisplayName.ToString()}:can={item.CanRequest}:price={item.Price}");
        AppendBuffer(builder, "summaries", entityManager.GetBuffer<BuildingRuntimeUnitProductionSummary>(entity, true), item =>
            $"faction={item.FactionId}:unit={item.UnitId.ToString()}:produced={item.ProducedCount}:queued={item.QueuedCount}");
        AppendBuffer(builder, "requests", entityManager.GetBuffer<BuildingFactionUnitProductionRequest>(entity, true), item =>
            $"faction={item.FactionId}:unit={item.UnitId.ToString()}:status={item.Status}:result={item.ResultCode}:queue={item.QueueCount}");
        return builder.Length == 0 ? "<empty production boundary>" : builder.ToString();
    }

    private static Entity GetOrCreateBuildingRuntimeStateEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
        Entity entity = query.CalculateEntityCount() > 0
            ? query.GetSingletonEntity()
            : entityManager.CreateEntity();

        EnsureBuildingRuntimeStateBuffers(entityManager, entity);
        return entity;
    }

    private static void EnsureBuildingRuntimeStateBuffers(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<BuildingRuntimeStateTag>(entity))
            entityManager.AddComponent<BuildingRuntimeStateTag>(entity);
        EnsureBuffer<BuildingConfiguredSpawnableReadModel>(entityManager, entity);
        EnsureBuffer<BuildingConfiguredUnitReadModel>(entityManager, entity);
        EnsureBuffer<BuildingProductionSlotReadModel>(entityManager, entity);
        EnsureBuffer<BuildingProductionSpawnRequest>(entityManager, entity);
        EnsureBuffer<BuildingRecentSpawnReservation>(entityManager, entity);
        EnsureBuffer<BuildingProducedUnitReadModel>(entityManager, entity);
        EnsureBuffer<MapVehiclePlacementReadModel>(entityManager, entity);
        EnsureBuffer<BuildingRuntimeFactionSummary>(entityManager, entity);
        EnsureBuffer<BuildingRuntimeFactionUsableFuelSummary>(entityManager, entity);
        EnsureBuffer<BuildingRuntimeOwnedBuildingSummary>(entityManager, entity);
        EnsureBuffer<BuildingRuntimeUnitProductionSummary>(entityManager, entity);
        EnsureBuffer<BuildingFactionProductionSpawnPointReadModel>(entityManager, entity);
        EnsureBuffer<BuildingFactionUnitProductionRequest>(entityManager, entity);
        EnsureBuffer<BuildingFactionResourceSellRequest>(entityManager, entity);
        EnsureBuffer<BuildingRuntimeSpawnRequest>(entityManager, entity);
    }

    private static void EnsureBuffer<T>(EntityManager entityManager, Entity entity)
        where T : unmanaged, IBufferElementData
    {
        if (!entityManager.HasBuffer<T>(entity))
            entityManager.AddBuffer<T>(entity);
    }

    private static void AppendBuffer<T>(StringBuilder builder, string label, DynamicBuffer<T> buffer, Func<T, string> describe)
        where T : unmanaged, IBufferElementData
    {
        if (builder.Length > 0)
            builder.Append(" | ");
        builder.Append(label);
        builder.Append('=');
        if (buffer.Length == 0)
        {
            builder.Append("<empty>");
            return;
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            if (i > 0)
                builder.Append(',');
            builder.Append(describe(buffer[i]));
        }
    }

    private static string NormalizeKey(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\0", string.Empty).Trim().ToLowerInvariant();
    }

    private static Entity GetOrCreateRuntimeStateEntity(EntityManager entityManager)
    {
        using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<RuntimeGameplayStateComponent>());
        if (query.CalculateEntityCount() > 0)
        {
            Entity entity = query.GetSingletonEntity();
            EnsureCameraComponents(entityManager, entity);
            return entity;
        }

        return entityManager.CreateEntity(
            typeof(RuntimeGameplayStateComponent),
            typeof(RuntimeCameraInputComponent),
            typeof(RuntimeCameraFocusRequestComponent));
    }

    private static void EnsureCameraComponents(EntityManager entityManager, Entity entity)
    {
        if (!entityManager.HasComponent<RuntimeCameraInputComponent>(entity))
            entityManager.AddComponent<RuntimeCameraInputComponent>(entity);
        if (!entityManager.HasComponent<RuntimeCameraFocusRequestComponent>(entity))
            entityManager.AddComponent<RuntimeCameraFocusRequestComponent>(entity);
    }
}
#endif
