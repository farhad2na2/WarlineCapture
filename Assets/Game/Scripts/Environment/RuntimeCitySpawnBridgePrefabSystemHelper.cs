using System.Collections.Generic;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class RuntimeCitySpawnBridgePrefabSystemHelper
    {
        private readonly RuntimeCitySpawnBridgeState _state = new();

        public RuntimeCitySpawnBridgeState State => _state;

        public bool HasSpawnSystem => _state.HasSpawnSystem;
        public int VisualSpawnCount => _state.VisualSpawnCount;

        public void Configure(
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper buildingRuntimeCitySpawnSystem,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context buildingRuntimeCitySpawnContext)
        {
            _state.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
        }

        public void ConfigureVisualOnly(RuntimeCityVisualPresentationSystemHelper visualSystem, GridConfig grid)
        {
            _state.ConfigureVisualOnly(visualSystem, grid);
        }

        public void ConfigurePlanOnly()
        {
            _state.ConfigurePlanOnly();
        }

        public void Clear()
        {
            _state.Clear();
        }

        public void BeginDeferredSideEffects()
        {
            _state.BeginDeferredSideEffects();
        }

        public void EndDeferredSideEffects()
        {
            _state.EndDeferredSideEffects();
        }

        public bool TrySpawnCityBuilding(
            GameObject prefab,
            Vector2Int preferredOrigin,
            out int buildingId,
            out Vector2Int actualOrigin,
            out Vector2Int actualFootprint,
            string fallbackDisplayName,
            string fallbackDescription,
            Vector2Int? fallbackFootprint,
            int fallbackMaxHealth,
            Quaternion? visualRotation = null)
        {
            return _state.TrySpawnCityBuilding(
                prefab,
                preferredOrigin,
                out buildingId,
                out actualOrigin,
                out actualFootprint,
                fallbackDisplayName,
                fallbackDescription,
                fallbackFootprint,
                fallbackMaxHealth,
                visualRotation);
        }

        public bool DeleteCityBuilding(int buildingId)
        {
            return _state.DeleteCityBuilding(buildingId);
        }
    }

    internal sealed class RuntimeCitySpawnBridgeState
    {
        private BuildingRuntimeCitySpawnBridgeCompositionSystemHelper _buildingRuntimeCitySpawnSystem;
        private BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context _buildingRuntimeCitySpawnContext;
        private RuntimeCityVisualPresentationSystemHelper _visualSystem;
        private GridConfig _visualGrid;
        private readonly Dictionary<int, GameObject> _visualBuildings = new();
        private int _nextVisualBuildingId = -1;
        private bool _planOnly;
        private int _plannedBuildingCount;

        public bool HasSpawnSystem => _buildingRuntimeCitySpawnSystem != null || _visualSystem != null || _planOnly;
        public int VisualSpawnCount => _visualBuildings.Count;
        public int PlannedBuildingCount => _plannedBuildingCount;

        public void Configure(
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper buildingRuntimeCitySpawnSystem,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context buildingRuntimeCitySpawnContext)
        {
            _buildingRuntimeCitySpawnSystem = buildingRuntimeCitySpawnSystem;
            _buildingRuntimeCitySpawnContext = buildingRuntimeCitySpawnContext;
            _visualSystem = null;
            _visualGrid = default;
            _planOnly = false;
            _plannedBuildingCount = 0;
        }

        public void ConfigureVisualOnly(RuntimeCityVisualPresentationSystemHelper visualSystem, GridConfig grid)
        {
            _buildingRuntimeCitySpawnSystem = null;
            _buildingRuntimeCitySpawnContext = default;
            _visualSystem = visualSystem;
            _visualGrid = grid;
            _nextVisualBuildingId = -1;
            _planOnly = false;
            _plannedBuildingCount = 0;
        }

        public void ConfigurePlanOnly()
        {
            _buildingRuntimeCitySpawnSystem = null;
            _buildingRuntimeCitySpawnContext = default;
            _visualSystem = null;
            _visualGrid = default;
            _nextVisualBuildingId = -1;
            _planOnly = true;
            _plannedBuildingCount = 0;
        }

        public void Clear()
        {
            foreach (GameObject visual in _visualBuildings.Values)
                DestroyVisual(visual);
            _visualBuildings.Clear();
            _buildingRuntimeCitySpawnSystem = null;
            _buildingRuntimeCitySpawnContext = default;
            _visualSystem = null;
            _visualGrid = default;
            _nextVisualBuildingId = -1;
            _planOnly = false;
            _plannedBuildingCount = 0;
        }

        public void BeginDeferredSideEffects()
        {
            _buildingRuntimeCitySpawnSystem?.BeginDeferredSideEffects(_buildingRuntimeCitySpawnContext);
        }

        public void EndDeferredSideEffects()
        {
            _buildingRuntimeCitySpawnSystem?.EndDeferredSideEffects(_buildingRuntimeCitySpawnContext);
        }

        public bool TrySpawnCityBuilding(
            GameObject prefab,
            Vector2Int preferredOrigin,
            out int buildingId,
            out Vector2Int actualOrigin,
            out Vector2Int actualFootprint,
            string fallbackDisplayName,
            string fallbackDescription,
            Vector2Int? fallbackFootprint,
            int fallbackMaxHealth,
            Quaternion? visualRotation = null)
        {
            buildingId = 0;
            actualOrigin = default;
            actualFootprint = default;
            if (_buildingRuntimeCitySpawnSystem != null)
            {
                return _buildingRuntimeCitySpawnSystem.TrySpawnRuntimeBuilding(
                    _buildingRuntimeCitySpawnContext,
                    prefab,
                    preferredOrigin,
                    out buildingId,
                    out actualOrigin,
                    out actualFootprint,
                    fallbackDisplayName,
                    fallbackDescription,
                    fallbackFootprint,
                    fallbackMaxHealth);
            }

            if (_visualSystem == null || prefab == null)
            {
                if (!_planOnly || prefab == null)
                    return false;

                actualOrigin = preferredOrigin;
                actualFootprint = fallbackFootprint ?? Vector2Int.one;
                actualFootprint.x = Mathf.Max(1, actualFootprint.x);
                actualFootprint.y = Mathf.Max(1, actualFootprint.y);
                buildingId = _nextVisualBuildingId--;
                _plannedBuildingCount++;
                return true;
            }

            actualOrigin = preferredOrigin;
            Vector2Int baseFootprint = fallbackFootprint ?? Vector2Int.one;
            baseFootprint.x = Mathf.Max(1, baseFootprint.x);
            baseFootprint.y = Mathf.Max(1, baseFootprint.y);
            Quaternion rotation = visualRotation ?? Quaternion.identity;
            actualFootprint = baseFootprint;
            if (SwapsHorizontalAxes(rotation))
            {
                actualFootprint = new Vector2Int(baseFootprint.y, baseFootprint.x);
                actualOrigin += new Vector2Int(
                    Mathf.FloorToInt((baseFootprint.x - actualFootprint.x) * 0.5f),
                    Mathf.FloorToInt((baseFootprint.y - actualFootprint.y) * 0.5f));
            }
            actualFootprint.x = Mathf.Max(1, actualFootprint.x);
            actualFootprint.y = Mathf.Max(1, actualFootprint.y);
            GameObject visual = _visualSystem.SpawnVisualOnlyPrefab(
                prefab,
                actualOrigin,
                actualFootprint,
                rotation,
                _visualGrid);
            if (visual == null)
                return false;

            buildingId = _nextVisualBuildingId--;
            _visualBuildings.Add(buildingId, visual);
            return true;
        }

        private static bool SwapsHorizontalAxes(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            return Mathf.Abs(forward.x) > Mathf.Abs(forward.z);
        }

        public bool DeleteCityBuilding(int buildingId)
        {
            if (_buildingRuntimeCitySpawnSystem != null)
                return _buildingRuntimeCitySpawnSystem.DeleteBuildingById(_buildingRuntimeCitySpawnContext, buildingId);

            if (_planOnly && buildingId < 0)
            {
                _plannedBuildingCount = Mathf.Max(0, _plannedBuildingCount - 1);
                return true;
            }

            if (!_visualBuildings.TryGetValue(buildingId, out GameObject visual))
                return false;

            _visualBuildings.Remove(buildingId);
            DestroyVisual(visual);
            return true;
        }

        private static void DestroyVisual(GameObject visual)
        {
            if (visual == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(visual);
            else
                Object.DestroyImmediate(visual);
        }
    }
}
