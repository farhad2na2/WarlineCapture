using System;
using System.Collections.Generic;
using System.Globalization;
using Stopwatch = System.Diagnostics.Stopwatch;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Rendering
{
    public sealed class StaticMapChunkBatchingPresentationSystemHelper
    {
        private const string ForceMobileStaticDetailCullingEnv = "WARLINE_FORCE_MOBILE_STATIC_DETAIL_CULLING";

        private static readonly ProfilerMarker InitializeMarker = new("StaticMapBatching.Initialize");
        private static readonly ProfilerMarker RendererScanMarker = new("StaticMapBatching.RendererScan");
        private static readonly ProfilerMarker StaticDetailCullMarker = new("StaticMapBatching.StaticDetailCull");
        private static readonly ProfilerMarker SourceCollectionMarker = new("StaticMapBatching.SourceCollection");
        private static readonly ProfilerMarker BatchBuildMarker = new("StaticMapBatching.BatchBuild");

        private readonly List<RendererState> _disabledRenderers = new();
        private readonly List<Mesh> _combinedMeshes = new();
        private readonly List<SourceRenderer> _batchScratch = new(StaticMapChunkBatchingPolicy.MaxBatchRenderers);
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

        private readonly struct StaticDetailCullStats
        {
            public readonly int Renderers;
            public readonly long Triangles;

            public StaticDetailCullStats(int renderers, long triangles)
            {
                Renderers = renderers;
                Triangles = triangles;
            }
        }

        public void Initialize(
            Transform mapRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            using (InitializeMarker.Auto())
            {
                if (_initialized)
                    return;

                if (mapRoot == null)
                    return;

                long initializeStartTicks = Stopwatch.GetTimestamp();
                Dispose();
                _initialized = true;
                _combinedRoot = EnsureCombinedRoot(mapRoot);

                long rendererScanStartTicks = Stopwatch.GetTimestamp();
                MeshRenderer[] renderers;
                using (RendererScanMarker.Auto())
                {
                    renderers = mapRoot.GetComponentsInChildren<MeshRenderer>(false);
                }
                double rendererScanMilliseconds = GetElapsedMilliseconds(rendererScanStartTicks);

                Dictionary<StaticMapChunkBatchKey, List<SourceRenderer>> batches = new();
                BatchStats stats = new();

                long staticDetailCullStartTicks = Stopwatch.GetTimestamp();
                StaticDetailCullStats staticDetailCullStats;
                using (StaticDetailCullMarker.Auto())
                {
                    staticDetailCullStats = CullMobileStaticDetails(
                        renderers,
                        mapRoot,
                        mapBuildingAuthoringRoot,
                        mapVehicleAuthoringRoot,
                        decorationRoot);
                }
                double staticDetailCullMilliseconds = GetElapsedMilliseconds(staticDetailCullStartTicks);

                long sourceCollectionStartTicks = Stopwatch.GetTimestamp();
                using (SourceCollectionMarker.Auto())
                {
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        MeshRenderer renderer = renderers[i];
                        if (!TryCollectSource(
                                renderer,
                                mapBuildingAuthoringRoot,
                                mapVehicleAuthoringRoot,
                                decorationRoot,
                                stats,
                                out SourceRenderer source,
                                out StaticMapChunkBatchKey key))
                        {
                            continue;
                        }

                        if (!batches.TryGetValue(key, out List<SourceRenderer> sources))
                        {
                            sources = new List<SourceRenderer>(StaticMapChunkBatchingPolicy.MaxBatchRenderers);
                            batches.Add(key, sources);
                        }

                        sources.Add(source);
                        stats.Eligible++;
                    }
                }
                double sourceCollectionMilliseconds = GetElapsedMilliseconds(sourceCollectionStartTicks);

                long batchBuildStartTicks = Stopwatch.GetTimestamp();
                using (BatchBuildMarker.Auto())
                {
                    foreach (KeyValuePair<StaticMapChunkBatchKey, List<SourceRenderer>> pair in batches)
                        BuildKeyBatches(pair.Key, pair.Value, stats);
                }
                double batchBuildMilliseconds = GetElapsedMilliseconds(batchBuildStartTicks);

                if (stats.Batches == 0 && _combinedRoot != null)
                {
                    DestroyObject(_combinedRoot.gameObject);
                    _combinedRoot = null;
                }

                double initializeMilliseconds = GetElapsedMilliseconds(initializeStartTicks);
                LogNoStackTrace(
                    $"[StaticMapBatching] result={(stats.Batches > 0 ? "Applied" : "Skipped")} " +
                    $"eligible={stats.Eligible} batches={stats.Batches} disabled={stats.Disabled} vertices={stats.CombinedVertices} " +
                    $"skippedUnreadable={stats.SkippedUnreadable} skippedUnsafe={stats.SkippedUnsafe} skippedLarge={stats.SkippedLarge} " +
                    $"skippedMaterial={stats.SkippedMaterial} skippedSmallBatch={stats.SkippedBatchTooSmall} " +
                    $"mobileStaticDetailCull={staticDetailCullStats.Renderers}r/{staticDetailCullStats.Triangles}tris " +
                    $"scannedRenderers={renderers.Length} initializeMs={FormatMilliseconds(initializeMilliseconds)} " +
                    $"rendererScanMs={FormatMilliseconds(rendererScanMilliseconds)} " +
                    $"staticDetailCullMs={FormatMilliseconds(staticDetailCullMilliseconds)} " +
                    $"sourceCollectionMs={FormatMilliseconds(sourceCollectionMilliseconds)} " +
                    $"batchBuildMs={FormatMilliseconds(batchBuildMilliseconds)}");
            }
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

        private StaticDetailCullStats CullMobileStaticDetails(
            MeshRenderer[] renderers,
            Transform mapRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            if (!ShouldApplyMobileStaticDetailCulling())
                return default;

            int culledRenderers = 0;
            long culledTriangles = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (!ShouldCullMobileStaticDetail(
                        renderer,
                        mapRoot,
                        mapBuildingAuthoringRoot,
                        mapVehicleAuthoringRoot,
                        decorationRoot))
                {
                    continue;
                }

                _disabledRenderers.Add(new RendererState
                {
                    Renderer = renderer,
                    WasEnabled = renderer.enabled
                });
                renderer.enabled = false;
                culledRenderers++;
                culledTriangles += CountRendererTriangles(renderer);
            }

            return new StaticDetailCullStats(culledRenderers, culledTriangles);
        }

        private static bool ShouldApplyMobileStaticDetailCulling()
        {
            if (Application.isMobilePlatform)
                return true;

            string forced = Environment.GetEnvironmentVariable(ForceMobileStaticDetailCullingEnv);
            return string.Equals(forced, "1", StringComparison.Ordinal) ||
                   string.Equals(forced, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldCullMobileStaticDetail(
            MeshRenderer renderer,
            Transform mapRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                return false;
            if (mapRoot != null && !renderer.transform.IsChildOf(mapRoot))
                return false;
            if (IsInRoot(renderer.transform, mapBuildingAuthoringRoot) ||
                IsInRoot(renderer.transform, mapVehicleAuthoringRoot) ||
                IsInRoot(renderer.transform, decorationRoot))
            {
                return false;
            }

            if (HasAncestorNamed(renderer.transform, "Clouds", mapRoot) ||
                HasAncestorNamed(renderer.transform, "_UnmappedVehicleSources", mapRoot))
            {
                return true;
            }

            string name = renderer.gameObject.name;
            return name.Contains("SM_Prop_BarrelPile", StringComparison.Ordinal) ||
                   name.Contains("SM_Prop_Shelves", StringComparison.Ordinal) ||
                   name.Contains("SM_Prop_Drone_Control_Room", StringComparison.Ordinal);
        }

        private static bool IsInRoot(Transform transform, Transform root)
        {
            return root != null && transform != null && transform.IsChildOf(root);
        }

        private static bool HasAncestorNamed(Transform transform, string name, Transform stopRoot)
        {
            for (Transform current = transform; current != null; current = current.parent)
            {
                if (string.Equals(current.name, name, StringComparison.Ordinal))
                    return true;
                if (current == stopRoot)
                    return false;
            }

            return false;
        }

        private static int CountRendererTriangles(MeshRenderer renderer)
        {
            MeshFilter meshFilter = renderer != null ? renderer.GetComponent<MeshFilter>() : null;
            Mesh mesh = meshFilter != null ? meshFilter.sharedMesh : null;
            if (mesh == null)
                return 0;

            int triangles = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                triangles += (int)(mesh.GetIndexCount(i) / 3);
            return triangles;
        }

        private bool TryCollectSource(
            MeshRenderer renderer,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot,
            BatchStats stats,
            out SourceRenderer source,
            out StaticMapChunkBatchKey key)
        {
            source = default;
            key = default;

            StaticMapChunkSourceEvaluation evaluation = StaticMapChunkBatchingPolicy.EvaluateSource(
                renderer,
                _combinedRoot,
                mapBuildingAuthoringRoot,
                mapVehicleAuthoringRoot,
                decorationRoot);
            switch (evaluation.Eligibility)
            {
                case StaticMapChunkSourceEligibility.UnreadableMesh:
                    stats.SkippedUnreadable++;
                    break;
                case StaticMapChunkSourceEligibility.Unsafe:
                    stats.SkippedUnsafe++;
                    break;
                case StaticMapChunkSourceEligibility.TooLarge:
                    stats.SkippedLarge++;
                    break;
                case StaticMapChunkSourceEligibility.UnsupportedMaterialLayout:
                    stats.SkippedMaterial++;
                    break;
            }

            if (!evaluation.IsEligible)
                return false;

            key = StaticMapChunkBatchingPolicy.CreateBatchKey(renderer, evaluation.Material);
            source = new SourceRenderer
            {
                Renderer = renderer,
                MeshFilter = evaluation.MeshFilter,
                Mesh = evaluation.Mesh,
                Material = evaluation.Material
            };
            return true;
        }

        private void BuildKeyBatches(StaticMapChunkBatchKey key, List<SourceRenderer> sources, BatchStats stats)
        {
            _batchScratch.Clear();
            int vertexCount = 0;
            for (int i = 0; i < sources.Count; i++)
            {
                SourceRenderer source = sources[i];
                int sourceVertices = source.Mesh.vertexCount;
                if (_batchScratch.Count > 0 &&
                    (_batchScratch.Count >= StaticMapChunkBatchingPolicy.MaxBatchRenderers ||
                     vertexCount + sourceVertices > StaticMapChunkBatchingPolicy.MaxBatchVertices))
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

        private void FlushBatch(StaticMapChunkBatchKey key, List<SourceRenderer> sources, BatchStats stats)
        {
            if (sources.Count < StaticMapChunkBatchingPolicy.MinBatchRenderers)
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
            Transform existing = mapRoot.Find(StaticMapChunkBatchingPolicy.CombinedRootName);
            if (existing != null)
                DestroyObject(existing.gameObject);

            GameObject root = new GameObject(StaticMapChunkBatchingPolicy.CombinedRootName)
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

        private static double GetElapsedMilliseconds(long startTicks)
        {
            return (Stopwatch.GetTimestamp() - startTicks) * 1000d / Stopwatch.Frequency;
        }

        private static string FormatMilliseconds(double milliseconds)
        {
            return milliseconds.ToString("F3", CultureInfo.InvariantCulture);
        }

        private static void LogNoStackTrace(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
        }
    }
}
