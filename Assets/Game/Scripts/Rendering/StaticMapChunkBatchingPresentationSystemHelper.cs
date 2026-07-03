using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering
{
    public sealed class StaticMapChunkBatchingPresentationSystemHelper
    {
        private const string CombinedRootName = "RuntimeStaticMapBatches";
        private const float ChunkSize = 96f;
        private const float MaxSourceExtent = 80f;
        private const int MaxSourceVertices = 8000;
        private const int MaxBatchVertices = 55000;
        private const int MaxBatchRenderers = 64;
        private const int MinBatchRenderers = 2;

        private readonly List<RendererState> _disabledRenderers = new();
        private readonly List<Mesh> _combinedMeshes = new();
        private readonly List<SourceRenderer> _batchScratch = new(MaxBatchRenderers);
        private Transform _combinedRoot;
        private bool _initialized;

        private struct RendererState
        {
            public MeshRenderer Renderer;
            public bool WasEnabled;
        }

        private struct SourceRenderer
        {
            public MeshRenderer Renderer;
            public MeshFilter MeshFilter;
            public Mesh Mesh;
            public Material Material;
        }

        private readonly struct BatchKey : IEquatable<BatchKey>
        {
            public readonly int ChunkX;
            public readonly int ChunkZ;
            public readonly Material Material;
            public readonly int LightmapIndex;
            public readonly int Layer;
            public readonly ShadowCastingMode ShadowCastingMode;
            public readonly bool ReceiveShadows;
            public readonly LightProbeUsage LightProbeUsage;
            public readonly ReflectionProbeUsage ReflectionProbeUsage;

            public BatchKey(
                int chunkX,
                int chunkZ,
                Material material,
                int lightmapIndex,
                int layer,
                ShadowCastingMode shadowCastingMode,
                bool receiveShadows,
                LightProbeUsage lightProbeUsage,
                ReflectionProbeUsage reflectionProbeUsage)
            {
                ChunkX = chunkX;
                ChunkZ = chunkZ;
                Material = material;
                LightmapIndex = lightmapIndex;
                Layer = layer;
                ShadowCastingMode = shadowCastingMode;
                ReceiveShadows = receiveShadows;
                LightProbeUsage = lightProbeUsage;
                ReflectionProbeUsage = reflectionProbeUsage;
            }

            public bool Equals(BatchKey other)
            {
                return ChunkX == other.ChunkX &&
                       ChunkZ == other.ChunkZ &&
                       ReferenceEquals(Material, other.Material) &&
                       LightmapIndex == other.LightmapIndex &&
                       Layer == other.Layer &&
                       ShadowCastingMode == other.ShadowCastingMode &&
                       ReceiveShadows == other.ReceiveShadows &&
                       LightProbeUsage == other.LightProbeUsage &&
                       ReflectionProbeUsage == other.ReflectionProbeUsage;
            }

            public override bool Equals(object obj)
            {
                return obj is BatchKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = ChunkX;
                    hash = (hash * 397) ^ ChunkZ;
                    hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(Material);
                    hash = (hash * 397) ^ LightmapIndex;
                    hash = (hash * 397) ^ Layer;
                    hash = (hash * 397) ^ (int)ShadowCastingMode;
                    hash = (hash * 397) ^ (ReceiveShadows ? 1 : 0);
                    hash = (hash * 397) ^ (int)LightProbeUsage;
                    hash = (hash * 397) ^ (int)ReflectionProbeUsage;
                    return hash;
                }
            }
        }

        private sealed class BatchStats
        {
            public int Eligible;
            public int Disabled;
            public int Batches;
            public int CombinedVertices;
            public int SkippedUnreadable;
            public int SkippedUnsafe;
            public int SkippedLarge;
            public int SkippedMaterial;
            public int SkippedBatchTooSmall;
        }

        public void Initialize(
            Transform mapRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            if (_initialized)
                return;

            if (mapRoot == null)
                return;

            Dispose();
            _initialized = true;
            _combinedRoot = EnsureCombinedRoot(mapRoot);
            MeshRenderer[] renderers = mapRoot.GetComponentsInChildren<MeshRenderer>(false);
            Dictionary<BatchKey, List<SourceRenderer>> batches = new();
            BatchStats stats = new();

            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (!TryCollectSource(
                        renderer,
                        mapRoot,
                        mapBuildingAuthoringRoot,
                        mapVehicleAuthoringRoot,
                        decorationRoot,
                        stats,
                        out SourceRenderer source,
                        out BatchKey key))
                {
                    continue;
                }

                if (!batches.TryGetValue(key, out List<SourceRenderer> sources))
                {
                    sources = new List<SourceRenderer>(MaxBatchRenderers);
                    batches.Add(key, sources);
                }

                sources.Add(source);
                stats.Eligible++;
            }

            foreach (KeyValuePair<BatchKey, List<SourceRenderer>> pair in batches)
                BuildKeyBatches(pair.Key, pair.Value, stats);

            if (stats.Batches == 0 && _combinedRoot != null)
            {
                DestroyObject(_combinedRoot.gameObject);
                _combinedRoot = null;
            }

            LogNoStackTrace(
                $"[StaticMapBatching] result={(stats.Batches > 0 ? "Applied" : "Skipped")} " +
                $"eligible={stats.Eligible} batches={stats.Batches} disabled={stats.Disabled} vertices={stats.CombinedVertices} " +
                $"skippedUnreadable={stats.SkippedUnreadable} skippedUnsafe={stats.SkippedUnsafe} skippedLarge={stats.SkippedLarge} " +
                $"skippedMaterial={stats.SkippedMaterial} skippedSmallBatch={stats.SkippedBatchTooSmall}");
        }

        public void Dispose()
        {
            for (int i = 0; i < _disabledRenderers.Count; i++)
            {
                RendererState state = _disabledRenderers[i];
                if (state.Renderer != null)
                    state.Renderer.enabled = state.WasEnabled;
            }

            _disabledRenderers.Clear();

            if (_combinedRoot != null)
                DestroyObject(_combinedRoot.gameObject);
            _combinedRoot = null;

            for (int i = 0; i < _combinedMeshes.Count; i++)
            {
                if (_combinedMeshes[i] != null)
                    DestroyObject(_combinedMeshes[i]);
            }

            _combinedMeshes.Clear();
            _initialized = false;
        }

        private bool TryCollectSource(
            MeshRenderer renderer,
            Transform mapRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot,
            BatchStats stats,
            out SourceRenderer source,
            out BatchKey key)
        {
            source = default;
            key = default;

            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;
            if (_combinedRoot != null && renderer.transform.IsChildOf(_combinedRoot))
                return false;
            if (mapBuildingAuthoringRoot != null && renderer.transform.IsChildOf(mapBuildingAuthoringRoot))
                return false;
            if (mapVehicleAuthoringRoot != null && renderer.transform.IsChildOf(mapVehicleAuthoringRoot))
                return false;
            if (decorationRoot != null && renderer.transform.IsChildOf(decorationRoot))
                return false;
            if (renderer.isPartOfStaticBatch || renderer.GetComponentInParent<LODGroup>() != null)
            {
                stats.SkippedUnsafe++;
                return false;
            }

            if (!HasOnlySafeComponents(renderer.gameObject))
            {
                stats.SkippedUnsafe++;
                return false;
            }

            MeshFilter meshFilter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null || mesh.vertexCount == 0)
                return false;
            if (!mesh.isReadable)
            {
                stats.SkippedUnreadable++;
                return false;
            }
            if (mesh.vertexCount > MaxSourceVertices || IsLargeRenderer(renderer))
            {
                stats.SkippedLarge++;
                return false;
            }
            if (mesh.subMeshCount != 1 || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial == null)
            {
                stats.SkippedMaterial++;
                return false;
            }

            Vector3 center = renderer.bounds.center;
            int chunkX = Mathf.FloorToInt(center.x / ChunkSize);
            int chunkZ = Mathf.FloorToInt(center.z / ChunkSize);
            Material material = renderer.sharedMaterial;
            key = new BatchKey(
                chunkX,
                chunkZ,
                material,
                renderer.lightmapIndex,
                renderer.gameObject.layer,
                renderer.shadowCastingMode,
                renderer.receiveShadows,
                renderer.lightProbeUsage,
                renderer.reflectionProbeUsage);
            source = new SourceRenderer
            {
                Renderer = renderer,
                MeshFilter = meshFilter,
                Mesh = mesh,
                Material = material
            };
            return true;
        }

        private static bool HasOnlySafeComponents(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                    return false;
                if (component is Transform ||
                    component is MeshFilter ||
                    component is MeshRenderer ||
                    component is Collider)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool IsLargeRenderer(Renderer renderer)
        {
            Vector3 size = renderer.bounds.size;
            return size.x > MaxSourceExtent || size.y > MaxSourceExtent || size.z > MaxSourceExtent;
        }

        private void BuildKeyBatches(BatchKey key, List<SourceRenderer> sources, BatchStats stats)
        {
            _batchScratch.Clear();
            int vertexCount = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                SourceRenderer source = sources[i];
                int sourceVertices = source.Mesh.vertexCount;
                if (_batchScratch.Count > 0 &&
                    (_batchScratch.Count >= MaxBatchRenderers || vertexCount + sourceVertices > MaxBatchVertices))
                {
                    FlushBatch(key, _batchScratch, stats);
                    _batchScratch.Clear();
                    vertexCount = 0;
                }

                _batchScratch.Add(source);
                vertexCount += sourceVertices;
            }

            FlushBatch(key, _batchScratch, stats);
            _batchScratch.Clear();
        }

        private void FlushBatch(BatchKey key, List<SourceRenderer> sources, BatchStats stats)
        {
            if (sources.Count < MinBatchRenderers)
            {
                stats.SkippedBatchTooSmall += sources.Count;
                return;
            }

            Mesh combinedMesh = new Mesh
            {
                name = $"StaticMapBatch_{key.ChunkX}_{key.ChunkZ}_{stats.Batches}",
                hideFlags = HideFlags.DontSave
            };
            combinedMesh.indexFormat = IndexFormat.UInt32;

            CombineInstance[] combines = new CombineInstance[sources.Count];
            Matrix4x4 rootWorldToLocal = _combinedRoot.worldToLocalMatrix;
            for (int i = 0; i < sources.Count; i++)
            {
                SourceRenderer source = sources[i];
                combines[i] = new CombineInstance
                {
                    mesh = source.Mesh,
                    subMeshIndex = 0,
                    transform = rootWorldToLocal * source.MeshFilter.transform.localToWorldMatrix,
                    lightmapScaleOffset = source.Renderer.lightmapScaleOffset
                };
            }

            bool hasLightmapData = key.LightmapIndex >= 0;
            combinedMesh.CombineMeshes(combines, true, true, hasLightmapData);
            combinedMesh.RecalculateBounds();
            _combinedMeshes.Add(combinedMesh);

            SourceRenderer first = sources[0];
            GameObject batchObject = new GameObject(combinedMesh.name)
            {
                hideFlags = HideFlags.DontSave,
                layer = key.Layer
            };
            batchObject.transform.SetParent(_combinedRoot, false);
            batchObject.transform.localPosition = Vector3.zero;
            batchObject.transform.localRotation = Quaternion.identity;
            batchObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = batchObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = combinedMesh;

            MeshRenderer meshRenderer = batchObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = first.Material;
            meshRenderer.shadowCastingMode = key.ShadowCastingMode;
            meshRenderer.receiveShadows = key.ReceiveShadows;
            meshRenderer.lightProbeUsage = key.LightProbeUsage;
            meshRenderer.reflectionProbeUsage = key.ReflectionProbeUsage;
            meshRenderer.lightmapIndex = key.LightmapIndex;

            for (int i = 0; i < sources.Count; i++)
            {
                MeshRenderer renderer = sources[i].Renderer;
                _disabledRenderers.Add(new RendererState
                {
                    Renderer = renderer,
                    WasEnabled = renderer.enabled
                });
                renderer.enabled = false;
                stats.Disabled++;
                stats.CombinedVertices += sources[i].Mesh.vertexCount;
            }

            stats.Batches++;
        }

        private static Transform EnsureCombinedRoot(Transform mapRoot)
        {
            Transform existing = mapRoot.Find(CombinedRootName);
            if (existing != null)
                DestroyObject(existing.gameObject);

            GameObject root = new GameObject(CombinedRootName)
            {
                hideFlags = HideFlags.DontSave,
                layer = mapRoot.gameObject.layer
            };
            root.transform.SetParent(mapRoot, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root.transform;
        }

        private static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        private static void LogNoStackTrace(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        }
    }
}
