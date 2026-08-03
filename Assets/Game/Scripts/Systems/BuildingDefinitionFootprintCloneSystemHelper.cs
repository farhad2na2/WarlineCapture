using UnityEngine;

namespace Game.Runtime
{
    internal static class BuildingDefinitionFootprintCloneSystemHelper
    {
        internal static BuildingDefinition Clone(BuildingDefinition definition, Vector2Int footprintCells)
        {
            if (definition == null)
                return null;

            return new BuildingDefinition
            {
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                MaxHealth = definition.MaxHealth,
                ProductionSlots = definition.ProductionSlots,
                SpawnUnitPrefab = definition.SpawnUnitPrefab,
                SecondarySpawnUnitPrefab = definition.SecondarySpawnUnitPrefab,
                TertiarySpawnUnitPrefab = definition.TertiarySpawnUnitPrefab,
                QuaternarySpawnUnitPrefab = definition.QuaternarySpawnUnitPrefab,
                Prefab = definition.Prefab,
                DestroyedVisualPrefab = definition.DestroyedVisualPrefab,
                SelectionPortraitSprite = definition.SelectionPortraitSprite,
                CardPortraitSprite = definition.CardPortraitSprite,
                FootprintCells = footprintCells,
                Role = definition.Role,
                IsWall = definition.IsWall,
                CreditsCost = definition.CreditsCost,
                MaterialsCost = definition.MaterialsCost,
                ProductionDurationSeconds = definition.ProductionDurationSeconds,
                OilBarrelsPerDay = definition.OilBarrelsPerDay,
                OilStorageCapacity = definition.OilStorageCapacity,
                FuelBarrelsPerDay = definition.FuelBarrelsPerDay,
                FuelStorageCapacity = definition.FuelStorageCapacity,
                MaterialFabricationEnabled = definition.MaterialFabricationEnabled,
                MaterialFabricationOilConsumedPerCycle = definition.MaterialFabricationOilConsumedPerCycle,
                MaterialFabricationMaterialsOutputPerCycle = definition.MaterialFabricationMaterialsOutputPerCycle,
                MaterialFabricationCycleDurationSeconds = definition.MaterialFabricationCycleDurationSeconds,
                MaterialFabricationOutputCapacityPolicy = definition.MaterialFabricationOutputCapacityPolicy,
                RefugeeCapacity = definition.RefugeeCapacity,
                RefugeeUpkeepPerCitizenPerDay = definition.RefugeeUpkeepPerCitizenPerDay,
                ThreatDetectionKind = definition.ThreatDetectionKind,
                ThreatDetectionRadiusCells = definition.ThreatDetectionRadiusCells,
                CanAttack = definition.CanAttack,
                MaxConcurrentAttacks = definition.MaxConcurrentAttacks,
                AttackRange = definition.AttackRange,
                AttackCooldownSeconds = definition.AttackCooldownSeconds,
                AttackDamage = definition.AttackDamage,
                AttackImpactPrefab = definition.AttackImpactPrefab,
                MuzzleFlashPrefab = definition.MuzzleFlashPrefab,
                MuzzleFlashHeightOffset = definition.MuzzleFlashHeightOffset,
                MuzzleFlashForwardOffset = definition.MuzzleFlashForwardOffset,
                AttackTraceColor = definition.AttackTraceColor,
                AttackTraceWidth = definition.AttackTraceWidth,
                AttackTraceScrollSpeed = definition.AttackTraceScrollSpeed,
                AttackTraceDashDensity = definition.AttackTraceDashDensity,
                AttackTraceVisibleSeconds = definition.AttackTraceVisibleSeconds,
                AttackTracerEveryNthShot = definition.AttackTracerEveryNthShot,
                LocalBounds = definition.LocalBounds,
                HasLocalBounds = definition.HasLocalBounds,
                VisualTemplate = definition.VisualTemplate,
                GeneratedMeshes = definition.GeneratedMeshes,
                ProductionSpawnLocalPositions = definition.ProductionSpawnLocalPositions,
                HasRunway = definition.HasRunway,
                RunwayLocalPosition = definition.RunwayLocalPosition,
                RunwayLocalRotation = definition.RunwayLocalRotation,
                RunwayHalfExtents = definition.RunwayHalfExtents
            };
        }
    }
}
