#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.Build;

    internal readonly struct DenseCityCandidatePackageFile
    {
        internal DenseCityCandidatePackageFile(string sourcePath, string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }

        internal string SourcePath { get; }
        internal string DestinationPath { get; }
    }

    /// <summary>
    /// Adds the isolated dense-city candidate content to one explicitly scoped player build.
    /// It never changes production Addressables settings or copies files into Assets.
    /// </summary>
    internal sealed class OperationMapDenseCityCandidateAndroidPackageDeployment : IDisposable
    {
        internal const string AddressablesDestinationRoot = "aa/DenseCityCandidate";

        private static OperationMapDenseCityCandidateAndroidPackageDeployment active;
        private readonly DenseCityCandidatePackageFile[] files;
        private bool disposed;

        private OperationMapDenseCityCandidateAndroidPackageDeployment(
            DenseCityCandidatePackageFile[] files)
        {
            this.files = files;
        }

        internal static OperationMapDenseCityCandidateAndroidPackageDeployment Begin(
            string projectRoot)
        {
            if (active != null)
                throw new InvalidOperationException(
                    "A dense-city candidate Android package deployment is already active.");

            DenseCityCandidatePackageFile[] files = CreateFilePlan(projectRoot);
            var deployment =
                new OperationMapDenseCityCandidateAndroidPackageDeployment(files);
            active = deployment;
            return deployment;
        }

        internal static DenseCityCandidatePackageFile[] CreateFilePlan(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
                throw new InvalidOperationException(
                    $"Dense candidate package project root is missing: {projectRoot}");

            string normalizedRoot = Path.GetFullPath(projectRoot);
            string addressablesRoot = ResolveOwnedDirectory(
                normalizedRoot,
                OperationMapDenseCityCandidateRuntimeContentBuilder.AddressablesOutputPath);
            string androidBundleRoot = Path.Combine(addressablesRoot, "Android");
            if (!Directory.Exists(androidBundleRoot))
                throw new InvalidOperationException(
                    $"Dense candidate Android bundle directory is missing: {androidBundleRoot}");

            var files = new List<DenseCityCandidatePackageFile>();
            AddRequiredFile(
                files,
                Path.Combine(addressablesRoot, "catalog.bin"),
                AddressablesDestinationRoot + "/catalog.bin");
            AddRequiredFile(
                files,
                Path.Combine(addressablesRoot, "catalog.hash"),
                AddressablesDestinationRoot + "/catalog.hash");

            foreach (string bundlePath in Directory
                         .EnumerateFiles(androidBundleRoot, "*.bundle", SearchOption.TopDirectoryOnly)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                AddRequiredFile(
                    files,
                    bundlePath,
                    AddressablesDestinationRoot + "/Android/" +
                    Path.GetFileName(bundlePath));
            }

            RequireValidPlan(files);
            return files.ToArray();
        }

        internal static string[] ResolvePlayerScenes(
            IEnumerable<string> enabledScenePaths,
            Func<string, bool> sceneExists)
        {
            if (enabledScenePaths == null)
                throw new InvalidOperationException(
                    "Dense candidate Android build scenes are missing.");
            if (sceneExists == null)
                throw new ArgumentNullException(nameof(sceneExists));

            string[] scenes = enabledScenePaths
                .Select(path => (path ?? string.Empty).Replace('\\', '/').Trim())
                .ToArray();
            OperationMapDenseCityCandidateRuntimeContentBuilder.RequireSourceHierarchyExclusion(
                OperationMapDenseCityCandidateRuntimeContentBuilder.MeasureSourceHierarchyExclusion(
                    Array.Empty<string>(),
                    scenes),
                expectedExplicitEntryCount: 0);

            if (scenes.Length == 0)
                throw new InvalidOperationException(
                    "Dense candidate Android build has no enabled player scenes.");
            if (scenes.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException(
                    "Dense candidate Android build contains an invalid scene path.");
            if (scenes.Distinct(StringComparer.Ordinal).Count() != scenes.Length)
                throw new InvalidOperationException(
                    "Dense candidate Android build contains duplicate player scenes.");

            foreach (string scene in scenes)
            {
                if (!sceneExists(scene))
                {
                    throw new InvalidOperationException(
                        $"Dense candidate Android base scene is missing: {scene}");
                }
                if (scene.StartsWith(
                        StaticMapPresentationOutputPathContract.OperationMapsRoot + "/",
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Dense candidate Android build contains a legacy static scene: {scene}");
                }
            }

            return scenes;
        }

        internal static string GetPackageValidationError(
            IReadOnlyList<string> packageEntries,
            string denseEntitySceneGuid,
            string productionEntitySceneGuid,
            string archiveDependenciesText)
        {
            if (packageEntries == null)
                return "Android package entries are required.";
            if (string.IsNullOrWhiteSpace(denseEntitySceneGuid))
                return "The dense candidate EntityScene GUID is required.";

            string catalogSuffix =
                "/" + AddressablesDestinationRoot + "/catalog.bin";
            string bundleFragment =
                "/" + AddressablesDestinationRoot + "/Android/";
            string entityHeaderSuffix =
                $"/EntityScenes/{denseEntitySceneGuid}.entityheader";
            bool hasCatalog = false;
            int bundleCount = 0;
            bool hasEntityHeader = false;
            bool hasEntityArchiveCatalog = false;
            bool hasEntityArchiveCatalogText = false;
            int entityArchivePayloadCount = 0;
            bool hasProductionEntityScene = false;
            string productionHeaderSuffix = string.IsNullOrWhiteSpace(productionEntitySceneGuid)
                ? null
                : $"/EntityScenes/{productionEntitySceneGuid}.entityheader";

            foreach (string entry in packageEntries)
            {
                string path = "/" + (entry ?? string.Empty)
                    .Replace('\\', '/')
                    .TrimStart('/');
                hasCatalog |= path.EndsWith(catalogSuffix, StringComparison.Ordinal);
                hasEntityHeader |= path.EndsWith(
                    entityHeaderSuffix,
                    StringComparison.Ordinal);
                hasEntityArchiveCatalog |= path.EndsWith(
                    "/ContentArchives/archive_dependencies.bin",
                    StringComparison.Ordinal);
                hasEntityArchiveCatalogText |= path.EndsWith(
                    "/ContentArchives/archive_dependencies.txt",
                    StringComparison.Ordinal);
                string fileName = Path.GetFileName(path);
                if (path.Contains("/ContentArchives/", StringComparison.Ordinal) &&
                    fileName.Length == 32 &&
                    fileName.All(Uri.IsHexDigit))
                {
                    entityArchivePayloadCount++;
                }
                if (path.Contains(bundleFragment, StringComparison.Ordinal) &&
                    path.EndsWith(".bundle", StringComparison.Ordinal))
                {
                    bundleCount++;
                }
                hasProductionEntityScene |= productionHeaderSuffix != null &&
                    path.EndsWith(productionHeaderSuffix, StringComparison.Ordinal);
            }

            if (!hasEntityHeader)
            {
                return "Android package is missing " +
                       $"EntityScenes/{denseEntitySceneGuid}.entityheader.";
            }
            if (!hasEntityArchiveCatalog)
            {
                return "Android package is missing the dense candidate Entities " +
                       "archive dependency catalog.";
            }
            if (!hasEntityArchiveCatalogText)
            {
                return "Android package is missing the readable dense candidate Entities " +
                       "archive dependency catalog.";
            }
            if (entityArchivePayloadCount == 0)
                return "Android package contains no Entities archive payload.";
            if (string.IsNullOrWhiteSpace(archiveDependenciesText) ||
                !archiveDependenciesText.Contains(
                    $"Object: {denseEntitySceneGuid}:",
                    StringComparison.Ordinal))
            {
                return "Android package Entities archive catalog does not own the dense " +
                       $"candidate EntityScene {denseEntitySceneGuid}.";
            }
            if (!hasCatalog)
                return $"Android package is missing {AddressablesDestinationRoot}/catalog.bin.";
            if (bundleCount == 0)
                return "Android package contains no dense candidate Android bundles.";
            if (hasProductionEntityScene)
            {
                return "Android package contains the production EntityScene in addition to " +
                       "the isolated dense candidate.";
            }
            if (!string.IsNullOrWhiteSpace(productionEntitySceneGuid) &&
                archiveDependenciesText.Contains(
                    $"Object: {productionEntitySceneGuid}:",
                    StringComparison.Ordinal))
            {
                return "Android package Entities archive catalog contains the production " +
                       "EntityScene.";
            }
            return null;
        }

        internal static void ValidatePackage(
            string packagePath,
            string denseEntitySceneGuid,
            string productionEntitySceneGuid)
        {
            if (!File.Exists(packagePath))
                throw new InvalidOperationException($"Android package not found: {packagePath}");

            using var archive = ZipFile.OpenRead(packagePath);
            string[] entries = archive.Entries
                .Select(entry => entry.FullName)
                .ToArray();
            ZipArchiveEntry archiveCatalogEntry = archive.Entries.SingleOrDefault(entry =>
                ("/" + entry.FullName.Replace('\\', '/').TrimStart('/')).EndsWith(
                    "/ContentArchives/archive_dependencies.txt",
                    StringComparison.Ordinal));
            string archiveDependenciesText = null;
            if (archiveCatalogEntry != null)
            {
                using StreamReader reader = new(archiveCatalogEntry.Open());
                archiveDependenciesText = reader.ReadToEnd();
            }
            string error = GetPackageValidationError(
                entries,
                denseEntitySceneGuid,
                productionEntitySceneGuid,
                archiveDependenciesText);
            if (error != null)
                throw new InvalidOperationException(error);
        }

        internal static bool TryGetActiveFiles(
            out IReadOnlyList<DenseCityCandidatePackageFile> activeFiles)
        {
            activeFiles = active?.files;
            return activeFiles != null;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (ReferenceEquals(active, this))
                active = null;
        }

        private static string ResolveOwnedDirectory(string projectRoot, string relativePath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            string requiredPrefix = projectRoot.EndsWith(
                Path.DirectorySeparatorChar.ToString(),
                StringComparison.Ordinal)
                ? projectRoot
                : projectRoot + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(requiredPrefix, StringComparison.Ordinal) ||
                !Directory.Exists(fullPath))
            {
                throw new InvalidOperationException(
                    $"Dense candidate package directory is missing or outside the project: {relativePath}");
            }
            return fullPath;
        }

        private static void AddRequiredFile(
            ICollection<DenseCityCandidatePackageFile> files,
            string sourcePath,
            string destinationPath)
        {
            if (!File.Exists(sourcePath))
                throw new InvalidOperationException(
                    $"Dense candidate package file is missing: {sourcePath}");
            files.Add(new DenseCityCandidatePackageFile(
                Path.GetFullPath(sourcePath),
                destinationPath.Replace('\\', '/')));
        }

        private static void RequireValidPlan(
            IReadOnlyCollection<DenseCityCandidatePackageFile> files)
        {
            if (files.Count == 0)
                throw new InvalidOperationException("Dense candidate package file plan is empty.");

            var destinations = new HashSet<string>(StringComparer.Ordinal);
            int bundleCount = 0;
            foreach (DenseCityCandidatePackageFile file in files)
            {
                if (Path.IsPathRooted(file.DestinationPath) ||
                    file.DestinationPath.Split('/').Any(segment =>
                        segment.Length == 0 ||
                        string.Equals(segment, ".", StringComparison.Ordinal) ||
                        string.Equals(segment, "..", StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        $"Dense candidate package destination is unsafe: {file.DestinationPath}");
                }
                if (!destinations.Add(file.DestinationPath))
                    throw new InvalidOperationException(
                        $"Dense candidate package destination is duplicated: {file.DestinationPath}");
                bundleCount += file.DestinationPath.EndsWith(
                    ".bundle",
                    StringComparison.Ordinal) ? 1 : 0;
            }

            if (bundleCount == 0)
                throw new InvalidOperationException(
                    "Dense candidate package file plan contains no Android bundles.");
        }
    }

    internal sealed class OperationMapDenseCityCandidateBuildPlayerProcessor :
        BuildPlayerProcessor
    {
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (!OperationMapDenseCityCandidateAndroidPackageDeployment.TryGetActiveFiles(
                    out IReadOnlyList<DenseCityCandidatePackageFile> files))
            {
                return;
            }

            foreach (DenseCityCandidatePackageFile file in files)
            {
                buildPlayerContext.AddAdditionalPathToStreamingAssets(
                    file.SourcePath,
                    file.DestinationPath);
            }
        }
    }
}

#endif
