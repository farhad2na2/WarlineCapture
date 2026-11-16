using Game.Simulation;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    internal sealed partial class BuildingProductionUpdateCompositionSystemHelper
    {
        private const int MaxTransportLaunchesPerTick = SharedProductionQueueKernel.MaxTransportLaunchesPerTick;
        private const int MaxImmediateProductionSpawnsPerTick = SharedProductionQueueKernel.MaxImmediateProductionSpawnsPerTick;
        private readonly List<RuntimeBuildingEntity> _activeTransportBuildings = new();
        private bool _activeTransportCachePrimed;

        public readonly struct Context
        {
            public readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            public readonly Dictionary<int, RuntimeBuildingEntity> RuntimeBuildingMap;
            public readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            public readonly BuildingProductionTransportPresentationSystemHelper TransportSystem;
            public readonly BuildingProductionTransportPresentationSystemHelper.Context TransportContext;
            internal readonly SkirmishNativeCheckpointPrefabRegistry CheckpointPrefabs;

            public Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                BuildingProductionTransportPresentationSystemHelper transportSystem,
                BuildingProductionTransportPresentationSystemHelper.Context transportContext,
                SkirmishNativeCheckpointPrefabRegistry checkpointPrefabs = null)
            {
                RuntimeBuildings = runtimeBuildings;
                RuntimeBuildingMap = runtimeBuildings as Dictionary<int, RuntimeBuildingEntity>;
                ProductionSystem = productionSystem;
                TransportSystem = transportSystem;
                TransportContext = transportContext;
                CheckpointPrefabs = checkpointPrefabs;
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
                {
                    RuntimeBuildingEntity building = pair.Value;
                    bool buildingHasActiveTransport = UpdatePendingProductionForBuilding(
                        context,
                        building,
                        now,
                        deltaTime,
                        ref randomState,
                        ref remainingTransportLaunches,
                        ref remainingImmediateProductionSpawns);
                    if (buildingHasActiveTransport)
                        TrackActiveTransportBuilding(building);
                    hasActiveTransport |= buildingHasActiveTransport;
                }

                return hasActiveTransport;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                bool buildingHasActiveTransport = UpdatePendingProductionForBuilding(
                    context,
                    building,
                    now,
                    deltaTime,
                    ref randomState,
                    ref remainingTransportLaunches,
                    ref remainingImmediateProductionSpawns);
                if (buildingHasActiveTransport)
                    TrackActiveTransportBuilding(building);
                hasActiveTransport |= buildingHasActiveTransport;
            }

            return hasActiveTransport;
        }

        public bool UpdateActiveProductionTransports(Context context, float now, float deltaTime, ref uint randomState)
        {
            if (context.RuntimeBuildings == null || context.RuntimeBuildings.Count == 0 || context.TransportSystem == null)
                return false;

            if (!_activeTransportCachePrimed)
                PrimeActiveTransportCache(context);
            if (_activeTransportBuildings.Count == 0)
                return false;

            bool hasActiveTransport = false;
            for (int i = _activeTransportBuildings.Count - 1; i >= 0; i--)
            {
                RuntimeBuildingEntity building = _activeTransportBuildings[i];
                if (building == null || building.ActiveTransport == null)
                {
                    _activeTransportBuildings.RemoveAt(i);
                    continue;
                }

                context.TransportSystem.UpdateActiveProductionTransport(context.TransportContext, building, now, deltaTime, ref randomState);
                if (building.ActiveTransport == null)
                {
                    _activeTransportBuildings.RemoveAt(i);
                    continue;
                }

                hasActiveTransport = true;
            }

            return hasActiveTransport;
        }

        private void PrimeActiveTransportCache(Context context)
        {
            _activeTransportBuildings.Clear();
            _activeTransportCachePrimed = true;
            if (context.RuntimeBuildingMap != null)
            {
                foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildingMap)
                    TrackActiveTransportBuilding(pair.Value);
                return;
            }

            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
                TrackActiveTransportBuilding(pair.Value);
        }

        private void TrackActiveTransportBuilding(RuntimeBuildingEntity building)
        {
            if (building == null || building.ActiveTransport == null || _activeTransportBuildings.Contains(building))
                return;

            _activeTransportBuildings.Add(building);
        }

        private static bool UpdatePendingProductionForBuilding(
            Context context,
            RuntimeBuildingEntity building,
            float now,
            float deltaTime,
            ref uint randomState,
            ref int remainingTransportLaunches,
            ref int remainingSpawns)
        {
            var view = new NativeProductionQueueView(context);
            return SharedProductionQueueKernel.UpdateBuilding<RuntimeBuildingEntity, RuntimeBuildingEntity.PendingProduction, NativeProductionQueueView>(ref view, building, now, deltaTime, ref randomState, ref remainingTransportLaunches, ref remainingSpawns);
        }
    }
}
