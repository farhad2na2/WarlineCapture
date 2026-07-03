using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed class BuildingProductionUpdateCompositionSystemHelper
    {
        private const int MaxTransportLaunchesPerTick = 1;
        private const int MaxImmediateProductionSpawnsPerTick = 2;

        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Dictionary<int, RuntimeBuildingEntity> RuntimeBuildingMap;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingProductionTransportPresentationSystemHelper TransportSystem;
            public readonly BuildingProductionTransportPresentationSystemHelper.Context TransportContext;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingProductionTransportPresentationSystemHelper transportSystem,
                BuildingProductionTransportPresentationSystemHelper.Context transportContext)
            {
                RuntimeBuildings = runtimeBuildings;
                RuntimeBuildingMap = runtimeBuildings as Dictionary<int, RuntimeBuildingEntity>;
                ProductionSystem = productionSystem;
                TransportSystem = transportSystem;
                TransportContext = transportContext;
            }
        }

        public bool UpdatePendingProductions(Context context, float now, float deltaTime, ref uint randomState)
        {
            if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0)
                return false;

            int remainingTransportLaunches = MaxTransportLaunchesPerTick;
            int remainingImmediateProductionSpawns = MaxImmediateProductionSpawnsPerTick;
            bool hasActiveTransport = false;
            if (context.RuntimeBuildingMap != null)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildingMap)
                    hasActiveTransport |= UpdatePendingProductionForBuilding(
                        context,
                        pair.Value,
                        now,
                        deltaTime,
                        ref randomState,
                        ref remainingTransportLaunches,
                        ref remainingImmediateProductionSpawns);
                return hasActiveTransport;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                hasActiveTransport |= UpdatePendingProductionForBuilding(
                    context,
                    pair.Value,
                    now,
                    deltaTime,
                    ref randomState,
                    ref remainingTransportLaunches,
                    ref remainingImmediateProductionSpawns);
            }

            return hasActiveTransport;
        }

        public bool UpdateActiveProductionTransports(Context context, float now, float deltaTime, ref uint randomState)
        {
            if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0 || context.TransportSystem == null)
                return false;

            bool hasActiveTransport = false;
            if (context.RuntimeBuildingMap != null)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildingMap)
                    hasActiveTransport |= UpdateActiveProductionTransportForBuilding(context, pair.Value, now, deltaTime, ref randomState);
                return hasActiveTransport;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                hasActiveTransport |= UpdateActiveProductionTransportForBuilding(context, pair.Value, now, deltaTime, ref randomState);

            return hasActiveTransport;
        }

        private static bool UpdateActiveProductionTransportForBuilding(
            Context context,
            RuntimeBuildingEntity building,
            float now,
            float deltaTime,
            ref uint randomState)
        {
            if (building == null || building.ActiveTransport == null)
                return false;

            context.TransportSystem.UpdateActiveProductionTransport(context.TransportContext, building, now, deltaTime, ref randomState);
            return true;
        }

        private static bool UpdatePendingProductionForBuilding(
            Context context,
            RuntimeBuildingEntity building,
            float now,
            float deltaTime,
            ref uint randomState,
            ref int remainingTransportLaunches,
            ref int remainingImmediateProductionSpawns)
        {
            if (building == null)
                return false;

            bool hasActiveTransport = building.ActiveTransport != null;
            if (building.PendingProductions == null || building.PendingProductions.Count == 0)
                return hasActiveTransport;

            for (int i = building.PendingProductions.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingEntity.PendingProduction pending = building.PendingProductions[i];
                if (pending == null)
                {
                    if (context.ProductionSystem.RemovePendingAt(building.PendingProductions, i))
                        context.ProductionSystem.RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: i > 0);
                    continue;
                }

                BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress = context.ProductionSystem.GetProgress(
                    pending,
                    now,
                    pending.TransportPrefab != null);

                if (pending.TransportPrefab != null)
                {
                    float transportLaunchWindow = Mathf.Max(0.5f, pending.TransportArrivalSeconds);
                    if (context.ProductionSystem.IsReadyWithin(pending, now, transportLaunchWindow) ||
                        context.ProductionSystem.ShouldLaunchTransport(pending, now))
                    {
                        if (remainingTransportLaunches <= 0)
                            continue;

                        bool hadActiveTransport = building.ActiveTransport != null;
                        if (!context.TransportSystem.TryEnsureActiveProductionTransport(context.TransportContext, building, pending, now, ref randomState))
                        {
                            context.ProductionSystem.DelayPendingProduction(pending, deltaTime);
                        }
                        else if (!hadActiveTransport)
                        {
                            hasActiveTransport = true;
                            remainingTransportLaunches--;
                        }
                    }
                    continue;
                }

                if (progress.RemainingSeconds > 0f || !context.ProductionSystem.IsReady(pending, now))
                    continue;

                if (remainingImmediateProductionSpawns <= 0)
                    continue;

                if (BuildingProductionTransportPresentationSystemHelper.TrySpawnPlayerUnitNearBuilding(
                        context.TransportContext,
                        building,
                        pending.ProductionIndex,
                        pending.ReservedProductionSlotIndex,
                        null,
                        null,
                        ref randomState))
                {
                    remainingImmediateProductionSpawns--;
                    if (context.ProductionSystem.RemovePendingAt(building.PendingProductions, i))
                        context.ProductionSystem.RebuildPendingProductionTimeline(building.PendingProductions, now, preserveActiveProgress: false);
                }
            }

            return hasActiveTransport || building.ActiveTransport != null;
        }
    }
}
