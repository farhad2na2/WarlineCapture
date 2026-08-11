using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class RuntimeCityRoadBuildBridgeCompositionSystemHelper
    {
        private readonly RuntimeCityRoadBuildBridgeState _state = new();

        public RuntimeCityRoadBuildBridgeState State => _state;

        public bool HasRoadRuntimeGenerationCompositionSystemHelper => _state.HasRoadRuntimeGenerationCompositionSystemHelper;

        public void Configure(
            RoadRuntimeGenerationCompositionSystemHelper roadRuntimeGenerationHelper,
            RoadRuntimeGenerationCompositionSystemHelper.Context roadRuntimeGenerationContext,
            World queryWorld)
        {
            _state.Configure(roadRuntimeGenerationHelper, roadRuntimeGenerationContext, queryWorld);
        }

        public void Clear()
        {
            _state.Clear();
        }

        public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
        {
            return _state.TryGetRoadCellSizeInGridCells(out roadCellSizeInGridCells);
        }

        public void BeginDeferredRoadEcsSync()
        {
            _state.BeginDeferredRoadEcsSync();
        }

        public void EndDeferredRoadEcsSync()
        {
            _state.EndDeferredRoadEcsSync();
        }

        public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
        {
            return _state.CreateRoadStrokeFromRoadCells(cells);
        }

        public bool CreateAutobahnStrokeFromRoadCells(
            IReadOnlyList<Vector2Int> cells,
            bool useAutobahnConnectorAtStart,
            bool useAutobahnConnectorAtEnd)
        {
            return _state.CreateAutobahnStrokeFromRoadCells(
                cells,
                useAutobahnConnectorAtStart,
                useAutobahnConnectorAtEnd);
        }

        public bool CreateStandaloneStraightRoadChainFromConnector(
            Vector2Int connectorCell,
            Vector2Int direction,
            int length)
        {
            return _state.CreateStandaloneStraightRoadChainFromConnector(connectorCell, direction, length);
        }

        public bool TryGetStandaloneStraightChainEndRoadCell(
            Vector2Int direction,
            out Vector2Int roadConnectionCell)
        {
            return _state.TryGetStandaloneStraightChainEndRoadCell(direction, out roadConnectionCell);
        }
    }

    internal sealed class RuntimeCityRoadBuildBridgeState
    {
        private const int DefaultRoadCellSizeInGridCells = 10;
        private const string RoadCellSizeFallbackFixTag = "RuntimeCityRoadCellFallbackFix_2026-05-26";
        private RoadRuntimeGenerationCompositionSystemHelper _roadRuntimeGenerationHelper;
        private RoadRuntimeGenerationCompositionSystemHelper.Context _roadRuntimeGenerationContext;
        private World _queryWorld;

        public bool HasRoadRuntimeGenerationCompositionSystemHelper => _roadRuntimeGenerationHelper != null;

        public void Configure(
            RoadRuntimeGenerationCompositionSystemHelper roadRuntimeGenerationHelper,
            RoadRuntimeGenerationCompositionSystemHelper.Context roadRuntimeGenerationContext,
            World queryWorld)
        {
            _roadRuntimeGenerationHelper = roadRuntimeGenerationHelper;
            _roadRuntimeGenerationContext = roadRuntimeGenerationContext;
            _queryWorld = queryWorld;
        }

        public void Clear()
        {
            _roadRuntimeGenerationHelper = null;
            _roadRuntimeGenerationContext = default;
            _queryWorld = null;
        }

        public bool TryGetRoadCellSizeInGridCells(out int roadCellSizeInGridCells)
        {
            roadCellSizeInGridCells = 0;
            if (_roadRuntimeGenerationHelper != null &&
                _roadRuntimeGenerationHelper.TryGetRoadCellSizeInGridCells(
                    _roadRuntimeGenerationContext,
                    out roadCellSizeInGridCells))
            {
                return true;
            }

            if (TryGetGridCellSize(out float gridCellSize) && gridCellSize > 0f)
            {
                roadCellSizeInGridCells = Mathf.Max(1, Mathf.RoundToInt(DefaultRoadCellSizeInGridCells / gridCellSize));
                Debug.LogWarning($"[RuntimeCity] {RoadCellSizeFallbackFixTag} fallback={roadCellSizeInGridCells} gridCellSize={gridCellSize:0.###}");
                return true;
            }

            Debug.LogWarning($"[RuntimeCity] {RoadCellSizeFallbackFixTag} unavailable=missingGridCellSize");
            return false;
        }

        public void BeginDeferredRoadEcsSync()
        {
            _roadRuntimeGenerationHelper?.BeginDeferredRoadEcsSync(_roadRuntimeGenerationContext);
        }

        public void EndDeferredRoadEcsSync()
        {
            _roadRuntimeGenerationHelper?.EndDeferredRoadEcsSync(_roadRuntimeGenerationContext);
        }

        public bool CreateRoadStrokeFromRoadCells(IReadOnlyList<Vector2Int> cells)
        {
            return _roadRuntimeGenerationHelper != null &&
                _roadRuntimeGenerationHelper.CreateRoadStrokeFromRoadCells(
                    _roadRuntimeGenerationContext,
                    cells);
        }

        public bool CreateAutobahnStrokeFromRoadCells(
            IReadOnlyList<Vector2Int> cells,
            bool useAutobahnConnectorAtStart,
            bool useAutobahnConnectorAtEnd)
        {
            return _roadRuntimeGenerationHelper != null &&
                _roadRuntimeGenerationHelper.CreateAutobahnStrokeFromRoadCells(
                    _roadRuntimeGenerationContext,
                    cells,
                    useAutobahnConnectorAtStart,
                    useAutobahnConnectorAtEnd);
        }

        public bool CreateStandaloneStraightRoadChainFromConnector(
            Vector2Int connectorCell,
            Vector2Int direction,
            int length)
        {
            return _roadRuntimeGenerationHelper != null &&
                _roadRuntimeGenerationHelper.CreateStandaloneStraightRoadChainFromConnector(
                    _roadRuntimeGenerationContext,
                    connectorCell,
                    direction,
                    length);
        }

        public bool TryGetStandaloneStraightChainEndRoadCell(
            Vector2Int direction,
            out Vector2Int roadConnectionCell)
        {
            roadConnectionCell = default;
            return _roadRuntimeGenerationHelper != null &&
                _roadRuntimeGenerationHelper.TryGetStandaloneStraightChainEndRoadCell(
                    _roadRuntimeGenerationContext,
                    direction,
                    out roadConnectionCell);
        }

        private bool TryGetGridCellSize(out float cellSize)
        {
            cellSize = 0f;
            World world = _queryWorld;
            if (world == null || !world.IsCreated)
                return false;

            EntityManager entityManager = world.EntityManager;
            using EntityQuery query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<GridConfig>());
            if (query.IsEmptyIgnoreFilter)
                return false;

            cellSize = entityManager.GetComponentData<GridConfig>(query.GetSingletonEntity()).CellSize;
            return cellSize > 0f;
        }
    }
}
