using System.Collections.Generic;
using UnityEngine;
using RuntimeBuildingData = BuildingPlacementSystem.RuntimeBuildingData;

internal sealed class BuildingProductionUpdateSystem
{
    public readonly struct Context
    {
        public readonly IReadOnlyDictionary<int, RuntimeBuildingData> RuntimeBuildings;
        public readonly BuildingProductionSystem ProductionSystem;
        public readonly BuildingProductionTransportSystem TransportSystem;
        public readonly BuildingProductionTransportSystem.Context TransportContext;

        public Context(
            IReadOnlyDictionary<int, RuntimeBuildingData> runtimeBuildings,
            BuildingProductionSystem productionSystem,
            BuildingProductionTransportSystem transportSystem,
            BuildingProductionTransportSystem.Context transportContext)
        {
            RuntimeBuildings = runtimeBuildings;
            ProductionSystem = productionSystem;
            TransportSystem = transportSystem;
            TransportContext = transportContext;
        }
    }

    public void UpdatePendingProductions(Context context, float now, float deltaTime)
    {
        if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0)
            return;

        foreach (KeyValuePair<int, RuntimeBuildingData> pair in context.RuntimeBuildings)
        {
            RuntimeBuildingData building = pair.Value;
            if (building == null || building.PendingProductions == null || building.PendingProductions.Count == 0)
            {
                context.TransportSystem.UpdateActiveProductionTransport(context.TransportContext, building, now, deltaTime);
                continue;
            }

            context.TransportSystem.UpdateActiveProductionTransport(context.TransportContext, building, now, deltaTime);

            for (int i = building.PendingProductions.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingData.PendingProduction pending = building.PendingProductions[i];
                if (pending == null)
                {
                    context.ProductionSystem.RemovePendingAt(building.PendingProductions, i);
                    continue;
                }

                BuildingProductionSystem.PendingProductionProgress progress = context.ProductionSystem.GetProgress(
                    pending,
                    now,
                    pending.TransportPrefab != null);

                if (pending.TransportPrefab != null)
                {
                    float transportLaunchWindow = Mathf.Max(0.5f, pending.TransportArrivalSeconds);
                    if (context.ProductionSystem.IsReadyWithin(pending, now, transportLaunchWindow) ||
                        context.ProductionSystem.ShouldLaunchTransport(pending, now))
                    {
                        if (!context.TransportSystem.TryEnsureActiveProductionTransport(context.TransportContext, building, pending, now))
                            context.ProductionSystem.DelayPendingProduction(pending, deltaTime);
                    }
                    continue;
                }

                if (progress.RemainingSeconds > 0f || !context.ProductionSystem.IsReady(pending, now))
                    continue;

                if (context.TransportContext.TrySpawnPlayerUnitNearBuilding(
                        building,
                        pending.ProductionIndex,
                        pending.ReservedProductionSlotIndex,
                        null,
                        null))
                {
                    context.ProductionSystem.RemovePendingAt(building.PendingProductions, i);
                }
            }
        }
    }
}
