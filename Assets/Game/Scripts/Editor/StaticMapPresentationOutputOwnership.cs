using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Rendering;
using UnityEngine;

namespace Game.Editor
{
    internal static class StaticMapPresentationOutputOwnership
    {
        private const string LegacySceneFilePrefix = "StaticMapPresentation_chunk_";
        private const string SceneExtension = ".unity";

        internal static string[] CaptureOwnedScenePaths(StaticMapPresentationManifest manifest)
        {
            return CaptureOwnedScenePaths(
                manifest,
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationBaker.OutputRoot);
        }

        internal static string[] CaptureOwnedScenePaths(
            StaticMapPresentationManifest manifest,
            string operationMapId,
            string outputRoot)
        {
            if (manifest == null)
                return Array.Empty<string>();
            RequireManifestOwner(manifest, operationMapId, outputRoot);

            string[] paths = new string[manifest.Chunks.Count];
            for (int i = 0; i < manifest.Chunks.Count; i++)
            {
                StaticMapPresentationChunkEntry chunk = manifest.Chunks[i];
                if (chunk == null)
                    throw new InvalidOperationException($"Static map presentation manifest contains a null chunk at index {i}.");

                paths[i] = RequireOwnedScenePath(
                    operationMapId,
                    outputRoot,
                    chunk.ScenePath,
                    $"manifest chunk {i}");
            }

            return paths
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        internal static string[] ComputeStaleScenePaths(
            IEnumerable<string> previousOwnedPaths,
            IEnumerable<string> expectedPaths)
        {
            HashSet<string> expected = BuildValidatedSet(expectedPaths, nameof(expectedPaths));
            HashSet<string> previous = BuildValidatedSet(previousOwnedPaths, nameof(previousOwnedPaths));
            previous.ExceptWith(expected);
            return previous.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }

        internal static int DeleteStaleSceneAssets(
            IEnumerable<string> previousOwnedPaths,
            IEnumerable<string> expectedPaths,
            Func<string, bool> assetExists,
            Func<string, bool> deleteAsset)
        {
            return DeleteStaleSceneAssets(
                previousOwnedPaths,
                expectedPaths,
                assetExists,
                _ => false,
                deleteAsset,
                _ => false);
        }

        internal static int DeleteStaleSceneAssets(
            string operationMapId,
            string outputRoot,
            StaticMapPresentationManifest previousManifest,
            IEnumerable<string> expectedPaths,
            Func<string, bool> databaseAssetExists,
            Func<string, bool> physicalAssetExists,
            Func<string, bool> deleteDatabaseAsset,
            Func<string, bool> deletePhysicalAsset)
        {
            RequireDeleteDelegates(
                databaseAssetExists,
                physicalAssetExists,
                deleteDatabaseAsset,
                deletePhysicalAsset);
            string[] previousOwnedPaths = CaptureOwnedScenePaths(
                previousManifest,
                operationMapId,
                outputRoot);
            HashSet<string> expected = BuildValidatedSet(
                operationMapId,
                outputRoot,
                expectedPaths,
                nameof(expectedPaths));
            HashSet<string> previous = BuildValidatedSet(
                operationMapId,
                outputRoot,
                previousOwnedPaths,
                nameof(previousManifest));
            previous.ExceptWith(expected);
            return DeleteValidatedStaleSceneAssets(
                previous.OrderBy(path => path, StringComparer.Ordinal).ToArray(),
                databaseAssetExists,
                physicalAssetExists,
                deleteDatabaseAsset,
                deletePhysicalAsset);
        }

        internal static int DeleteStaleSceneAssets(
            IEnumerable<string> previousOwnedPaths,
            IEnumerable<string> expectedPaths,
            Func<string, bool> databaseAssetExists,
            Func<string, bool> physicalAssetExists,
            Func<string, bool> deleteDatabaseAsset,
            Func<string, bool> deletePhysicalAsset)
        {
            RequireDeleteDelegates(
                databaseAssetExists,
                physicalAssetExists,
                deleteDatabaseAsset,
                deletePhysicalAsset);

            string[] stalePaths = ComputeStaleScenePaths(previousOwnedPaths, expectedPaths);
            return DeleteValidatedStaleSceneAssets(
                stalePaths,
                databaseAssetExists,
                physicalAssetExists,
                deleteDatabaseAsset,
                deletePhysicalAsset);
        }

        private static int DeleteValidatedStaleSceneAssets(
            IReadOnlyList<string> stalePaths,
            Func<string, bool> databaseAssetExists,
            Func<string, bool> physicalAssetExists,
            Func<string, bool> deleteDatabaseAsset,
            Func<string, bool> deletePhysicalAsset)
        {
            int deleted = 0;
            for (int i = 0; i < stalePaths.Count; i++)
            {
                string path = stalePaths[i];
                bool existedInDatabase = databaseAssetExists(path);
                bool existedPhysically = physicalAssetExists(path);
                if (!existedInDatabase && !existedPhysically)
                    continue;

                bool databaseDeleteAccepted = !existedInDatabase || deleteDatabaseAsset(path);
                if (physicalAssetExists(path) && !deletePhysicalAsset(path))
                    throw new InvalidOperationException($"Failed to delete manifest-owned stale scene: {path}");
                if (physicalAssetExists(path) || (!databaseDeleteAccepted && !existedPhysically))
                    throw new InvalidOperationException($"Failed to delete manifest-owned stale scene: {path}");
                deleted++;
            }

            return deleted;
        }

        private static void RequireDeleteDelegates(
            Func<string, bool> databaseAssetExists,
            Func<string, bool> physicalAssetExists,
            Func<string, bool> deleteDatabaseAsset,
            Func<string, bool> deletePhysicalAsset)
        {
            if (databaseAssetExists == null)
                throw new ArgumentNullException(nameof(databaseAssetExists));
            if (physicalAssetExists == null)
                throw new ArgumentNullException(nameof(physicalAssetExists));
            if (deleteDatabaseAsset == null)
                throw new ArgumentNullException(nameof(deleteDatabaseAsset));
            if (deletePhysicalAsset == null)
                throw new ArgumentNullException(nameof(deletePhysicalAsset));
        }

        internal static bool CanReuseExpectedScenes(
            int previousSchemaVersion,
            string previousCanonicalScenePath,
            float previousChunkSize,
            string previousContentHash,
            string expectedCanonicalScenePath,
            float expectedChunkSize,
            string expectedContentHash,
            IEnumerable<string> previousOwnedPaths,
            IEnumerable<string> expectedPaths,
            Func<string, bool> sceneIntegrityIsValid)
        {
            return CanReuseExpectedScenes(
                previousSchemaVersion,
                previousCanonicalScenePath,
                previousChunkSize,
                previousContentHash,
                expectedCanonicalScenePath,
                expectedChunkSize,
                expectedContentHash,
                previousOwnedPaths,
                expectedPaths,
                sceneIntegrityIsValid,
                out _);
        }

        internal static bool CanReuseExpectedScenes(
            int previousSchemaVersion,
            string previousCanonicalScenePath,
            float previousChunkSize,
            string previousContentHash,
            string expectedCanonicalScenePath,
            float expectedChunkSize,
            string expectedContentHash,
            IEnumerable<string> previousOwnedPaths,
            IEnumerable<string> expectedPaths,
            Func<string, bool> sceneIntegrityIsValid,
            out string rejectionReason)
        {
            return CanReuseExpectedScenes(
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationBaker.OutputRoot,
                previousSchemaVersion,
                previousCanonicalScenePath,
                previousChunkSize,
                previousContentHash,
                expectedCanonicalScenePath,
                expectedChunkSize,
                expectedContentHash,
                previousOwnedPaths,
                expectedPaths,
                sceneIntegrityIsValid,
                out rejectionReason);
        }

        internal static bool CanReuseExpectedScenes(
            string operationMapId,
            string outputRoot,
            int previousSchemaVersion,
            string previousCanonicalScenePath,
            float previousChunkSize,
            string previousContentHash,
            string expectedCanonicalScenePath,
            float expectedChunkSize,
            string expectedContentHash,
            IEnumerable<string> previousOwnedPaths,
            IEnumerable<string> expectedPaths,
            Func<string, bool> sceneIntegrityIsValid,
            out string rejectionReason)
        {
            if (previousSchemaVersion <= 0)
            {
                rejectionReason = "manifest-missing";
                return false;
            }
            if (sceneIntegrityIsValid == null)
            {
                rejectionReason = "scene-integrity-delegate-missing";
                return false;
            }
            if (!StaticMapPresentationManifest.IsSchemaReadable(previousSchemaVersion))
            {
                rejectionReason = "schema-version-unsupported";
                return false;
            }
            if (!string.Equals(previousCanonicalScenePath, expectedCanonicalScenePath, StringComparison.Ordinal))
            {
                rejectionReason = "canonical-scene-path-changed";
                return false;
            }
            if (Math.Abs(previousChunkSize - expectedChunkSize) > 0.0001f)
            {
                rejectionReason = "chunk-size-changed";
                return false;
            }
            if (!string.Equals(previousContentHash, expectedContentHash, StringComparison.Ordinal))
            {
                rejectionReason = "presentation-content-changed";
                return false;
            }

            string[] owned = BuildValidatedSet(
                    operationMapId,
                    outputRoot,
                    previousOwnedPaths,
                    nameof(previousOwnedPaths))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            string[] expected = BuildValidatedSet(
                    operationMapId,
                    outputRoot,
                    expectedPaths,
                    nameof(expectedPaths))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            if (!owned.SequenceEqual(expected, StringComparer.Ordinal))
            {
                rejectionReason = "owned-scene-set-changed";
                return false;
            }

            for (int i = 0; i < expected.Length; i++)
            {
                if (!sceneIntegrityIsValid(expected[i]))
                {
                    rejectionReason = $"owned-scene-integrity-invalid:{expected[i]}";
                    return false;
                }
            }

            rejectionReason = "none";
            return true;
        }

        internal static bool IsOwnedScenePath(string path)
        {
            return IsOwnedScenePath(
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationBaker.OutputRoot,
                path);
        }

        internal static bool IsOwnedScenePath(
            string operationMapId,
            string outputRoot,
            string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path.IndexOf('\\') >= 0)
                return false;
            if (!StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
                    operationMapId,
                    out string expectedOutputRoot,
                    out _) ||
                !string.Equals(outputRoot, expectedOutputRoot, StringComparison.Ordinal))
            {
                return false;
            }

            string folderPrefix = outputRoot + "/Scenes/";
            if (!path.StartsWith(folderPrefix, StringComparison.Ordinal) ||
                !path.EndsWith(SceneExtension, StringComparison.Ordinal))
            {
                return false;
            }

            string fileName = path.Substring(
                folderPrefix.Length,
                path.Length - folderPrefix.Length - SceneExtension.Length);
            string sceneFilePrefix =
                StaticMapPresentationOutputPathContract.RequireSceneFilePrefix(operationMapId) + "chunk_";
            string coordinates;
            if (fileName.StartsWith(sceneFilePrefix, StringComparison.Ordinal))
                coordinates = fileName.Substring(sceneFilePrefix.Length);
            else if (IsCurrentCompatibilityOwner(operationMapId, outputRoot) &&
                     fileName.StartsWith(LegacySceneFilePrefix, StringComparison.Ordinal))
                coordinates = fileName.Substring(LegacySceneFilePrefix.Length);
            else
                return false;

            int separator = coordinates.IndexOf('_');
            if (separator <= 0 || separator != coordinates.LastIndexOf('_'))
                return false;

            return IsCoordinateToken(coordinates.Substring(0, separator)) &&
                   IsCoordinateToken(coordinates.Substring(separator + 1));
        }

        private static HashSet<string> BuildValidatedSet(IEnumerable<string> paths, string argumentName)
        {
            return BuildValidatedSet(
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationBaker.OutputRoot,
                paths,
                argumentName);
        }

        private static HashSet<string> BuildValidatedSet(
            string operationMapId,
            string outputRoot,
            IEnumerable<string> paths,
            string argumentName)
        {
            if (paths == null)
                throw new ArgumentNullException(argumentName);

            HashSet<string> result = new(StringComparer.Ordinal);
            int index = 0;
            foreach (string path in paths)
            {
                result.Add(RequireOwnedScenePath(
                    operationMapId,
                    outputRoot,
                    path,
                    $"{argumentName}[{index}]"));
                index++;
            }

            return result;
        }

        private static string RequireOwnedScenePath(string path, string owner)
        {
            return RequireOwnedScenePath(
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationBaker.OutputRoot,
                path,
                owner);
        }

        private static string RequireOwnedScenePath(
            string operationMapId,
            string outputRoot,
            string path,
            string owner)
        {
            if (!IsOwnedScenePath(operationMapId, outputRoot, path))
                throw new InvalidOperationException($"{owner} is not a valid static map presentation scene path: '{path ?? "<null>"}'.");
            return path;
        }

        private static void RequireManifestOwner(
            StaticMapPresentationManifest manifest,
            string operationMapId,
            string outputRoot)
        {
            bool currentCompatibility = IsCurrentCompatibilityOwner(operationMapId, outputRoot);
            bool ownerMatches = manifest.SchemaVersion == 1
                ? currentCompatibility
                : string.Equals(manifest.OperationMapId, operationMapId, StringComparison.Ordinal);
            if (!ownerMatches)
                throw new InvalidOperationException("Static map presentation manifest belongs to another operation map.");
        }

        private static bool IsCurrentCompatibilityOwner(string operationMapId, string outputRoot)
        {
            return string.Equals(
                       operationMapId,
                       StaticMapPresentationBaker.CurrentOperationMapId,
                       StringComparison.Ordinal) &&
                   string.Equals(outputRoot, StaticMapPresentationBaker.OutputRoot, StringComparison.Ordinal);
        }

        private static bool IsCoordinateToken(string token)
        {
            if (token.Length < 4 || (token[0] != 'p' && token[0] != 'n'))
                return false;

            for (int i = 1; i < token.Length; i++)
            {
                if (token[i] < '0' || token[i] > '9')
                    return false;
            }

            return true;
        }
    }

    internal sealed class StaticMapPresentationBakeTransaction : IDisposable
    {
        private readonly string backupRoot;
        private readonly List<FileSnapshot> snapshots;
        private bool completed;

        private sealed class FileSnapshot
        {
            internal string DestinationPath;
            internal string BackupPath;
            internal bool Existed;
        }

        private StaticMapPresentationBakeTransaction(
            string backupRoot,
            List<FileSnapshot> snapshots)
        {
            this.backupRoot = backupRoot;
            this.snapshots = snapshots;
        }

        internal static StaticMapPresentationBakeTransaction Begin(
            string projectRoot,
            IEnumerable<string> mutableAssetPaths)
        {
            return Begin(
                projectRoot,
                StaticMapPresentationBaker.CurrentOperationMapId,
                StaticMapPresentationBaker.OutputRoot,
                StaticMapPresentationBaker.ManifestPath,
                StaticMapPresentationSceneIntegrity.CurrentIntegrityAssetPath,
                mutableAssetPaths);
        }

        internal static StaticMapPresentationBakeTransaction Begin(
            string projectRoot,
            string operationMapId,
            string outputRoot,
            string manifestPath,
            string integrityPath,
            IEnumerable<string> mutableAssetPaths)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            if (mutableAssetPaths == null)
                throw new ArgumentNullException(nameof(mutableAssetPaths));
            RequireTransactionOwner(
                operationMapId,
                outputRoot,
                manifestPath,
                integrityPath);

            string normalizedProjectRoot = Path.GetFullPath(projectRoot);
            string backupRoot = Path.Combine(
                normalizedProjectRoot,
                "Library",
                "StaticMapPresentationBakeTransactions",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(backupRoot);

            List<FileSnapshot> snapshots = new();
            try
            {
                string[] assetPaths = mutableAssetPaths
                    .Select(path => RequireMutableAssetPath(
                        operationMapId,
                        outputRoot,
                        manifestPath,
                        integrityPath,
                        path))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray();
                for (int i = 0; i < assetPaths.Length; i++)
                {
                    CaptureFile(normalizedProjectRoot, backupRoot, assetPaths[i], snapshots);
                    CaptureFile(normalizedProjectRoot, backupRoot, assetPaths[i] + ".meta", snapshots);
                }

                return new StaticMapPresentationBakeTransaction(backupRoot, snapshots);
            }
            catch
            {
                DeleteBackupDirectory(backupRoot);
                throw;
            }
        }

        internal void Commit()
        {
            ThrowIfCompleted();
            completed = true;
            try
            {
                DeleteBackupDirectory(backupRoot);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Static map presentation bake committed, but its rollback journal could not be removed: " +
                    $"{backupRoot}\n{exception}");
            }
        }

        internal void Rollback()
        {
            if (completed)
                return;

            List<Exception> failures = null;
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                FileSnapshot snapshot = snapshots[i];
                try
                {
                    if (snapshot.Existed)
                    {
                        string directory = Path.GetDirectoryName(snapshot.DestinationPath);
                        if (!string.IsNullOrEmpty(directory))
                            Directory.CreateDirectory(directory);
                        File.Copy(snapshot.BackupPath, snapshot.DestinationPath, true);
                    }
                    else if (File.Exists(snapshot.DestinationPath))
                    {
                        File.Delete(snapshot.DestinationPath);
                    }
                }
                catch (Exception exception)
                {
                    failures ??= new List<Exception>();
                    failures.Add(new IOException(
                        $"Failed to restore static map presentation transaction file: {snapshot.DestinationPath}",
                        exception));
                }
            }

            completed = true;
            try
            {
                DeleteBackupDirectory(backupRoot);
            }
            catch (Exception exception)
            {
                failures ??= new List<Exception>();
                failures.Add(exception);
            }

            if (failures != null)
                throw new AggregateException("Static map presentation bake rollback was incomplete.", failures);
        }

        public void Dispose()
        {
            if (!completed)
                Rollback();
        }

        private static void CaptureFile(
            string projectRoot,
            string backupRoot,
            string relativePath,
            List<FileSnapshot> snapshots)
        {
            string destinationPath = ResolveProjectPath(projectRoot, relativePath);
            bool existed = File.Exists(destinationPath);
            string backupPath = Path.Combine(backupRoot, snapshots.Count.ToString("D6"));
            if (existed)
                File.Copy(destinationPath, backupPath, false);

            snapshots.Add(new FileSnapshot
            {
                DestinationPath = destinationPath,
                BackupPath = backupPath,
                Existed = existed
            });
        }

        private static string RequireMutableAssetPath(
            string operationMapId,
            string outputRoot,
            string manifestPath,
            string integrityPath,
            string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) ||
                assetPath.IndexOf('\\') >= 0 ||
                Path.IsPathRooted(assetPath) ||
                assetPath.Split('/').Any(segment => string.Equals(segment, "..", StringComparison.Ordinal)) ||
                (!string.Equals(assetPath, manifestPath, StringComparison.Ordinal) &&
                 !string.Equals(assetPath, integrityPath, StringComparison.Ordinal) &&
                 !StaticMapPresentationOutputOwnership.IsOwnedScenePath(
                     operationMapId,
                     outputRoot,
                     assetPath)))
            {
                throw new InvalidOperationException(
                    $"Refusing to journal a path outside static map presentation ownership: '{assetPath ?? "<null>"}'.");
            }

            return assetPath;
        }

        private static void RequireTransactionOwner(
            string operationMapId,
            string outputRoot,
            string manifestPath,
            string integrityPath)
        {
            if (!StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
                    operationMapId,
                    out string expectedOutputRoot,
                    out string ownershipError) ||
                !string.Equals(outputRoot, expectedOutputRoot, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    ownershipError ?? "Transaction output root does not match its operation-map owner.");
            }

            if (!IsOwnedFile(manifestPath, outputRoot, ".asset"))
                throw new InvalidOperationException("Transaction manifest is outside its operation-map output root.");
            if (!StaticMapPresentationOutputPathContract.TryResolveIntegrityAssetPath(
                    operationMapId,
                    out string expectedIntegrityPath,
                    out _) ||
                !string.Equals(integrityPath, expectedIntegrityPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Transaction integrity ledger does not match its operation-map owner.");
            }
        }

        private static bool IsOwnedFile(string path, string outputRoot, string extension)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.IndexOf('\\') < 0 &&
                   !Path.IsPathRooted(path) &&
                   !path.Split('/').Any(segment => string.Equals(segment, "..", StringComparison.Ordinal)) &&
                   path.StartsWith(outputRoot + "/", StringComparison.Ordinal) &&
                   path.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveProjectPath(string projectRoot, string relativePath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            string requiredPrefix = projectRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? projectRoot
                : projectRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(requiredPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException($"Resolved path escaped the project root: {relativePath}");
            return fullPath;
        }

        private void ThrowIfCompleted()
        {
            if (completed)
                throw new InvalidOperationException("Static map presentation bake transaction is already complete.");
        }

        private static void DeleteBackupDirectory(string path)
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
    }

    internal sealed class StaticMapPresentationSceneIntegrity
    {
        internal static string CurrentIntegrityAssetPath =>
            StaticMapPresentationOutputPathContract.RequireIntegrityAssetPath(
                StaticMapPresentationBaker.CurrentOperationMapId);

        private const int CurrentSchemaVersion = 1;

        [Serializable]
        private sealed class IntegrityDocument
        {
            public int schemaVersion;
            public string contentHash;
            public IntegrityEntry[] scenes;
        }

        [Serializable]
        private sealed class IntegrityEntry
        {
            public string scenePath;
            public string fileHash;
            public string metaHash;
        }

        private readonly string projectRoot;
        private readonly string operationMapId;
        private readonly string outputRoot;
        private readonly Dictionary<string, string> expectedFileHashes;

        private StaticMapPresentationSceneIntegrity(
            string projectRoot,
            string operationMapId,
            string outputRoot,
            Dictionary<string, string> expectedFileHashes)
        {
            this.projectRoot = Path.GetFullPath(projectRoot);
            this.operationMapId = operationMapId;
            this.outputRoot = outputRoot;
            this.expectedFileHashes = expectedFileHashes;
        }

        internal static bool TryLoadAndValidate(
            string projectRoot,
            string operationMapId,
            string integrityAssetPath,
            string expectedContentHash,
            IEnumerable<string> expectedScenePaths,
            out StaticMapPresentationSceneIntegrity integrity,
            out string rejectionReason)
        {
            integrity = null;
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            if (!StaticMapPresentationOutputPathContract.TryResolveIntegrityAssetPath(
                    operationMapId,
                    out string expectedIntegrityAssetPath,
                    out _) ||
                !string.Equals(integrityAssetPath, expectedIntegrityAssetPath, StringComparison.Ordinal))
            {
                rejectionReason = "integrity-ledger-owner-changed";
                return false;
            }
            if (string.IsNullOrWhiteSpace(expectedContentHash))
            {
                rejectionReason = "integrity-content-hash-missing";
                return false;
            }

            StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
                operationMapId,
                out string outputRoot,
                out _);
            string[] expected = RequireScenePaths(operationMapId, outputRoot, expectedScenePaths);
            string integrityFilePath = ResolveProjectPath(projectRoot, integrityAssetPath);
            if (!File.Exists(integrityFilePath))
            {
                rejectionReason = "integrity-ledger-missing";
                return false;
            }

            IntegrityDocument document;
            try
            {
                document = JsonUtility.FromJson<IntegrityDocument>(File.ReadAllText(integrityFilePath));
            }
            catch (Exception exception)
            {
                rejectionReason = $"integrity-ledger-unreadable:{exception.GetType().Name}";
                return false;
            }

            if (document == null || document.schemaVersion != CurrentSchemaVersion)
            {
                rejectionReason = "integrity-schema-version-changed";
                return false;
            }
            if (!string.Equals(document.contentHash, expectedContentHash, StringComparison.Ordinal))
            {
                rejectionReason = "integrity-content-hash-changed";
                return false;
            }

            Dictionary<string, string> hashes = new(StringComparer.Ordinal);
            IntegrityEntry[] entries = document.scenes ?? Array.Empty<IntegrityEntry>();
            for (int i = 0; i < entries.Length; i++)
            {
                IntegrityEntry entry = entries[i];
                if (entry == null ||
                    !StaticMapPresentationOutputOwnership.IsOwnedScenePath(
                        operationMapId,
                        outputRoot,
                        entry.scenePath) ||
                    string.IsNullOrWhiteSpace(entry.fileHash) ||
                    string.IsNullOrWhiteSpace(entry.metaHash) ||
                    hashes.ContainsKey(entry.scenePath))
                {
                    rejectionReason = $"integrity-entry-invalid:{i}";
                    return false;
                }

                hashes.Add(entry.scenePath, CombineSceneAndMetaHashes(entry.fileHash, entry.metaHash));
            }

            if (!hashes.Keys.OrderBy(path => path, StringComparer.Ordinal)
                    .SequenceEqual(expected, StringComparer.Ordinal))
            {
                rejectionReason = "integrity-scene-set-changed";
                return false;
            }

            StaticMapPresentationSceneIntegrity candidate = new(
                projectRoot,
                operationMapId,
                outputRoot,
                hashes);
            for (int i = 0; i < expected.Length; i++)
            {
                if (!candidate.IsSceneFileValid(expected[i]))
                {
                    rejectionReason = $"integrity-scene-file-changed:{expected[i]}";
                    return false;
                }
            }

            integrity = candidate;
            rejectionReason = "none";
            return true;
        }

        internal static void Write(
            string projectRoot,
            string operationMapId,
            string integrityAssetPath,
            string contentHash,
            IEnumerable<string> scenePaths)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            if (!StaticMapPresentationOutputPathContract.TryResolveIntegrityAssetPath(
                    operationMapId,
                    out string expectedIntegrityAssetPath,
                    out string ownershipError) ||
                !string.Equals(integrityAssetPath, expectedIntegrityAssetPath, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    ownershipError ?? "Integrity ledger path does not match its operation-map owner.");
            }
            if (string.IsNullOrWhiteSpace(contentHash))
                throw new ArgumentException("Content hash is required.", nameof(contentHash));

            StaticMapPresentationOutputPathContract.TryResolveOutputRoot(
                operationMapId,
                out string outputRoot,
                out _);
            string[] paths = RequireScenePaths(operationMapId, outputRoot, scenePaths);
            IntegrityEntry[] entries = new IntegrityEntry[paths.Length];
            for (int i = 0; i < paths.Length; i++)
            {
                string sceneFilePath = ResolveProjectPath(projectRoot, paths[i]);
                if (!File.Exists(sceneFilePath))
                    throw new FileNotFoundException("Generated scene is missing before integrity capture.", sceneFilePath);
                string sceneMetaPath = sceneFilePath + ".meta";
                if (!File.Exists(sceneMetaPath))
                    throw new FileNotFoundException("Generated scene metadata is missing before integrity capture.", sceneMetaPath);
                entries[i] = new IntegrityEntry
                {
                    scenePath = paths[i],
                    fileHash = ComputeFileHash(sceneFilePath),
                    metaHash = ComputeFileHash(sceneMetaPath)
                };
            }

            IntegrityDocument document = new()
            {
                schemaVersion = CurrentSchemaVersion,
                contentHash = contentHash,
                scenes = entries
            };
            string destinationPath = ResolveProjectPath(projectRoot, integrityAssetPath);
            string directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            string temporaryPath = destinationPath + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                File.WriteAllText(temporaryPath, JsonUtility.ToJson(document, true) + Environment.NewLine, Encoding.UTF8);
                if (File.Exists(destinationPath))
                    File.Replace(temporaryPath, destinationPath, null);
                else
                    File.Move(temporaryPath, destinationPath);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        internal bool IsSceneFileValid(string scenePath)
        {
            if (!StaticMapPresentationOutputOwnership.IsOwnedScenePath(
                    operationMapId,
                    outputRoot,
                    scenePath) ||
                !expectedFileHashes.TryGetValue(scenePath, out string expectedHash))
            {
                return false;
            }

            try
            {
                string sceneFilePath = ResolveProjectPath(projectRoot, scenePath);
                string sceneMetaPath = sceneFilePath + ".meta";
                return File.Exists(sceneFilePath) &&
                       File.Exists(sceneMetaPath) &&
                       string.Equals(
                           CombineSceneAndMetaHashes(
                               ComputeFileHash(sceneFilePath),
                               ComputeFileHash(sceneMetaPath)),
                           expectedHash,
                           StringComparison.Ordinal);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        internal static string ComputeFileHash(string filePath)
        {
            using SHA256 algorithm = SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);
            byte[] hash = algorithm.ComputeHash(stream);
            StringBuilder builder = new(hash.Length * 2);
            for (int i = 0; i < hash.Length; i++)
                builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        private static string CombineSceneAndMetaHashes(string sceneHash, string metaHash)
        {
            return sceneHash + ":" + metaHash;
        }

        private static string[] RequireScenePaths(
            string operationMapId,
            string outputRoot,
            IEnumerable<string> scenePaths)
        {
            if (scenePaths == null)
                throw new ArgumentNullException(nameof(scenePaths));

            string[] paths = scenePaths
                .Select(path => StaticMapPresentationOutputOwnership.IsOwnedScenePath(
                        operationMapId,
                        outputRoot,
                        path)
                    ? path
                    : throw new InvalidOperationException($"Invalid static map presentation scene path: '{path ?? "<null>"}'."))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            return paths;
        }

        private static string ResolveProjectPath(string projectRoot, string assetPath)
        {
            string normalizedRoot = Path.GetFullPath(projectRoot);
            string fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, assetPath));
            string requiredPrefix = normalizedRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? normalizedRoot
                : normalizedRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(requiredPrefix, StringComparison.Ordinal))
                throw new InvalidOperationException($"Resolved path escaped the project root: {assetPath}");
            return fullPath;
        }
    }
}
