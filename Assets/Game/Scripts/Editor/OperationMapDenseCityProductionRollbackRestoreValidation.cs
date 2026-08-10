using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Composition;
using Game.Configs;
using Game.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapDenseCityProductionRollbackRestoreValidation
    {
        private const string CheckpointPath =
            "Design/AgentReports/2026-08-09_dense_city_production_cutover_rollback_checkpoint.json";
        private const string ReportPath =
            "Design/AgentReports/2026-08-10_dense_city_production_rollback_restore_validation.json";
        private const string StaticRoot =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01";
        private const string AddressablesRoot = "Assets/AddressableAssetsData";
        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/Validate Dense City Production Rollback Restore")]
        public static void Run()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            Checkpoint checkpoint = JsonUtility.FromJson<Checkpoint>(
                File.ReadAllText(Path.Combine(projectRoot, CheckpointPath)));
            RequireCheckpoint(checkpoint);

            string[] rollbackAddressables = ListRevisionFiles(
                projectRoot,
                checkpoint.sourceRevision,
                AddressablesRoot);
            string[] currentAddressables = Directory.GetFiles(
                    Path.Combine(projectRoot, AddressablesRoot),
                    "*",
                    SearchOption.AllDirectories)
                .Select(path => RelativePath(projectRoot, path))
                .ToArray();
            string rollbackSourceAddressablesSha256 = ComputeRevisionDirectoryHash(
                projectRoot,
                checkpoint.sourceRevision,
                AddressablesRoot,
                rollbackAddressables);
            int transientAddressablesFileCount = currentAddressables.Count(path =>
                !rollbackAddressables.Contains(path, StringComparer.Ordinal));
            string[] transactionPaths = currentAddressables
                .Concat(rollbackAddressables)
                .Concat(new[]
                {
                    OperationMapAddressablesLayoutBuilder.DefinitionPath,
                    OperationMapAddressablesLayoutBuilder.SourceScenePath
                })
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            string productionTransactionSha256 = ComputePathSetHash(
                projectRoot,
                transactionPaths);
            string staticBeforeSha256 = ComputeDirectoryHash(
                Path.Combine(projectRoot, StaticRoot));
            OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction transaction =
                OperationMapEntitySceneCandidateBakeAll.CandidateFileTransaction.Capture(
                    projectRoot,
                    transactionPaths);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            Exception validationError = null;
            try
            {
                RestoreRollbackRevision(
                    projectRoot,
                    checkpoint.sourceRevision,
                    rollbackAddressables,
                    currentAddressables);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                ValidateRollback(
                    projectRoot,
                    checkpoint,
                    rollbackSourceAddressablesSha256);
            }
            catch (Exception error)
            {
                validationError = error;
            }
            finally
            {
                try
                {
                    transaction.Rollback();
                    if (previousSetup.Length > 0)
                        EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                catch (Exception restoreError)
                {
                    validationError = validationError == null
                        ? restoreError
                        : new AggregateException(validationError, restoreError);
                }
            }

            if (validationError != null)
                throw validationError;

            string productionRestoredSha256 = ComputePathSetHash(
                projectRoot,
                transactionPaths);
            if (!string.Equals(
                    productionTransactionSha256,
                    productionRestoredSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Production files were not restored byte-exactly after rollback validation.");
            }

            ValidateProduction();
            string staticAfterSha256 = ComputeDirectoryHash(
                Path.Combine(projectRoot, StaticRoot));
            if (!string.Equals(staticBeforeSha256, staticAfterSha256, StringComparison.Ordinal) ||
                !string.Equals(staticAfterSha256, checkpoint.staticRollbackSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Frozen static rollback ownership changed during restore validation.");
            }

            var report = new Report
            {
                schemaVersion = 1,
                result = "Passed",
                sourceRevision = checkpoint.sourceRevision,
                exactValidationRevision =
                    AndroidBuildReportGenerator.CaptureGitProvenance().ExactCommit,
                rollbackDefinitionSha256 = checkpoint.rollbackDefinitionSha256,
                rollbackRuntimeBindingSha256 = checkpoint.rollbackRuntimeBindingSha256,
                rollbackAddressablesSha256 = checkpoint.rollbackAddressablesSha256,
                rollbackSourceAddressablesSha256 = rollbackSourceAddressablesSha256,
                productionTransactionSha256 = productionTransactionSha256,
                productionRestoredSha256 = productionRestoredSha256,
                staticRollbackSha256 = staticAfterSha256,
                staticRollbackFileCount = checkpoint.staticRollbackFileCount,
                staticRollbackSceneCount = checkpoint.staticRollbackSceneCount,
                staticRollbackBytes = checkpoint.staticRollbackBytes,
                transientAddressablesFileCount = transientAddressablesFileCount,
                checkpointAddressablesIncludesTransient =
                    string.Equals(
                        checkpoint.rollbackAddressablesSha256,
                        rollbackSourceAddressablesSha256,
                        StringComparison.Ordinal) ? 0 : 1,
                rollbackRestored = 1,
                productionReapplied = 1,
                staticPackagePreserved = 1,
                transientAddressablesRestored = 1
            };
            string reportPhysical = Path.Combine(projectRoot, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportPhysical) ?? projectRoot);
            File.WriteAllText(
                reportPhysical,
                JsonUtility.ToJson(report, true),
                Utf8WithoutBom);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            UnityEngine.Debug.Log(
                "[OperationMapDenseCityProductionRollbackRestoreValidation] result=Passed " +
                $"sourceRevision={checkpoint.sourceRevision} " +
                "rollbackRestored=1 productionReapplied=1 " +
                $"staticFiles={checkpoint.staticRollbackFileCount} " +
                $"staticScenes={checkpoint.staticRollbackSceneCount} " +
                $"staticBytes={checkpoint.staticRollbackBytes}");
        }

        private static void RequireCheckpoint(Checkpoint checkpoint)
        {
            if (checkpoint == null || checkpoint.schemaVersion != 1 ||
                !string.Equals(checkpoint.result, "Passed", StringComparison.Ordinal) ||
                checkpoint.productionCutover != 1 ||
                string.IsNullOrWhiteSpace(checkpoint.sourceRevision) ||
                string.IsNullOrWhiteSpace(checkpoint.rollbackDefinitionSha256) ||
                string.IsNullOrWhiteSpace(checkpoint.rollbackRuntimeBindingSha256) ||
                string.IsNullOrWhiteSpace(checkpoint.rollbackAddressablesSha256) ||
                string.IsNullOrWhiteSpace(checkpoint.staticRollbackSha256))
            {
                throw new InvalidOperationException(
                    "Production rollback checkpoint is missing or incomplete.");
            }
        }

        private static void RestoreRollbackRevision(
            string projectRoot,
            string revision,
            IReadOnlyCollection<string> rollbackAddressables,
            IEnumerable<string> currentAddressables)
        {
            HashSet<string> rollbackSet = new(
                rollbackAddressables,
                StringComparer.Ordinal);
            AssetDatabase.ReleaseCachedFileHandles();
            foreach (string path in currentAddressables)
            {
                if (rollbackSet.Contains(path))
                    continue;
                string physical = Path.Combine(projectRoot, path);
                if (File.Exists(physical))
                    File.Delete(physical);
            }

            foreach (string path in rollbackAddressables.Concat(new[]
                     {
                         OperationMapAddressablesLayoutBuilder.DefinitionPath,
                         OperationMapAddressablesLayoutBuilder.SourceScenePath
                     }))
            {
                string physical = Path.Combine(projectRoot, path);
                Directory.CreateDirectory(Path.GetDirectoryName(physical) ?? projectRoot);
                File.WriteAllBytes(physical, ReadRevisionFile(projectRoot, revision, path));
            }
        }

        private static void ValidateRollback(
            string projectRoot,
            Checkpoint checkpoint,
            string rollbackSourceAddressablesSha256)
        {
            RequireHash(
                Path.Combine(projectRoot, OperationMapAddressablesLayoutBuilder.DefinitionPath),
                checkpoint.rollbackDefinitionSha256,
                "rollback definition");
            RequireHash(
                Path.Combine(projectRoot, OperationMapAddressablesLayoutBuilder.SourceScenePath),
                checkpoint.rollbackRuntimeBindingSha256,
                "rollback runtime binding");
            string addressablesHash = ComputeDirectoryHash(
                Path.Combine(projectRoot, AddressablesRoot));
            if (!string.Equals(
                    addressablesHash,
                    rollbackSourceAddressablesSha256,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Rollback Addressables hash mismatch: {addressablesHash}.");
            }

            ValidateStaticPackage(projectRoot, checkpoint);
            OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            string definitionError = null;
            if (definition == null ||
                definition.PresentationKind != OperationMapPresentationKind.StaticSceneChunks ||
                definition.RenderResidencyMode != OperationMapRenderResidencyMode.ResidentEntities ||
                !definition.TryValidateLocalContentReferences(out definitionError))
            {
                throw new InvalidOperationException(
                    definitionError ?? "Rollback definition is not the valid static baseline.");
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
            if (!OperationMapRuntimeBindingSceneValidator.TryValidateLoadedScene(
                    scene,
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    out string sceneError))
            {
                throw new InvalidOperationException(sceneError);
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
                    definitionError ?? "Production definition was not reapplied.");
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

        private static void ValidateStaticPackage(string projectRoot, Checkpoint checkpoint)
        {
            string physical = Path.Combine(projectRoot, StaticRoot);
            string[] files = Directory.GetFiles(physical, "*", SearchOption.AllDirectories);
            int sceneCount = files.Count(path =>
                path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));
            long bytes = files.Sum(path => new FileInfo(path).Length);
            string hash = ComputeDirectoryHash(physical);
            if (files.Length != checkpoint.staticRollbackFileCount ||
                sceneCount != checkpoint.staticRollbackSceneCount ||
                bytes != checkpoint.staticRollbackBytes ||
                !string.Equals(hash, checkpoint.staticRollbackSha256, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Frozen static rollback package does not match its checkpoint.");
            }
        }

        private static string[] ListRevisionFiles(
            string projectRoot,
            string revision,
            string root)
        {
            string output = RunGitText(
                projectRoot,
                $"ls-tree -r --name-only {revision} -- {root}");
            return output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(path => path.Replace('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
        }

        private static byte[] ReadRevisionFile(
            string projectRoot,
            string revision,
            string path)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = projectRoot,
                Arguments = $"show \"{revision}:{path}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            using Process process = Process.Start(startInfo) ??
                throw new InvalidOperationException("Unable to start git show.");
            using var stream = new MemoryStream();
            process.StandardOutput.BaseStream.CopyTo(stream);
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git show failed for {path}: {error}");
            return stream.ToArray();
        }

        private static string RunGitText(string projectRoot, string arguments)
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
            return output;
        }

        private static void RequireHash(string path, string expected, string label)
        {
            string actual = ComputeFileHash(path);
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException($"{label} hash mismatch: {actual}.");
        }

        private static string ComputePathSetHash(
            string projectRoot,
            IEnumerable<string> paths)
        {
            var builder = new StringBuilder();
            foreach (string path in paths.OrderBy(path => path, StringComparer.Ordinal))
            {
                string physical = Path.Combine(projectRoot, path);
                builder.Append(path).Append(':');
                if (!File.Exists(physical))
                {
                    builder.Append("missing\n");
                    continue;
                }
                builder.Append(new FileInfo(physical).Length).Append(':');
                builder.Append(ComputeFileHash(physical)).Append('\n');
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
                builder.Append(ComputeFileHash(file)).Append('\n');
            }
            return HashText(builder.ToString());
        }

        private static string ComputeRevisionDirectoryHash(
            string projectRoot,
            string revision,
            string root,
            IEnumerable<string> paths)
        {
            var builder = new StringBuilder();
            foreach (string path in paths.OrderBy(path => path, StringComparer.Ordinal))
            {
                byte[] bytes = ReadRevisionFile(projectRoot, revision, path);
                string relative = path.Substring(root.Length + 1);
                builder.Append(relative).Append(':').Append(bytes.Length).Append(':');
                using SHA256 sha = SHA256.Create();
                builder.Append(ToHex(sha.ComputeHash(bytes))).Append('\n');
            }
            return HashText(builder.ToString());
        }

        private static string ComputeFileHash(string path)
        {
            using Stream stream = File.OpenRead(path);
            using SHA256 sha = SHA256.Create();
            return ToHex(sha.ComputeHash(stream));
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

        [Serializable]
        private sealed class Checkpoint
        {
            public int schemaVersion;
            public string result;
            public string sourceRevision;
            public string rollbackDefinitionSha256;
            public string rollbackRuntimeBindingSha256;
            public string rollbackAddressablesSha256;
            public string staticRollbackSha256;
            public int staticRollbackFileCount;
            public int staticRollbackSceneCount;
            public long staticRollbackBytes;
            public int productionCutover;
        }

        [Serializable]
        private sealed class Report
        {
            public int schemaVersion;
            public string result;
            public string sourceRevision;
            public string exactValidationRevision;
            public string rollbackDefinitionSha256;
            public string rollbackRuntimeBindingSha256;
            public string rollbackAddressablesSha256;
            public string rollbackSourceAddressablesSha256;
            public string productionTransactionSha256;
            public string productionRestoredSha256;
            public string staticRollbackSha256;
            public int staticRollbackFileCount;
            public int staticRollbackSceneCount;
            public long staticRollbackBytes;
            public int transientAddressablesFileCount;
            public int checkpointAddressablesIncludesTransient;
            public int rollbackRestored;
            public int productionReapplied;
            public int staticPackagePreserved;
            public int transientAddressablesRestored;
        }
    }
}
