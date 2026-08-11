using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Runtime
{
    using CombinedRoadVisualData = RoadGridProjectionSystem.CombinedRoadVisualData;
    using DragFirstAxis = RoadPathPlanningUtilitySystemHelper.DragFirstAxis;
    using RoadVisualType = RoadNetworkCompositionSystemHelper.RoadVisualType;
    using TileConnectionMask = RoadNetworkCompositionSystemHelper.TileConnectionMask;
    using VariantData = RoadVisualVariantSystem.VariantData;

    public sealed class RoadPreviewPresentationSystemHelper : IDisposable
    {
        public const int DefaultPoolCapacity = 256;

        public delegate RoadVisualType ResolveVisualTypeAction(Vector2Int cell, TileConnectionMask mask);
        public delegate bool TryGetVariantAction(RoadVisualType type, TileConnectionMask mask, out VariantData variant);

        public readonly struct Context
        {
            public readonly Dictionary<RoadVisualType, CombinedRoadVisualData> VisualData;
            public readonly Transform RoadRoot;
            public readonly Vector3 GridOrigin;
            public readonly float BuildPlaneY;
            public readonly float RoadGridSize;
            public readonly float PreviewAlpha;
            public readonly GameObject EndPrefab;
            public readonly RoadPathPlanningUtilitySystemHelper PathPlanningSystem;
            public readonly RoadNetworkCompositionSystemHelper NetworkSystem;
            public readonly ResolveVisualTypeAction ResolveVisualType;
            public readonly TryGetVariantAction TryGetVariant;

            public Context(
                Dictionary<RoadVisualType, CombinedRoadVisualData> visualData,
                Transform roadRoot,
                Vector3 gridOrigin,
                float buildPlaneY,
                float roadGridSize,
                float previewAlpha,
                GameObject endPrefab,
                RoadPathPlanningUtilitySystemHelper pathPlanningSystem,
                RoadNetworkCompositionSystemHelper networkSystem,
                ResolveVisualTypeAction resolveVisualType,
                TryGetVariantAction tryGetVariant)
            {
                VisualData = visualData;
                RoadRoot = roadRoot;
                GridOrigin = gridOrigin;
                BuildPlaneY = buildPlaneY;
                RoadGridSize = roadGridSize;
                PreviewAlpha = previewAlpha;
                EndPrefab = endPrefab;
                PathPlanningSystem = pathPlanningSystem;
                NetworkSystem = networkSystem;
                ResolveVisualType = resolveVisualType;
                TryGetVariant = tryGetVariant;
            }
        }

        private readonly List<GameObject> _previewObjects = new();
        private readonly Dictionary<RoadVisualType, Stack<GameObject>> _previewPool = new();
        private readonly Dictionary<GameObject, RoadVisualType> _previewObjectTypes = new();
        private readonly Dictionary<GameObject, Material[]> _previewOwnedMaterials = new();
        private readonly int _poolCapacity;
        private int _pooledObjectCount;
        private int _createdObjectCount;
        private int _destroyedObjectCount;
        private bool _disposed;

        public RoadPreviewPresentationSystemHelper(int poolCapacity = DefaultPoolCapacity)
        {
            if (poolCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(poolCapacity), "Pool capacity must be non-negative.");

            _poolCapacity = poolCapacity;
        }

        public int PoolCapacity => _poolCapacity;
        public int ActiveObjectCount => _previewObjects.Count;
        public int PooledObjectCount => _pooledObjectCount;
        public int RetainedObjectCount => _previewObjectTypes.Count;
        public int CreatedObjectCount => _createdObjectCount;
        public int DestroyedObjectCount => _destroyedObjectCount;
        public bool IsDisposed => _disposed;

        public void DisposePreview()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            ClearPreview();

            foreach (var pool in _previewPool.Values)
            {
                while (pool.Count > 0)
                {
                    GameObject preview = pool.Pop();
                    _pooledObjectCount--;
                    DestroyOwnedPreviewObject(preview);
                }
            }

            _previewPool.Clear();
            _previewObjectTypes.Clear();
            _previewOwnedMaterials.Clear();
            _pooledObjectCount = 0;
            _disposed = true;
        }

        public void ClearPreview()
        {
            for (int i = 0; i < _previewObjects.Count; i++)
            {
                if (_previewObjects[i] != null)
                    ReleasePreviewObject(_previewObjects[i]);
            }

            _previewObjects.Clear();
        }

        public void UpdatePreview(
            Context context,
            bool isDrawing,
            Vector2Int? pendingStartCell,
            Vector2Int currentDragCell,
            DragFirstAxis dragFirstAxis)
        {
            if (!isDrawing || !pendingStartCell.HasValue)
            {
                ClearPreview();
                return;
            }

            RebuildPreview(context, pendingStartCell.Value, currentDragCell, dragFirstAxis);
        }

        private void RebuildPreview(Context context, Vector2Int startCell, Vector2Int endCell, DragFirstAxis dragFirstAxis)
        {
            ClearPreview();

            RoadPathPlanningUtilitySystemHelper.PreviewPlan previewPlan = context.PathPlanningSystem.BuildPreviewPlan(
                startCell,
                endCell,
                dragFirstAxis,
                context.NetworkSystem);
            List<Vector2Int> path = previewPlan.Path;
            if (path.Count == 0)
                return;

            if (path.Count == 1)
            {
                TileConnectionMask defaultMask = new(false, true, false, false);
                if (context.TryGetVariant(RoadVisualType.End, defaultMask, out var defaultVariant) && context.EndPrefab != null)
                {
                    GameObject preview = GetPreviewObject(context, RoadVisualType.End);
                    if (preview == null)
                        return;

                    preview.name = $"End_Preview_{startCell.x}_{startCell.y}";
                    ApplyPlacement(context, preview.transform, startCell, defaultVariant);
                    _previewObjects.Add(preview);
                }

                return;
            }

            foreach (var cell in previewPlan.DirtyCells)
            {
                TileConnectionMask mask = context.PathPlanningSystem.GetPreviewMask(cell, previewPlan.ProposedEdges, context.NetworkSystem);
                RoadVisualType type = context.ResolveVisualType(cell, mask);
                if (type == RoadVisualType.None || !context.TryGetVariant(type, mask, out var variant))
                    continue;

                GameObject preview = GetPreviewObject(context, type);
                if (preview == null)
                    continue;

                preview.name = $"{type}_Preview_{cell.x}_{cell.y}";
                ApplyPlacement(context, preview.transform, cell, variant);
                _previewObjects.Add(preview);
            }
        }

        private GameObject GetPreviewObject(Context context, RoadVisualType type)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RoadPreviewPresentationSystemHelper));

            if (_previewPool.TryGetValue(type, out var pool))
            {
                while (pool.Count > 0)
                {
                    GameObject pooled = pool.Pop();
                    _pooledObjectCount--;
                    if (pooled == null)
                    {
                        DestroyOwnedPreviewObject(pooled);
                        continue;
                    }

                    pooled.SetActive(true);
                    return pooled;
                }
            }

            GameObject preview = CreateRuntimeRoadObject(context, type);
            if (preview != null)
            {
                _previewObjectTypes[preview] = type;
                _createdObjectCount++;
            }

            return preview;
        }

        private void ReleasePreviewObject(GameObject preview)
        {
            if (ReferenceEquals(preview, null))
                return;

            if (preview == null)
            {
                DestroyOwnedPreviewObject(preview);
                return;
            }

            preview.SetActive(false);

            if (!_previewObjectTypes.TryGetValue(preview, out var type))
            {
                DestroyRuntimeObject(preview);
                return;
            }

            if (_pooledObjectCount >= _poolCapacity)
            {
                DestroyOwnedPreviewObject(preview);
                return;
            }

            if (!_previewPool.TryGetValue(type, out var pool))
            {
                pool = new Stack<GameObject>();
                _previewPool.Add(type, pool);
            }

            pool.Push(preview);
            _pooledObjectCount++;
        }

        private GameObject CreateRuntimeRoadObject(Context context, RoadVisualType type)
        {
            if (!context.VisualData.TryGetValue(type, out var visualData) ||
                visualData.Mesh == null ||
                visualData.Materials == null)
            {
                return null;
            }

            GameObject roadObject = new($"{type}_Preview");
            roadObject.transform.SetParent(context.RoadRoot, false);

            var meshFilter = roadObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = visualData.Mesh;

            var meshRenderer = roadObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterials = visualData.Materials;
            _previewOwnedMaterials[roadObject] = SetPreviewMaterials(meshRenderer, context.PreviewAlpha);

            return roadObject;
        }

        private static Material[] SetPreviewMaterials(Renderer renderer, float alpha)
        {
            var materials = renderer.sharedMaterials;
            var previewMaterials = new Material[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                {
                    previewMaterials[i] = null;
                    continue;
                }

                previewMaterials[i] = new Material(materials[i]);
                if (previewMaterials[i].HasProperty("_Color"))
                {
                    Color color = previewMaterials[i].color;
                    color.a = alpha;
                    previewMaterials[i].color = color;
                }
            }

            renderer.sharedMaterials = previewMaterials;
            return previewMaterials;
        }

        private void DestroyOwnedPreviewObject(GameObject preview)
        {
            if (ReferenceEquals(preview, null))
                return;

            if (_previewOwnedMaterials.Remove(preview, out Material[] materials))
            {
                for (int i = 0; i < materials.Length; i++)
                    DestroyRuntimeObject(materials[i]);
            }

            _previewObjectTypes.Remove(preview);
            if (preview != null)
                DestroyRuntimeObject(preview);
            _destroyedObjectCount++;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        private static void ApplyPlacement(Context context, Transform target, Vector2Int cell, VariantData variant)
        {
            target.SetPositionAndRotation(
                GetPlacementPosition(context, cell, variant),
                variant.Rotation);
            target.localScale = variant.Scale;
        }

        private static Vector3 GetPlacementPosition(Context context, Vector2Int cell, VariantData variant)
        {
            Vector3 basePosition = context.GridOrigin + new Vector3(
                cell.x * context.RoadGridSize,
                context.BuildPlaneY,
                cell.y * context.RoadGridSize);
            Vector3[] corners =
            {
                new(0f, 0f, 0f),
                new(context.RoadGridSize, 0f, 0f),
                new(0f, 0f, context.RoadGridSize),
                new(context.RoadGridSize, 0f, context.RoadGridSize)
            };

            float minX = float.PositiveInfinity;
            float minZ = float.PositiveInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 transformed = variant.Rotation * Vector3.Scale(corners[i], variant.Scale);
                if (transformed.x < minX)
                    minX = transformed.x;
                if (transformed.z < minZ)
                    minZ = transformed.z;
            }

            return basePosition - new Vector3(minX, 0f, minZ);
        }
    }
}
