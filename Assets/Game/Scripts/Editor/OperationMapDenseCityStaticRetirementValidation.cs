using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapDenseCityStaticRetirementValidation
    {
        private const string StaticRoot =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01";
        private const string CheckpointPath =
            "Design/AgentReports/2026-08-09_dense_city_production_cutover_rollback_checkpoint.json";
        private const string RollbackReportPath =
            "Design/AgentReports/2026-08-10_dense_city_production_rollback_restore_validation.json";
        private const string AndroidAcceptancePath =
            "Design/AgentReports/2026-08-10_dense_city_production_android_acceptance.json";
        private const string RetirementReportPath =
            "Design/AgentReports/2026-08-10_dense_city_static_retirement_validation.json";
        private const string MinimapPath = StaticRoot + "/MinimapRaster.png";
        private const string MinimapMetaPath = MinimapPath + ".meta";
        private const int ExpectedRetainedFileCount = 2;
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/Validate Dense City Static Retirement Preflight")]
        public static void RunPreflight()
        {
            Context context = LoadContext();
            ValidateProduction();
            ValidateRollbackArchive(context);
            ValidateLiveFrozenPackage(context);
            UnityEngine.Debug.Log(
                "[OperationMapDenseCityStaticRetirementPreflight] result=Passed " +
                $"staticFiles={context.Checkpoint.staticRollbackFileCount} " +
                $"staticScenes={context.Checkpoint.staticRollbackSceneCount} " +
                $"staticBytes={context.Checkpoint.staticRollbackBytes} " +
                $"rollbackSha256={context.Checkpoint.staticRollbackSha256}");
        }

        [MenuItem("Game/Operation Maps/Validate Dense City Static Retirement")]
        public static void RunPostRetirement()
        {
            Context context = LoadContext();
            ValidateProduction();
            ValidateRollbackArchive(context);

            string projectRoot = context.ProjectRoot;
            string physicalRoot = Path.Combine(projectRoot, StaticRoot);
            string[] remainingFiles = Directory.Exists(physicalRoot)
                ? Directory.GetFiles(physicalRoot, "*", SearchOption.AllDirectories)
                    .Select(path => RelativePath(projectRoot, path))
                    .OrderBy(path => path, StringComparer.Ordinal)
                    .ToArray()
                : Array.Empty<string>();
            string[] expectedRemaining = { MinimapPath, MinimapMetaPath };
            if (!remainingFiles.SequenceEqual(expectedRemaining, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Static retirement must retain exactly MinimapRaster.png and its meta: " +
                    string.Join(", ", remainingFiles));
            }

            foreach (string path in expectedRemaining)
            {
                byte[] revisionBytes = context.RevisionFiles[path];
                byte[] liveBytes = File.ReadAllBytes(Path.Combine(projectRoot, path));
                if (!revisionBytes.SequenceEqual(liveBytes))
                    throw new InvalidOperationException($"Retained minimap file changed: {path}.");
            }

            long retainedBytes = expectedRemaining.Sum(path =>
                new FileInfo(Path.Combine(projectRoot, path)).Length);
            int retiredFileCount =
                context.Checkpoint.staticRollbackFileCount - ExpectedRetainedFileCount;
            long retiredBytes = context.Checkpoint.staticRollbackBytes - retainedBytes;
            if (retiredFileCount != 543 || retiredBytes <= 0)
                throw new InvalidOperationException("Static retirement count/byte reconciliation failed.");

            var report = new Report
            {
                schemaVersion = 1,
                result = "Passed",
                exactValidationRevision =
                    AndroidBuildReportGenerator.CaptureGitProvenance().ExactCommit,
                rollbackSourceRevision = context.Checkpoint.sourceRevision,
                rollbackArchiveSha256 = context.RevisionStaticSha256,
                rollbackArchiveFileCount = context.RevisionFiles.Count,
                rollbackArchiveSceneCount = context.RevisionSceneCount,
                rollbackArchiveBytes = context.RevisionBytes,
                retiredFileCount = retiredFileCount,
                retiredSceneCount = context.Checkpoint.staticRollbackSceneCount,
                retiredBytes = retiredBytes,
                retainedFileCount = remainingFiles.Length,
                retainedBytes = retainedBytes,
                retainedMinimapPath = MinimapPath,
                productionEntitySceneValidated = 1,
                productionStaticAddressableManifestCount = 0,
                productionStaticAddressableChunkCount = 0,
                rollbackRestorePreviouslyValidated = 1,
                androidProductionPreviouslyAccepted = 1
            };
            string reportPhysical = Path.Combine(projectRoot, RetirementReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPhysical) ?? projectRoot);
            File.WriteAllText(reportPhysical, JsonUtility.ToJson(report, true), Utf8WithoutBom);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            UnityEngine.Debug.Log(
                "[OperationMapDenseCityStaticRetirementValidation] result=Passed " +
                $"retiredFiles={retiredFileCount} retiredScenes={report.retiredSceneCount} " +
                $"retiredBytes={retiredBytes} retainedFiles={remainingFiles.Length} " +
                $"rollbackFiles={context.RevisionFiles.Count} " +
                $"rollbackSha256={context.RevisionStaticSha256} productionStaticEntries=0");
        }

        private static Context LoadContext()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Checkpoint checkpoint = JsonUtility.FromJson<Checkpoint>(
                File.ReadAllText(Path.Combine(projectRoot, CheckpointPath)));
            RollbackReport rollback = JsonUtility.FromJson<RollbackReport>(
                File.ReadAllText(Path.Combine(projectRoot, RollbackReportPath)));
            AndroidAcceptance android = JsonUtility.FromJson<AndroidAcceptance>(
                File.ReadAllText(Path.Combine(projectRoot, AndroidAcceptancePath)));
            if (checkpoint == null || checkpoint.schemaVersion != 1 ||
                !string.Equals(checkpoint.result, "Passed", StringComparison.Ordinal) ||
                checkpoint.productionCutover != 1 ||
                checkpoint.staticRollbackFileCount != 545 ||
                checkpoint.staticRollbackSceneCount != 269 ||
                checkpoint.staticRollbackBytes != 42765094 ||
                string.IsNullOrWhiteSpace(checkpoint.sourceRevision) ||
                string.IsNullOrWhiteSpace(checkpoint.staticRollbackSha256))
            {
                throw new InvalidOperationException("Static rollback checkpoint is incomplete.");
            }
            if (rollback == null || rollback.schemaVersion != 1 ||
                !string.Equals(rollback.result, "Passed", StringComparison.Ordinal) ||
                rollback.rollbackRestored != 1 || rollback.productionReapplied != 1 ||
                rollback.staticPackagePreserved != 1 ||
                !string.Equals(
                    rollback.sourceRevision,
                    checkpoint.sourceRevision,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    rollback.staticRollbackSha256,
                    checkpoint.staticRollbackSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Accepted rollback-restore evidence is missing.");
            }
            if (android == null ||
                !string.Equals(android.result, "Passed", StringComparison.Ordinal) ||
                !string.Equals(android.task, "VRP-103", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Accepted production Android evidence is missing.");
            }

            Dictionary<string, byte[]> revisionFiles = ReadRevisionArchive(
                projectRoot,
                checkpoint.sourceRevision,
                StaticRoot);
            long revisionBytes = revisionFiles.Values.Sum(bytes => bytes.LongLength);
            int revisionSceneCount = revisionFiles.Keys.Count(path =>
                path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
            string revisionStaticSha256 = ComputeRevisionDirectoryHash(
                StaticRoot,
                revisionFiles);
            return new Context(
                projectRoot,
                checkpoint,
                revisionFiles,
                revisionBytes,
                revisionSceneCount,
                revisionStaticSha256);
        }

        private static void ValidateRollbackArchive(Context context)
        {
            if (context.RevisionFiles.Count != context.Checkpoint.staticRollbackFileCount ||
                context.RevisionSceneCount != context.Checkpoint.staticRollbackSceneCount ||
                context.RevisionBytes != context.Checkpoint.staticRollbackBytes ||
                !string.Equals(
                    context.RevisionStaticSha256,
                    context.Checkpoint.staticRollbackSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Revision-owned static rollback archive does not match its checkpoint.");
            }
        }

        private static void ValidateLiveFrozenPackage(Context context)
        {
            string physical = Path.Combine(context.ProjectRoot, StaticRoot);
            string[] files = Directory.GetFiles(physical, "*", SearchOption.AllDirectories);
            int sceneCount = files.Count(path =>
                path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
            long bytes = files.Sum(path => new FileInfo(path).Length);
            string hash = ComputeDirectoryHash(physical);
            if (files.Length != context.Checkpoint.staticRollbackFileCount ||
                sceneCount != context.Checkpoint.staticRollbackSceneCount ||
                bytes != context.Checkpoint.staticRollbackBytes ||
                !string.Equals(hash, context.Checkpoint.staticRollbackSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Live frozen static package does not match the rollback checkpoint.");
            }
        }

        private static void ValidateProduction()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            string definitionError = null;
            if (definition == null ||
                definition.PresentationKind != OperationMapPresentationKind.EntityScene ||
                definition.RenderResidencyMode != OperationMapRenderResidencyMode.VirtualizedProxyPool ||
                !definition.TryValidateLocalContentReferences(out definitionError))
            {
                throw new InvalidOperationException(
                    definitionError ?? "Production definition is not the accepted EntityScene cutover.");
            }
            if (!OperationMapAddressablesLayoutValidator.TryValidateCurrentLayout(
                    true,
                    out string layoutError))
            {
                throw new InvalidOperationException(layoutError);
            }

            Scene scene = EditorSceneManager.OpenScene(
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OpenSceneMode.Single);
            if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedEntityScene(
                    scene,
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath,
                    out string sceneError))
            {
                throw new InvalidOperationException(sceneError);
            }
        }

        private static Dictionary<string, byte[]> ReadRevisionArchive(
            string projectRoot,
            string revision,
            string root)
        {
            string tempRoot = Path.Combine(projectRoot, "Library", "Temp");
            Directory.CreateDirectory(tempRoot);
            string archivePath = Path.Combine(
                tempRoot,
                $"dense-city-static-retirement-{Guid.NewGuid():N}.zip");
            try
            {
                RunGit(
                    projectRoot,
                    $"archive --format=zip --output=\"{archivePath}\" {revision} -- {root}");
                var files = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                using ZipArchive archive = ZipFile.OpenRead(archivePath);
                foreach (ZipArchiveEntry entry in archive.Entries
                             .Where(entry => !string.IsNullOrEmpty(entry.Name))
                             .OrderBy(entry => entry.FullName, StringComparer.Ordinal))
                {
                    string path = entry.FullName.Replace('\\', '/');
                    using Stream source = entry.Open();
                    using var destination = new MemoryStream();
                    source.CopyTo(destination);
                    if (!files.TryAdd(path, destination.ToArray()))
                        throw new InvalidOperationException($"Duplicate rollback archive path: {path}.");
                }
                return files;
            }
            finally
            {
                if (File.Exists(archivePath))
                    File.Delete(archivePath);
            }
        }

        private static void RunGit(string projectRoot, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = projectRoot,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process process = Process.Start(startInfo) ??
                throw new InvalidOperationException("Unable to start git.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git {arguments} failed: {error}");
            if (!string.IsNullOrWhiteSpace(output))
                throw new InvalidOperationException($"git {arguments} produced unexpected output.");
        }

        private static string ComputeRevisionDirectoryHash(
            string root,
            IReadOnlyDictionary<string, byte[]> files)
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, byte[]> file in files
                         .OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                string relative = file.Key.Substring(root.Length + 1);
                byte[] bytes = file.Value;
                builder.Append(relative).Append(':').Append(bytes.Length).Append(':');
                using SHA256 sha = SHA256.Create();
                builder.Append(ToHex(sha.ComputeHash(bytes))).Append('\n');
            }
            return HashText(builder.ToString());
        }

        private static string ComputeDirectoryHash(string directory)
        {
            var builder = new StringBuilder();
            foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                builder.Append(RelativePath(directory, file)).Append(':');
                builder.Append(new FileInfo(file).Length).Append(':');
                using Stream stream = File.OpenRead(file);
                using SHA256 sha = SHA256.Create();
                builder.Append(ToHex(sha.ComputeHash(stream))).Append('\n');
            }
            return HashText(builder.ToString());
        }

        private static string HashText(string value)
        {
            using SHA256 sha = SHA256.Create();
            return ToHex(sha.ComputeHash(Utf8WithoutBom.GetBytes(value)));
        }

        private static string RelativePath(string root, string path) =>
            Path.GetRelativePath(root, path).Replace('\\', '/');

        private static string ToHex(byte[] bytes) =>
            string.Concat(bytes.Select(value => value.ToString("x2")));

        private sealed class Context
        {
            internal Context(
                string projectRoot,
                Checkpoint checkpoint,
                Dictionary<string, byte[]> revisionFiles,
                long revisionBytes,
                int revisionSceneCount,
                string revisionStaticSha256)
            {
                ProjectRoot = projectRoot;
                Checkpoint = checkpoint;
                RevisionFiles = revisionFiles;
                RevisionBytes = revisionBytes;
                RevisionSceneCount = revisionSceneCount;
                RevisionStaticSha256 = revisionStaticSha256;
            }

            internal string ProjectRoot { get; }
            internal Checkpoint Checkpoint { get; }
            internal Dictionary<string, byte[]> RevisionFiles { get; }
            internal long RevisionBytes { get; }
            internal int RevisionSceneCount { get; }
            internal string RevisionStaticSha256 { get; }
        }

        [Serializable]
        private sealed class Checkpoint
        {
            public int schemaVersion;
            public string result;
            public string sourceRevision;
            public string staticRollbackSha256;
            public int staticRollbackFileCount;
            public int staticRollbackSceneCount;
            public long staticRollbackBytes;
            public int productionCutover;
        }

        [Serializable]
        private sealed class RollbackReport
        {
            public int schemaVersion;
            public string result;
            public string sourceRevision;
            public string staticRollbackSha256;
            public int rollbackRestored;
            public int productionReapplied;
            public int staticPackagePreserved;
        }

        [Serializable]
        private sealed class AndroidAcceptance
        {
            public string result;
            public string task;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion;
            public string result;
            public string exactValidationRevision;
            public string rollbackSourceRevision;
            public string rollbackArchiveSha256;
            public int rollbackArchiveFileCount;
            public int rollbackArchiveSceneCount;
            public long rollbackArchiveBytes;
            public int retiredFileCount;
            public int retiredSceneCount;
            public long retiredBytes;
            public int retainedFileCount;
            public long retainedBytes;
            public string retainedMinimapPath;
            public int productionEntitySceneValidated;
            public int productionStaticAddressableManifestCount;
            public int productionStaticAddressableChunkCount;
            public int rollbackRestorePreviouslyValidated;
            public int androidProductionPreviouslyAccepted;
        }
    }
}
