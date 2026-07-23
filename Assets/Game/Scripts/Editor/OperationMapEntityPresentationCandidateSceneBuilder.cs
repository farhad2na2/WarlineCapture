#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using Game.Authoring;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class OperationMapEntityPresentationCandidateSceneBuilder
    {
        internal const string OperationMapId = "opmap.skirmish.desert_base_01";
        internal const string AcceptedOperationMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        internal const string StaticRollbackRoot =
            "Assets/Game/GeneratedStaticMapPresentation/OperationMaps/opmap/skirmish/desert_base_01";

        private const string PresentationRootName = "AuthoredOperationMapEntityPresentation";
        private const string GameplayBuildingsName = "GameplayBuildings";
        private const string GameplayVehiclesName = "GameplayVehicles";
        private const string RenderOnlyName = "RenderOnly";

        private static readonly UTF8Encoding Utf8WithoutBom = new(false);

        [MenuItem("Game/Operation Maps/EntityScene Migration/Create Protected Candidate Hierarchy")]
        public static void CreateProtectedCandidateHierarchy()
        {
            OperationMapEntityPresentationMigrationPlan plan =
                OperationMapEntityPresentationMigrationEditor.CreateCurrentDryRunPlan(out string reportPath);
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string protectedStaticRoot = Path.Combine(projectRoot, StaticRollbackRoot);
            string staticSnapshot = ComputeDirectorySnapshot(protectedStaticRoot);

            if (!TryCreateProtectedCandidateHierarchy(
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                    OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                    OperationMapId,
                    plan.RecordSetHash,
                    new[]
                    {
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                        AcceptedOperationMapScenePath
                    },
                    out OperationMapEntityPresentationCandidateBuildResult result,
                    out string rejectionReason))
            {
                throw new InvalidOperationException(
                    $"Candidate hierarchy transaction rejected: {rejectionReason}");
            }

            if (!string.Equals(staticSnapshot, ComputeDirectorySnapshot(protectedStaticRoot), StringComparison.Ordinal))
            {
                AssetDatabase.DeleteAsset(result.CandidateScenePath);
                throw new InvalidOperationException(
                    "Candidate hierarchy transaction changed the static rollback package.");
            }

            Debug.Log(
                $"[OperationMapEntityPresentationCandidateSceneBuilder] status=Created " +
                $"candidate={result.CandidateScenePath} candidateGuid={result.CandidateSceneGuid} " +
                $"sourceGuid={result.SourceSceneGuid} recordSetHash={result.RecordSetHash} " +
                $"productionReferenced=0 report={reportPath}");
        }

        internal static bool TryCreateProtectedCandidateHierarchy(
            string sourceScenePath,
            string candidateScenePath,
            string operationMapId,
            string recordSetHash,
            IReadOnlyList<string> protectedAssetPaths,
            out OperationMapEntityPresentationCandidateBuildResult result,
            out string rejectionReason)
        {
            result = default;
            rejectionReason = null;
            Scene candidateScene = default;
            bool candidateCreated = false;

            try
            {
                if (!IsScenePath(sourceScenePath) || !IsScenePath(candidateScenePath) ||
                    string.Equals(sourceScenePath, candidateScenePath, StringComparison.Ordinal))
                {
                    rejectionReason = "source-or-candidate-scene-path-invalid";
                    return false;
                }

                if (!File.Exists(ToPhysicalPath(sourceScenePath)))
                {
                    rejectionReason = "accepted-source-subscene-missing";
                    return false;
                }

                if (!OperationMapHashRules.IsValidSha256(recordSetHash))
                {
                    rejectionReason = "record-set-hash-invalid";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(operationMapId))
                {
                    rejectionReason = "operation-map-id-empty";
                    return false;
                }

                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(candidateScenePath)) ||
                    File.Exists(ToPhysicalPath(candidateScenePath)))
                {
                    rejectionReason = "candidate-already-exists";
                    return false;
                }

                Dictionary<string, string> protectedSnapshots = CaptureAssetSnapshots(protectedAssetPaths);
                EnsureAssetFolder(Path.GetDirectoryName(candidateScenePath)?.Replace('\\', '/'));

                if (!AssetDatabase.CopyAsset(sourceScenePath, candidateScenePath))
                {
                    rejectionReason = "candidate-copy-failed";
                    return false;
                }

                candidateCreated = true;
                AssetDatabase.ImportAsset(candidateScenePath, ImportAssetOptions.ForceSynchronousImport);
                string sourceGuid = AssetDatabase.AssetPathToGUID(sourceScenePath);
                string candidateGuid = AssetDatabase.AssetPathToGUID(candidateScenePath);
                if (string.IsNullOrEmpty(sourceGuid) || string.IsNullOrEmpty(candidateGuid) ||
                    string.Equals(sourceGuid, candidateGuid, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Candidate scene did not receive an independent asset GUID.");
                }

                candidateScene = EditorSceneManager.OpenScene(candidateScenePath, OpenSceneMode.Additive);
                RequireSingleRoot(candidateScene, "Grid");
                RequireSingleRoot(candidateScene, "InitialUnitsSpawnerAuthoring");
                if (FindRoot(candidateScene, PresentationRootName) != null)
                    throw new InvalidOperationException("Candidate source already contains the protected presentation root.");

                GameObject presentationRoot = CreateRoot(candidateScene, PresentationRootName);
                Transform buildings = CreateChild(presentationRoot.transform, GameplayBuildingsName);
                CreateChild(buildings, "MilitaryBase");
                CreateChild(buildings, "HandmadeCity");
                CreateChild(buildings, "Infrastructure");

                Transform vehicles = CreateChild(presentationRoot.transform, GameplayVehiclesName);
                Transform renderOnly = CreateChild(presentationRoot.transform, RenderOnlyName);
                CreateChild(renderOnly, "Terrain");
                CreateChild(renderOnly, "RoadsAndBridges");
                CreateChild(renderOnly, "Mountains");
                CreateChild(renderOnly, "Vegetation");
                CreateChild(renderOnly, "Props");
                CreateChild(renderOnly, "Infrastructure");
                CreateChild(renderOnly, "Horizon");

                ConfigureRole(buildings.gameObject, OperationMapEntityPresentationRole.GameplayBuildings,
                    operationMapId, recordSetHash);
                ConfigureRole(vehicles.gameObject, OperationMapEntityPresentationRole.GameplayVehicles,
                    operationMapId, recordSetHash);
                ConfigureRole(renderOnly.gameObject, OperationMapEntityPresentationRole.RenderOnly,
                    operationMapId, recordSetHash);

                if (!EditorSceneManager.SaveScene(candidateScene, candidateScenePath, false))
                    throw new InvalidOperationException("Candidate scene save failed.");
                EditorSceneManager.CloseScene(candidateScene, true);
                candidateScene = default;

                AssetDatabase.ImportAsset(candidateScenePath, ImportAssetOptions.ForceSynchronousImport);
                RequireSnapshotsUnchanged(protectedSnapshots);
                RequireProductionDoesNotReferenceCandidate(protectedAssetPaths, candidateGuid);

                result = new OperationMapEntityPresentationCandidateBuildResult(
                    sourceScenePath,
                    sourceGuid,
                    candidateScenePath,
                    candidateGuid,
                    recordSetHash);
                return true;
            }
            catch (Exception exception)
            {
                rejectionReason = exception.Message;
                if (candidateScene.IsValid() && candidateScene.isLoaded)
                    EditorSceneManager.CloseScene(candidateScene, true);
                if (candidateCreated)
                    AssetDatabase.DeleteAsset(candidateScenePath);
                return false;
            }
        }

        private static void ConfigureRole(
            GameObject owner,
            OperationMapEntityPresentationRole role,
            string operationMapId,
            string recordSetHash)
        {
            var marker = owner.AddComponent<OperationMapEntityPresentationRootAuthoring>();
            var serialized = new SerializedObject(marker);
            serialized.FindProperty("operationMapId").stringValue = operationMapId;
            serialized.FindProperty("role").intValue = (int)role;
            serialized.FindProperty("schemaVersion").intValue =
                OperationMapEntityPresentationRootAuthoring.CurrentSchemaVersion;
            serialized.FindProperty("migrationRecordSetHash").stringValue = recordSetHash;
            if (role == OperationMapEntityPresentationRole.GameplayBuildings)
            {
                serialized.FindProperty("expectedGameplayBuildingCount").intValue = 432;
                serialized.FindProperty("expectedGameplayVehicleCount").intValue = 22;
                serialized.FindProperty("expectedRenderOnlyCount").intValue = 9090;
                serialized.FindProperty("expectedGeneratedIdentityCount").intValue = 0;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!marker.TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        private static GameObject CreateRoot(Scene scene, string name)
        {
            var owner = new GameObject(name);
            SceneManager.MoveGameObjectToScene(owner, scene);
            Reset(owner.transform);
            return owner;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            Reset(child.transform);
            return child.transform;
        }

        private static void Reset(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void RequireSingleRoot(Scene scene, string name)
        {
            int count = scene.GetRootGameObjects().Count(root => string.Equals(root.name, name, StringComparison.Ordinal));
            if (count != 1)
                throw new InvalidOperationException($"Candidate requires exactly one '{name}' root; found {count}.");
        }

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(root => string.Equals(root.name, name, StringComparison.Ordinal));

        private static Dictionary<string, string> CaptureAssetSnapshots(IReadOnlyList<string> assetPaths)
        {
            var snapshots = new Dictionary<string, string>(StringComparer.Ordinal);
            if (assetPaths == null)
                return snapshots;

            for (int i = 0; i < assetPaths.Count; i++)
            {
                string path = assetPaths[i];
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                snapshots[path] = ComputeFileSha256(ToPhysicalPath(path));
                string metaPath = path + ".meta";
                if (File.Exists(ToPhysicalPath(metaPath)))
                    snapshots[metaPath] = ComputeFileSha256(ToPhysicalPath(metaPath));
            }

            return snapshots;
        }

        private static void RequireSnapshotsUnchanged(IReadOnlyDictionary<string, string> snapshots)
        {
            foreach (KeyValuePair<string, string> snapshot in snapshots)
            {
                string current = ComputeFileSha256(ToPhysicalPath(snapshot.Key));
                if (!string.Equals(snapshot.Value, current, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Protected source changed: {snapshot.Key}");
            }
        }

        private static void RequireProductionDoesNotReferenceCandidate(
            IReadOnlyList<string> protectedAssetPaths,
            string candidateGuid)
        {
            if (protectedAssetPaths == null)
                return;
            for (int i = 0; i < protectedAssetPaths.Count; i++)
            {
                string path = protectedAssetPaths[i];
                if (!IsScenePath(path))
                    continue;
                string text = File.ReadAllText(ToPhysicalPath(path), Utf8WithoutBom);
                if (text.IndexOf(candidateGuid, StringComparison.Ordinal) >= 0)
                    throw new InvalidOperationException($"Production scene references candidate GUID: {path}");
            }
        }

        private static string ComputeDirectorySnapshot(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(directoryPath);
            using var sha = SHA256.Create();
            var builder = new StringBuilder();
            foreach (string file in Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                builder.Append(Path.GetRelativePath(directoryPath, file).Replace('\\', '/'))
                    .Append('=')
                    .Append(ComputeFileSha256(file))
                    .Append('\n');
            }
            return ToLowerHex(sha.ComputeHash(Utf8WithoutBom.GetBytes(builder.ToString())));
        }

        private static string ComputeFileSha256(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Protected asset is missing.", path);
            using var stream = File.OpenRead(path);
            using var sha = SHA256.Create();
            return ToLowerHex(sha.ComputeHash(stream));
        }

        private static string ToLowerHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
                builder.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
            return builder.ToString();
        }

        private static string ToPhysicalPath(string assetPath) =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Application.dataPath), assetPath));

        private static bool IsScenePath(string path) =>
            !string.IsNullOrWhiteSpace(path) && path.StartsWith("Assets/", StringComparison.Ordinal) &&
            path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
                return;
            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }
    }

    internal readonly struct OperationMapEntityPresentationCandidateBuildResult
    {
        internal OperationMapEntityPresentationCandidateBuildResult(
            string sourceScenePath,
            string sourceSceneGuid,
            string candidateScenePath,
            string candidateSceneGuid,
            string recordSetHash)
        {
            SourceScenePath = sourceScenePath;
            SourceSceneGuid = sourceSceneGuid;
            CandidateScenePath = candidateScenePath;
            CandidateSceneGuid = candidateSceneGuid;
            RecordSetHash = recordSetHash;
        }

        internal string SourceScenePath { get; }
        internal string SourceSceneGuid { get; }
        internal string CandidateScenePath { get; }
        internal string CandidateSceneGuid { get; }
        internal string RecordSetHash { get; }
    }
}

#endif
