using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Game.Authoring;
using Game.Composition;
using Game.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Game.Editor
{
    public static class StaticMapPresentationBaker
    {
        public const string CanonicalMatchScenePath = "Assets/Game/Scenes/Match.unity";
        public const string OutputRoot = "Assets/Game/GeneratedStaticMapPresentation";
        public const string SceneOutputFolder = OutputRoot + "/Scenes";
        public const string ManifestPath = OutputRoot + "/StaticMapPresentationManifest.asset";
        public const float ChunkSize = 32f;

        private const float MatrixTolerance = 0.0005f;

        private sealed class SourceDescriptor
        {
            public MeshRenderer Renderer;
            public MeshFilter Filter;
            public Mesh Mesh;
            public Material[] Materials;
            public string GlobalObjectId;
            public string HierarchyPath;
            public string DependencyHash;
            public string MeshGuid;
            public long MeshLocalId;
            public List<StaticMapPresentationMaterialEntry> MaterialEntries;
            public Bounds WorldBounds;
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public Vector3 WorldScale;
            public bool OverlaySource;
            public ChunkKey Chunk;
        }

        private readonly struct ChunkKey : IEquatable<ChunkKey>, IComparable<ChunkKey>
        {
            public readonly int X;
            public readonly int Z;

            public ChunkKey(Vector3 worldCenter)
            {
                X = Mathf.FloorToInt(worldCenter.x / ChunkSize);
                Z = Mathf.FloorToInt(worldCenter.z / ChunkSize);
            }

            public string Id => $"chunk_{FormatCoordinate(X)}_{FormatCoordinate(Z)}";
            public string ScenePath => $"{SceneOutputFolder}/StaticMapPresentation_{Id}.unity";

            public int CompareTo(ChunkKey other)
            {
                int x = X.CompareTo(other.X);
                return x != 0 ? x : Z.CompareTo(other.Z);
            }

            public bool Equals(ChunkKey other) => X == other.X && Z == other.Z;
            public override bool Equals(object obj) => obj is ChunkKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(X, Z);

            private static string FormatCoordinate(int value)
            {
                return value >= 0
                    ? $"p{value:D3}"
                    : $"n{Math.Abs(value):D3}";
            }
        }

        private sealed class BakeStats
        {
            public int Scanned;
            public int Included;
            public int ExcludedAuthoring;
            public int ExcludedLod;
            public int ExcludedPropertyBlock;
            public int ExcludedProbe;
            public int ExcludedAssetIdentity;
            public int ExcludedMaterial;
            public int ExcludedTransform;
            public int ExcludedOther;
            public int OverlaySources;
        }

        [MenuItem("Warline Capture/Performance/Bake Static Map Presentation")]
        public static void Bake()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BakeInternal();
            }
            finally
            {
                if (!Application.isBatchMode && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static void BakeInternal()
        {
            EnsureAssetFolder(OutputRoot);
            EnsureAssetFolder(SceneOutputFolder);

            Scene sourceScene = EditorSceneManager.OpenScene(CanonicalMatchScenePath, OpenSceneMode.Single);
            MatchSceneView matchScene = FindMatchSceneView(sourceScene);
            Transform mapRoot = ResolveMapRoot(matchScene);
            BakeStats stats = new();
            List<SourceDescriptor> sources = CollectSources(matchScene, mapRoot, stats);
            if (sources.Count == 0)
                throw new InvalidOperationException("Static map presentation bake found no compatible source renderers.");

            sources.Sort(static (left, right) =>
            {
                int chunk = left.Chunk.CompareTo(right.Chunk);
                return chunk != 0
                    ? chunk
                    : string.CompareOrdinal(left.GlobalObjectId, right.GlobalObjectId);
            });

            List<StaticMapPresentationChunkEntry> chunkEntries = new();
            List<StaticMapPresentationSourceEntry> sourceEntries = new(sources.Count);
            foreach (IGrouping<ChunkKey, SourceDescriptor> group in sources.GroupBy(source => source.Chunk).OrderBy(group => group.Key))
            {
                CreateChunkScene(sourceScene, group.Key, group.ToList(), chunkEntries, sourceEntries);
            }

            string canonicalHash = AssetDatabase.GetAssetDependencyHash(CanonicalMatchScenePath).ToString();
            string contentHash = BuildContentHash(canonicalHash, chunkEntries, sourceEntries);
            StaticMapPresentationManifest manifest = AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(ManifestPath);
            if (manifest == null)
            {
                manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
                AssetDatabase.CreateAsset(manifest, ManifestPath);
            }

            manifest.EditorSetData(
                CanonicalMatchScenePath,
                canonicalHash,
                ChunkSize,
                contentHash,
                chunkEntries,
                sourceEntries);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[StaticMapPresentationBake] result=Passed sources={0} chunks={1} scanned={2} overlaySources={3} excludedAuthoring={4} excludedLod={5} excludedPropertyBlock={6} excludedProbe={7} excludedAssetIdentity={8} excludedMaterial={9} excludedTransform={10} excludedOther={11} manifest={12} contentHash={13}",
                stats.Included,
                chunkEntries.Count,
                stats.Scanned,
                stats.OverlaySources,
                stats.ExcludedAuthoring,
                stats.ExcludedLod,
                stats.ExcludedPropertyBlock,
                stats.ExcludedProbe,
                stats.ExcludedAssetIdentity,
                stats.ExcludedMaterial,
                stats.ExcludedTransform,
                stats.ExcludedOther,
                ManifestPath,
                contentHash);
        }

        private static List<SourceDescriptor> CollectSources(
            MatchSceneView matchScene,
            Transform mapRoot,
            BakeStats stats)
        {
            MeshRenderer[] renderers = mapRoot.GetComponentsInChildren<MeshRenderer>(true);
            stats.Scanned = renderers.Length;
            List<SourceDescriptor> sources = new(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!TryCreateSourceDescriptor(matchScene, mapRoot, renderers[i], stats, out SourceDescriptor source))
                    continue;

                sources.Add(source);
                stats.Included++;
                if (source.OverlaySource)
                    stats.OverlaySources++;
            }

            return sources;
        }

        private static bool TryCreateSourceDescriptor(
            MatchSceneView matchScene,
            Transform mapRoot,
            MeshRenderer renderer,
            BakeStats stats,
            out SourceDescriptor source)
        {
            source = null;
            if (renderer == null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
            {
                stats.ExcludedOther++;
                return false;
            }

            if (IsInRoot(renderer.transform, matchScene.MapBuildingAuthoringRoot) ||
                IsInRoot(renderer.transform, matchScene.MapVehicleAuthoringRoot) ||
                IsInRoot(renderer.transform, matchScene.DecorationRoot))
            {
                stats.ExcludedAuthoring++;
                return false;
            }

            if (renderer.GetComponentInParent<LODGroup>(true) != null)
            {
                stats.ExcludedLod++;
                return false;
            }

            if (renderer.HasPropertyBlock())
            {
                stats.ExcludedPropertyBlock++;
                return false;
            }

            if (renderer.probeAnchor != null ||
                renderer.lightProbeProxyVolumeOverride != null ||
                renderer.lightProbeUsage == LightProbeUsage.UseProxyVolume)
            {
                stats.ExcludedProbe++;
                return false;
            }

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || mesh.vertexCount == 0 ||
                !TryGetAssetId(mesh, out string meshGuid, out long meshLocalId))
            {
                stats.ExcludedAssetIdentity++;
                return false;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0 || materials.Length != mesh.subMeshCount)
            {
                stats.ExcludedMaterial++;
                return false;
            }

            List<StaticMapPresentationMaterialEntry> materialEntries = new(materials.Length);
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || material.renderQueue >= (int)RenderQueue.Transparent ||
                    !TryGetAssetId(material, out string materialGuid, out long materialLocalId))
                {
                    stats.ExcludedMaterial++;
                    return false;
                }

                materialEntries.Add(new StaticMapPresentationMaterialEntry(material, materialGuid, materialLocalId));
            }

            Vector3 worldPosition = renderer.transform.position;
            Quaternion worldRotation = renderer.transform.rotation;
            Vector3 worldScale = renderer.transform.lossyScale;
            if (!IsRepresentableWorldTransform(renderer.transform.localToWorldMatrix, worldPosition, worldRotation, worldScale))
            {
                stats.ExcludedTransform++;
                return false;
            }

            string globalObjectId = GlobalObjectId.GetGlobalObjectIdSlow(renderer).ToString();
            string hierarchyPath = BuildHierarchyPath(renderer.transform, mapRoot);
            Bounds worldBounds = renderer.bounds;
            bool overlaySource = renderer.GetComponentInParent<MapBakeGroupAuthoring>(true) != null;
            source = new SourceDescriptor
            {
                Renderer = renderer,
                Filter = filter,
                Mesh = mesh,
                Materials = materials,
                GlobalObjectId = globalObjectId,
                HierarchyPath = hierarchyPath,
                MeshGuid = meshGuid,
                MeshLocalId = meshLocalId,
                MaterialEntries = materialEntries,
                WorldBounds = worldBounds,
                WorldPosition = worldPosition,
                WorldRotation = worldRotation,
                WorldScale = worldScale,
                OverlaySource = overlaySource,
                Chunk = new ChunkKey(worldBounds.center)
            };
            source.DependencyHash = BuildSourceDependencyHash(source);
            return true;
        }

        private static void CreateChunkScene(
            Scene sourceScene,
            ChunkKey chunk,
            List<SourceDescriptor> sources,
            List<StaticMapPresentationChunkEntry> chunkEntries,
            List<StaticMapPresentationSourceEntry> sourceEntries)
        {
            Scene chunkScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            int sourceStartIndex = sourceEntries.Count;
            Bounds chunkBounds = sources[0].WorldBounds;
            try
            {
                GameObject root = new($"StaticMapPresentation_{chunk.Id}");
                SceneManager.MoveGameObjectToScene(root, chunkScene);
                for (int i = 0; i < sources.Count; i++)
                {
                    SourceDescriptor source = sources[i];
                    string generatedObjectName = $"Visual_{i:D5}_{SanitizeName(source.Renderer.gameObject.name)}";
                    CreateRendererClone(root.transform, generatedObjectName, source);
                    chunkBounds.Encapsulate(source.WorldBounds);

                    sourceEntries.Add(new StaticMapPresentationSourceEntry(
                        source.GlobalObjectId,
                        source.HierarchyPath,
                        source.DependencyHash,
                        chunk.Id,
                        generatedObjectName,
                        source.WorldBounds,
                        source.Mesh,
                        source.MeshGuid,
                        source.MeshLocalId,
                        source.MaterialEntries,
                        source.OverlaySource));
                }

                if (!EditorSceneManager.SaveScene(chunkScene, chunk.ScenePath, true))
                    throw new InvalidOperationException($"Failed to save static map presentation scene: {chunk.ScenePath}");
            }
            finally
            {
                if (chunkScene.IsValid() && chunkScene.isLoaded)
                    EditorSceneManager.CloseScene(chunkScene, true);
                if (sourceScene.IsValid() && sourceScene.isLoaded)
                    SceneManager.SetActiveScene(sourceScene);
            }

            chunkEntries.Add(new StaticMapPresentationChunkEntry(
                chunk.Id,
                chunk.ScenePath,
                chunkBounds,
                sourceStartIndex,
                sources.Count));
        }

        private static void CreateRendererClone(Transform root, string objectName, SourceDescriptor source)
        {
            GameObject clone = new(objectName)
            {
                layer = source.Renderer.gameObject.layer
            };
            clone.transform.SetParent(root, false);
            clone.transform.SetPositionAndRotation(source.WorldPosition, source.WorldRotation);
            clone.transform.localScale = source.WorldScale;

            MeshFilter filter = clone.AddComponent<MeshFilter>();
            filter.sharedMesh = source.Mesh;

            MeshRenderer renderer = clone.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = source.Materials;
            renderer.shadowCastingMode = source.Renderer.shadowCastingMode;
            renderer.receiveShadows = source.Renderer.receiveShadows;
            renderer.lightProbeUsage = source.Renderer.lightProbeUsage;
            renderer.reflectionProbeUsage = source.Renderer.reflectionProbeUsage;
            renderer.lightmapIndex = source.Renderer.lightmapIndex;
            renderer.lightmapScaleOffset = source.Renderer.lightmapScaleOffset;
            renderer.realtimeLightmapIndex = source.Renderer.realtimeLightmapIndex;
            renderer.realtimeLightmapScaleOffset = source.Renderer.realtimeLightmapScaleOffset;
            renderer.motionVectorGenerationMode = source.Renderer.motionVectorGenerationMode;
            renderer.renderingLayerMask = source.Renderer.renderingLayerMask;
            renderer.rendererPriority = source.Renderer.rendererPriority;
            renderer.sortingLayerID = source.Renderer.sortingLayerID;
            renderer.sortingOrder = source.Renderer.sortingOrder;
            renderer.allowOcclusionWhenDynamic = source.Renderer.allowOcclusionWhenDynamic;
        }

        private static MatchSceneView FindMatchSceneView(Scene scene)
        {
            MatchSceneView[] views = Object.FindObjectsByType<MatchSceneView>(FindObjectsInactive.Include);
            MatchSceneView view = views.FirstOrDefault(candidate => candidate != null && candidate.gameObject.scene == scene);
            return view != null
                ? view
                : throw new InvalidOperationException($"No MatchSceneView found in {scene.path}.");
        }

        private static Transform ResolveMapRoot(MatchSceneView matchScene)
        {
            Transform current = matchScene.MapSurfaceAuthoring != null ? matchScene.MapSurfaceAuthoring.transform : null;
            while (current != null)
            {
                if (string.Equals(current.name, "Map", StringComparison.Ordinal))
                    return current;
                current = current.parent;
            }

            throw new InvalidOperationException("MatchSceneView does not resolve to a serialized Map root.");
        }

        private static bool IsInRoot(Transform transform, Transform root)
        {
            return root != null && transform != null && (transform == root || transform.IsChildOf(root));
        }

        private static bool TryGetAssetId(Object asset, out string guid, out long localId)
        {
            guid = string.Empty;
            localId = 0;
            return asset != null &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out localId) &&
                   !string.IsNullOrWhiteSpace(guid);
        }

        private static bool IsRepresentableWorldTransform(
            Matrix4x4 source,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            Matrix4x4 reconstructed = Matrix4x4.TRS(position, rotation, scale);
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    if (Mathf.Abs(source[row, column] - reconstructed[row, column]) > MatrixTolerance)
                        return false;
                }
            }

            return true;
        }

        private static string BuildSourceDependencyHash(SourceDescriptor source)
        {
            StringBuilder builder = new(1024);
            builder.Append(source.GlobalObjectId).Append('|');
            builder.Append(source.HierarchyPath).Append('|');
            AppendMatrix(builder, source.Renderer.transform.localToWorldMatrix);
            builder.Append('|').Append(source.MeshGuid).Append(':').Append(source.MeshLocalId);
            AppendAssetDependencyHash(builder, source.Mesh);
            for (int i = 0; i < source.MaterialEntries.Count; i++)
            {
                StaticMapPresentationMaterialEntry material = source.MaterialEntries[i];
                builder.Append('|').Append(material.AssetGuid).Append(':').Append(material.LocalId);
                AppendAssetDependencyHash(builder, material.Material);
            }

            builder.Append('|').Append((int)source.Renderer.shadowCastingMode);
            builder.Append('|').Append(source.Renderer.receiveShadows ? 1 : 0);
            builder.Append('|').Append((int)source.Renderer.lightProbeUsage);
            builder.Append('|').Append((int)source.Renderer.reflectionProbeUsage);
            builder.Append('|').Append(source.Renderer.lightmapIndex);
            AppendVector(builder, source.Renderer.lightmapScaleOffset);
            builder.Append('|').Append(source.Renderer.realtimeLightmapIndex);
            AppendVector(builder, source.Renderer.realtimeLightmapScaleOffset);
            builder.Append('|').Append((int)source.Renderer.motionVectorGenerationMode);
            builder.Append('|').Append(source.Renderer.renderingLayerMask);
            builder.Append('|').Append(source.Renderer.rendererPriority);
            builder.Append('|').Append(source.Renderer.sortingLayerID);
            builder.Append('|').Append(source.Renderer.sortingOrder);
            builder.Append('|').Append(source.Renderer.allowOcclusionWhenDynamic ? 1 : 0);
            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static string BuildContentHash(
            string canonicalHash,
            List<StaticMapPresentationChunkEntry> chunks,
            List<StaticMapPresentationSourceEntry> sources)
        {
            StringBuilder builder = new(256 + sources.Count * 72);
            builder.Append(StaticMapPresentationManifest.CurrentSchemaVersion).Append('|');
            builder.Append(canonicalHash).Append('|');
            builder.Append(ChunkSize.ToString("R", CultureInfo.InvariantCulture));
            for (int i = 0; i < chunks.Count; i++)
            {
                StaticMapPresentationChunkEntry chunk = chunks[i];
                builder.Append('|').Append(chunk.ChunkId).Append('|').Append(chunk.ScenePath);
                AppendBounds(builder, chunk.WorldBounds);
            }

            for (int i = 0; i < sources.Count; i++)
            {
                StaticMapPresentationSourceEntry source = sources[i];
                builder.Append('|').Append(source.SourceGlobalObjectId);
                builder.Append('|').Append(source.SourceDependencyHash);
                builder.Append('|').Append(source.ChunkId);
            }

            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void AppendAssetDependencyHash(StringBuilder builder, Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            builder.Append('@');
            builder.Append(string.IsNullOrWhiteSpace(path)
                ? "missing"
                : AssetDatabase.GetAssetDependencyHash(path).ToString());
        }

        private static void AppendMatrix(StringBuilder builder, Matrix4x4 matrix)
        {
            for (int row = 0; row < 4; row++)
            {
                for (int column = 0; column < 4; column++)
                {
                    builder.Append(matrix[row, column].ToString("R", CultureInfo.InvariantCulture)).Append(',');
                }
            }
        }

        private static void AppendVector(StringBuilder builder, Vector4 vector)
        {
            builder.Append('|')
                .Append(vector.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(vector.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(vector.z.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(vector.w.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AppendBounds(StringBuilder builder, Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 size = bounds.size;
            builder.Append('|')
                .Append(center.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(center.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(center.z.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(size.x.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(size.y.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                .Append(size.z.ToString("R", CultureInfo.InvariantCulture));
        }

        private static string BuildHierarchyPath(Transform transform, Transform stopRoot)
        {
            Stack<string> parts = new();
            for (Transform current = transform; current != null; current = current.parent)
            {
                parts.Push($"{current.name}[{current.GetSiblingIndex()}]");
                if (current == stopRoot)
                    break;
            }

            return string.Join("/", parts);
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Renderer";

            StringBuilder builder = new(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '-'
                    ? character
                    : '_');
            }

            return builder.ToString();
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
