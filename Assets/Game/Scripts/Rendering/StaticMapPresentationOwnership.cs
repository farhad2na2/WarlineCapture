using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.Rendering
{
    public sealed class StaticMapPresentationOwnership
    {
        private const float BoundsTolerance = 0.005f;
        private readonly StaticMapChunkBatchingPresentationSystemHelper _legacyBatching = new();
        private readonly List<RendererState> _suppressed = new();
        private readonly List<MeshRenderer> _resolved = new();
        private readonly List<Material> _materials = new();
        private readonly List<Transform> _path = new();
        private readonly StringBuilder _pathBuilder = new(256);
        public bool UsingPresentation { get; private set; }
        public bool UsingLegacyFallback { get; private set; }
        public int SuppressedRendererCount => _suppressed.Count;
        public string Failure { get; private set; }

        private readonly struct RendererState
        {
            public readonly MeshRenderer Renderer;
            public readonly bool WasEnabled;

            public RendererState(MeshRenderer renderer)
            {
                Renderer = renderer;
                WasEnabled = renderer.enabled;
            }
        }

        public void Initialize(
            RuntimePlatform platform,
            StaticMapPresentationManifest manifest,
            Transform mapRoot,
            Transform mapBuildingAuthoringRoot,
            Transform mapVehicleAuthoringRoot,
            Transform decorationRoot)
        {
            Dispose();
            string error = null;
            if (platform == RuntimePlatform.Android &&
                TrySuppressCanonicalRenderers(manifest, mapRoot, out error))
            {
                UsingPresentation = true;
                Debug.Log($"[StaticMapPresentationOwnership] result=Presentation suppressed={_suppressed.Count}");
                return;
            }

            Failure = platform == RuntimePlatform.Android ? error : null;
            UsingLegacyFallback = true;
            _legacyBatching.Initialize(
                mapRoot,
                mapBuildingAuthoringRoot,
                mapVehicleAuthoringRoot,
                decorationRoot);
            Debug.Log(
                $"[StaticMapPresentationOwnership] result=LegacyFallback reason={Failure ?? "platform"}");
        }

        public void Dispose()
        {
            for (int i = 0; i < _suppressed.Count; i++)
            {
                RendererState state = _suppressed[i];
                if (state.Renderer != null)
                    state.Renderer.enabled = state.WasEnabled;
            }

            _suppressed.Clear();
            _resolved.Clear();
            _legacyBatching.Dispose();
            UsingPresentation = false;
            UsingLegacyFallback = false;
            Failure = null;
        }

        private bool TrySuppressCanonicalRenderers(
            StaticMapPresentationManifest manifest,
            Transform mapRoot,
            out string error)
        {
            _resolved.Clear();
            if (manifest == null || manifest.SchemaVersion != StaticMapPresentationManifest.CurrentSchemaVersion)
                return Fail("presentation manifest is missing or unsupported", out error);
            if (mapRoot == null)
                return Fail("map root or presentation sources are missing", out error);
            if (!ValidateManifestShape(manifest, out error))
                return false;

            MeshRenderer[] renderers = mapRoot.GetComponentsInChildren<MeshRenderer>(true);
            Dictionary<string, MeshRenderer> byPath = new(renderers.Length, StringComparer.Ordinal);
            Dictionary<Mesh, List<MeshRenderer>> byMesh = new();
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                string path = BuildHierarchyPath(renderer.transform, mapRoot);
                if (!byPath.TryAdd(path, renderer))
                    return Fail($"duplicate canonical renderer path: {path}", out error);
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                Mesh mesh = filter != null ? filter.sharedMesh : null;
                if (mesh == null)
                    continue;
                if (!byMesh.TryGetValue(mesh, out List<MeshRenderer> candidates))
                {
                    candidates = new List<MeshRenderer>();
                    byMesh.Add(mesh, candidates);
                }
                candidates.Add(renderer);
            }

            HashSet<string> manifestPaths = new(StringComparer.Ordinal);
            HashSet<MeshRenderer> assigned = new();
            for (int i = 0; i < manifest.Sources.Count; i++)
            {
                StaticMapPresentationSourceEntry source = manifest.Sources[i];
                string path = source.SourceHierarchyPath;
                if (!manifestPaths.Add(path))
                    return Fail($"duplicate manifest source path: {path}", out error);
                bool resolvedByPath = byPath.TryGetValue(path, out MeshRenderer renderer) &&
                    !assigned.Contains(renderer) &&
                    ValidateRenderer(renderer, source, mapRoot, out _);
                if (!resolvedByPath &&
                    !TryResolveByIdentity(source, mapRoot, byMesh, assigned, out renderer, out string mismatch))
                {
                    return Fail($"canonical renderer does not match manifest ({mismatch}): {path}", out error);
                }
                assigned.Add(renderer);
                _resolved.Add(renderer);
            }

            for (int i = 0; i < _resolved.Count; i++)
            {
                MeshRenderer renderer = _resolved[i];
                _suppressed.Add(new RendererState(renderer));
                renderer.enabled = false;
            }

            error = null;
            return true;
        }

        private bool TryResolveByIdentity(
            StaticMapPresentationSourceEntry source,
            Transform mapRoot,
            IReadOnlyDictionary<Mesh, List<MeshRenderer>> byMesh,
            HashSet<MeshRenderer> assigned,
            out MeshRenderer renderer,
            out string mismatch)
        {
            renderer = null;
            if (!byMesh.TryGetValue(source.Mesh, out List<MeshRenderer> candidates))
                return Mismatch("mesh", out mismatch);

            mismatch = "identity";
            for (int i = 0; i < candidates.Count; i++)
            {
                MeshRenderer candidate = candidates[i];
                if (assigned.Contains(candidate) ||
                    !ValidateRenderer(candidate, source, mapRoot, out mismatch))
                {
                    continue;
                }

                renderer = candidate;
                return Pass(out mismatch);
            }

            return false;
        }

        private static bool ValidateManifestShape(
            StaticMapPresentationManifest manifest,
            out string error)
        {
            IReadOnlyList<StaticMapPresentationChunkEntry> chunks = manifest.Chunks;
            IReadOnlyList<StaticMapPresentationSourceEntry> sources = manifest.Sources;
            if (chunks == null || sources == null || chunks.Count == 0 || sources.Count == 0)
                return Fail("presentation manifest must contain chunks and sources", out error);

            HashSet<string> chunkIds = new(StringComparer.Ordinal);
            HashSet<string> scenePaths = new(StringComparer.Ordinal);
            HashSet<string> sourceIds = new(StringComparer.Ordinal);
            int expectedStart = 0;
            for (int chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
            {
                StaticMapPresentationChunkEntry chunk = chunks[chunkIndex];
                if (chunk == null || string.IsNullOrWhiteSpace(chunk.ChunkId) ||
                    !chunkIds.Add(chunk.ChunkId) || string.IsNullOrWhiteSpace(chunk.ScenePath) ||
                    !scenePaths.Add(chunk.ScenePath) || chunk.SourceStartIndex != expectedStart ||
                    chunk.SourceCount <= 0 || chunk.SourceStartIndex < 0 ||
                    chunk.SourceStartIndex > sources.Count - chunk.SourceCount)
                {
                    return Fail($"presentation manifest chunk {chunkIndex} is invalid", out error);
                }

                int end = chunk.SourceStartIndex + chunk.SourceCount;
                for (int sourceIndex = chunk.SourceStartIndex; sourceIndex < end; sourceIndex++)
                {
                    StaticMapPresentationSourceEntry source = sources[sourceIndex];
                    if (source == null ||
                        !string.Equals(source.ChunkId, chunk.ChunkId, StringComparison.Ordinal) ||
                        string.IsNullOrWhiteSpace(source.SourceGlobalObjectId) ||
                        !sourceIds.Add(source.SourceGlobalObjectId) ||
                        string.IsNullOrWhiteSpace(source.SourceHierarchyPath) || source.Mesh == null ||
                        source.Materials == null || source.Materials.Count == 0)
                    {
                        return Fail($"presentation manifest source {sourceIndex} is invalid", out error);
                    }

                    for (int materialIndex = 0; materialIndex < source.Materials.Count; materialIndex++)
                    {
                        StaticMapPresentationMaterialEntry material = source.Materials[materialIndex];
                        if (material == null || material.Material == null)
                        {
                            return Fail(
                                $"presentation manifest source {sourceIndex} material {materialIndex} is invalid",
                                out error);
                        }
                    }
                }

                expectedStart = end;
            }

            return expectedStart == sources.Count
                ? Pass(out error)
                : Fail("presentation manifest chunk ranges do not cover every source", out error);
        }

        private bool ValidateRenderer(
            MeshRenderer renderer,
            StaticMapPresentationSourceEntry source,
            Transform mapRoot,
            out string mismatch)
        {
            if (renderer == null || renderer.gameObject.scene != mapRoot.gameObject.scene)
                return Mismatch("scene", out mismatch);
            if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
                return Mismatch("renderable-state", out mismatch);
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null)
                return Mismatch("mesh-filter", out mismatch);
            if (filter.sharedMesh != source.Mesh)
                return Mismatch("mesh", out mismatch);
            _materials.Clear();
            renderer.GetSharedMaterials(_materials);
            if (_materials.Count != source.Materials.Count)
                return Mismatch("material-count", out mismatch);
            for (int i = 0; i < _materials.Count; i++)
            {
                if (_materials[i] != source.Materials[i].Material)
                    return Mismatch($"material-{i}", out mismatch);
            }
            return BoundsMatch(renderer.bounds, source.WorldBounds)
                ? Pass(out mismatch)
                : Mismatch("bounds", out mismatch);
        }

        private string BuildHierarchyPath(Transform transform, Transform mapRoot)
        {
            _path.Clear();
            for (Transform current = transform; current != null; current = current.parent)
            {
                _path.Add(current);
                if (current == mapRoot)
                    break;
            }
            if (_path.Count == 0 || _path[^1] != mapRoot)
                return string.Empty;
            _pathBuilder.Clear();
            for (int i = _path.Count - 1; i >= 0; i--)
            {
                if (_pathBuilder.Length > 0)
                    _pathBuilder.Append('/');
                Transform current = _path[i];
                _pathBuilder.Append(current.name).Append('[').Append(current.GetSiblingIndex()).Append(']');
            }
            return _pathBuilder.ToString();
        }

        private static bool BoundsMatch(Bounds left, Bounds right)
        {
            return VectorClose(left.center, right.center) && VectorClose(left.size, right.size);
        }

        private static bool VectorClose(Vector3 left, Vector3 right)
        {
            Vector3 delta = left - right;
            return Mathf.Abs(delta.x) <= BoundsTolerance &&
                Mathf.Abs(delta.y) <= BoundsTolerance &&
                Mathf.Abs(delta.z) <= BoundsTolerance;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }

        private static bool Pass(out string error)
        {
            error = null;
            return true;
        }

        private static bool Mismatch(string reason, out string mismatch)
        {
            mismatch = reason;
            return false;
        }
    }
}
