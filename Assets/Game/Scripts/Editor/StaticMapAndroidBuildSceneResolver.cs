using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Rendering;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    internal sealed class StaticMapAndroidBuildManifestSnapshot
    {
        internal int SchemaVersion { get; }
        internal string CanonicalScenePath { get; }
        internal string CanonicalSceneDependencyHash { get; }
        internal string ContentHash { get; }
        internal IReadOnlyList<string> ChunkScenePaths { get; }

        internal StaticMapAndroidBuildManifestSnapshot(
            int schemaVersion,
            string canonicalScenePath,
            string canonicalSceneDependencyHash,
            string contentHash,
            IReadOnlyList<string> chunkScenePaths)
        {
            SchemaVersion = schemaVersion;
            CanonicalScenePath = canonicalScenePath;
            CanonicalSceneDependencyHash = canonicalSceneDependencyHash;
            ContentHash = contentHash;
            ChunkScenePaths = chunkScenePaths;
        }
    }

    internal static class StaticMapAndroidBuildSceneResolver
    {
        private const string GeneratedSceneFolder =
            StaticMapPresentationBaker.SceneOutputFolder + "/";

        internal static string[] ResolveForCurrentProject(IEnumerable<string> enabledScenePaths)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new InvalidOperationException("Unable to resolve the project root for Android build scenes.");

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(StaticMapPresentationBaker.ManifestPath);
            StaticMapAndroidBuildManifestSnapshot snapshot = manifest == null
                ? null
                : new StaticMapAndroidBuildManifestSnapshot(
                    manifest.SchemaVersion,
                    manifest.CanonicalScenePath,
                    manifest.CanonicalSceneDependencyHash,
                    manifest.ContentHash,
                    manifest.Chunks?.Select(chunk => chunk?.ScenePath).ToArray());

            return Resolve(
                enabledScenePaths,
                snapshot,
                assetPath => SceneExists(projectRoot, assetPath),
                StaticMapPresentationOutputOwnership.IsOwnedScenePath,
                () => StaticMapPresentationCanonicalSourceHash.Compute(
                    StaticMapPresentationBaker.CanonicalMatchScenePath),
                (contentHash, scenePaths) =>
                    StaticMapPresentationSceneIntegrity.TryLoadAndValidate(
                        projectRoot,
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
            if (enabledScenePaths == null)
                throw new InvalidOperationException("Android build scenes are missing.");
            if (manifest == null)
                throw new InvalidOperationException("Static map presentation manifest is missing.");
            if (sceneExists == null || isOwnedChunkScene == null ||
                computeCanonicalDependencyHash == null || integrityMatches == null)
                throw new ArgumentNullException("Android scene resolver delegates are required.");
            if (manifest.SchemaVersion != StaticMapPresentationManifest.CurrentSchemaVersion)
                throw new InvalidOperationException($"Static map presentation manifest schema is unsupported: {manifest.SchemaVersion}.");
            if (!string.Equals(
                    manifest.CanonicalScenePath,
                    StaticMapPresentationBaker.CanonicalMatchScenePath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Static map presentation manifest does not target the canonical Match scene.");
            }
            if (string.IsNullOrWhiteSpace(manifest.ContentHash))
                throw new InvalidOperationException("Static map presentation manifest content hash is missing.");
            if (string.IsNullOrWhiteSpace(manifest.CanonicalSceneDependencyHash))
            {
                throw new InvalidOperationException(
                    "Static map presentation manifest canonical dependency hash is missing.");
            }
            string actualCanonicalDependencyHash = computeCanonicalDependencyHash();
            if (!string.Equals(
                    manifest.CanonicalSceneDependencyHash,
                    actualCanonicalDependencyHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Static map presentation manifest is stale for the canonical Match scene dependencies. " +
                    $"Expected '{manifest.CanonicalSceneDependencyHash}', actual '{actualCanonicalDependencyHash ?? "<null>"}'.");
            }
            if (manifest.ChunkScenePaths == null || manifest.ChunkScenePaths.Count == 0)
                throw new InvalidOperationException("Static map presentation manifest has no chunk scenes.");

            var chunks = new List<string>(manifest.ChunkScenePaths.Count);
            var manifestSet = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < manifest.ChunkScenePaths.Count; index++)
            {
                string path = manifest.ChunkScenePaths[index];
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException($"Static map presentation manifest chunk {index} has no scene path.");
                if (!isOwnedChunkScene(path))
                    throw new InvalidOperationException($"Static map presentation manifest chunk {index} is not an owned scene: {path}");
                if (!manifestSet.Add(path))
                    throw new InvalidOperationException($"Static map presentation manifest contains a duplicate chunk scene: {path}");
                if (!sceneExists(path))
                    throw new InvalidOperationException($"Static map presentation chunk scene is missing: {path}");
                chunks.Add(path);
            }

            var result = new List<string>();
            var included = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in enabledScenePaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new InvalidOperationException("Enabled build settings contain an invalid scene path.");

                bool generatedScene = path.StartsWith(GeneratedSceneFolder, StringComparison.Ordinal) &&
                                      path.EndsWith(".unity", StringComparison.Ordinal);
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

            string canonicalScene = StaticMapPresentationBaker.CanonicalMatchScenePath;
            if (!included.Contains(canonicalScene) || !sceneExists(canonicalScene))
                throw new InvalidOperationException($"Enabled canonical Match base scene is missing: {canonicalScene}");
            if (!integrityMatches(manifest.ContentHash, chunks))
                throw new InvalidOperationException("Static map presentation integrity ledger does not match the manifest chunk scenes.");

            for (int index = 0; index < chunks.Count; index++)
            {
                if (included.Add(chunks[index]))
                    result.Add(chunks[index]);
            }

            return result.ToArray();
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
