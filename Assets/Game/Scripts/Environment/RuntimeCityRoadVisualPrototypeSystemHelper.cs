using System.Collections.Generic;
using UnityEngine;
using Game.Components;

namespace Game.Runtime
{
    internal sealed class RuntimeCityRoadVisualPrototypeSystemHelper
    {
        private readonly struct RoadEdge
        {
            public RoadEdge(Vector2Int first, Vector2Int second)
            {
                if (first.x < second.x || (first.x == second.x && first.y <= second.y))
                {
                    First = first;
                    Second = second;
                }
                else
                {
                    First = second;
                    Second = first;
                }
            }

            public Vector2Int First { get; }
            public Vector2Int Second { get; }
        }

        private sealed class RoadEdgeComparer : IEqualityComparer<RoadEdge>
        {
            public bool Equals(RoadEdge left, RoadEdge right)
            {
                return left.First == right.First && left.Second == right.Second;
            }

            public int GetHashCode(RoadEdge edge)
            {
                unchecked
                {
                    return (edge.First.GetHashCode() * 397) ^ edge.Second.GetHashCode();
                }
            }
        }

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.right,
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.down
        };

        private readonly HashSet<Vector2Int> _spawnedRoadCells = new();
        private readonly HashSet<RoadEdge> _spawnedRoadEdges = new(new RoadEdgeComparer());
        private Transform _roadRoot;
        private Material _material;
        private Material _shoulderMaterial;
        private bool _ownsMaterial;
        private bool _ownsShoulderMaterial;
        private GridConfig _grid;
        private int _roadCellSizeInGridCells;
        private bool _createVisuals;

        public int StrokeCount { get; private set; }
        public int RoadCellCount => _spawnedRoadCells.Count;

        public void Configure(
            Transform runtimeRoot,
            GridConfig grid,
            int roadCellSizeInGridCells,
            Material material,
            Material shoulderMaterial,
            Color roadColor,
            Color shoulderColor,
            bool createVisuals = true,
            bool cloneSourceMaterials = true)
        {
            _grid = grid;
            _roadCellSizeInGridCells = Mathf.Max(1, roadCellSizeInGridCells);
            _createVisuals = createVisuals;

            if (_createVisuals)
            {
                _material = material != null && !cloneSourceMaterials
                    ? material
                    : material != null
                        ? CreateRuntimeMaterial(material, "RuntimeCityRoad_RnD", roadColor)
                        : CreateFallbackMaterial(roadColor);
                _ownsMaterial = _material != null && (material == null || cloneSourceMaterials);
                _shoulderMaterial = shoulderMaterial != null && !cloneSourceMaterials
                    ? shoulderMaterial
                    : shoulderMaterial != null
                        ? CreateRuntimeMaterial(shoulderMaterial, "RuntimeCityRoadShoulder_RnD", shoulderColor)
                        : CreateFallbackShoulderMaterial(shoulderColor);
                _ownsShoulderMaterial = _shoulderMaterial != null &&
                                        (shoulderMaterial == null || cloneSourceMaterials);
                var root = new GameObject("RuntimeCityRoadVisuals");
                _roadRoot = root.transform;
                _roadRoot.SetParent(runtimeRoot, false);
            }
        }

        public bool TryGetRoadCellSize(out int roadCellSizeInGridCells)
        {
            roadCellSizeInGridCells = _roadCellSizeInGridCells;
            return roadCellSizeInGridCells > 0;
        }

        public void CreateStroke(
            List<Vector2Int> cells,
            bool isAutobahn,
            bool useAutobahnConnectorAtStart,
            bool useAutobahnConnectorAtEnd)
        {
            if (cells == null || cells.Count == 0)
                return;

            StrokeCount++;
            float tileSize = _roadCellSizeInGridCells * _grid.CellSize;
            float roadWidth = tileSize * (isAutobahn ? 0.88f : 0.74f);
            float shoulderWidth = Mathf.Min(tileSize, roadWidth + Mathf.Max(1.2f, tileSize * 0.18f));
            var newCells = new List<Vector2Int>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                if (_spawnedRoadCells.Add(cell))
                    newCells.Add(cell);
            }

            if (!_createVisuals || _roadRoot == null)
                return;

            for (int i = 0; i < newCells.Count; i++)
                CreateRoadNode(newCells[i], tileSize, roadWidth, shoulderWidth);

            for (int i = 0; i < cells.Count; i++)
            {
                Vector2Int cell = cells[i];
                for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
                {
                    Vector2Int neighbor = cell + CardinalDirections[directionIndex];
                    if (!_spawnedRoadCells.Contains(neighbor))
                        continue;

                    var edge = new RoadEdge(cell, neighbor);
                    if (_spawnedRoadEdges.Add(edge))
                        CreateRoadConnection(edge, tileSize, roadWidth, shoulderWidth);
                }
            }
        }

        public void Dispose()
        {
            if (_roadRoot != null)
                DestroyObject(_roadRoot.gameObject);
            if (_ownsMaterial && _material != null)
                DestroyObject(_material);
            if (_ownsShoulderMaterial && _shoulderMaterial != null)
                DestroyObject(_shoulderMaterial);

            _spawnedRoadCells.Clear();
            _spawnedRoadEdges.Clear();
            _roadRoot = null;
            _material = null;
            _shoulderMaterial = null;
            _ownsMaterial = false;
            _ownsShoulderMaterial = false;
            _createVisuals = false;
            StrokeCount = 0;
        }

        private Vector3 GetRoadCellCenter(Vector2Int cell, float tileSize)
        {
            return new Vector3(
                _grid.Origin.x + (cell.x * tileSize) + (tileSize * 0.5f),
                _grid.Origin.y + 0.06f,
                _grid.Origin.z + (cell.y * tileSize) + (tileSize * 0.5f));
        }

        private void CreateRoadNode(Vector2Int cell, float tileSize, float roadWidth, float shoulderWidth)
        {
            Vector3 center = GetRoadCellCenter(cell, tileSize);
            CreateSlab(
                $"RoadShoulder_{cell.x}_{cell.y}",
                center + Vector3.down * 0.035f,
                new Vector3(shoulderWidth, 0.08f, shoulderWidth),
                _shoulderMaterial);
            CreateSlab(
                $"Road_{cell.x}_{cell.y}",
                center,
                new Vector3(roadWidth, 0.12f, roadWidth),
                _material);
        }

        private void CreateRoadConnection(RoadEdge edge, float tileSize, float roadWidth, float shoulderWidth)
        {
            Vector3 first = GetRoadCellCenter(edge.First, tileSize);
            Vector3 second = GetRoadCellCenter(edge.Second, tileSize);
            Vector3 center = (first + second) * 0.5f;
            bool horizontal = edge.First.x != edge.Second.x;
            float connectionLength = tileSize + 0.08f;
            Vector3 shoulderScale = horizontal
                ? new Vector3(connectionLength, 0.08f, shoulderWidth)
                : new Vector3(shoulderWidth, 0.08f, connectionLength);
            Vector3 roadScale = horizontal
                ? new Vector3(connectionLength, 0.12f, roadWidth)
                : new Vector3(roadWidth, 0.12f, connectionLength);
            string edgeName = $"{edge.First.x}_{edge.First.y}_{edge.Second.x}_{edge.Second.y}";
            CreateSlab("RoadShoulderLink_" + edgeName, center + Vector3.down * 0.035f, shoulderScale, _shoulderMaterial);
            CreateSlab("RoadLink_" + edgeName, center, roadScale, _material);
        }

        private void CreateSlab(string objectName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = objectName;
            slab.transform.SetParent(_roadRoot, false);
            slab.transform.position = position;
            slab.transform.localScale = scale;

            Collider collider = slab.GetComponent<Collider>();
            if (collider != null)
                DestroyObject(collider);

            MeshRenderer renderer = slab.GetComponent<MeshRenderer>();
            if (renderer != null && material != null)
                renderer.sharedMaterial = material;
        }

        private static Material CreateFallbackMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return null;

            var material = new Material(shader) { name = "RuntimeCityRoad_RnD" };
            ApplyColor(material, color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.18f);
            return material;
        }

        private static Material CreateFallbackShoulderMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                return null;

            var material = new Material(shader) { name = "RuntimeCityRoadShoulder_RnD" };
            ApplyColor(material, color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.05f);
            return material;
        }

        private static Material CreateRuntimeMaterial(Material source, string materialName, Color color)
        {
            var material = new Material(source)
            {
                name = materialName,
                enableInstancing = true
            };
            ApplyColor(material, color);
            return material;
        }

        private static void ApplyColor(Material material, Color color)
        {
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void DestroyObject(Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }
    }
}
