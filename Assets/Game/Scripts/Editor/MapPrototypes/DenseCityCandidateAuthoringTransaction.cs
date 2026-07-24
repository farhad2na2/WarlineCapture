using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Game.Authoring;
using Game.Configs;
using Game.Runtime;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal static class DenseCityCandidateAuthoringTransaction
    {
        internal const string CandidateMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_dense_city_authoring_candidate.unity";
        internal const string CandidateEntityScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_dense_city_candidate.unity";

        private const string SourceMapScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity";
        private const string SourceEntityScenePath =
            "Assets/Game/Scenes/OperationMaps/Skirmish/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_presentation_candidate.unity";
        private const string ConfigPath =
            "Assets/Game/Configs/OperationMaps/Skirmish/" +
            "SkirmishDesertBase_MapWideCity_Config.asset";
        private const string GeneratorSchema = "dense-city-v1";
        private const int GeneratorSchemaVersion = 1;
        private const string CandidateGeneratedAssetRoot =
            "Assets/Game/GeneratedOperationMaps/DenseCity";
        private const string LegacySkyMaterialPath =
            "Assets/PolygonMilitary/Materials/Misc/SkyBox.mat";
        private const string CandidateSkyMaterialPath =
            "Assets/Game/GeneratedOperationMaps/DenseCity/" +
            "opmap.skirmish.desert_base_01/Candidate/SharedMaterials/" +
            "DenseCity_SkyBox_DOTS.mat";
        private const string CandidateSharedMaterialFolder =
            "Assets/Game/GeneratedOperationMaps/DenseCity/" +
            "opmap.skirmish.desert_base_01/Candidate/SharedMaterials";
        private const string SyntyGenericBasicShaderName = "Synty/Generic_Basic";

        [MenuItem("Game/Maps/Skirmish Desert Base/Create Dense City Candidate Hierarchy")]
        public static void CreateCandidateHierarchy()
        {
            RuntimeCitySpawnerSystemConfig config =
                AssetDatabase.LoadAssetAtPath<RuntimeCitySpawnerSystemConfig>(ConfigPath);
            if (config == null)
                throw new InvalidOperationException($"Dense-city config is missing: '{ConfigPath}'.");

            int seed = unchecked((int)config.RandomSeed);
            string generationHash = ComputeGenerationHash(
                SourceMapScenePath,
                SourceEntityScenePath,
                ConfigPath,
                GeneratorSchema,
                GeneratorSchemaVersion,
                seed);
            string generationId =
                $"dense-city:{OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId}:" +
                generationHash.Substring(0, 16);

            if (!TryCreate(
                    SourceMapScenePath,
                    SourceEntityScenePath,
                    CandidateMapScenePath,
                    CandidateEntityScenePath,
                    generationId,
                    GeneratorSchema,
                    GeneratorSchemaVersion,
                    seed,
                    generationHash,
                    out string error))
            {
                throw new InvalidOperationException(
                    $"Dense-city candidate hierarchy transaction rejected: {error}");
            }

            Debug.Log(
                $"[DenseCityCandidateAuthoringTransaction] result=Created " +
                $"generationId={generationId} generationHash={generationHash} " +
                $"mapCandidate={CandidateMapScenePath} entityCandidate={CandidateEntityScenePath}");
        }

        [MenuItem("Game/Maps/Skirmish Desert Base/Realize Dense City Candidate")]
        public static void RealizeCandidate()
        {
            if (!TryRealizeCandidate(out string summary, out string error))
            {
                string message = $"Dense-city candidate realization rejected: {error}";
                if (Application.isBatchMode)
                {
                    Debug.LogError(message);
                    EditorApplication.Exit(1);
                    return;
                }

                throw new InvalidOperationException(message);
            }

            Debug.Log($"[DenseCityCandidateAuthoringTransaction] result=Realized {summary}");
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        [MenuItem(
            "Game/Maps/Skirmish Desert Base/Apply Dense City Candidate DOTS Materials")]
        public static void ApplyCandidateMaterialCompatibilityBatch()
        {
            string sourceMapHash = ComputeFileHash(SourceMapScenePath);
            string sourceEntityHash = ComputeFileHash(SourceEntityScenePath);
            string candidateBackup = CreateBackup(CandidateEntityScenePath);
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            Scene candidate = default;
            try
            {
                candidate = EditorSceneManager.OpenScene(
                    CandidateEntityScenePath,
                    OpenSceneMode.Additive);
                int compatibleRendererCount =
                    ApplyCandidateMaterialCompatibility(
                        candidate,
                        out int syntyMaterialSlotCount);
                if (compatibleRendererCount != 1)
                {
                    throw new InvalidOperationException(
                        "Dense-city candidate requires exactly one legacy sky renderer; " +
                        $"found {compatibleRendererCount}.");
                }
                if (!EditorSceneManager.SaveScene(
                        candidate,
                        CandidateEntityScenePath,
                        false))
                {
                    throw new InvalidOperationException(
                        "Dense-city candidate DOTS material save failed.");
                }
                AssetDatabase.SaveAssets();
                RequireProtectedSourceHashes(sourceMapHash, sourceEntityHash);
                Debug.Log(
                    "[DenseCityCandidateMaterialCompatibility] result=Passed " +
                    $"compatibleRenderers={compatibleRendererCount} " +
                    $"syntyMaterialSlots={syntyMaterialSlotCount} " +
                    $"material={CandidateSkyMaterialPath}");
            }
            catch
            {
                RestoreBackup(candidateBackup, CandidateEntityScenePath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                throw;
            }
            finally
            {
                CloseScene(ref candidate);
                DeleteBackup(candidateBackup);
                RestoreSceneSetup(previousSetup);
            }
        }

        internal static bool TryRealizeCandidate(out string summary, out string error)
        {
            summary = null;
            error = null;
            Scene mapScene = default;
            Scene entityScene = default;
            Scene previousActiveScene = SceneManager.GetActiveScene();
            string mapBackup = null;
            string entityBackup = null;
            string proxyFolder = null;

            try
            {
                if (!AssetExists(CandidateMapScenePath) ||
                    !AssetExists(CandidateEntityScenePath))
                {
                    error = "Dense-city candidate hierarchy scenes are missing.";
                    return false;
                }

                string sourceMapHash = ComputeFileHash(SourceMapScenePath);
                string sourceEntityHash = ComputeFileHash(SourceEntityScenePath);
                mapBackup = CreateBackup(CandidateMapScenePath);
                entityBackup = CreateBackup(CandidateEntityScenePath);
                mapScene = EditorSceneManager.OpenScene(
                    CandidateMapScenePath,
                    OpenSceneMode.Additive);
                entityScene = EditorSceneManager.OpenScene(
                    CandidateEntityScenePath,
                    OpenSceneMode.Additive);
                if (!SceneManager.SetActiveScene(mapScene))
                {
                    throw new InvalidOperationException(
                        "Dense-city map candidate could not become the active generation scene.");
                }

                DenseCityGeneratedRootAuthoring mapRoot =
                    RequireGeneratedRoot(mapScene, DenseCityGeneratedRootRole.MapBakeSource);
                DenseCityGeneratedRootAuthoring entityRoot =
                    RequireGeneratedRoot(
                        entityScene,
                        DenseCityGeneratedRootRole.EntityPresentationSource);
                string generationId = mapRoot.GenerationId;
                if (!DenseCitySemanticHierarchyBuilder.TryValidate(
                        mapScene,
                        entityScene,
                        generationId,
                        out error))
                {
                    throw new InvalidOperationException(error);
                }
                RequireEmptyCandidateOwnership(mapRoot, entityRoot);

                RuntimeCityRAndDMapView view = RequireMapView(mapScene);
                DenseMiddleEasternCityEditModeBuilder.Result result =
                    RuntimeCityRAndDEditModeBuilder.BuildDenseMapWide(view);
                if (result.Records == null ||
                    result.Records.Buildings.Count != result.SemanticBuildings ||
                    result.Records.Surfaces.Count != result.SemanticSurfaces ||
                    result.Records.Presentations.Count != result.SemanticPresentations)
                {
                    throw new InvalidOperationException(
                        "Dense-city generator result and replay snapshot counts differ.");
                }

                DenseCityPresentationHierarchyContext hierarchy =
                    DenseCityPresentationHierarchyContext.Create(entityRoot);
                DenseCityRealizedPresentationSet realized =
                    DenseCityPresentationReplayTransaction.Realize(
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        result.Records,
                        hierarchy,
                        DenseCityBuildingDefinitionLibrary.LoadExisting(),
                        DenseCityBuildingMaterialLibrary.LoadExisting());
                ApplyCandidateMaterialCompatibility(entityScene, out _);

                proxyFolder = CandidateGeneratedAssetRoot + "/" +
                              OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId +
                              "/Candidate/" + mapRoot.DeterministicGenerationHash +
                              "/SurfaceProxies";
                Rect mapBounds = new(
                    view.GridOrigin.x,
                    view.GridOrigin.z,
                    view.GridWidth * view.GridCellSize,
                    view.GridHeight * view.GridCellSize);
                DenseCitySurfaceProxyBuildResult proxies = DenseCitySurfaceProxyBuilder.Build(
                    result.Records,
                    mapRoot,
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    mapBounds,
                    proxyFolder);

                ClearTemporaryLegacyVisuals(view.GeneratedRoot);
                if (!DenseCityBakeReadinessValidator.TryValidateAuthoringOwnership(
                        mapScene,
                        entityScene,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        generationId,
                        out error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!EditorSceneManager.SaveScene(mapScene, CandidateMapScenePath, false) ||
                    !EditorSceneManager.SaveScene(entityScene, CandidateEntityScenePath, false))
                {
                    throw new InvalidOperationException(
                        "Dense-city realized candidate scene save failed.");
                }

                RestoreActiveScene(previousActiveScene);
                CloseScene(ref entityScene);
                CloseScene(ref mapScene);
                RequireProtectedSourceHashes(sourceMapHash, sourceEntityHash);
                AssetDatabase.SaveAssets();
                summary =
                    $"generationId={generationId} buildings={realized.Buildings.Count} " +
                    $"renderOnly={realized.RenderOnly.Count} proxies={proxies.Partitions} " +
                    $"surfaces={proxies.Records} proxyFolder={proxyFolder}";
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                RestoreActiveScene(previousActiveScene);
                CloseScene(ref entityScene);
                CloseScene(ref mapScene);
                if (!string.IsNullOrEmpty(proxyFolder))
                    AssetDatabase.DeleteAsset(proxyFolder);
                RestoreBackup(mapBackup, CandidateMapScenePath);
                RestoreBackup(entityBackup, CandidateEntityScenePath);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                return false;
            }
            finally
            {
                DeleteBackup(mapBackup);
                DeleteBackup(entityBackup);
            }
        }

        internal static bool TryCreate(
            string sourceMapScenePath,
            string sourceEntityScenePath,
            string candidateMapScenePath,
            string candidateEntityScenePath,
            string generationId,
            string generatorSchema,
            int generatorSchemaVersion,
            int deterministicSeed,
            string deterministicGenerationHash,
            out string error)
        {
            error = null;
            Scene candidateMapScene = default;
            Scene candidateEntityScene = default;
            bool mapCandidateCreated = false;
            bool entityCandidateCreated = false;

            try
            {
                if (!IsDistinctScenePair(
                        sourceMapScenePath,
                        sourceEntityScenePath,
                        candidateMapScenePath,
                        candidateEntityScenePath))
                {
                    error = "Dense-city candidate transaction requires four distinct scene asset paths.";
                    return false;
                }
                if (!File.Exists(ToPhysicalPath(sourceMapScenePath)) ||
                    !File.Exists(ToPhysicalPath(sourceEntityScenePath)))
                {
                    error = "Dense-city candidate source scene is missing.";
                    return false;
                }
                if (!OperationMapHashRules.IsValidSha256(deterministicGenerationHash))
                {
                    error = "Dense-city candidate generation hash is invalid.";
                    return false;
                }
                if (generatorSchemaVersion <= 0 ||
                    string.IsNullOrWhiteSpace(generationId) ||
                    string.IsNullOrWhiteSpace(generatorSchema))
                {
                    error = "Dense-city candidate generation identity is invalid.";
                    return false;
                }
                if (AssetExists(candidateMapScenePath) ||
                    AssetExists(candidateEntityScenePath))
                {
                    error = "Dense-city candidate scene already exists.";
                    return false;
                }

                string sourceMapHash = ComputeFileHash(sourceMapScenePath);
                string sourceEntityHash = ComputeFileHash(sourceEntityScenePath);
                EnsureAssetFolder(Path.GetDirectoryName(candidateMapScenePath)?.Replace('\\', '/'));
                EnsureAssetFolder(Path.GetDirectoryName(candidateEntityScenePath)?.Replace('\\', '/'));

                if (!AssetDatabase.CopyAsset(sourceMapScenePath, candidateMapScenePath))
                    throw new InvalidOperationException("Dense-city map candidate copy failed.");
                mapCandidateCreated = true;
                if (!AssetDatabase.CopyAsset(sourceEntityScenePath, candidateEntityScenePath))
                    throw new InvalidOperationException("Dense-city entity candidate copy failed.");
                entityCandidateCreated = true;
                AssetDatabase.ImportAsset(
                    candidateMapScenePath,
                    ImportAssetOptions.ForceSynchronousImport);
                AssetDatabase.ImportAsset(
                    candidateEntityScenePath,
                    ImportAssetOptions.ForceSynchronousImport);

                RequireIndependentGuid(sourceMapScenePath, candidateMapScenePath);
                RequireIndependentGuid(sourceEntityScenePath, candidateEntityScenePath);
                candidateMapScene =
                    EditorSceneManager.OpenScene(candidateMapScenePath, OpenSceneMode.Additive);
                candidateEntityScene =
                    EditorSceneManager.OpenScene(candidateEntityScenePath, OpenSceneMode.Additive);

                RuntimeCityRAndDEditModeBuilder.ReplaceDenseCitySemanticHierarchy(
                    candidateMapScene,
                    candidateEntityScene,
                    generationId,
                    generatorSchema,
                    generatorSchemaVersion,
                    deterministicSeed,
                    deterministicGenerationHash);
                if (!DenseCitySemanticHierarchyBuilder.TryValidate(
                        candidateMapScene,
                        candidateEntityScene,
                        generationId,
                        out error))
                {
                    throw new InvalidOperationException(error);
                }
                if (!DenseCityBakeReadinessValidator.TryResolveGenerationState(
                        candidateMapScene,
                        candidateEntityScene,
                        out bool generated,
                        out string resolvedGenerationId,
                        out error) ||
                    !generated ||
                    !string.Equals(
                        generationId,
                        resolvedGenerationId,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        error ?? "Dense-city candidate generation state did not resolve.");
                }

                if (!EditorSceneManager.SaveScene(
                        candidateMapScene,
                        candidateMapScenePath,
                        false) ||
                    !EditorSceneManager.SaveScene(
                        candidateEntityScene,
                        candidateEntityScenePath,
                        false))
                {
                    throw new InvalidOperationException("Dense-city candidate scene save failed.");
                }
                EditorSceneManager.CloseScene(candidateEntityScene, true);
                candidateEntityScene = default;
                EditorSceneManager.CloseScene(candidateMapScene, true);
                candidateMapScene = default;

                if (!string.Equals(
                        sourceMapHash,
                        ComputeFileHash(sourceMapScenePath),
                        StringComparison.Ordinal) ||
                    !string.Equals(
                        sourceEntityHash,
                        ComputeFileHash(sourceEntityScenePath),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Dense-city candidate transaction changed a protected source scene.");
                }

                AssetDatabase.SaveAssets();
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                if (candidateEntityScene.IsValid() && candidateEntityScene.isLoaded)
                    EditorSceneManager.CloseScene(candidateEntityScene, true);
                if (candidateMapScene.IsValid() && candidateMapScene.isLoaded)
                    EditorSceneManager.CloseScene(candidateMapScene, true);
                if (entityCandidateCreated)
                    AssetDatabase.DeleteAsset(candidateEntityScenePath);
                if (mapCandidateCreated)
                    AssetDatabase.DeleteAsset(candidateMapScenePath);
                return false;
            }
        }

        internal static string ComputeGenerationHash(
            string sourceMapScenePath,
            string sourceEntityScenePath,
            string configPath,
            string generatorSchema,
            int generatorSchemaVersion,
            int deterministicSeed)
        {
            string input =
                ComputeFileHash(sourceMapScenePath) + "\n" +
                ComputeFileHash(sourceEntityScenePath) + "\n" +
                ComputeFileHash(configPath) + "\n" +
                generatorSchema + "\n" +
                generatorSchemaVersion.ToString(CultureInfo.InvariantCulture) + "\n" +
                deterministicSeed.ToString(CultureInfo.InvariantCulture);
            using SHA256 sha = SHA256.Create();
            return ToLowerHex(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private static bool IsDistinctScenePair(params string[] paths)
        {
            var unique = new System.Collections.Generic.HashSet<string>(
                StringComparer.Ordinal);
            for (int index = 0; index < paths.Length; index++)
            {
                string path = paths[index];
                if (string.IsNullOrWhiteSpace(path) ||
                    !path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) ||
                    !unique.Add(path))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool AssetExists(string assetPath) =>
            AssetDatabase.LoadAssetAtPath<SceneAsset>(assetPath) != null ||
            File.Exists(ToPhysicalPath(assetPath));

        private static DenseCityGeneratedRootAuthoring RequireGeneratedRoot(
            Scene scene,
            DenseCityGeneratedRootRole role)
        {
            DenseCityGeneratedRootAuthoring[] roots = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                .Where(root => root.Role == role)
                .ToArray();
            if (roots.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Dense-city candidate requires exactly one {role} root.");
            }
            return roots[0];
        }

        private static RuntimeCityRAndDMapView RequireMapView(Scene scene)
        {
            RuntimeCityRAndDMapView[] views = scene.GetRootGameObjects()
                .SelectMany(root =>
                    root.GetComponentsInChildren<RuntimeCityRAndDMapView>(true))
                .ToArray();
            if (views.Length != 1)
            {
                throw new InvalidOperationException(
                    "Dense-city map candidate requires exactly one runtime city map view.");
            }
            return views[0];
        }

        private static void RequireEmptyCandidateOwnership(
            DenseCityGeneratedRootAuthoring mapRoot,
            DenseCityGeneratedRootAuthoring entityRoot)
        {
            if (mapRoot.GetComponentsInChildren<MeshFilter>(true).Length != 0 ||
                entityRoot.GetComponentsInChildren<Renderer>(true).Length != 0 ||
                entityRoot.GetComponentsInChildren<OperationMapBuildingAuthoring>(true).Length != 0)
            {
                throw new InvalidOperationException(
                    "Dense-city candidate ownership is already realized.");
            }
        }

        private static void ClearTemporaryLegacyVisuals(Transform generatedRoot)
        {
            if (generatedRoot == null)
                throw new InvalidOperationException("Dense-city temporary generated root is missing.");
            for (int index = generatedRoot.childCount - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(generatedRoot.GetChild(index).gameObject);
        }

        private static string CreateBackup(string assetPath)
        {
            string backup = Path.GetTempFileName();
            File.Copy(ToPhysicalPath(assetPath), backup, true);
            return backup;
        }

        private static void RestoreBackup(string backup, string assetPath)
        {
            if (!string.IsNullOrEmpty(backup) && File.Exists(backup))
                File.Copy(backup, ToPhysicalPath(assetPath), true);
        }

        private static void DeleteBackup(string backup)
        {
            if (!string.IsNullOrEmpty(backup) && File.Exists(backup))
                File.Delete(backup);
        }

        private static void CloseScene(ref Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                EditorSceneManager.CloseScene(scene, true);
            scene = default;
        }

        private static void RestoreActiveScene(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.SetActiveScene(scene);
        }

        private static void RestoreSceneSetup(SceneSetup[] setup)
        {
            if (setup != null && setup.Any(entry => entry.isLoaded))
                EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        private static int ApplyCandidateMaterialCompatibility(
            Scene candidate,
            out int syntyMaterialSlotCount)
        {
            Material legacy =
                AssetDatabase.LoadAssetAtPath<Material>(LegacySkyMaterialPath);
            if (legacy == null)
            {
                throw new InvalidOperationException(
                    $"Dense-city legacy sky material is missing: '{LegacySkyMaterialPath}'.");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                throw new InvalidOperationException("URP Unlit shader is unavailable.");
            EnsureAssetFolder(
                Path.GetDirectoryName(CandidateSkyMaterialPath)?.Replace('\\', '/'));
            Material compatible =
                AssetDatabase.LoadAssetAtPath<Material>(CandidateSkyMaterialPath);
            if (compatible == null)
            {
                compatible = new Material(shader)
                {
                    name = "DenseCity_SkyBox_DOTS"
                };
                AssetDatabase.CreateAsset(compatible, CandidateSkyMaterialPath);
            }
            else
            {
                compatible.shader = shader;
            }

            compatible.SetTexture("_BaseMap", legacy.GetTexture("_MainTex"));
            compatible.SetColor(
                "_BaseColor",
                legacy.HasProperty("_Color") ? legacy.GetColor("_Color") : Color.white);
            compatible.enableInstancing = true;
            compatible.renderQueue = legacy.renderQueue;
            EditorUtility.SetDirty(compatible);

            int compatibleRendererCount = 0;
            syntyMaterialSlotCount = 0;
            var syntyMaterialCopies = new Dictionary<Material, Material>();
            Renderer[] renderers = candidate.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
                .ToArray();
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                bool changed = false;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material == legacy || material == compatible)
                    {
                        compatibleRendererCount++;
                        if (material == legacy)
                        {
                            materials[materialIndex] = compatible;
                            changed = true;
                        }
                        continue;
                    }

                    if (material == null ||
                        material.shader == null ||
                        !string.Equals(
                            material.shader.name,
                            SyntyGenericBasicShaderName,
                            StringComparison.Ordinal))
                        continue;

                    if (!syntyMaterialCopies.TryGetValue(material, out Material converted))
                    {
                        converted = CreateOrUpdateSyntyCompatibleMaterial(material);
                        syntyMaterialCopies.Add(material, converted);
                    }
                    materials[materialIndex] = converted;
                    syntyMaterialSlotCount++;
                    changed = true;
                }
                if (changed)
                    renderers[rendererIndex].sharedMaterials = materials;
            }
            return compatibleRendererCount;
        }

        private static Material CreateOrUpdateSyntyCompatibleMaterial(Material source)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(sourceGuid))
            {
                throw new InvalidOperationException(
                    $"Synty candidate material '{source.name}' is not a persistent asset.");
            }
            if (source.HasProperty("_ZTest") &&
                !Mathf.Approximately(source.GetFloat("_ZTest"), 4f))
            {
                throw new InvalidOperationException(
                    $"Synty candidate material '{sourcePath}' uses unsupported _ZTest " +
                    $"{source.GetFloat("_ZTest")}.");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                throw new InvalidOperationException("URP Lit shader is unavailable.");
            EnsureAssetFolder(CandidateSharedMaterialFolder);
            string destinationPath =
                $"{CandidateSharedMaterialFolder}/Synty_Generic_Basic_{sourceGuid}.mat";
            Material destination =
                AssetDatabase.LoadAssetAtPath<Material>(destinationPath);
            if (destination == null)
            {
                destination = new Material(shader);
                AssetDatabase.CreateAsset(destination, destinationPath);
            }
            else
            {
                destination.shader = shader;
            }

            destination.name =
                $"DenseCity_DOTS_{Path.GetFileNameWithoutExtension(sourcePath)}";
            CopyTexture(source, "_Albedo_Map", destination, "_BaseMap");
            CopyColor(source, "_BaseColor", destination, "_BaseColor");
            CopyTexture(source, "_Normal_Map", destination, "_BumpMap");
            CopyFloat(source, "_Normal_Amount", destination, "_BumpScale");
            CopyFloat(source, "_Metallic", destination, "_Metallic");
            CopyFloat(source, "_Smoothness", destination, "_Smoothness");
            CopyTexture(source, "_Emission_Map", destination, "_EmissionMap");
            CopyColor(source, "_Emission_Color", destination, "_EmissionColor");
            CopyFloat(source, "_Alpha_Clip_Threshold", destination, "_Cutoff");
            CopyFloat(source, "_AlphaClip", destination, "_AlphaClip");
            CopyFloat(source, "_Surface", destination, "_Surface");
            CopyFloat(source, "_Blend", destination, "_Blend");
            CopyFloat(source, "_Cull", destination, "_Cull");
            CopyFloat(source, "_ReceiveShadows", destination, "_ReceiveShadows");
            CopyFloat(
                source,
                "_BlendModePreserveSpecular",
                destination,
                "_BlendModePreserveSpecular");
            CopyFloat(source, "_ZWrite", destination, "_ZWrite");
            CopyFloat(source, "_AlphaToMask", destination, "_AlphaToMask");
            if (destination.HasProperty("_WorkflowMode"))
                destination.SetFloat("_WorkflowMode", 1f);

            BaseShaderGUI.SetMaterialKeywords(destination, LitGUI.SetMaterialKeywords);
            destination.enableInstancing = true;
            destination.renderQueue = source.renderQueue;
            destination.SetOverrideTag(
                "RenderType",
                source.GetTag("RenderType", false, string.Empty));
            destination.doubleSidedGI = source.doubleSidedGI;
            destination.globalIlluminationFlags = source.globalIlluminationFlags;
            CopyShaderPassState(source, destination, "ShadowCaster");
            CopyShaderPassState(source, destination, "DepthOnly");
            CopyShaderPassState(source, destination, "MotionVectors");
            EditorUtility.SetDirty(destination);
            return destination;
        }

        private static void CopyTexture(
            Material source,
            string sourceProperty,
            Material destination,
            string destinationProperty)
        {
            if (!source.HasProperty(sourceProperty) ||
                !destination.HasProperty(destinationProperty))
                return;
            destination.SetTexture(
                destinationProperty,
                source.GetTexture(sourceProperty));
            destination.SetTextureScale(
                destinationProperty,
                source.GetTextureScale(sourceProperty));
            destination.SetTextureOffset(
                destinationProperty,
                source.GetTextureOffset(sourceProperty));
        }

        private static void CopyColor(
            Material source,
            string sourceProperty,
            Material destination,
            string destinationProperty)
        {
            if (source.HasProperty(sourceProperty) &&
                destination.HasProperty(destinationProperty))
                destination.SetColor(destinationProperty, source.GetColor(sourceProperty));
        }

        private static void CopyFloat(
            Material source,
            string sourceProperty,
            Material destination,
            string destinationProperty)
        {
            if (source.HasProperty(sourceProperty) &&
                destination.HasProperty(destinationProperty))
                destination.SetFloat(destinationProperty, source.GetFloat(sourceProperty));
        }

        private static void CopyShaderPassState(
            Material source,
            Material destination,
            string passName)
        {
            destination.SetShaderPassEnabled(
                passName,
                source.GetShaderPassEnabled(passName));
        }

        private static void RequireProtectedSourceHashes(
            string expectedMapHash,
            string expectedEntityHash)
        {
            if (!string.Equals(
                    expectedMapHash,
                    ComputeFileHash(SourceMapScenePath),
                    StringComparison.Ordinal) ||
                !string.Equals(
                    expectedEntityHash,
                    ComputeFileHash(SourceEntityScenePath),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Dense-city candidate realization changed a protected source scene.");
            }
        }

        private static void RequireIndependentGuid(string sourcePath, string candidatePath)
        {
            string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
            string candidateGuid = AssetDatabase.AssetPathToGUID(candidatePath);
            if (string.IsNullOrEmpty(sourceGuid) ||
                string.IsNullOrEmpty(candidateGuid) ||
                string.Equals(sourceGuid, candidateGuid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Dense-city candidate did not receive an independent GUID: '{candidatePath}'.");
            }
        }

        private static string ComputeFileHash(string assetPath)
        {
            string physicalPath = ToPhysicalPath(assetPath);
            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("Dense-city transaction input is missing.", assetPath);
            using SHA256 sha = SHA256.Create();
            using FileStream stream = File.OpenRead(physicalPath);
            return ToLowerHex(sha.ComputeHash(stream));
        }

        private static string ToLowerHex(byte[] bytes) =>
            BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();

        private static string ToPhysicalPath(string assetPath) =>
            Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty,
                assetPath));

        private static void EnsureAssetFolder(string path)
        {
            if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            string name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                throw new InvalidOperationException($"Invalid asset folder path: '{path}'.");
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
