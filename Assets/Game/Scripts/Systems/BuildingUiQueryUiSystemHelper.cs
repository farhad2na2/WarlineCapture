using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    public readonly struct UiMaterialFabricationReadModel
    {
        public readonly int RuntimeBuildingId;
        public readonly byte OwnerFactionId;
        public readonly int OilInputCurrentBarrels;
        public readonly int OilInputCapacityBarrels;
        public readonly float OilConsumedPerCycle;
        public readonly float CycleDurationSeconds;
        public readonly float CycleProgressSeconds;
        public readonly float Progress01;
        public readonly int MaterialsOutputPerCycle;
        public readonly int FactionMaterialsCurrent;
        public readonly int FactionMaterialsCapacity;
        public readonly bool ProductionEnabled;
        public readonly MaterialFabricationStatusCode Status;
        public readonly MaterialFabricationBlockReasonCode BlockReason;
        public readonly uint Version;

        public UiMaterialFabricationReadModel(
            int runtimeBuildingId,
            byte ownerFactionId,
            int oilInputCurrentBarrels,
            int oilInputCapacityBarrels,
            float oilConsumedPerCycle,
            float cycleDurationSeconds,
            float cycleProgressSeconds,
            float progress01,
            int materialsOutputPerCycle,
            int factionMaterialsCurrent,
            int factionMaterialsCapacity,
            bool productionEnabled,
            MaterialFabricationStatusCode status,
            MaterialFabricationBlockReasonCode blockReason,
            uint version)
        {
            RuntimeBuildingId = runtimeBuildingId;
            OwnerFactionId = ownerFactionId;
            OilInputCurrentBarrels = oilInputCurrentBarrels;
            OilInputCapacityBarrels = oilInputCapacityBarrels;
            OilConsumedPerCycle = oilConsumedPerCycle;
            CycleDurationSeconds = cycleDurationSeconds;
            CycleProgressSeconds = cycleProgressSeconds;
            Progress01 = progress01;
            MaterialsOutputPerCycle = materialsOutputPerCycle;
            FactionMaterialsCurrent = factionMaterialsCurrent;
            FactionMaterialsCapacity = factionMaterialsCapacity;
            ProductionEnabled = productionEnabled;
            Status = status;
            BlockReason = blockReason;
            Version = version;
        }

        public UiMaterialFabricationReadModel(
            int runtimeBuildingId,
            byte ownerFactionId,
            float oilInputCurrentBarrels,
            int oilInputCapacityBarrels,
            float oilConsumedPerCycle,
            float cycleDurationSeconds,
            float cycleProgressSeconds,
            float progress01,
            int materialsOutputPerCycle,
            int factionMaterialsCurrent,
            int factionMaterialsCapacity,
            bool productionEnabled,
            MaterialFabricationStatusCode status,
            MaterialFabricationBlockReasonCode blockReason,
            uint version)
            : this(
                runtimeBuildingId,
                ownerFactionId,
                Mathf.FloorToInt(Mathf.Max(0f, oilInputCurrentBarrels)),
                oilInputCapacityBarrels,
                oilConsumedPerCycle,
                cycleDurationSeconds,
                cycleProgressSeconds,
                progress01,
                materialsOutputPerCycle,
                factionMaterialsCurrent,
                factionMaterialsCapacity,
                productionEnabled,
                status,
                blockReason,
                version)
        {
        }
    }

    public sealed class BuildingUiQueryUiSystemHelper
    {
        private readonly BuildingMaterialFabricationReadModelUiSystemHelper _materialFabricationReadModel = new();

        public delegate bool TryGetEntityManagerDelegate(out EntityManager entityManager);
        public delegate bool TryGetSelectedBuildingHealthDelegate(out int current, out int max);
        public delegate bool TryGetSelectedBuildingPreviewPrefabDelegate(out GameObject prefab);
        public delegate bool TryGetRuntimeBuildingOwnerFactionDelegate(int buildingId, out byte ownerFactionId);
        public delegate bool TryResolveLiveUnitPreviewPrefabDelegate(Entity unitEntity, out GameObject prefab);
        public delegate bool TryGetFactionResourceEntityDelegate(byte factionId, out Entity entity);

        public readonly struct Context
        {
            internal readonly IReadOnlyDictionary<int, RuntimeBuildingEntity> RuntimeBuildings;
            internal readonly Func<int?> GetActiveBuildingId;
            internal readonly TryGetEntityManagerDelegate TryGetEntityManager;
            internal readonly BuildingProductionQueueCompositionSystemHelper ProductionSystem;
            internal readonly Func<float> GetNow;
            internal readonly Func<bool> HasSelectedBuilding;
            internal readonly Func<bool> HasActiveBuilding;
            internal readonly Func<string> GetPlacementStatusText;
            internal readonly Func<string> GetSelectedBuildingLabel;
            internal readonly Func<string> GetSelectedBuildingDisplayName;
            internal readonly Func<string> GetSelectedBuildingDescription;
            internal readonly TryGetSelectedBuildingHealthDelegate TryGetSelectedBuildingHealth;
            internal readonly TryGetSelectedBuildingPreviewPrefabDelegate TryGetSelectedBuildingPreviewPrefab;
            internal readonly BuildingProductionRequestSystemHelper ProductionRequestSystem;
            internal readonly Func<BuildingProductionRequestSystemHelper.Context> CreateProductionRequestContext;
            internal readonly Func<int, bool> IsRuntimeBuildingWall;
            internal readonly Func<int, bool> IsRuntimeBuildingCityGenerated;
            internal readonly TryGetRuntimeBuildingOwnerFactionDelegate TryGetRuntimeBuildingOwnerFaction;
            internal readonly Func<Camera, bool> HasVisibleSelectableBuilding;
            internal readonly TryResolveLiveUnitPreviewPrefabDelegate TryResolveLiveUnitPreviewPrefab;
            internal readonly IReadOnlyList<Entity> FactionResourceEntities;
            internal readonly TryGetFactionResourceEntityDelegate TryGetFactionResourceEntity;

            internal Context(
                IReadOnlyDictionary<int, RuntimeBuildingEntity> runtimeBuildings,
                Func<int?> getActiveBuildingId,
                TryGetEntityManagerDelegate tryGetEntityManager,
                BuildingProductionQueueCompositionSystemHelper productionSystem,
                Func<float> getNow,
                Func<bool> hasSelectedBuilding,
                Func<bool> hasActiveBuilding,
                Func<string> getPlacementStatusText,
                Func<string> getSelectedBuildingLabel,
                Func<string> getSelectedBuildingDisplayName,
                Func<string> getSelectedBuildingDescription,
                TryGetSelectedBuildingHealthDelegate tryGetSelectedBuildingHealth,
                TryGetSelectedBuildingPreviewPrefabDelegate tryGetSelectedBuildingPreviewPrefab,
                BuildingProductionRequestSystemHelper productionRequestSystem,
                Func<BuildingProductionRequestSystemHelper.Context> createProductionRequestContext,
                Func<int, bool> isRuntimeBuildingWall,
                Func<int, bool> isRuntimeBuildingCityGenerated,
                TryGetRuntimeBuildingOwnerFactionDelegate tryGetRuntimeBuildingOwnerFaction,
                Func<Camera, bool> hasVisibleSelectableBuilding,
                TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab,
                IReadOnlyList<Entity> factionResourceEntities = null,
                TryGetFactionResourceEntityDelegate tryGetFactionResourceEntity = null)
            {
                RuntimeBuildings = runtimeBuildings;
                GetActiveBuildingId = getActiveBuildingId;
                TryGetEntityManager = tryGetEntityManager;
                ProductionSystem = productionSystem;
                GetNow = getNow;
                HasSelectedBuilding = hasSelectedBuilding;
                HasActiveBuilding = hasActiveBuilding;
                GetPlacementStatusText = getPlacementStatusText;
                GetSelectedBuildingLabel = getSelectedBuildingLabel;
                GetSelectedBuildingDisplayName = getSelectedBuildingDisplayName;
                GetSelectedBuildingDescription = getSelectedBuildingDescription;
                TryGetSelectedBuildingHealth = tryGetSelectedBuildingHealth;
                TryGetSelectedBuildingPreviewPrefab = tryGetSelectedBuildingPreviewPrefab;
                ProductionRequestSystem = productionRequestSystem;
                CreateProductionRequestContext = createProductionRequestContext;
                IsRuntimeBuildingWall = isRuntimeBuildingWall;
                IsRuntimeBuildingCityGenerated = isRuntimeBuildingCityGenerated;
                TryGetRuntimeBuildingOwnerFaction = tryGetRuntimeBuildingOwnerFaction;
                HasVisibleSelectableBuilding = hasVisibleSelectableBuilding;
                TryResolveLiveUnitPreviewPrefab = tryResolveLiveUnitPreviewPrefab;
                FactionResourceEntities = factionResourceEntities;
                TryGetFactionResourceEntity = tryGetFactionResourceEntity;
            }
        }

        internal bool TryGetSelectedMaterialFabricationReadModel(
            Context context,
            out UiMaterialFabricationReadModel readModel)
        {
            return _materialFabricationReadModel.TryGetSelected(context, out readModel);
        }

        public readonly struct ProducedUnitUiEntry
        {
            public readonly Entity Unit;
            public readonly GameObject Prefab;
            public readonly bool IsReady;
            public readonly float Progress01;

            public ProducedUnitUiEntry(Entity unit, GameObject prefab, bool isReady, float progress01)
            {
                Unit = unit;
                Prefab = prefab;
                IsReady = isReady;
                Progress01 = progress01;
            }
        }

        public readonly struct PendingProductionUiEntry
        {
            public readonly int BuildingId;
            public readonly int PendingProductionIndex;
            public readonly GameObject Prefab;
            public readonly float RemainingSeconds;
            public readonly float DurationSeconds;
            public readonly float Progress01;
            public readonly float StartedAt;
            public readonly float ReadyAt;
            public readonly string ProducerDisplayName;

            public PendingProductionUiEntry(int buildingId, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt)
                : this(buildingId, -1, prefab, remainingSeconds, durationSeconds, progress01, startedAt, readyAt, string.Empty)
            {
            }

            public PendingProductionUiEntry(int buildingId, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt, string producerDisplayName)
                : this(buildingId, -1, prefab, remainingSeconds, durationSeconds, progress01, startedAt, readyAt, producerDisplayName)
            {
            }

            public PendingProductionUiEntry(int buildingId, int pendingProductionIndex, GameObject prefab, float remainingSeconds, float durationSeconds, float progress01, float startedAt, float readyAt, string producerDisplayName)
            {
                BuildingId = buildingId;
                PendingProductionIndex = pendingProductionIndex;
                Prefab = prefab;
                RemainingSeconds = remainingSeconds;
                DurationSeconds = durationSeconds;
                Progress01 = progress01;
                StartedAt = startedAt;
                ReadyAt = readyAt;
                ProducerDisplayName = producerDisplayName ?? string.Empty;
            }
        }

        public void GetProducedUnits(
            List<Entity> producedUnits,
            EntityManager entityManager,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            List<Entity> results)
        {
            BuildingProductionEntriesUiSystemHelper.GetProducedUnits(
                producedUnits,
                entityManager,
                productionSystem,
                results);
        }

        internal void GetSelectedBuildingProducedUnits(Context context, List<Entity> results)
        {
            BuildingProductionEntriesUiSystemHelper.GetSelectedBuildingProducedUnits(context, results);
        }

        internal bool HasSelectedBuilding(Context context)
        {
            return context.HasSelectedBuilding != null &&
                   context.HasSelectedBuilding();
        }

        internal bool HasActiveBuilding(Context context)
        {
            return context.HasActiveBuilding != null &&
                   context.HasActiveBuilding();
        }

        internal string PlacementStatusText(Context context)
        {
            return context.GetPlacementStatusText?.Invoke() ?? string.Empty;
        }

        internal string SelectedBuildingLabel(Context context)
        {
            return context.GetSelectedBuildingLabel?.Invoke() ?? string.Empty;
        }

        internal string SelectedBuildingDisplayName(Context context)
        {
            return context.GetSelectedBuildingDisplayName?.Invoke() ?? string.Empty;
        }

        internal string SelectedBuildingDescription(Context context)
        {
            return context.GetSelectedBuildingDescription?.Invoke() ?? string.Empty;
        }

        internal bool TryGetSelectedBuildingHealth(Context context, out int current, out int max)
        {
            current = 0;
            max = 0;
            return context.TryGetSelectedBuildingHealth != null &&
                   context.TryGetSelectedBuildingHealth(out current, out max);
        }

        internal bool TryGetSelectedBuildingPreviewPrefab(Context context, out GameObject prefab)
        {
            prefab = null;
            return context.TryGetSelectedBuildingPreviewPrefab != null &&
                   context.TryGetSelectedBuildingPreviewPrefab(out prefab);
        }

        internal bool CanCreateUnitFromSelectedBuilding(Context context, int productionIndex)
        {
            return context.ProductionRequestSystem != null &&
                   context.ProductionRequestSystem.CanCreateUnitFromSelectedBuilding(
                       context.CreateProductionRequestContext != null ? context.CreateProductionRequestContext() : default,
                       context.GetActiveBuildingId?.Invoke(),
                       productionIndex);
        }

        internal bool IsRuntimeBuildingWall(Context context, int buildingId)
        {
            return context.IsRuntimeBuildingWall != null &&
                   context.IsRuntimeBuildingWall(buildingId);
        }

        internal bool IsRuntimeBuildingCityGenerated(Context context, int buildingId)
        {
            return context.IsRuntimeBuildingCityGenerated != null &&
                   context.IsRuntimeBuildingCityGenerated(buildingId);
        }

        internal bool TryGetRuntimeBuildingOwnerFaction(Context context, int buildingId, out byte ownerFactionId)
        {
            ownerFactionId = 0;
            return context.TryGetRuntimeBuildingOwnerFaction != null &&
                   context.TryGetRuntimeBuildingOwnerFaction(buildingId, out ownerFactionId);
        }

        internal bool HasVisibleSelectableBuilding(Context context, Camera camera)
        {
            return context.HasVisibleSelectableBuilding != null &&
                   context.HasVisibleSelectableBuilding(camera);
        }

        internal bool TryResolveLiveUnitPreviewPrefab(Context context, Entity unitEntity, out GameObject prefab)
        {
            prefab = null;
            return context.TryResolveLiveUnitPreviewPrefab != null &&
                   context.TryResolveLiveUnitPreviewPrefab(unitEntity, out prefab);
        }

        public void AddProducedUnitEntries(
            List<Entity> producedUnits,
            Dictionary<Entity, GameObject> producedUnitPrefabs,
            Dictionary<Entity, FixedString64Bytes> producedUnitSourceKeys,
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            EntityManager entityManager,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<ProducedUnitUiEntry> entries,
            TryResolveLiveUnitPreviewPrefabDelegate tryResolveLiveUnitPreviewPrefab = null)
        {
            BuildingProductionEntriesUiSystemHelper.AddProducedUnitEntries(
                producedUnits,
                producedUnitPrefabs,
                producedUnitSourceKeys,
                pendingProductions,
                entityManager,
                productionSystem,
                now,
                entries,
                tryResolveLiveUnitPreviewPrefab);
        }

        public void AddPendingProducedUnitEntries(
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<ProducedUnitUiEntry> entries)
        {
            BuildingProductionEntriesUiSystemHelper.AddPendingProducedUnitEntries(
                pendingProductions,
                productionSystem,
                now,
                entries);
        }

        internal void GetSelectedBuildingProducedUnitEntries(Context context, List<ProducedUnitUiEntry> entries)
        {
            BuildingProductionEntriesUiSystemHelper.GetSelectedBuildingProducedUnitEntries(context, entries);
        }

        public void AddPendingProductionUiEntries(
            int buildingId,
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<PendingProductionUiEntry> entries,
            string producerDisplayName = "")
        {
            BuildingProductionEntriesUiSystemHelper.AddPendingProductionUiEntries(
                buildingId,
                pendingProductions,
                productionSystem,
                now,
                entries,
                producerDisplayName);
        }

        internal void GetFriendlyPendingProductionUiEntries(Context context, List<PendingProductionUiEntry> entries)
        {
            BuildingProductionEntriesUiSystemHelper.GetFriendlyPendingProductionUiEntries(context, entries);
        }
    }
}
