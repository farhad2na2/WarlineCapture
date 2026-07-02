using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;
using Game.Authoring;
using Game.Runtime;

namespace Game.Composition
{
    internal static class MatchBootstrapStartupConfigProjection
    {
        public static void ProjectRuntimeStartupConfig(
            World world,
            RuntimeGridBootstrapStartupSystemHelper runtimeGridBootstrapSystem,
            MapSurfaceRuntimeBootstrapSceneSystemHelper mapSurfaceRuntimeBootstrapSystem,
            GridAuthoringConfig runtimeGridConfig,
            MapSurfaceAuthoring mapSurfaceAuthoring,
            BuildingPlacementSystemConfig buildingPlacementConfig,
            AIStartupSystem aiStartupSystem,
            IReadOnlyList<AIControllerConfig> aiControllerConfigs,
            AISettingsSnapshot aiSettings,
            List<InitialFactionSpawnCellFallbackEntry> initialFactionSpawnCellFallbackEntries)
        {
            if (runtimeGridConfig == null)
            {
                Debug.LogError("[MatchBootstrap] missingRuntimeGridConfig");
                return;
            }

            if (runtimeGridBootstrapSystem == null)
            {
                Debug.LogWarning("[MatchBootstrap] missingRuntimeGridBootstrapStartupSystemHelper");
                return;
            }

            runtimeGridBootstrapSystem.Ensure(
                world.EntityManager,
                runtimeGridConfig.Width,
                runtimeGridConfig.Height,
                runtimeGridConfig.CellSize,
                runtimeGridConfig.Origin);
            if (mapSurfaceRuntimeBootstrapSystem == null)
            {
                Debug.LogWarning("[MatchBootstrap] missingMapSurfaceRuntimeBootstrapSystem");
                return;
            }

            mapSurfaceRuntimeBootstrapSystem.Ensure(mapSurfaceAuthoring);
            ProjectInitialFactionSpawnCellFallbackEntries(
                buildingPlacementConfig != null ? buildingPlacementConfig.InitialUnitsConfig : null,
                initialFactionSpawnCellFallbackEntries);
            aiStartupSystem.LogConfigValidation(aiControllerConfigs, aiSettings);
        }

        public static AIStartupSystem.Result InitializeAiStartupConfig(
            AIStartupSystem aiStartupSystem,
            IReadOnlyList<AIControllerConfig> aiControllerConfigs,
            AIPlanEntryStartupConfig aiPlanEntryConfig,
            AISettingsSnapshot aiSettings,
            AIStartupSystem.TryResolveFactionSpawnCell tryResolveFactionSpawnCell)
        {
            return aiStartupSystem.Initialize(
                aiControllerConfigs,
                aiPlanEntryConfig,
                tryResolveFactionSpawnCell,
                aiSettings);
        }

        public static bool FocusInitialCameraOnConfiguredFactionBase(
            World world,
            SelectionUiCameraSystemHelper selectionUiCameraSystem,
            AIStartupSystem.TryResolveFactionSpawnCell resolveFactionSpawnCell,
            byte fallbackFactionId)
        {
            if (selectionUiCameraSystem == null ||
                resolveFactionSpawnCell == null ||
                !resolveFactionSpawnCell(fallbackFactionId, out int2 spawnCell))
            {
                return false;
            }

            Vector3 focusWorldPosition = new(spawnCell.x, 0f, spawnCell.y);
            if (world != null && world.IsCreated)
            {
                EntityManager em = world.EntityManager;
                using EntityQuery gridQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
                if (!gridQuery.IsEmptyIgnoreFilter)
                {
                    Entity gridEntity = gridQuery.GetSingletonEntity();
                    GridConfig grid = em.GetComponentData<GridConfig>(gridEntity);
                    focusWorldPosition = GridUtils.CellToWorldCenter(grid, spawnCell);
                }
            }

            selectionUiCameraSystem.FollowCameraGroundCenterTo(focusWorldPosition);
            return true;
        }

        private static void ProjectInitialFactionSpawnCellFallbackEntries(
            InitialUnitsSpawnerAuthoringConfig config,
            List<InitialFactionSpawnCellFallbackEntry> entries)
        {
            entries.Clear();

            if (config == null || config.Factions == null)
                return;

            for (int i = 0; i < config.Factions.Count; i++)
            {
                InitialUnitsSpawnerAuthoringConfig.FactionEntry faction = config.Factions[i];
                if (faction == null)
                    continue;

                byte factionId = (byte)Mathf.Clamp(faction.FactionId, 0, byte.MaxValue);
                Vector2Int spawnCell = faction.SpawnCell;
                entries.Add(new InitialFactionSpawnCellFallbackEntry(
                    factionId,
                    new int2(spawnCell.x, spawnCell.y)));
            }
        }
    }
}
