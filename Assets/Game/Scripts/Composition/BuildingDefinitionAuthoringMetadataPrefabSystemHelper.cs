using UnityEngine;
using Game.Components;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal static partial class BuildingDefinitionAuthoringMetadataPrefabSystemHelper
    {
        public static bool TryGetBuildingDefinitionMetadata(
            GameObject prefab,
            out BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata metadata)
        {
            metadata = default;
            if (prefab == null || !prefab.TryGetComponent(out BuildingDefinitionAuthoring authoring))
                return false;

            authoring.ApplyConfigIfAvailable();
            int productionCount = Mathf.Max(0, authoring.ConfiguredProductionCount);
            GameObject[] productionPrefabs = productionCount > 0 ? new GameObject[productionCount] : null;
            for (int i = 0; i < productionCount; i++)
                productionPrefabs[i] = authoring.GetProductionOrDefault(i)?.spawnUnitPrefab;

            metadata = new BuildingDefinitionPrefabSystemHelper.BuildingDefinitionMetadata
            {
                DisplayName = authoring.ConfiguredDisplayName,
                Description = authoring.ConfiguredDescription,
                MaxHealth = authoring.ConfiguredMaxHealth,
                DestroyedVisualPrefab = authoring.ConfiguredDestroyedVisualPrefab,
                SelectionPortraitSprite = authoring.ConfiguredPortraitActionSprite != null
                    ? authoring.ConfiguredPortraitActionSprite
                    : authoring.ConfiguredPortraitCardSprite,
                CardPortraitSprite = authoring.ConfiguredPortraitCardSprite != null
                    ? authoring.ConfiguredPortraitCardSprite
                    : authoring.ConfiguredPortraitSprite,
                FootprintCells = authoring.ConfiguredFootprintCells,
                Role = authoring.ConfiguredRole,
                IsWall = authoring.ConfiguredIsWall,
                CanRequest = authoring.ConfiguredCanRequest,
                Price = authoring.ConfiguredPrice,
                MaterialsCost = authoring.ConfiguredMaterialsCost,
                ProductionDurationSeconds = authoring.ConfiguredProductionDurationSeconds,
                OilBarrelsPerDay = authoring.ConfiguredOilBarrelsPerDay,
                OilStorageCapacity = authoring.ConfiguredOilStorageCapacity,
                FuelBarrelsPerDay = authoring.ConfiguredFuelBarrelsPerDay,
                FuelStorageCapacity = authoring.ConfiguredFuelStorageCapacity,
                MaterialFabricationEnabled = authoring.ConfiguredMaterialFabricationEnabled,
                MaterialFabricationOilConsumedPerCycle = authoring.ConfiguredMaterialFabricationOilConsumedPerCycle,
                MaterialFabricationMaterialsOutputPerCycle = authoring.ConfiguredMaterialFabricationMaterialsOutputPerCycle,
                MaterialFabricationCycleDurationSeconds = authoring.ConfiguredMaterialFabricationCycleDurationSeconds,
                MaterialFabricationOutputCapacityPolicy = authoring.ConfiguredMaterialFabricationOutputCapacityPolicy,
                RefugeeCapacity = authoring.ConfiguredRefugeeCapacity,
                RefugeeUpkeepPerCitizenPerDay = authoring.ConfiguredRefugeeUpkeepPerCitizenPerDay,
                ThreatDetectionKind = authoring.ConfiguredThreatDetectionKind,
                ThreatDetectionRadiusCells = authoring.ConfiguredThreatDetectionRadiusCells,
                CanAttack = authoring.ConfiguredCanAttack,
                MaxConcurrentAttacks = authoring.ConfiguredMaxConcurrentAttacks,
                AttackRange = authoring.ConfiguredAttackRange,
                AttackCooldownSeconds = authoring.ConfiguredAttackCooldownSeconds,
                AttackDamage = authoring.ConfiguredAttackDamage,
                AttackImpactPrefab = authoring.ConfiguredAttackImpactPrefab,
                MuzzleFlashPrefab = authoring.ConfiguredMuzzleFlashPrefab,
                MuzzleFlashHeightOffset = authoring.ConfiguredMuzzleFlashHeightOffset,
                MuzzleFlashForwardOffset = authoring.ConfiguredMuzzleFlashForwardOffset,
                AttackTraceColor = authoring.ConfiguredAttackTraceColor,
                AttackTraceWidth = authoring.ConfiguredAttackTraceWidth,
                AttackTraceScrollSpeed = authoring.ConfiguredAttackTraceScrollSpeed,
                AttackTraceDashDensity = authoring.ConfiguredAttackTraceDashDensity,
                AttackTraceVisibleSeconds = authoring.ConfiguredAttackTraceVisibleSeconds,
                AttackTracerEveryNthShot = authoring.ConfiguredAttackTracerEveryNthShot,
                ProductionSpawnUnitPrefabs = productionPrefabs
            };
            return true;
        }

    }
}
