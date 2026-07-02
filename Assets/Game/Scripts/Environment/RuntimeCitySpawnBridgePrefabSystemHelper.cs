using UnityEngine;

namespace Game.Runtime
{
    internal sealed class RuntimeCitySpawnBridgePrefabSystemHelper
    {
        private readonly RuntimeCitySpawnBridgeState _state = new();

        public RuntimeCitySpawnBridgeState State => _state;

        public bool HasSpawnSystem => _state.HasSpawnSystem;

        public void Configure(
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper buildingRuntimeCitySpawnSystem,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context buildingRuntimeCitySpawnContext)
        {
            _state.Configure(buildingRuntimeCitySpawnSystem, buildingRuntimeCitySpawnContext);
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
            int fallbackMaxHealth)
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
                fallbackMaxHealth);
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

        public bool HasSpawnSystem => _buildingRuntimeCitySpawnSystem != null;

        public void Configure(
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper buildingRuntimeCitySpawnSystem,
            BuildingRuntimeCitySpawnBridgeCompositionSystemHelper.Context buildingRuntimeCitySpawnContext)
        {
            _buildingRuntimeCitySpawnSystem = buildingRuntimeCitySpawnSystem;
            _buildingRuntimeCitySpawnContext = buildingRuntimeCitySpawnContext;
        }

        public void Clear()
        {
            _buildingRuntimeCitySpawnSystem = null;
            _buildingRuntimeCitySpawnContext = default;
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
            int fallbackMaxHealth)
        {
            buildingId = 0;
            actualOrigin = default;
            actualFootprint = default;
            return _buildingRuntimeCitySpawnSystem != null &&
                _buildingRuntimeCitySpawnSystem.TrySpawnRuntimeBuilding(
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

        public bool DeleteCityBuilding(int buildingId)
        {
            return _buildingRuntimeCitySpawnSystem != null &&
                _buildingRuntimeCitySpawnSystem.DeleteBuildingById(_buildingRuntimeCitySpawnContext, buildingId);
        }
    }
}
