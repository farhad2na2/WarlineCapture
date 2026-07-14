using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class BuildingMaterialFabricationReadModelUiSystemHelper
    {
        private bool _hasReadModelState;
        private int _runtimeBuildingId;
        private Entity _combatEntity;
        private Entity _factionEntity;
        private BuildingResourceStorageComponent _storage;
        private MaterialFabricationComponent _fabrication;
        private FactionTacticalMaterialsComponent _factionMaterials;
        private uint _readModelVersion;

        internal bool TryGetSelected(
            BuildingUiQueryUiSystemHelper.Context context,
            out UiMaterialFabricationReadModel readModel)
        {
            readModel = default;
            if (context.RuntimeBuildings == null ||
                context.GetActiveBuildingId == null ||
                context.TryGetEntityManager == null ||
                (context.FactionResourceEntities == null && context.TryGetFactionResourceEntity == null) ||
                !context.TryGetEntityManager(out EntityManager entityManager))
            {
                InvalidateReadModelState();
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
                InvalidateReadModelState();
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
                InvalidateReadModelState();
                return false;
            }

            if (!_hasReadModelState ||
                _runtimeBuildingId != selectedBuildingId.Value ||
                _combatEntity != building.CombatEntity ||
                _factionEntity != factionEntity ||
                !HasSameStorageReadState(_storage, storage) ||
                !HasSameFabricationReadState(_fabrication, fabrication) ||
                !HasSameFactionMaterialsReadState(_factionMaterials, factionMaterials))
            {
                _hasReadModelState = true;
                _runtimeBuildingId = selectedBuildingId.Value;
                _combatEntity = building.CombatEntity;
                _factionEntity = factionEntity;
                _storage = storage;
                _fabrication = fabrication;
                _factionMaterials = factionMaterials;
                _readModelVersion = NextVersion(_readModelVersion);
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
                _readModelVersion);
            return true;
        }

        private static bool TryResolveFactionMaterialsEntity(
            BuildingUiQueryUiSystemHelper.Context context,
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

        private void InvalidateReadModelState()
        {
            _hasReadModelState = false;
        }

        private static uint NextVersion(uint version)
        {
            version++;
            return version != 0 ? version : 1;
        }
    }
}
