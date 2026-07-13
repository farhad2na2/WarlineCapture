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

        private bool _hasMaterialFabricationReadModelState;
        private int _materialFabricationRuntimeBuildingId;
        private Entity _materialFabricationCombatEntity;
        private Entity _materialFabricationFactionEntity;
        private BuildingResourceStorageComponent _materialFabricationStorage;
        private MaterialFabricationComponent _materialFabrication;
        private FactionTacticalMaterialsComponent _materialFabricationFactionMaterials;
        private uint _materialFabricationReadModelVersion;

        internal bool TryGetSelectedMaterialFabricationReadModel(
            Context context,
            out UiMaterialFabricationReadModel readModel)
        {
            readModel = default;
            if (context.RuntimeBuildings == null ||
                context.GetActiveBuildingId == null ||
                context.TryGetEntityManager == null ||
                (context.FactionResourceEntities == null && context.TryGetFactionResourceEntity == null) ||
                !context.TryGetEntityManager(out EntityManager entityManager))
            {
                InvalidateMaterialFabricationReadModelState();
                return false;
            }

            int? selectedBuildingId = context.GetActiveBuildingId();
            if (!selectedBuildingId.HasValue ||
                !context.RuntimeBuildings.TryGetValue(selectedBuildingId.Value, out RuntimeBuildingEntity building) ||
                building == null ||
                building.CombatEntity == Entity.Null ||
                !entityManager.Exists(building.CombatEntity) ||
                !entityManager.HasComponent<MaterialFabricationComponent>(building.CombatEntity) ||
                !entityManager.HasComponent<MaterialFabricationInputTag>(building.CombatEntity) ||
                !entityManager.HasComponent<BuildingResourceStorageComponent>(building.CombatEntity))
            {
                InvalidateMaterialFabricationReadModelState();
                return false;
            }

            MaterialFabricationComponent fabrication =
                entityManager.GetComponentData<MaterialFabricationComponent>(building.CombatEntity);
            BuildingResourceStorageComponent storage =
                entityManager.GetComponentData<BuildingResourceStorageComponent>(building.CombatEntity);
            if (fabrication.RuntimeBuildingId != selectedBuildingId.Value ||
                storage.RuntimeBuildingId != selectedBuildingId.Value ||
                storage.OwnerFactionId != fabrication.OwnerFactionId ||
                !TryResolveFactionMaterialsEntity(
                    context,
                    entityManager,
                    fabrication.OwnerFactionId,
                    out Entity factionEntity,
                    out FactionTacticalMaterialsComponent factionMaterials))
            {
                InvalidateMaterialFabricationReadModelState();
                return false;
            }

            if (!_hasMaterialFabricationReadModelState ||
                _materialFabricationRuntimeBuildingId != selectedBuildingId.Value ||
                _materialFabricationCombatEntity != building.CombatEntity ||
                _materialFabricationFactionEntity != factionEntity ||
                !HasSameStorageReadState(_materialFabricationStorage, storage) ||
                !HasSameFabricationReadState(_materialFabrication, fabrication) ||
                !HasSameFactionMaterialsReadState(_materialFabricationFactionMaterials, factionMaterials))
            {
                _hasMaterialFabricationReadModelState = true;
                _materialFabricationRuntimeBuildingId = selectedBuildingId.Value;
                _materialFabricationCombatEntity = building.CombatEntity;
                _materialFabricationFactionEntity = factionEntity;
                _materialFabricationStorage = storage;
                _materialFabrication = fabrication;
                _materialFabricationFactionMaterials = factionMaterials;
                _materialFabricationReadModelVersion = NextVersion(_materialFabricationReadModelVersion);
            }

            float progress01 = fabrication.CycleDurationSeconds > 0f
                ? Mathf.Clamp01(fabrication.CycleProgressSeconds / fabrication.CycleDurationSeconds)
                : 0f;
            int oilInputCurrent = Mathf.RoundToInt(Mathf.Max(0f, storage.StoredOilBarrels));
            if (storage.OilStorageCapacity > 0)
                oilInputCurrent = Mathf.Min(oilInputCurrent, storage.OilStorageCapacity);
            readModel = new UiMaterialFabricationReadModel(
                selectedBuildingId.Value,
                fabrication.OwnerFactionId,
                oilInputCurrent,
                storage.OilStorageCapacity,
                fabrication.OilConsumedPerCycle,
                fabrication.CycleDurationSeconds,
                fabrication.CycleProgressSeconds,
                progress01,
                fabrication.MaterialsOutputPerCycle,
                factionMaterials.Current,
                factionMaterials.Capacity,
                fabrication.ProductionEnabled != 0,
                fabrication.Status,
                fabrication.BlockReason,
                _materialFabricationReadModelVersion);
            return true;
        }

        private static bool TryResolveUniqueFactionMaterialsEntity(
            IReadOnlyList<Entity> factionResourceEntities,
            EntityManager entityManager,
            byte ownerFactionId,
            out Entity factionEntity,
            out FactionTacticalMaterialsComponent factionMaterials)
        {
            factionEntity = Entity.Null;
            factionMaterials = default;
            for (int i = 0; i < factionResourceEntities.Count; i++)
            {
                Entity candidate = factionResourceEntities[i];
                if (candidate == Entity.Null ||
                    !entityManager.Exists(candidate) ||
                    !entityManager.HasComponent<FactionEconomy>(candidate) ||
                    !entityManager.HasComponent<FactionTacticalMaterialsComponent>(candidate))
                {
                    continue;
                }

                FactionEconomy economy = entityManager.GetComponentData<FactionEconomy>(candidate);
                FactionTacticalMaterialsComponent materials =
                    entityManager.GetComponentData<FactionTacticalMaterialsComponent>(candidate);
                bool economyMatches = economy.FactionId == ownerFactionId;
                bool materialsMatch = materials.FactionId == ownerFactionId;
                if (economyMatches != materialsMatch)
                    return false;
                if (!economyMatches)
                    continue;
                if (factionEntity != Entity.Null)
                    return false;

                factionEntity = candidate;
                factionMaterials = materials;
            }

            return factionEntity != Entity.Null;
        }

        private static bool TryResolveFactionMaterialsEntity(
            Context context,
            EntityManager entityManager,
            byte ownerFactionId,
            out Entity factionEntity,
            out FactionTacticalMaterialsComponent factionMaterials)
        {
            if (context.FactionResourceEntities != null)
            {
                return TryResolveUniqueFactionMaterialsEntity(
                    context.FactionResourceEntities,
                    entityManager,
                    ownerFactionId,
                    out factionEntity,
                    out factionMaterials);
            }

            factionEntity = Entity.Null;
            factionMaterials = default;
            if (context.TryGetFactionResourceEntity == null ||
                !context.TryGetFactionResourceEntity(ownerFactionId, out factionEntity) ||
                factionEntity == Entity.Null ||
                !entityManager.Exists(factionEntity) ||
                !entityManager.HasComponent<FactionEconomy>(factionEntity) ||
                !entityManager.HasComponent<FactionTacticalMaterialsComponent>(factionEntity))
            {
                return false;
            }

            FactionEconomy economy = entityManager.GetComponentData<FactionEconomy>(factionEntity);
            factionMaterials = entityManager.GetComponentData<FactionTacticalMaterialsComponent>(factionEntity);
            return economy.FactionId == ownerFactionId && factionMaterials.FactionId == ownerFactionId;
        }

        private static bool HasSameStorageReadState(
            in BuildingResourceStorageComponent left,
            in BuildingResourceStorageComponent right)
        {
            return left.RuntimeBuildingId == right.RuntimeBuildingId &&
                   left.OwnerFactionId == right.OwnerFactionId &&
                   left.OilStorageCapacity == right.OilStorageCapacity &&
                   left.StoredOilBarrels == right.StoredOilBarrels &&
                   left.Version == right.Version;
        }

        private static bool HasSameFabricationReadState(
            in MaterialFabricationComponent left,
            in MaterialFabricationComponent right)
        {
            return left.RuntimeBuildingId == right.RuntimeBuildingId &&
                   left.OwnerFactionId == right.OwnerFactionId &&
                   left.ProductionEnabled == right.ProductionEnabled &&
                   left.OilConsumedPerCycle == right.OilConsumedPerCycle &&
                   left.MaterialsOutputPerCycle == right.MaterialsOutputPerCycle &&
                   left.CycleDurationSeconds == right.CycleDurationSeconds &&
                   left.CycleProgressSeconds == right.CycleProgressSeconds &&
                   left.Status == right.Status &&
                   left.BlockReason == right.BlockReason &&
                   left.Version == right.Version;
        }

        private static bool HasSameFactionMaterialsReadState(
            in FactionTacticalMaterialsComponent left,
            in FactionTacticalMaterialsComponent right)
        {
            return left.FactionId == right.FactionId &&
                   left.Current == right.Current &&
                   left.Capacity == right.Capacity &&
                   left.Version == right.Version;
        }

        private void InvalidateMaterialFabricationReadModelState()
        {
            _hasMaterialFabricationReadModelState = false;
        }

        private static uint NextVersion(uint version)
        {
            version++;
            return version != 0 ? version : 1;
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
            results?.Clear();
            if (producedUnits == null || productionSystem == null || results == null)
                return;

            productionSystem.PruneProducedUnits(producedUnits, null, null, entityManager);
            for (int i = 0; i < producedUnits.Count; i++)
                results.Add(producedUnits[i]);
        }

        internal void GetSelectedBuildingProducedUnits(Context context, List<Entity> results)
        {
            results?.Clear();
            if (results == null ||
                context.RuntimeBuildings == null ||
                context.GetActiveBuildingId == null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em))
            {
                return;
            }

            int? buildingId = context.GetActiveBuildingId();
            if (!buildingId.HasValue ||
                !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingEntity building) ||
                building == null)
            {
                return;
            }

            if (TryAddProducedUnitsFromReadModel(building, em, results))
                return;

            building.ProducedUnits ??= new List<Entity>();
            GetProducedUnits(building.ProducedUnits, em, context.ProductionSystem, results);
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
            if (entries == null)
                return;

            if (producedUnits != null)
            {
                productionSystem?.PruneProducedUnits(
                    producedUnits,
                    null,
                    producedUnitPrefabs,
                    entityManager,
                    producedUnitSourceKeys);
                for (int i = 0; i < producedUnits.Count; i++)
                {
                    Entity unit = producedUnits[i];
                    GameObject prefab = null;
                    producedUnitPrefabs?.TryGetValue(unit, out prefab);
                    if (prefab == null && tryResolveLiveUnitPreviewPrefab != null)
                        tryResolveLiveUnitPreviewPrefab(unit, out prefab);
                    entries.Add(new ProducedUnitUiEntry(unit, prefab, true, 1f));
                }
            }

            AddPendingProducedUnitEntries(pendingProductions, productionSystem, now, entries);
        }

        public void AddPendingProducedUnitEntries(
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<ProducedUnitUiEntry> entries)
        {
            if (pendingProductions == null || productionSystem == null || entries == null)
                return;

            foreach (BuildingProductionQueueCompositionSystemHelper.IPendingProduction pending in pendingProductions)
            {
                if (pending == null || pending.Prefab == null)
                    continue;

                BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, true);
                entries.Add(new ProducedUnitUiEntry(Entity.Null, pending.Prefab, false, progress.Progress01));
            }
        }

        internal void GetSelectedBuildingProducedUnitEntries(Context context, List<ProducedUnitUiEntry> entries)
        {
            entries?.Clear();
            if (entries == null ||
                context.RuntimeBuildings == null ||
                context.GetActiveBuildingId == null ||
                context.TryGetEntityManager == null ||
                !context.TryGetEntityManager(out EntityManager em))
            {
                return;
            }

            int? buildingId = context.GetActiveBuildingId();
            if (!buildingId.HasValue ||
                !context.RuntimeBuildings.TryGetValue(buildingId.Value, out RuntimeBuildingEntity building) ||
                building == null)
            {
                return;
            }

            float now = context.GetNow != null ? context.GetNow() : UnityEngine.Time.time;
            if (TryAddProducedUnitEntriesFromReadModel(context, building, em, entries))
            {
                AddPendingProducedUnitEntries(building.PendingProductions, context.ProductionSystem, now, entries);
                return;
            }

            building.ProducedUnits ??= new List<Entity>();
            AddProducedUnitEntries(
                building.ProducedUnits,
                building.ProducedUnitPrefabs,
                building.ProducedUnitSourceKeys,
                building.PendingProductions,
                em,
                context.ProductionSystem,
                now,
                entries,
                context.TryResolveLiveUnitPreviewPrefab);
        }

        private static bool TryAddProducedUnitsFromReadModel(
            RuntimeBuildingEntity building,
            EntityManager em,
            List<Entity> results)
        {
            if (!TryGetProducedUnitReadModelRows(em, out DynamicBuffer<BuildingProducedUnitReadModel> producedUnits))
                return false;

            bool matchedBuilding = false;
            for (int i = 0; i < producedUnits.Length; i++)
            {
                BuildingProducedUnitReadModel producedUnit = producedUnits[i];
                if (producedUnit.BuildingRuntimeId != building.Id)
                    continue;

                matchedBuilding = true;
                if (IsProducedUnitAlive(producedUnit.Unit, em))
                    results.Add(producedUnit.Unit);
            }

            return matchedBuilding;
        }

        private static bool TryAddProducedUnitEntriesFromReadModel(
            Context context,
            RuntimeBuildingEntity building,
            EntityManager em,
            List<ProducedUnitUiEntry> entries)
        {
            if (!TryGetProducedUnitReadModelRows(em, out DynamicBuffer<BuildingProducedUnitReadModel> producedUnits))
                return false;

            bool matchedBuilding = false;
            for (int i = 0; i < producedUnits.Length; i++)
            {
                BuildingProducedUnitReadModel producedUnit = producedUnits[i];
                if (producedUnit.BuildingRuntimeId != building.Id)
                    continue;

                matchedBuilding = true;
                Entity unit = producedUnit.Unit;
                if (!IsProducedUnitAlive(unit, em))
                    continue;

                GameObject prefab = null;
                context.TryResolveLiveUnitPreviewPrefab?.Invoke(unit, out prefab);
                entries.Add(new ProducedUnitUiEntry(unit, prefab, true, 1f));
            }

            return matchedBuilding;
        }

        private static bool TryGetProducedUnitReadModelRows(
            EntityManager em,
            out DynamicBuffer<BuildingProducedUnitReadModel> producedUnits)
        {
            producedUnits = default;
            if (em.World == null || !em.World.IsCreated)
                return false;

            using EntityQuery boundaryQuery = em.CreateEntityQuery(ComponentType.ReadOnly<BuildingRuntimeStateTag>());
            if (boundaryQuery.IsEmptyIgnoreFilter)
                return false;

            using NativeArray<ArchetypeChunk> boundaryChunks = boundaryQuery.ToArchetypeChunkArray(Allocator.Temp);
            if (boundaryChunks.Length == 0)
                return false;

            EntityTypeHandle entityType = em.GetEntityTypeHandle();
            NativeArray<Entity> firstChunkEntities = boundaryChunks[0].GetNativeArray(entityType);
            if (firstChunkEntities.Length == 0)
                return false;

            Entity boundaryEntity = firstChunkEntities[0];
            if (boundaryEntity == Entity.Null ||
                !em.Exists(boundaryEntity) ||
                !em.HasBuffer<BuildingProducedUnitReadModel>(boundaryEntity))
            {
                return false;
            }

            producedUnits = em.GetBuffer<BuildingProducedUnitReadModel>(boundaryEntity, true);
            return true;
        }

        private static bool IsProducedUnitAlive(Entity unit, EntityManager em)
        {
            if (unit == Entity.Null ||
                em.World == null ||
                !em.World.IsCreated ||
                !em.Exists(unit))
            {
                return false;
            }

            return !em.HasComponent<UnitHealth>(unit) ||
                   em.GetComponentData<UnitHealth>(unit).Current > 0;
        }

        public void AddPendingProductionUiEntries(
            int buildingId,
            IEnumerable<BuildingProductionQueueCompositionSystemHelper.IPendingProduction> pendingProductions,
            BuildingProductionQueueCompositionSystemHelper productionSystem,
            float now,
            List<PendingProductionUiEntry> entries,
            string producerDisplayName = "")
        {
            if (pendingProductions == null || productionSystem == null || entries == null)
                return;

            int pendingIndex = 0;
            foreach (BuildingProductionQueueCompositionSystemHelper.IPendingProduction pending in pendingProductions)
            {
                if (pending == null || pending.Prefab == null)
                {
                    pendingIndex++;
                    continue;
                }

                BuildingProductionQueueCompositionSystemHelper.PendingProductionProgress progress = productionSystem.GetProgress(pending, now, false);
                entries.Add(new PendingProductionUiEntry(
                    buildingId,
                    pendingIndex,
                    pending.Prefab,
                    progress.RemainingSeconds,
                    progress.DurationSeconds,
                    progress.Progress01,
                    pending.StartedAt,
                    pending.ReadyAt,
                    producerDisplayName));
                pendingIndex++;
            }
        }

        internal void GetFriendlyPendingProductionUiEntries(Context context, List<PendingProductionUiEntry> entries)
        {
            if (entries == null)
                return;

            entries.Clear();
            if (context.RuntimeBuildings == null)
                return;

            float now = context.GetNow != null ? context.GetNow() : UnityEngine.Time.time;
            foreach (KeyValuePair<int, RuntimeBuildingEntity> pair in context.RuntimeBuildings)
            {
                RuntimeBuildingEntity building = pair.Value;
                if (building == null ||
                    building.IsDestroyed ||
                    building.PendingProductions == null ||
                    building.PendingProductions.Count == 0)
                {
                    continue;
                }

                if (building.IsCityGenerated)
                    continue;
                if (!IsFriendlyProductionBuilding(building))
                    continue;

                AddPendingProductionUiEntries(
                    pair.Key,
                    building.PendingProductions,
                    context.ProductionSystem,
                    now,
                    entries,
                    ResolveProducerDisplayName(pair.Key, building));
            }
        }

        private static bool IsFriendlyProductionBuilding(RuntimeBuildingEntity building)
        {
            if (building == null)
                return false;

            return !building.HasOwnerFaction ||
                   building.OwnerFactionId == FactionIdentity.NeutralFactionId ||
                   building.OwnerFactionId == FactionIdentity.PlayerFactionId;
        }

        private static string ResolveProducerDisplayName(int buildingId, RuntimeBuildingEntity building)
        {
            string displayName = building != null && building.Definition != null
                ? building.Definition.DisplayName
                : string.Empty;
            return string.IsNullOrWhiteSpace(displayName)
                ? $"Building {buildingId}"
                : displayName;
        }
    }
}
