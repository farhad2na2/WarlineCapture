using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Game.Components;
using Game.Configs;

namespace Game.Runtime
{
    internal sealed class MapBuildingPlacementSpawnPrefabSystemHelper
    {
        private const int MaxPlacementsPerUpdate = 32;

        public delegate bool TryGetGridDataDelegate(
            out Entity gridEntity,
            out GridConfig grid,
            out DynamicBuffer<GridRoad> roads,
            out DynamicBlockerComponent blockerData);

        public readonly struct Context
        {
            public readonly MapBuildingPlacementConfig Config;
            public readonly Transform AuthoringBuildingsRoot;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper RuntimeSpawnSystem;
            public readonly BuildingRuntimeSpawnCompositionSystemHelper.Context RuntimeSpawnContext;
            public readonly TryGetGridDataDelegate TryGetGridData;
            public readonly Action<string> LogWarning;

            public Context(
                MapBuildingPlacementConfig config,
                Transform authoringBuildingsRoot,
                BuildingRuntimeSpawnCompositionSystemHelper runtimeSpawnSystem,
                BuildingRuntimeSpawnCompositionSystemHelper.Context runtimeSpawnContext,
                TryGetGridDataDelegate tryGetGridData,
                Action<string> logWarning)
            {
                Config = config;
                AuthoringBuildingsRoot = authoringBuildingsRoot;
                RuntimeSpawnSystem = runtimeSpawnSystem;
                RuntimeSpawnContext = runtimeSpawnContext;
                TryGetGridData = tryGetGridData;
                LogWarning = logWarning;
            }
        }

        private bool _queued;
        private bool _authoringHidden;
        private bool _warnedMissingConfig;
        private int _nextPlacementIndex;
        private bool _warnedFailedPlacement;
        private bool _priorityPlayerProductionQueued;
        private readonly HashSet<int> _spawnedPlacementIndices = new();

        public bool IsComplete => _queued && _authoringHidden;

        public void Update(Context context)
        {
            if (context.Config == null || !context.Config.SpawnOnMatchStart || IsComplete)
                return;

            if (_queued)
            {
                HideAuthoringVisuals(context);
                return;
            }

            SpawnPlacements(context);
        }

        private void SpawnPlacements(Context context)
        {
            if (context.Config.Placements == null || context.Config.Placements.Count == 0)
            {
                WarnOnce(ref _warnedMissingConfig, context, "[MapBuildingPlacement] no baked map building placements configured.");
                _queued = true;
                return;
            }

            if (context.TryGetGridData == null || !context.TryGetGridData(out _, out GridConfig grid, out _, out _))
                return;

            if (!_priorityPlayerProductionQueued)
            {
                SpawnPriorityPlayerProductionPlacements(context, grid);
                _priorityPlayerProductionQueued = true;
            }

            int processed = 0;
            for (; _nextPlacementIndex < context.Config.Placements.Count && processed < MaxPlacementsPerUpdate; _nextPlacementIndex++, processed++)
            {
                if (_spawnedPlacementIndices.Contains(_nextPlacementIndex))
                    continue;

                TrySpawnPlacementAtIndex(context, grid, _nextPlacementIndex);
            }

            if (_nextPlacementIndex >= context.Config.Placements.Count)
            {
                _queued = true;
                HideAuthoringVisuals(context);
            }
        }

        private void SpawnPriorityPlayerProductionPlacements(Context context, GridConfig grid)
        {
            if (context.Config?.Placements == null)
                return;

            for (int i = 0; i < context.Config.Placements.Count; i++)
            {
                if (_spawnedPlacementIndices.Contains(i))
                    continue;

                MapBuildingPlacementConfigEntry placement = context.Config.Placements[i];
                if (!IsPlayerProductionPlacement(context, placement))
                    continue;

                if (TrySpawnPlacementAtIndex(context, grid, i))
                    _spawnedPlacementIndices.Add(i);
            }
        }

        private bool TrySpawnPlacementAtIndex(Context context, GridConfig grid, int placementIndex)
        {
            MapBuildingPlacementConfigEntry placement = context.Config.Placements[placementIndex];
            if (placement == null || placement.BuildingPrefab == null)
                return false;

            if (!context.RuntimeSpawnSystem.TryGetRuntimeBuildingPlacementFootprint(
                    context.RuntimeSpawnContext,
                    placement.BuildingPrefab,
                    placement.RotateVertical,
                    out Vector2Int footprint))
            {
                context.LogWarning?.Invoke($"[MapBuildingPlacement] skipped {placement.SourcePath}: could not resolve footprint for {placement.BuildingPrefab.name}.");
                return false;
            }

            Vector3 worldCenter = placement.WorldCenter;
            int2 centerCell = GridUtils.WorldToCell(grid, new float3(worldCenter.x, worldCenter.y, worldCenter.z));
            int2 originCell = CenterCellToOrigin(centerCell, footprint, grid);
            if (TrySpawnAuthoredPlacement(context, placement, new Vector2Int(originCell.x, originCell.y), footprint))
                return true;

            WarnOnce(
                ref _warnedFailedPlacement,
                context,
                $"[MapBuildingPlacement] at least one authored building failed to register. First failed source={placement.SourcePath} prefab={placement.BuildingPrefab.name}.");
            return false;
        }

        private static bool IsPlayerProductionPlacement(Context context, MapBuildingPlacementConfigEntry placement)
        {
            if (placement == null ||
                placement.FactionId != FactionIdentity.PlayerFactionId ||
                placement.BuildingPrefab == null ||
                context.RuntimeSpawnContext.DefinitionSystem == null)
            {
                return false;
            }

            BuildingDefinition definition = context.RuntimeSpawnContext.DefinitionSystem.CreateRuntimeBuildingDefinition(
                placement.BuildingPrefab,
                placement.BuildingPrefab.name,
                "Authored map building.",
                new Vector2Int(10, 10),
                500,
                context.RuntimeSpawnContext.RunwaySystem);
            return BuildingDefinitionPrefabSystemHelper.GetProductionCount(definition) > 0;
        }

        private static bool TrySpawnAuthoredPlacement(
            Context context,
            MapBuildingPlacementConfigEntry placement,
            Vector2Int originCell,
            Vector2Int footprint)
        {
            BuildingRuntimeSpawnCompositionSystemHelper.Context spawnContext = context.RuntimeSpawnContext;
            if (spawnContext.DefinitionSystem == null ||
                spawnContext.RegisterRuntimeBuilding == null)
            {
                return false;
            }

            BuildingDefinition definition = spawnContext.DefinitionSystem.CreateRuntimeBuildingDefinition(
                placement.BuildingPrefab,
                placement.BuildingPrefab.name,
                "Authored map building.",
                footprint,
                500,
                spawnContext.RunwaySystem);
            GameObject instance = CreateAuthoredMapVisualInstance(context, placement, spawnContext.BuildingRoot);
            if (instance == null)
                return false;

            RuntimeBuildingEntity building = spawnContext.RegisterRuntimeBuilding(
                BuildingRuntimeSpawnCompositionSystemHelper.CloneDefinitionWithFootprint(definition, footprint),
                instance,
                originCell,
                true);
            if (building == null)
                return false;

            spawnContext.SetRuntimeBuildingOwnerFaction?.Invoke(building, placement.FactionId);
            return true;
        }

        private static GameObject CreateAuthoredMapVisualInstance(
            Context context,
            MapBuildingPlacementConfigEntry placement,
            Transform parent)
        {
            if (placement == null || placement.BuildingPrefab == null)
                return null;

            if (!TryResolveAuthoringTransform(context.AuthoringBuildingsRoot, placement, out Transform source))
            {
                context.LogWarning?.Invoke($"[MapBuildingPlacement] skipped {placement.SourcePath}: could not resolve authored map visual.");
                return null;
            }

            GameObject wrapper = new GameObject($"{placement.BuildingPrefab.name}_MapVisualRoot");
            wrapper.transform.SetParent(parent, false);
            wrapper.transform.SetPositionAndRotation(placement.WorldPosition, Quaternion.Euler(placement.WorldEulerAngles));
            wrapper.transform.localScale = placement.WorldScale;
            wrapper.AddComponent<MapAuthoredBuildingVisualComponent>();

            GameObject visual = UnityEngine.Object.Instantiate(source.gameObject, wrapper.transform);
            visual.name = source.name;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            visual.SetActive(true);

            return wrapper;
        }

        private static bool TryResolveAuthoringTransform(
            Transform authoringRoot,
            MapBuildingPlacementConfigEntry placement,
            out Transform source)
        {
            source = null;
            if (authoringRoot == null || placement == null)
                return false;

            string sourcePath = placement.SourcePath;
            if (!string.IsNullOrEmpty(sourcePath))
            {
                string[] segments = sourcePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                int startIndex = 0;
                for (int i = 0; i < segments.Length; i++)
                {
                    if (string.Equals(segments[i], authoringRoot.name, StringComparison.Ordinal))
                    {
                        startIndex = i + 1;
                        break;
                    }
                }

                Transform current = authoringRoot;
                bool resolved = true;
                for (int i = startIndex; i < segments.Length; i++)
                {
                    current = FindDirectChildByName(current, segments[i]);
                    if (current == null)
                    {
                        resolved = false;
                        break;
                    }
                }

                if (resolved && current != null)
                {
                    source = current;
                    return true;
                }
            }

            if (!string.IsNullOrEmpty(placement.Category))
            {
                Transform category = FindDirectChildByName(authoringRoot, placement.Category);
                string leafName = GetLeafName(sourcePath);
                if (category != null && !string.IsNullOrEmpty(leafName) && TryFindDescendantByName(category, leafName, out source))
                {
                    return true;
                }
            }

            return !string.IsNullOrEmpty(sourcePath) &&
                TryFindDescendantByName(authoringRoot, GetLeafName(sourcePath), out source);
        }

        private static Transform FindDirectChildByName(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrEmpty(childName))
                return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    return child;
                }
            }

            return null;
        }

        private static bool TryFindDescendantByName(Transform root, string childName, out Transform result)
        {
            result = null;
            if (root == null || string.IsNullOrEmpty(childName))
                return false;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    result = child;
                    return true;
                }

                if (TryFindDescendantByName(child, childName, out result))
                    return true;
            }

            return false;
        }

        private static string GetLeafName(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
                return string.Empty;

            int index = sourcePath.LastIndexOf('/');
            return index >= 0 && index + 1 < sourcePath.Length
                ? sourcePath.Substring(index + 1)
                : sourcePath;
        }

        private void HideAuthoringVisuals(Context context)
        {
            if (_authoringHidden || !context.Config.HideAuthoringVisualsAfterSpawn || context.AuthoringBuildingsRoot == null)
                return;

            context.AuthoringBuildingsRoot.gameObject.SetActive(false);
            _authoringHidden = true;
        }

        private static int2 CenterCellToOrigin(int2 centerCell, Vector2Int footprint, GridConfig grid)
        {
            int originX = centerCell.x - Mathf.Max(0, footprint.x - 1) / 2;
            int originY = centerCell.y - Mathf.Max(0, footprint.y - 1) / 2;
            return new int2(
                Mathf.Clamp(originX, 0, Mathf.Max(0, grid.Width - footprint.x)),
                Mathf.Clamp(originY, 0, Mathf.Max(0, grid.Height - footprint.y)));
        }

        private static void WarnOnce(ref bool flag, Context context, string message)
        {
            if (flag)
                return;

            flag = true;
            context.LogWarning?.Invoke(message);
        }
    }
}
