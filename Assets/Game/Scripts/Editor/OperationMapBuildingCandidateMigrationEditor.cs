#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Game.Authoring;
    using Game.Configs;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    internal static class OperationMapBuildingCandidateMigrationEditor
    {
        internal const string AttachmentInventoryPath =
            "Design/AgentReports/2026-07-21_dense_city_phase0a_building_attachment_ownership_inventory.json";
        internal const string GridConfigPath =
            "Assets/Game/Configs/Scene/MatchSubScene_GridAuthoring_Config.asset";

        [MenuItem("Game/Operation Maps/EntityScene Migration/Populate Candidate Gameplay Buildings")]
        public static void PopulateCandidateGameplayBuildings()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string candidatePhysicalPath = ResolveProjectPath(projectRoot, candidatePath);
            string candidateMetaPhysicalPath = candidatePhysicalPath + ".meta";
            if (!File.Exists(candidatePhysicalPath) || !File.Exists(candidateMetaPhysicalPath))
                throw new FileNotFoundException("Protected candidate SubScene has not been created.", candidatePhysicalPath);

            byte[] candidateBackup = File.ReadAllBytes(candidatePhysicalPath);
            byte[] candidateMetaBackup = File.ReadAllBytes(candidateMetaPhysicalPath);
            string acceptedSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
            string acceptedSubSceneHash = ComputeSha256(
                ResolveProjectPath(projectRoot,
                    OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport report =
                    LoadInventory(projectRoot);
                MapBuildingPlacementConfig placements = AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(
                    report.buildingPlacementConfigPath);
                GridAuthoringConfig grid = AssetDatabase.LoadAssetAtPath<GridAuthoringConfig>(GridConfigPath);
                if (placements == null || grid == null || placements.Placements.Count != report.placements.Count)
                    throw new InvalidOperationException("Building placement or grid configuration does not match the accepted inventory.");

                Scene sourceScene = EditorSceneManager.OpenScene(
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    OpenSceneMode.Additive);
                Scene candidateScene = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Additive);
                Transform buildingRoot = RequirePath(
                    candidateScene,
                    "AuthoredOperationMapEntityPresentation/GameplayBuildings");
                if (buildingRoot.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length != 0 ||
                    buildingRoot.Cast<Transform>().Any(child => child.childCount != 0))
                {
                    throw new InvalidOperationException("Candidate gameplay-building roots are not empty; refusing to overwrite them.");
                }

                int migrated = 0;
                for (int i = 0; i < report.placements.Count; i++)
                {
                    OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildingPlacementReport row =
                        report.placements[i];
                    if (row == null || row.placementIndex != i ||
                        !string.Equals(row.authoredJoinResolveState, "Exact", StringComparison.Ordinal) ||
                        !string.Equals(row.intactDisposition, "AssignedIntact", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Building inventory row {i} is not mutation-ready.");
                    }

                    MapBuildingPlacementConfigEntry placement = placements.Placements[i];
                    if (placement == null || placement.BuildingPrefab == null ||
                        !string.Equals(placement.SourcePath, row.sourcePath, StringComparison.Ordinal) ||
                        !string.Equals(AssetDatabase.GetAssetPath(placement.BuildingPrefab), row.buildingPrefabPath,
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException($"Building placement {i} drifted from its accepted inventory row.");
                    }

                    if (!GlobalObjectId.TryParse(row.ownerSourceGlobalObjectId, out GlobalObjectId sourceId) ||
                        GlobalObjectId.GlobalObjectIdentifierToObjectSlow(sourceId) is not GameObject sourceOwner ||
                        sourceOwner.scene != sourceScene)
                    {
                        throw new InvalidOperationException($"Building source identity could not be resolved at placement {i}.");
                    }

                    BuildingDefinitionAuthoring definition =
                        placement.BuildingPrefab.GetComponent<BuildingDefinitionAuthoring>();
                    if (definition == null)
                        throw new InvalidOperationException($"Building prefab lacks definition authoring: {row.buildingPrefabPath}");

                    Transform categoryRoot = ResolveCategoryRoot(buildingRoot, placement.Category);
                    var candidateOwner = new GameObject($"Building_{i:D4}_{sourceOwner.name}");
                    SceneManager.MoveGameObjectToScene(candidateOwner, candidateScene);
                    candidateOwner.transform.SetParent(categoryRoot, false);
                    candidateOwner.transform.SetPositionAndRotation(
                        placement.WorldPosition,
                        Quaternion.Euler(placement.WorldEulerAngles));
                    candidateOwner.transform.localScale = Vector3.one;

                    GameObject intactRoot = CreateVisualRoot(candidateOwner.transform, "IntactVisual");
                    GameObject intactVisual = UnityEngine.Object.Instantiate(sourceOwner, intactRoot.transform, false);
                    intactVisual.name = sourceOwner.name;
                    intactVisual.transform.localPosition = Vector3.zero;
                    intactVisual.transform.localRotation = Quaternion.identity;
                    intactVisual.transform.localScale = placement.WorldScale;
                    intactVisual.SetActive(true);
                    ConfigurePresentationIdentity(
                        intactVisual,
                        row.ownerSourceGlobalObjectId,
                        OperationMapEntityPresentationRole.GameplayBuildings,
                        i);
                    RemoveProhibitedCandidateComponents(intactVisual);

                    GameObject destroyedRoot = null;
                    if (definition.ConfiguredDestroyedVisualPrefab != null)
                    {
                        destroyedRoot = CreateVisualRoot(candidateOwner.transform, "DestroyedVisual");
                        GameObject destroyedVisual = UnityEngine.Object.Instantiate(
                            definition.ConfiguredDestroyedVisualPrefab,
                            destroyedRoot.transform,
                            false);
                        destroyedVisual.name = definition.ConfiguredDestroyedVisualPrefab.name;
                        destroyedVisual.transform.localPosition = Vector3.zero;
                        destroyedVisual.transform.localRotation = Quaternion.identity;
                        destroyedVisual.transform.localScale = placement.WorldScale;
                        destroyedVisual.SetActive(true);
                        RemoveProhibitedCandidateComponents(destroyedVisual);
                    }

                    Vector2Int footprint = definition.ConfiguredFootprintCells;
                    if (placement.RotateVertical)
                        footprint = new Vector2Int(footprint.y, footprint.x);
                    int2 centerCell = WorldToCell(grid, placement.WorldCenter);
                    Vector2Int originCell = CenterCellToOrigin(centerCell, footprint, grid);
                    ConfigureAuthoring(
                        candidateOwner.AddComponent<OperationMapBuildingAuthoring>(),
                        row.ownerSourceGlobalObjectId,
                        i,
                        placement.FactionId,
                        originCell,
                        definition,
                        intactRoot,
                        destroyedRoot);
                    migrated++;
                }

                if (migrated != report.counts.placementCount || migrated != 432)
                    throw new InvalidOperationException($"Expected 432 migrated buildings, found {migrated}.");

                if (!EditorSceneManager.SaveScene(candidateScene, candidatePath, false))
                    throw new InvalidOperationException("Candidate building migration save failed.");
                EditorSceneManager.CloseScene(candidateScene, true);
                EditorSceneManager.CloseScene(sourceScene, true);

                RequireHashUnchanged(
                    acceptedSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath));
                RequireHashUnchanged(
                    acceptedSubSceneHash,
                    ResolveProjectPath(projectRoot,
                        OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath));

                Debug.Log(
                    $"[OperationMapBuildingCandidateMigrationEditor] status=Created buildings={migrated} " +
                    $"candidate={candidatePath} managedRuntimeBuildingEntity=0 productionCutover=0");
            }
            catch
            {
                File.WriteAllBytes(candidatePhysicalPath, candidateBackup);
                File.WriteAllBytes(candidateMetaPhysicalPath, candidateMetaBackup);
                AssetDatabase.ImportAsset(candidatePath, ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        internal static OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport
            LoadInventory(string projectRoot)
        {
            string path = ResolveProjectPath(projectRoot, AttachmentInventoryPath);
            if (!File.Exists(path))
                throw new FileNotFoundException("Accepted building attachment inventory is missing.", path);
            var report = JsonUtility.FromJson<
                OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport>(
                File.ReadAllText(path));
            if (report == null ||
                !OperationMapBuildingAttachmentOwnershipInventoryProbe.IsSuccessResult(report.result) ||
                report.counts == null || report.placements == null ||
                report.counts.placementCount != report.placements.Count ||
                report.counts.unresolvedJoinCount != 0 ||
                report.counts.reusedSourceJoinCount != 0 ||
                report.counts.unassignedOrphanCount != 0 ||
                report.counts.sharedAcrossBuildingsCount != 0 ||
                report.counts.dualStateCount != 0)
            {
                throw new InvalidOperationException("Accepted building attachment inventory is incomplete or drifted.");
            }
            return report;
        }

        private static Transform ResolveCategoryRoot(Transform buildingRoot, string category)
        {
            string childName = category switch
            {
                "Building_Hall" or "Building_House" or "Building_Shop" => "HandmadeCity",
                "Wall_Dirt_Straight" or "Wall_Fence_Straight" or "Building_Road_Barrier" => "Infrastructure",
                _ => "MilitaryBase"
            };
            Transform child = buildingRoot.Find(childName);
            if (child == null)
                throw new InvalidOperationException($"Candidate building category root is missing: {childName}");
            return child;
        }

        private static void RemoveProhibitedCandidateComponents(GameObject owner)
        {
            foreach (Collider collider in owner.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            foreach (Rigidbody body in owner.GetComponentsInChildren<Rigidbody>(true))
                UnityEngine.Object.DestroyImmediate(body);
            foreach (Animator animator in owner.GetComponentsInChildren<Animator>(true))
            {
                if (animator.runtimeAnimatorController == null)
                    UnityEngine.Object.DestroyImmediate(animator);
            }
        }

        private static void ConfigureAuthoring(
            OperationMapBuildingAuthoring authoring,
            string sourceGlobalObjectId,
            int placementIndex,
            byte factionId,
            Vector2Int originCell,
            BuildingDefinitionAuthoring definition,
            GameObject intactVisualRoot,
            GameObject destroyedVisualRoot)
        {
            var serialized = new SerializedObject(authoring);
            serialized.FindProperty("operationMapId").stringValue =
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId;
            serialized.FindProperty("sourceGlobalObjectId").stringValue = sourceGlobalObjectId;
            serialized.FindProperty("placementIndex").intValue = placementIndex;
            serialized.FindProperty("factionId").intValue = factionId;
            serialized.FindProperty("originCell").vector2IntValue = originCell;
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("intactVisualRoot").objectReferenceValue = intactVisualRoot;
            serialized.FindProperty("destroyedVisualRoot").objectReferenceValue = destroyedVisualRoot;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!authoring.TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        private static GameObject CreateVisualRoot(Transform parent, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static void ConfigurePresentationIdentity(
            GameObject owner,
            string sourceGlobalObjectId,
            OperationMapEntityPresentationRole role,
            int placementIndex)
        {
            OperationMapEntityPresentationIdentityAuthoring identity =
                owner.GetComponent<OperationMapEntityPresentationIdentityAuthoring>() ??
                owner.AddComponent<OperationMapEntityPresentationIdentityAuthoring>();
            identity.ConfigureForEditor(
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                sourceGlobalObjectId,
                role,
                placementIndex);
            if (!identity.TryValidate(out string error))
                throw new InvalidOperationException(error);
        }

        private static int2 WorldToCell(GridAuthoringConfig grid, Vector3 world)
        {
            Vector3 relative = world - grid.Origin;
            return new int2(
                Mathf.FloorToInt(relative.x / grid.CellSize),
                Mathf.FloorToInt(relative.z / grid.CellSize));
        }

        private static Vector2Int CenterCellToOrigin(int2 centerCell, Vector2Int footprint, GridAuthoringConfig grid)
        {
            int originX = centerCell.x - Mathf.Max(0, footprint.x - 1) / 2;
            int originY = centerCell.y - Mathf.Max(0, footprint.y - 1) / 2;
            return new Vector2Int(
                Mathf.Clamp(originX, 0, Mathf.Max(0, grid.Width - footprint.x)),
                Mathf.Clamp(originY, 0, Mathf.Max(0, grid.Height - footprint.y)));
        }

        private static Transform RequirePath(Scene scene, string path)
        {
            string[] segments = path.Split('/');
            GameObject root = scene.GetRootGameObjects().SingleOrDefault(owner => owner.name == segments[0]);
            Transform current = root != null ? root.transform : null;
            for (int i = 1; i < segments.Length && current != null; i++)
                current = current.Find(segments[i]);
            return current ?? throw new InvalidOperationException($"Candidate hierarchy path is missing: {path}");
        }

        private static string ResolveProjectPath(string projectRoot, string repositoryPath) =>
            Path.GetFullPath(Path.Combine(projectRoot, repositoryPath));

        private static string ComputeSha256(string path)
        {
            using var stream = File.OpenRead(path);
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Concat(sha.ComputeHash(stream).Select(value => value.ToString("x2")));
        }

        private static void RequireHashUnchanged(string expected, string path)
        {
            if (!string.Equals(expected, ComputeSha256(path), StringComparison.Ordinal))
                throw new InvalidOperationException($"Protected accepted source changed: {path}");
        }
    }
}

#endif
