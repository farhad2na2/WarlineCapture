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
            MatchSceneView sceneView,
            BuildingPlacementSystemConfig buildingPlacementConfig,
            AIStartupSystem aiStartupSystem,
            IReadOnlyList<AIControllerConfig> aiControllerConfigs,
            AISettingsSnapshot aiSettings,
            List<InitialFactionSpawnCellFallbackEntry> initialFactionSpawnCellFallbackEntries)
        {
            GridAuthoringConfig runtimeGridConfig = sceneView != null ? sceneView.RuntimeGridConfig : null;
            MapSurfaceAuthoring mapSurfaceAuthoring = sceneView != null ? sceneView.MapSurfaceAuthoring : null;
            OperationMapCameraPoseCameraHelper.TryApplyInitialPose(
                world,
                sceneView != null ? sceneView.WorldCamera : null);

            GridConfig startupGrid;
            bool resolvedActiveGrid = OperationMapMetadataUtility.TryResolveActiveGridConfig(
                world.EntityManager,
                out startupGrid,
                out bool hasActiveMap,
                out string operationMapGridError);
            if (!resolvedActiveGrid && hasActiveMap)
            {
                throw new System.InvalidOperationException(
                    $"Operation-map grid binding failed: {operationMapGridError}");
            }

            if (!resolvedActiveGrid && runtimeGridConfig == null)
            {
                throw new System.InvalidOperationException(
                    "Match startup requires active operation-map grid metadata or a compatibility grid config.");
            }

            if (!resolvedActiveGrid)
            {
                startupGrid = new GridConfig
                {
                    Width = runtimeGridConfig.Width,
                    Height = runtimeGridConfig.Height,
                    CellSize = runtimeGridConfig.CellSize,
                    Origin = runtimeGridConfig.Origin
                };
            }

            if (runtimeGridBootstrapSystem == null)
            {
                Debug.LogWarning("[MatchBootstrap] missingRuntimeGridBootstrapStartupSystemHelper");
                return;
            }

            runtimeGridBootstrapSystem.Ensure(
                world.EntityManager,
                startupGrid.Width,
                startupGrid.Height,
                startupGrid.CellSize,
                (Vector3)startupGrid.Origin);
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
