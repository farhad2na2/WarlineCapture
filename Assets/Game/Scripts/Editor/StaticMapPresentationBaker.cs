using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
        public const string CurrentStagedOperationMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        public const string CurrentOperationMapId = "opmap.skirmish.desert_base_01";
        public const string OutputRoot =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01";
        public const string SceneOutputFolder = OutputRoot + "/Scenes";
        public const string ManifestPath = OutputRoot + "/StaticMapPresentationManifest.asset";
        public const float ChunkSize = 32f;
        internal static string CurrentSceneFilePrefix =>
            StaticMapPresentationOutputPathContract.RequireSceneFilePrefix(CurrentOperationMapId);

        private const string LegacySceneOutputPrefix =
            "Assets/Game/GeneratedStaticMapPresentation/Scenes/";
        private const int ChunkContentSchemaVersion = 1;
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
            public int Layer;
            public bool OverlaySource;
            public ChunkKey Chunk;
        }

        private sealed class ChunkDescriptor
        {
            public ChunkKey Key;
            public List<SourceDescriptor> Sources;
        }

        private readonly struct ChunkKey : IEquatable<ChunkKey>, IComparable<ChunkKey>
        {
            public readonly int X;
            public readonly int Z;

            public ChunkKey(Vector3 worldCenter, float chunkSize)
            {
                X = Mathf.FloorToInt(worldCenter.x / chunkSize);
                Z = Mathf.FloorToInt(worldCenter.z / chunkSize);
            }

            public string Id => $"chunk_{FormatCoordinate(X)}_{FormatCoordinate(Z)}";
            public string GetScenePath(string sceneOutputFolder, string sceneFilePrefix) =>
                $"{sceneOutputFolder}/{sceneFilePrefix}{Id}.unity";

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

        private sealed class BakeSourceContext
        {
            public Transform MapRoot;
            public Transform BuildingAuthoringRoot;
            public Transform VehicleAuthoringRoot;
            public Transform DecorationRoot;
        }

        [MenuItem("Game/Tools/Performance/Bake Static Map Presentation")]
        public static void Bake()
        {
            Bake(CreateCurrentCompatibilityInput());
        }

        [MenuItem("Game/Tools/Performance/Bake Current Staged Operation Map Presentation")]
        public static void BakeCurrentStagedOperationMapPresentation()
        {
            Bake(CreateCurrentStagedInput());
        }

        internal static StaticMapPresentationBakeInput CreateCurrentCompatibilityInput()
        {
            return new StaticMapPresentationBakeInput(
                CurrentOperationMapId,
                CanonicalMatchScenePath,
                "Map",
                OutputRoot,
                ManifestPath,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                ChunkSize);
        }

        internal static StaticMapPresentationBakeInput CreateCurrentStagedInput()
        {
            return new StaticMapPresentationBakeInput(
                CurrentOperationMapId,
                CurrentStagedOperationMapScenePath,
                "Map",
                OutputRoot,
                ManifestPath,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                ChunkSize);
        }

        internal static void Bake(StaticMapPresentationBakeInput input)
        {
            ValidateSupportedInput(input);
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                BakeInternal(input);
            }
            finally
            {
                if (!Application.isBatchMode && previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        public static void ProbeManifestOwnership()
        {
            StaticMapPresentationManifest manifest = LoadExistingManifest(ManifestPath);
            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[StaticMapPresentationManifestProbe] result=Passed loaded={0} chunks={1} contentHash={2}",
                manifest != null ? 1 : 0,
                manifest != null ? manifest.Chunks.Count : 0,
                manifest != null ? manifest.ContentHash : "<none>");
        }

        [MenuItem("Game/Tools/Performance/Migrate Static Map Presentation Output Root")]
        public static void MigrateCurrentOutputRoot()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            StaticMapPresentationManifest manifest = LoadExistingManifest(ManifestPath);
            if (manifest == null)
                throw new InvalidOperationException($"Missing moved manifest at {ManifestPath}.");

            List<StaticMapPresentationChunkEntry> migratedChunks = new(manifest.Chunks.Count);
            for (int i = 0; i < manifest.Chunks.Count; i++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[i];
                string fileName = Path.GetFileName(chunk.ScenePath);
                bool knownOwner = chunk.ScenePath.StartsWith(LegacySceneOutputPrefix, StringComparison.Ordinal) ||
                                  chunk.ScenePath.StartsWith(SceneOutputFolder + "/", StringComparison.Ordinal);
                if (!knownOwner || string.IsNullOrWhiteSpace(fileName))
                {
                    throw new InvalidOperationException(
                        $"Manifest chunk has foreign output ownership: {chunk.ScenePath}");
                }

                string migratedScenePath = $"{SceneOutputFolder}/{fileName}";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(migratedScenePath) == null)
                    throw new InvalidOperationException($"Moved chunk scene is missing: {migratedScenePath}");

                migratedChunks.Add(new StaticMapPresentationChunkEntry(
                    chunk.ChunkId,
                    migratedScenePath,
                    chunk.WorldBounds,
                    chunk.SourceStartIndex,
                    chunk.SourceCount));
            }

            List<StaticMapPresentationSourceEntry> sources = manifest.Sources.ToList();
            string contentHash = ComputeContentHash(manifest.ChunkSize, migratedChunks, sources);
            string[] expectedScenePaths = migratedChunks.Select(chunk => chunk.ScenePath).ToArray();
            string projectRoot = RequireProjectRoot();
            using StaticMapPresentationBakeTransaction transaction =
                StaticMapPresentationBakeTransaction.Begin(
                    projectRoot,
                    CurrentOperationMapId,
                    OutputRoot,
                    ManifestPath,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    new[] { ManifestPath, StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath });
            try
            {
                manifest.EditorSetData(
                    CurrentOperationMapId,
                    AssetDatabase.AssetPathToGUID(CanonicalMatchScenePath),
                    CanonicalMatchScenePath,
                    manifest.CanonicalSceneDependencyHash,
                    manifest.ChunkSize,
                    contentHash,
                    migratedChunks,
                    sources);
                AssetDatabase.SaveAssetIfDirty(manifest);
                StaticMapPresentationSceneIntegrity.Write(
                    projectRoot,
                    CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    contentHash,
                    expectedScenePaths);
                AssetDatabase.ImportAsset(
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                throw;
            }

            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[StaticMapPresentationOutputRootMigration] result=Passed map={0} chunks={1} contentHash={2}",
                CurrentOperationMapId,
                migratedChunks.Count,
                contentHash);
        }

        [MenuItem("Game/Tools/Performance/Namespace Current Static Map Presentation Scenes")]
        public static void NamespaceCurrentSceneFiles()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            StaticMapPresentationManifest manifest = LoadExistingManifest(ManifestPath);
            if (manifest == null)
                throw new InvalidOperationException($"Missing manifest at {ManifestPath}.");
            if (!string.Equals(
                    manifest.OperationMapId,
                    CurrentOperationMapId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Manifest operation-map id is not {CurrentOperationMapId}: {manifest.OperationMapId}");
            }

            List<StaticMapPresentationChunkEntry> migratedChunks = new(manifest.Chunks.Count);
            List<string> sourcePaths = new(manifest.Chunks.Count);
            List<string> targetPaths = new(manifest.Chunks.Count);
            for (int i = 0; i < manifest.Chunks.Count; i++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[i];
                string sourcePath = chunk.ScenePath;
                string targetPath =
                    $"{SceneOutputFolder}/{CurrentSceneFilePrefix}{chunk.ChunkId}.unity";
                if (!string.Equals(Path.GetDirectoryName(sourcePath)?.Replace('\\', '/'), SceneOutputFolder, StringComparison.Ordinal) ||
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(sourcePath) == null)
                {
                    throw new InvalidOperationException(
                        $"Manifest chunk is not an existing current-map scene: {sourcePath}");
                }
                if (!string.Equals(sourcePath, targetPath, StringComparison.Ordinal) &&
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(targetPath) != null)
                {
                    throw new InvalidOperationException(
                        $"Namespaced chunk scene already exists: {targetPath}");
                }

                sourcePaths.Add(sourcePath);
                targetPaths.Add(targetPath);
                migratedChunks.Add(new StaticMapPresentationChunkEntry(
                    chunk.ChunkId,
                    targetPath,
                    chunk.WorldBounds,
                    chunk.SourceStartIndex,
                    chunk.SourceCount));
            }

            List<StaticMapPresentationSourceEntry> sources = manifest.Sources.ToList();
            string contentHash = ComputeContentHash(manifest.ChunkSize, migratedChunks, sources);
            string projectRoot = RequireProjectRoot();
            IEnumerable<string> mutablePaths = sourcePaths
                .Concat(targetPaths)
                .Append(ManifestPath)
                .Append(StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath);
            using StaticMapPresentationBakeTransaction transaction =
                StaticMapPresentationBakeTransaction.Begin(
                    projectRoot,
                    CurrentOperationMapId,
                    OutputRoot,
                    ManifestPath,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    mutablePaths);
            int movedScenes = 0;
            try
            {
                for (int i = 0; i < sourcePaths.Count; i++)
                {
                    if (string.Equals(sourcePaths[i], targetPaths[i], StringComparison.Ordinal))
                        continue;

                    string moveError = AssetDatabase.MoveAsset(sourcePaths[i], targetPaths[i]);
                    if (!string.IsNullOrEmpty(moveError))
                    {
                        throw new InvalidOperationException(
                            $"Failed to namespace chunk scene {sourcePaths[i]}: {moveError}");
                    }
                    movedScenes++;
                }

                manifest.EditorSetData(
                    CurrentOperationMapId,
                    manifest.CanonicalSceneGuid,
                    manifest.CanonicalScenePath,
                    manifest.CanonicalSceneDependencyHash,
                    manifest.ChunkSize,
                    contentHash,
                    migratedChunks,
                    sources);
                AssetDatabase.SaveAssetIfDirty(manifest);
                StaticMapPresentationSceneIntegrity.Write(
                    projectRoot,
                    CurrentOperationMapId,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    contentHash,
                    targetPaths);
                AssetDatabase.ImportAsset(
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                AssetDatabase.Refresh(
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                throw;
            }

            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[StaticMapPresentationSceneNamespaceMigration] result=Passed map={0} chunks={1} movedScenes={2} contentHash={3}",
                CurrentOperationMapId,
                migratedChunks.Count,
                movedScenes,
                contentHash);
        }

        internal static void ValidateCompatibilityInput(StaticMapPresentationBakeInput input)
        {
            ValidateSupportedInput(input);
            if (!string.Equals(input.SourceScenePath, CanonicalMatchScenePath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Static presentation compatibility validation requires the canonical Match scene.");
        }

        internal static void ValidateSupportedInput(StaticMapPresentationBakeInput input)
        {
            if (!input.TryValidate(out string error))
                throw new InvalidOperationException(error);
            bool supportedSource =
                string.Equals(input.SourceScenePath, CanonicalMatchScenePath, StringComparison.Ordinal) ||
                string.Equals(input.SourceScenePath, CurrentStagedOperationMapScenePath, StringComparison.Ordinal);
            if (!supportedSource ||
                !string.Equals(input.SourceMapRootPath, "Map", StringComparison.Ordinal) ||
                !string.Equals(input.OutputRoot, OutputRoot, StringComparison.Ordinal) ||
                !string.Equals(input.ManifestPath, ManifestPath, StringComparison.Ordinal) ||
                !string.Equals(
                    input.IntegrityPath,
                    StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                    StringComparison.Ordinal) ||
                input.ChunkSize != ChunkSize)
            {
                throw new InvalidOperationException(
                    "Static presentation baker accepts only the current map's compatibility or staged source ownership contract.");
            }
        }

        internal static void ValidateSourceSceneView(StaticMapPresentationBakeInput input)
        {
            ValidateSupportedInput(input);
            Scene scene = SceneManager.GetSceneByPath(input.SourceScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation)
                scene = EditorSceneManager.OpenScene(input.SourceScenePath, OpenSceneMode.Additive);
            try
            {
                ResolveSourceContext(scene, input);
            }
            finally
            {
                if (openedForValidation && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void BakeInternal(StaticMapPresentationBakeInput input)
        {
            EnsureAssetFolder(input.OutputRoot);
            EnsureAssetFolder(input.SceneOutputFolder);

            StaticMapPresentationManifest existingManifest = LoadExistingManifest(input.ManifestPath);
            int previousSchemaVersion = existingManifest != null ? existingManifest.SchemaVersion : 0;
            string previousCanonicalScenePath = existingManifest != null
                ? existingManifest.CanonicalScenePath
                : string.Empty;
            float previousChunkSize = existingManifest != null ? existingManifest.ChunkSize : 0f;
            string[] previousOwnedScenePaths =
                StaticMapPresentationOutputOwnership.CaptureOwnedScenePaths(
                    existingManifest,
                    input.OperationMapId,
                    input.OutputRoot);
            string previousContentHash = existingManifest != null
                ? ComputeContentHash(input.ChunkSize, existingManifest.Chunks, existingManifest.Sources)
                : string.Empty;
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Scene sourceScene = EditorSceneManager.OpenScene(input.SourceScenePath, OpenSceneMode.Single);
            BakeSourceContext sourceContext = ResolveSourceContext(sourceScene, input);
            BakeStats stats = new();
            List<SourceDescriptor> sources = CollectSources(sourceContext, input.ChunkSize, stats);
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
            List<ChunkDescriptor> chunks = sources
                .GroupBy(source => source.Chunk)
                .OrderBy(group => group.Key)
                .Select(group => new ChunkDescriptor
                {
                    Key = group.Key,
                    Sources = group.ToList()
                })
                .ToList();
            for (int i = 0; i < chunks.Count; i++)
            {
                AddManifestEntries(
                    chunks[i],
                    input.SceneOutputFolder,
                    input.SceneFilePrefix,
                    chunkEntries,
                    sourceEntries);
            }

            string canonicalHash = StaticMapPresentationCanonicalSourceHash.Compute(input.SourceScenePath);
            string contentHash = ComputeContentHash(input.ChunkSize, chunkEntries, sourceEntries);
            string[] expectedScenePaths = chunkEntries.Select(chunk => chunk.ScenePath).ToArray();
            string projectRoot = RequireProjectRoot();
            bool integrityReady = StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                projectRoot,
                input.OperationMapId,
                input.IntegrityPath,
                contentHash,
                expectedScenePaths,
                out StaticMapPresentationSceneIntegrity existingIntegrity,
                out string integrityRejectionReason);
            bool reusedScenes = StaticMapPresentationOutputOwnership.CanReuseExpectedScenes(
                input.OperationMapId,
                input.OutputRoot,
                previousSchemaVersion,
                previousCanonicalScenePath,
                previousChunkSize,
                previousContentHash,
                input.SourceScenePath,
                input.ChunkSize,
                contentHash,
                previousOwnedScenePaths,
                expectedScenePaths,
                path => integrityReady &&
                        AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null &&
                        existingIntegrity.IsSceneFileValid(path),
                out string reuseRejectionReason);
            if (!reusedScenes && reuseRejectionReason.StartsWith("owned-scene-integrity-invalid:", StringComparison.Ordinal))
                reuseRejectionReason = integrityRejectionReason;

            int scenesWritten = 0;
            int staleScenesDeleted = 0;
            IEnumerable<string> mutableAssetPaths = reusedScenes
                ? new[] { input.ManifestPath }
                : previousOwnedScenePaths
                    .Concat(expectedScenePaths)
                    .Append(input.ManifestPath)
                    .Append(input.IntegrityPath);
            using StaticMapPresentationBakeTransaction transaction =
                StaticMapPresentationBakeTransaction.Begin(
                    projectRoot,
                    input.OperationMapId,
                    input.OutputRoot,
                    input.ManifestPath,
                    input.IntegrityPath,
                    mutableAssetPaths);
            try
            {
                if (!reusedScenes)
                {
                    Debug.LogFormat(
                        LogType.Log,
                        LogOption.NoStacktrace,
                        null,
                        "[StaticMapPresentationBakeReuse] result=Regenerate reason={0} chunks={1} previousContentHash={2} expectedContentHash={3}",
                        reuseRejectionReason,
                        chunks.Count,
                        previousContentHash,
                        contentHash);
                    for (int i = 0; i < chunks.Count; i++)
                    {
                        CreateChunkScene(
                            sourceScene,
                            chunks[i],
                            input.SceneOutputFolder,
                            input.SceneFilePrefix);
                        scenesWritten++;
                    }

                    staleScenesDeleted = StaticMapPresentationOutputOwnership.DeleteStaleSceneAssets(
                        previousOwnedScenePaths,
                        expectedScenePaths,
                        AssetExists,
                        PhysicalAssetExists,
                        AssetDatabase.DeleteAsset,
                        DeletePhysicalOwnedAsset);
                    StaticMapPresentationSceneIntegrity.Write(
                        projectRoot,
                        input.OperationMapId,
                        input.IntegrityPath,
                        contentHash,
                        expectedScenePaths);
                    AssetDatabase.ImportAsset(
                        input.IntegrityPath,
                        ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }

                StaticMapPresentationManifest manifest = LoadExistingManifest(input.ManifestPath);
                if (manifest == null)
                {
                    manifest = ScriptableObject.CreateInstance<StaticMapPresentationManifest>();
                    AssetDatabase.CreateAsset(manifest, input.ManifestPath);
                }

                manifest.EditorSetData(
                    input.OperationMapId,
                    AssetDatabase.AssetPathToGUID(input.SourceScenePath),
                    input.SourceScenePath,
                    canonicalHash,
                    input.ChunkSize,
                    contentHash,
                    chunkEntries,
                    sourceEntries);
                AssetDatabase.SaveAssetIfDirty(manifest);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                transaction.Commit();
            }
            catch (Exception bakeException)
            {
                try
                {
                    transaction.Rollback();
                    AssetDatabase.Refresh(
                        ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(
                        "Static map presentation bake failed and rollback did not complete.",
                        bakeException,
                        rollbackException);
                }

                throw;
            }

            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[StaticMapPresentationBake] result=Passed sources={0} chunks={1} scanned={2} overlaySources={3} excludedAuthoring={4} excludedLod={5} excludedPropertyBlock={6} excludedProbe={7} excludedAssetIdentity={8} excludedMaterial={9} excludedTransform={10} excludedOther={11} manifest={12} contentHash={13} reusedScenes={14} scenesWritten={15} staleScenesDeleted={16} reuseRejectionReason={17}",
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
                input.ManifestPath,
                contentHash,
                reusedScenes ? 1 : 0,
                scenesWritten,
                staleScenesDeleted,
                reuseRejectionReason);
        }

        private static List<SourceDescriptor> CollectSources(
            BakeSourceContext sourceContext,
            float chunkSize,
            BakeStats stats)
        {
            Transform mapRoot = sourceContext.MapRoot;
            MeshRenderer[] renderers = mapRoot.GetComponentsInChildren<MeshRenderer>(true);
            stats.Scanned = renderers.Length;
            List<SourceDescriptor> sources = new(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!TryCreateSourceDescriptor(
                        sourceContext,
                        mapRoot,
                        renderers[i],
                        chunkSize,
                        stats,
                        out SourceDescriptor source))
                    continue;

                sources.Add(source);
                stats.Included++;
                if (source.OverlaySource)
                    stats.OverlaySources++;
            }

            return sources;
        }

        private static bool TryCreateSourceDescriptor(
            BakeSourceContext sourceContext,
            Transform mapRoot,
            MeshRenderer renderer,
            float chunkSize,
            BakeStats stats,
            out SourceDescriptor source)
        {
            source = null;
            if (renderer == null || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
            {
                stats.ExcludedOther++;
                return false;
            }

            if (IsInRoot(renderer.transform, sourceContext.BuildingAuthoringRoot) ||
                IsInRoot(renderer.transform, sourceContext.VehicleAuthoringRoot) ||
                IsInRoot(renderer.transform, sourceContext.DecorationRoot))
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
                Layer = renderer.gameObject.layer,
                OverlaySource = overlaySource,
                Chunk = new ChunkKey(worldBounds.center, chunkSize)
            };
            source.DependencyHash = BuildSourceDependencyHash(source);
            return true;
        }

        private static void AddManifestEntries(
            ChunkDescriptor chunk,
            string sceneOutputFolder,
            string sceneFilePrefix,
            List<StaticMapPresentationChunkEntry> chunkEntries,
            List<StaticMapPresentationSourceEntry> sourceEntries)
        {
            int sourceStartIndex = sourceEntries.Count;
            Bounds chunkBounds = chunk.Sources[0].WorldBounds;
            for (int i = 0; i < chunk.Sources.Count; i++)
            {
                SourceDescriptor source = chunk.Sources[i];
                string generatedObjectName = GetGeneratedObjectName(i, source);
                chunkBounds.Encapsulate(source.WorldBounds);

                sourceEntries.Add(new StaticMapPresentationSourceEntry(
                    source.GlobalObjectId,
                    source.HierarchyPath,
                    source.DependencyHash,
                    chunk.Key.Id,
                    generatedObjectName,
                    source.WorldBounds,
                    source.Mesh,
                    source.MeshGuid,
                    source.MeshLocalId,
                    source.MaterialEntries,
                    source.OverlaySource));
            }

            chunkEntries.Add(new StaticMapPresentationChunkEntry(
                chunk.Key.Id,
                chunk.Key.GetScenePath(sceneOutputFolder, sceneFilePrefix),
                chunkBounds,
                sourceStartIndex,
                chunk.Sources.Count));
        }

        private static void CreateChunkScene(
            Scene sourceScene,
            ChunkDescriptor chunk,
            string sceneOutputFolder,
            string sceneFilePrefix)
        {
            Scene chunkScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                GameObject root = new($"StaticMapPresentation_{chunk.Key.Id}");
                SceneManager.MoveGameObjectToScene(root, chunkScene);
                for (int i = 0; i < chunk.Sources.Count; i++)
                {
                    SourceDescriptor source = chunk.Sources[i];
                    string generatedObjectName = GetGeneratedObjectName(i, source);
                    CreateRendererClone(root.transform, generatedObjectName, source);
                }

                string scenePath = chunk.Key.GetScenePath(sceneOutputFolder, sceneFilePrefix);
                if (!EditorSceneManager.SaveScene(chunkScene, scenePath, true))
                    throw new InvalidOperationException($"Failed to save static map presentation scene: {scenePath}");
            }
            finally
            {
                if (chunkScene.IsValid() && chunkScene.isLoaded)
                    EditorSceneManager.CloseScene(chunkScene, true);
                if (sourceScene.IsValid() && sourceScene.isLoaded)
                    SceneManager.SetActiveScene(sourceScene);
            }
        }

        private static string GetGeneratedObjectName(int index, SourceDescriptor source)
        {
            return $"Visual_{index:D5}_{SanitizeName(source.Renderer.gameObject.name)}";
        }

        private static void CreateRendererClone(Transform root, string objectName, SourceDescriptor source)
        {
            GameObject clone = new(objectName)
            {
                layer = source.Layer
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

        private static BakeSourceContext ResolveSourceContext(
            Scene scene,
            StaticMapPresentationBakeInput input)
        {
            MatchSceneView[] matchViews = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<MatchSceneView>(true))
                .ToArray();
            OperationMapSceneView[] operationMapViews = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapSceneView>(true))
                .ToArray();
            if (matchViews.Length + operationMapViews.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Static presentation source scene requires exactly one supported scene view; found Match={matchViews.Length}, OperationMap={operationMapViews.Length}.");
            }

            if (matchViews.Length == 1)
            {
                MatchSceneView matchView = matchViews[0];
                return new BakeSourceContext
                {
                    MapRoot = ResolveMapRoot(matchView, input.SourceMapRootPath),
                    BuildingAuthoringRoot = matchView.MapBuildingAuthoringRoot,
                    VehicleAuthoringRoot = matchView.MapVehicleAuthoringRoot,
                    DecorationRoot = matchView.DecorationRoot
                };
            }

            OperationMapSceneView operationMapView = operationMapViews[0];
            if (!operationMapView.TryValidate(out string error) ||
                !string.Equals(operationMapView.OperationMapId, input.OperationMapId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    error ?? "Operation-map scene view identity does not match the bake input.");
            }

            Transform mapRoot = operationMapView.MapRoot;
            if (mapRoot == null ||
                !string.Equals(mapRoot.name, input.SourceMapRootPath, StringComparison.Ordinal) ||
                mapRoot.parent != null)
            {
                throw new InvalidOperationException(
                    $"Operation-map scene view does not bind root '{input.SourceMapRootPath}'.");
            }

            Transform buildings = mapRoot.Find("Buildings");
            Transform vehicles = mapRoot.Find("Vehicles");
            if (buildings == null || vehicles == null)
            {
                throw new InvalidOperationException(
                    "Operation-map scene view requires exact Map/Buildings and Map/Vehicles authoring roots.");
            }

            return new BakeSourceContext
            {
                MapRoot = mapRoot,
                BuildingAuthoringRoot = buildings,
                VehicleAuthoringRoot = vehicles,
                DecorationRoot = null
            };
        }

        private static Transform ResolveMapRoot(
            MatchSceneView matchScene,
            string sourceMapRootPath)
        {
            Transform current = matchScene.MapSurfaceAuthoring != null ? matchScene.MapSurfaceAuthoring.transform : null;
            while (current != null)
            {
                if (string.Equals(current.name, sourceMapRootPath, StringComparison.Ordinal))
                    return current;
                current = current.parent;
            }

            throw new InvalidOperationException(
                $"MatchSceneView does not resolve to serialized map root '{sourceMapRootPath}'.");
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

            AppendGameObjectLayerIdentity(builder, source.Layer);
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

        internal static void AppendGameObjectLayerIdentity(StringBuilder builder, int layer)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (layer < 0 || layer > 31)
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "Unity GameObject layer must be in [0, 31].");

            builder.Append("|layer:").Append(layer);
        }

        internal static string ComputeContentHash(
            float chunkSize,
            IReadOnlyList<StaticMapPresentationChunkEntry> chunks,
            IReadOnlyList<StaticMapPresentationSourceEntry> sources)
        {
            StringBuilder builder = new(256 + sources.Count * 72);
            builder.Append(ChunkContentSchemaVersion).Append('|');
            builder.Append(chunkSize.ToString("R", CultureInfo.InvariantCulture));
            for (int i = 0; i < chunks.Count; i++)
            {
                StaticMapPresentationChunkEntry chunk = chunks[i];
                builder.Append('|').Append(chunk.ChunkId);
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

        private static bool AssetExists(string assetPath)
        {
            return !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath));
        }

        private static bool PhysicalAssetExists(string assetPath)
        {
            string physicalPath = ResolveProjectAssetPath(assetPath);
            return File.Exists(physicalPath) || File.Exists(physicalPath + ".meta");
        }

        private static bool DeletePhysicalOwnedAsset(string assetPath)
        {
            if (!StaticMapPresentationOutputOwnership.IsOwnedScenePath(assetPath))
                throw new InvalidOperationException($"Refusing to physically delete unowned asset path: {assetPath}");

            string physicalPath = ResolveProjectAssetPath(assetPath);
            if (File.Exists(physicalPath))
                File.Delete(physicalPath);
            if (File.Exists(physicalPath + ".meta"))
                File.Delete(physicalPath + ".meta");
            return !File.Exists(physicalPath) && !File.Exists(physicalPath + ".meta");
        }

        private static string ResolveProjectAssetPath(string assetPath)
        {
            string projectRoot = RequireProjectRoot();
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            string requiredPrefix = projectRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? projectRoot
                : projectRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(requiredPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException($"Asset path escaped the project root: {assetPath}");
            return fullPath;
        }

        private static string RequireProjectRoot()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException($"Unable to resolve project root from Application.dataPath: {Application.dataPath}");
            return Path.GetFullPath(projectRoot);
        }

        private static StaticMapPresentationManifest LoadExistingManifest(string manifestPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string manifestFilePath = Path.Combine(projectRoot ?? string.Empty, manifestPath);
            bool manifestFileExists = System.IO.File.Exists(manifestFilePath);
            Debug.LogFormat(
                LogType.Log,
                LogOption.NoStacktrace,
                null,
                "[StaticMapPresentationManifestProbe] dataPath={0} projectRoot={1} manifestFile={2} fileExists={3} currentDirectory={4}",
                Application.dataPath,
                projectRoot ?? "<null>",
                manifestFilePath,
                manifestFileExists ? 1 : 0,
                Environment.CurrentDirectory);
            if (!manifestFileExists)
                return null;

            AssetDatabase.ImportAsset(
                manifestPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            string manifestGuid = AssetDatabase.AssetPathToGUID(manifestPath);
            if (string.IsNullOrEmpty(manifestGuid))
            {
                throw new InvalidOperationException(
                    $"Static map presentation manifest exists at {manifestPath} but Unity did not assign its GUID. " +
                    "Refusing to rewrite or clean owned output.");
            }

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(manifestPath);
            if (manifest != null)
                return manifest;

            Object mainAsset = AssetDatabase.LoadMainAssetAtPath(manifestPath);
            string actualType = mainAsset != null ? mainAsset.GetType().FullName : "<unloaded>";
            throw new InvalidOperationException(
                $"Static map presentation manifest GUID {manifestGuid} exists but cannot load as " +
                $"{nameof(StaticMapPresentationManifest)} (actual={actualType}). Refusing to rewrite or clean owned output.");
        }
    }
}
