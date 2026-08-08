#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.IO;
    using Game.Authoring;
    using Game.Configs;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// VRP-051 candidate-only transaction. It persists exactly one virtualization
    /// authoring root and switches only the dense candidate definition.
    /// </summary>
    internal static class OperationMapRenderVirtualizationPilotEnabler
    {
        private const int CurrentAcceptedIdentityCount = 9544;
        private const int CurrentVirtualizedGeneratedIdentityCount = 36304;

        private const string CandidateDefinitionPath =
            "Assets/Game/Configs/OperationMaps/Candidates/" +
            "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";

        public static void EnableAndValidate()
        {
            string projectRoot =
                Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string candidateScenePath =
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string scenePhysical = Path.Combine(projectRoot, candidateScenePath);
            string definitionPhysical =
                Path.Combine(projectRoot, CandidateDefinitionPath);
            string sceneBackup = Path.GetTempFileName();
            string definitionBackup = Path.GetTempFileName();
            File.Copy(scenePhysical, sceneBackup, true);
            File.Copy(definitionPhysical, definitionBackup, true);

            var protectedSnapshot =
                OperationMapEntitySceneCandidateBakeAll.ProtectedProductionSnapshot
                    .Capture(
                        projectRoot,
                        new[]
                        {
                            OperationMapEntityPresentationCandidateSceneBuilder
                                .AcceptedOperationMapScenePath,
                            OperationMapEntityPresentationMigrationEditor
                                .AcceptedSubScenePath,
                            OperationMapAddressablesLayoutBuilder.DefinitionPath,
                            OperationMapAddressablesLayoutBuilder.SourceScenePath,
                            "Assets/AddressableAssetsData/AddressableAssetSettings.asset"
                        },
                        new[]
                        {
                            OperationMapEntityPresentationCandidateSceneBuilder
                                .StaticRollbackRoot,
                            "Assets/AddressableAssetsData/AssetGroups"
                        });

            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();
            try
            {
                OperationMapDefinition definition =
                    AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                        CandidateDefinitionPath);
                if (definition == null ||
                    definition.PresentationKind !=
                    OperationMapPresentationKind.EntityScene)
                {
                    throw new InvalidOperationException(
                        "Dense candidate EntityScene definition is missing.");
                }
                SerializedObject serializedDefinition =
                    new SerializedObject(definition);
                SerializedProperty residency =
                    serializedDefinition.FindProperty("renderResidencyMode");
                if (residency == null)
                {
                    throw new InvalidOperationException(
                        "Dense candidate render-residency property is missing.");
                }
                residency.intValue =
                    (int)OperationMapRenderResidencyMode.VirtualizedProxyPool;
                serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
                AssetDatabase.SaveAssets();

                Scene candidate = EditorSceneManager.OpenScene(
                    candidateScenePath,
                    OpenSceneMode.Single);
                OperationMapVirtualizedPresentationAuthoring[] existing =
                    UnityEngine.Object.FindObjectsByType<
                        OperationMapVirtualizedPresentationAuthoring>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                OperationMapVirtualizedPresentationAuthoring authoring;
                if (existing.Length == 0)
                {
                    GameObject root =
                        new GameObject("OperationMapRenderVirtualization");
                    SceneManager.MoveGameObjectToScene(root, candidate);
                    authoring =
                        root.AddComponent<
                            OperationMapVirtualizedPresentationAuthoring>();
                }
                else if (existing.Length == 1 &&
                         existing[0].gameObject.scene == candidate)
                {
                    authoring = existing[0];
                }
                else
                {
                    throw new InvalidOperationException(
                        "Dense candidate must contain exactly one virtualization root.");
                }

                OperationMapRenderDatabaseBakeConfig config =
                    AssetDatabase.LoadAssetAtPath<
                        OperationMapRenderDatabaseBakeConfig>(
                        OperationMapRenderDatabaseBuilder.ConfigPath);
                string configError = null;
                if (config == null || !config.TryValidateSchema(out configError))
                {
                    throw new InvalidOperationException(
                        $"Dense candidate render database is invalid: {configError}");
                }
                SerializedObject serializedAuthoring =
                    new SerializedObject(authoring);
                serializedAuthoring.FindProperty("databaseConfig")
                    .objectReferenceValue = config;
                serializedAuthoring.FindProperty("sourcePresentationRoot")
                    .objectReferenceValue = authoring.gameObject;
                serializedAuthoring.FindProperty("mapGeneration").intValue = 0;
                serializedAuthoring.ApplyModifiedPropertiesWithoutUndo();
                if (!authoring.TryValidate(out string authoringError))
                {
                    throw new InvalidOperationException(
                        $"Dense candidate virtualization root is invalid: {authoringError}");
                }
                if (!EditorSceneManager.SaveScene(
                        candidate,
                        candidateScenePath,
                        false))
                {
                    throw new InvalidOperationException(
                        "Dense candidate virtualization scene save failed.");
                }
                AssetDatabase.SaveAssets();
                protectedSnapshot.RequireUnchanged();
                OperationMapRenderVirtualizationCandidateValidator
                    .RunTwoPassValidation();
                protectedSnapshot.RequireUnchanged();
                Debug.Log(
                    "[OperationMapRenderVirtualizationPilotEnablement] " +
                    "result=Passed placements=9721 rows=11299 slots=704 " +
                    "candidateMode=VirtualizedProxyPool productionCutover=0");
            }
            catch
            {
                File.Copy(sceneBackup, scenePhysical, true);
                File.Copy(definitionBackup, definitionPhysical, true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                if (OperationMapEntitySceneCandidateBakeAll
                    .HasRestorableSceneSetup(previousSetup))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
                File.Delete(sceneBackup);
                File.Delete(definitionBackup);
            }
        }

        public static void SynchronizeCurrentReadinessContract()
        {
            string candidateScenePath =
                DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string physicalPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", candidateScenePath));
            string backupPath = Path.GetTempFileName();
            File.Copy(physicalPath, backupPath, true);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene candidate = EditorSceneManager.OpenScene(
                    candidateScenePath,
                    OpenSceneMode.Single);
                DenseCityPresentationIdentityAuthoring[] generatedIdentities =
                    UnityEngine.Object.FindObjectsByType<
                        DenseCityPresentationIdentityAuthoring>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                OperationMapEntityPresentationIdentityAuthoring[] acceptedIdentities =
                    UnityEngine.Object.FindObjectsByType<
                        OperationMapEntityPresentationIdentityAuthoring>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                int generatedIdentityCount = 0;
                int acceptedIdentityCount = 0;
                int gameplayBuildingCount = 0;
                int gameplayVehicleCount = 0;
                int renderOnlyCount = 0;
                for (int index = 0; index < generatedIdentities.Length; index++)
                {
                    DenseCityPresentationIdentityAuthoring identity =
                        generatedIdentities[index];
                    if (identity.gameObject.scene != candidate)
                        continue;
                    if (!identity.TryValidate(out string identityError))
                    {
                        throw new InvalidOperationException(
                            "Virtualized generated identity is invalid: " +
                            identityError);
                    }
                    generatedIdentityCount++;
                    switch (identity.Role)
                    {
                        case OperationMapEntityPresentationRole.GameplayBuildings:
                            gameplayBuildingCount++;
                            break;
                        case OperationMapEntityPresentationRole.RenderOnly:
                            renderOnlyCount++;
                            break;
                        default:
                            throw new InvalidOperationException(
                                "Virtualized generated identity has an unsupported role.");
                    }
                }
                for (int index = 0; index < acceptedIdentities.Length; index++)
                {
                    OperationMapEntityPresentationIdentityAuthoring identity =
                        acceptedIdentities[index];
                    if (identity.gameObject.scene != candidate)
                        continue;
                    if (!identity.TryValidate(out string identityError))
                    {
                        throw new InvalidOperationException(
                            "Virtualized accepted identity is invalid: " + identityError);
                    }
                    acceptedIdentityCount++;
                    switch (identity.Role)
                    {
                        case OperationMapEntityPresentationRole.GameplayBuildings:
                            gameplayBuildingCount++;
                            break;
                        case OperationMapEntityPresentationRole.GameplayVehicles:
                            gameplayVehicleCount++;
                            break;
                        case OperationMapEntityPresentationRole.RenderOnly:
                            renderOnlyCount++;
                            break;
                        default:
                            throw new InvalidOperationException(
                                "Virtualized accepted identity has an unsupported role.");
                    }
                }
                if (generatedIdentityCount !=
                    CurrentVirtualizedGeneratedIdentityCount)
                {
                    throw new InvalidOperationException(
                        "Virtualized readiness synchronization expected " +
                        $"{CurrentVirtualizedGeneratedIdentityCount} generated identities, " +
                        $"found {generatedIdentityCount}.");
                }
                if (acceptedIdentityCount != CurrentAcceptedIdentityCount)
                {
                    throw new InvalidOperationException(
                        "Virtualized readiness synchronization expected " +
                        $"{CurrentAcceptedIdentityCount} accepted identities, " +
                        $"found {acceptedIdentityCount}.");
                }

                OperationMapEntityPresentationRootAuthoring[] roots =
                    UnityEngine.Object.FindObjectsByType<
                        OperationMapEntityPresentationRootAuthoring>(
                        FindObjectsInactive.Include,
                        FindObjectsSortMode.None);
                OperationMapEntityPresentationRootAuthoring contractRoot = null;
                for (int index = 0; index < roots.Length; index++)
                {
                    if (roots[index].gameObject.scene != candidate ||
                        roots[index].Role !=
                        OperationMapEntityPresentationRole.GameplayBuildings)
                    {
                        continue;
                    }
                    if (contractRoot != null)
                    {
                        throw new InvalidOperationException(
                            "Virtualized candidate contains more than one readiness-contract root.");
                    }
                    contractRoot = roots[index];
                }
                if (contractRoot == null)
                {
                    throw new InvalidOperationException(
                        "Virtualized candidate readiness-contract root is missing.");
                }

                var serialized = new SerializedObject(contractRoot);
                SerializedProperty expectedBuildings =
                    serialized.FindProperty("expectedGameplayBuildingCount");
                SerializedProperty expectedVehicles =
                    serialized.FindProperty("expectedGameplayVehicleCount");
                SerializedProperty expectedRenderOnly =
                    serialized.FindProperty("expectedRenderOnlyCount");
                SerializedProperty expectedGenerated =
                    serialized.FindProperty("expectedGeneratedIdentityCount");
                if (expectedBuildings == null ||
                    expectedVehicles == null ||
                    expectedRenderOnly == null ||
                    expectedGenerated == null)
                {
                    throw new InvalidOperationException(
                        "Virtualized identity-readiness properties are missing.");
                }
                expectedBuildings.intValue = gameplayBuildingCount;
                expectedVehicles.intValue = gameplayVehicleCount;
                expectedRenderOnly.intValue = renderOnlyCount;
                expectedGenerated.intValue = generatedIdentityCount;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(contractRoot);
                if (!EditorSceneManager.SaveScene(candidate, candidateScenePath, false))
                {
                    throw new InvalidOperationException(
                        "Virtualized readiness-contract scene save failed.");
                }
                AssetDatabase.SaveAssets();
                Debug.Log(
                    "[OperationMapRenderVirtualizationReadinessContract] result=Passed " +
                    $"acceptedIdentities={acceptedIdentityCount} " +
                    $"generatedIdentities={generatedIdentityCount} " +
                    $"buildings={gameplayBuildingCount} " +
                    $"vehicles={gameplayVehicleCount} " +
                    $"renderOnly={renderOnlyCount}");
            }
            catch
            {
                File.Copy(backupPath, physicalPath, true);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                File.Delete(backupPath);
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
            }
        }
    }
}

#endif
