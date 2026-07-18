using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Configs;
using Game.Rendering;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class StaticMapAndroidBuildManifestSnapshot
    {
        internal int SchemaVersion { get; }
        internal string OperationMapId { get; }
        internal string CanonicalSceneGuid { get; }
        internal string CanonicalScenePath { get; }
        internal string CanonicalSceneDependencyHash { get; }
        internal string ContentHash { get; }
        internal IReadOnlyList<string> ChunkScenePaths { get; }

        internal StaticMapAndroidBuildManifestSnapshot(
            int schemaVersion,
            string operationMapId,
            string canonicalSceneGuid,
            string canonicalScenePath,
            string canonicalSceneDependencyHash,
            string contentHash,
            IReadOnlyList<string> chunkScenePaths)
        {
            SchemaVersion = schemaVersion;
            OperationMapId = operationMapId;
            CanonicalSceneGuid = canonicalSceneGuid;
            CanonicalScenePath = canonicalScenePath;
            CanonicalSceneDependencyHash = canonicalSceneDependencyHash;
            ContentHash = contentHash;
            ChunkScenePaths = chunkScenePaths;
        }
    }

    internal static class StaticMapAndroidBuildSceneResolver
    {
        internal static string[] ResolveForCurrentProject(IEnumerable<string> enabledScenePaths)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve the project root for Android build scenes.");

            StaticMapAndroidBuildManifestSnapshot[] snapshots =
                LoadCatalogSelectedManifestSnapshots();

            return Resolve(
                enabledScenePaths,
                snapshots,
                assetPath => SceneExists(projectRoot, assetPath),
                (operationMapId, scenePath) =>
                    StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
                        operationMapId,
                        out string outputRoot,
                        out _) &&
                    StaticMapPresentationOutputOwnership.IsOwnedScenePath(
                        operationMapId,
                        outputRoot,
                        scenePath),
                canonicalScenePath => StaticMapPresentationCanonicalSourceHash.Compute(
                    canonicalScenePath),
                (operationMapId, contentHash, scenePaths) =>
                    StaticMapPresentationOutputPathContract.TryResolveIntegrityAssetPath(
                        operationMapId,
                        out string integrityAssetPath,
                        out _) &&
                    StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                        projectRoot,
                        operationMapId,
                        integrityAssetPath,
                        contentHash,
                        scenePaths,
                        out _,
                        out _));
        }

        internal static string[] Resolve(
            IEnumerable<string> enabledScenePaths,
            StaticMapAndroidBuildManifestSnapshot manifest,
            Func<string, bool> sceneExists,
            Func<string, bool> isOwnedChunkScene,
            Func<string> computeCanonicalDependencyHash,
            Func<string, IReadOnlyList<string>, bool> integrityMatches)
        {
            if (sceneExists == null || isOwnedChunkScene == null ||
                computeCanonicalDependencyHash == null || integrityMatches == null)
                throw new ArgumentNullException("Android scene resolver delegates are required.");

            return Resolve(
                enabledScenePaths,
                manifest == null ? null : new[] { manifest },
                sceneExists,
                (_, path) => isOwnedChunkScene(path),
                _ => computeCanonicalDependencyHash(),
                (_, contentHash, paths) => integrityMatches(contentHash, paths));
        }

        internal static string[] Resolve(
            IEnumerable<string> enabledScenePaths,
            IReadOnlyList<StaticMapAndroidBuildManifestSnapshot> manifests,
            Func<string, bool> sceneExists,
            Func<string, string, bool> isOwnedChunkScene,
            Func<string, string> computeCanonicalDependencyHash,
            Func<string, string, IReadOnlyList<string>, bool> integrityMatches)
        {
            if (enabledScenePaths == null)
                throw new InvalidOperationException("Android build scenes are missing.");
            if (manifests == null || manifests.Count == 0)
                throw new InvalidOperationException("Catalog-selected static map presentation manifests are missing.");
            if (sceneExists == null || isOwnedChunkScene == null ||
                computeCanonicalDependencyHash == null || integrityMatches == null)
                throw new ArgumentNullException("Android scene resolver delegates are required.");

            var chunks = new List<string>();
            var manifestSet = new HashSet<string>(StringComparer.Ordinal);
            var operationMapIds = new HashSet<string>(StringComparer.Ordinal);
            for (int manifestIndex = 0; manifestIndex < manifests.Count; manifestIndex++)
            {
                StaticMapAndroidBuildManifestSnapshot manifest = manifests[manifestIndex];
                ValidateAndCollectManifest(
                    manifest,
                    manifestIndex,
                    sceneExists,
                    isOwnedChunkScene,
                    computeCanonicalDependencyHash,
                    integrityMatches,
                    operationMapIds,
                    manifestSet,
                    chunks);
            }

            var result = new List<string>();
            var included = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in enabledScenePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException("Enabled build settings contain an invalid scene path.");

                bool generatedScene = IsGeneratedChunkScenePath(path);
                if (generatedScene)
                {
                    if (!manifestSet.Contains(path))
                        throw new InvalidOperationException($"Enabled stale generated scene is outside the manifest: {path}");
                    continue;
                }

                if (!sceneExists(path))
                    throw new InvalidOperationException($"Enabled base scene is missing: {path}");

                if (included.Add(path))
                    result.Add(path);
            }

            // Validated presentation scenes are delivered by the local Addressables build.
            return result.ToArray();
        }

        private static StaticMapAndroidBuildManifestSnapshot[] LoadCatalogSelectedManifestSnapshots()
        {
            string[] catalogPaths = AssetDatabase
                .GetDependencies(StaticMapPresentationBaker.CanonicalMatchScenePath, true)
                .Where(path => AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(path) != null)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (catalogPaths.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Canonical Match scene must resolve exactly one operation-map catalog; found {catalogPaths.Length}.");
            }

            OperationMapCatalogConfig catalog =
                AssetDatabase.LoadAssetAtPath<OperationMapCatalogConfig>(catalogPaths[0]);
            if (!catalog.TryValidate(out string catalogError))
                throw new InvalidOperationException($"Operation-map build catalog is invalid: {catalogError}");

            ReadOnlySpan<OperationMapDefinition> definitions = catalog.Definitions;
            var snapshots = new StaticMapAndroidBuildManifestSnapshot[definitions.Length];
            for (int index = 0; index < definitions.Length; index++)
            {
                OperationMapDefinition definition = definitions[index];
                if (!StaticMapPresentationOutputPathContract.TryResolveManifestAssetPath(
                        definition.OperationMapId,
                        out string manifestPath,
                        out string pathError))
                {
                    throw new InvalidOperationException(pathError);
                }

                StaticMapPresentationManifest manifest =
                    AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(manifestPath);
                if (manifest == null)
                {
                    throw new InvalidOperationException(
                        $"Catalog-selected static map presentation manifest is missing: {manifestPath}");
                }

                snapshots[index] = CreateSnapshot(manifest);
            }

            return snapshots;
        }

        private static StaticMapAndroidBuildManifestSnapshot CreateSnapshot(
            StaticMapPresentationManifest manifest)
        {
            return new StaticMapAndroidBuildManifestSnapshot(
                manifest.SchemaVersion,
                manifest.OperationMapId,
                manifest.CanonicalSceneGuid,
                manifest.CanonicalScenePath,
                manifest.CanonicalSceneDependencyHash,
                manifest.ContentHash,
                manifest.Chunks?.Select(chunk => chunk?.ScenePath).ToArray());
        }

        private static void ValidateAndCollectManifest(
            StaticMapAndroidBuildManifestSnapshot manifest,
            int manifestIndex,
            Func<string, bool> sceneExists,
            Func<string, string, bool> isOwnedChunkScene,
            Func<string, string> computeCanonicalDependencyHash,
            Func<string, string, IReadOnlyList<string>, bool> integrityMatches,
            HashSet<string> operationMapIds,
            HashSet<string> manifestSet,
            List<string> chunks)
        {
            if (manifest == null)
                throw new InvalidOperationException($"Catalog-selected manifest {manifestIndex} is missing.");
            if (!StaticMapPresentationManifest.IsSchemaReadable(manifest.SchemaVersion))
                throw new InvalidOperationException($"Static map presentation manifest schema is unsupported: {manifest.SchemaVersion}.");
            if (!StaticMapPresentationManifest.HasRequiredIdentity(
                    manifest.SchemaVersion,
                    manifest.OperationMapId,
                    manifest.CanonicalSceneGuid,
                    manifest.CanonicalScenePath))
                throw new InvalidOperationException("Static map presentation manifest identity is incomplete.");

            string operationMapId = string.IsNullOrWhiteSpace(manifest.OperationMapId)
                ? StaticMapPresentationBaker.CurrentOperationMapId
                : manifest.OperationMapId;
            if (!operationMapIds.Add(operationMapId))
                throw new InvalidOperationException($"Catalog-selected manifest set contains duplicate map id: {operationMapId}");
            if (!sceneExists(manifest.CanonicalScenePath))
                throw new InvalidOperationException(
                    $"Canonical operation-map source scene is missing: {manifest.CanonicalScenePath}");
            if (string.IsNullOrWhiteSpace(manifest.ContentHash))
                throw new InvalidOperationException("Static map presentation manifest content hash is missing.");
            if (string.IsNullOrWhiteSpace(manifest.CanonicalSceneDependencyHash))
                throw new InvalidOperationException("Static map presentation manifest canonical dependency hash is missing.");

            string actualCanonicalDependencyHash =
                computeCanonicalDependencyHash(manifest.CanonicalScenePath);
            if (!string.Equals(manifest.CanonicalSceneDependencyHash, actualCanonicalDependencyHash, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Static map presentation manifest for '{operationMapId}' is stale for canonical scene dependencies. " +
                    $"Expected '{manifest.CanonicalSceneDependencyHash}', actual '{actualCanonicalDependencyHash ?? "<null>"}'.");
            }
            if (manifest.ChunkScenePaths == null || manifest.ChunkScenePaths.Count == 0)
                throw new InvalidOperationException("Static map presentation manifest has no chunk scenes.");

            var mapChunks = new List<string>(manifest.ChunkScenePaths.Count);
            for (int index = 0; index < manifest.ChunkScenePaths.Count; index++)
            {
                string path = manifest.ChunkScenePaths[index];
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException($"Static map presentation manifest chunk {index} has no scene path.");
                if (!isOwnedChunkScene(operationMapId, path))
                    throw new InvalidOperationException($"Manifest '{operationMapId}' chunk {index} is not an owned scene: {path}");
                if (!manifestSet.Add(path))
                    throw new InvalidOperationException($"Catalog-selected manifests contain a duplicate chunk scene: {path}");
                if (!sceneExists(path))
                    throw new InvalidOperationException($"Static map presentation chunk scene is missing: {path}");
                mapChunks.Add(path);
                chunks.Add(path);
            }

            if (!integrityMatches(operationMapId, manifest.ContentHash, mapChunks))
            {
                throw new InvalidOperationException(
                    $"Static map presentation integrity ledger does not match manifest '{operationMapId}'.");
            }
        }

        private static bool IsGeneratedChunkScenePath(string path)
        {
            return path.StartsWith(
                       StaticMapPresentationOutputPathContract.OperationMapsRoot + "/",
                       StringComparison.Ordinal) &&
                   path.Contains("/Scenes/", StringComparison.Ordinal) &&
                   path.EndsWith(".unity", StringComparison.Ordinal);
        }

        private static bool SceneExists(string projectRoot, string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            string normalizedRoot = Path.GetFullPath(projectRoot);
            string fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, assetPath));
            string requiredPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? normalizedRoot
                : normalizedRoot + Path.DirectorySeparatorChar;
            return fullPath.StartsWith(requiredPrefix, StringComparison.Ordinal) &&
                   File.Exists(fullPath) &&
                   !string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(assetPath));
        }
    }
}
