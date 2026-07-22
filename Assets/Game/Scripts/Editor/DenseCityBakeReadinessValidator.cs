using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Configs;
using Game.Rendering;
using Game.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class DenseCityBakeReadinessValidator
    {
        internal readonly struct ProtectedRootContract
        {
            internal ProtectedRootContract(
                string globalObjectId,
                string name,
                string hierarchyPath,
                bool activeSelf,
                float overlapMargin)
            {
                GlobalObjectId = globalObjectId;
                Name = name;
                HierarchyPath = hierarchyPath;
                ActiveSelf = activeSelf;
                OverlapMargin = overlapMargin;
            }

            internal string GlobalObjectId { get; }
            internal string Name { get; }
            internal string HierarchyPath { get; }
            internal bool ActiveSelf { get; }
            internal float OverlapMargin { get; }
        }

        private static readonly ProtectedRootContract[] ApprovedProtectedRoots =
        {
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-902583272-0",
                "DenseCity_GradingArchive",
                "DenseCity_GradingArchive[13]",
                false,
                0f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-1836082762-0",
                "Mountains",
                "DenseCity_GradingArchive[13]/Mountains[1]",
                true,
                0f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2444809882377260586-0",
                "Buildings",
                "Map[5]/Buildings[18]",
                true,
                8f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-9027429371825681282-0",
                "Mountains",
                "Map[5]/Mountains[4]",
                true,
                14f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-2752690170442537164-0",
                "ResourceAreas",
                "Map[5]/ResourceAreas[25]",
                true,
                10f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-8740025226467099862-0",
                "Roads",
                "Map[5]/Roads[16]",
                true,
                5f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-7060769374196877377-0",
                "Runways",
                "Map[5]/Runways[24]",
                true,
                10f),
            new(
                "GlobalObjectId_V1-2-ca1f2d7f265d8495f8c815441d68fda0-5294699240646147300-0",
                "Vehicles",
                "Map[5]/Vehicles[20]",
                true,
                4f)
        };

        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Dense City Bake Readiness")]
        public static void ValidateCurrentCandidate() => ValidateCurrentCandidateCore();

        public static void ValidateCurrentCandidateBatch() => ValidateCurrentCandidateCore();

        internal static bool TryResolveGenerationState(
            Scene operationMapScene,
            Scene entityPresentationScene,
            out bool generated,
            out string generationId,
            out string error)
        {
            generated = false;
            generationId = null;
            DenseCityGeneratedRootAuthoring[] mapRoots = FindGeneratedRoots(operationMapScene);
            DenseCityGeneratedRootAuthoring[] entityRoots = FindGeneratedRoots(entityPresentationScene);
            if (mapRoots.Length == 0 && entityRoots.Length == 0)
            {
                error = null;
                return true;
            }
            if (mapRoots.Length != 1 || entityRoots.Length != 1)
            {
                error =
                    $"Dense-city generation ownership is partial or duplicated: " +
                    $"mapRoots={mapRoots.Length} entityRoots={entityRoots.Length}.";
                return false;
            }
            if (mapRoots[0].Role != DenseCityGeneratedRootRole.MapBakeSource ||
                entityRoots[0].Role != DenseCityGeneratedRootRole.EntityPresentationSource)
            {
                error = "Dense-city generated roots have incorrect scene roles.";
                return false;
            }
            if (!string.Equals(mapRoots[0].GenerationId, entityRoots[0].GenerationId, StringComparison.Ordinal))
            {
                error = "Dense-city generated roots do not share one generation id.";
                return false;
            }

            generated = true;
            generationId = mapRoots[0].GenerationId;
            error = null;
            return true;
        }

        public static bool TryValidateAuthoringOwnership(
            Scene operationMapScene,
            Scene entityPresentationScene,
            string expectedOperationMapId,
            string expectedGenerationId,
            out string error)
        {
            if (!OperationMapIdentityRules.IsValidOperationMapId(expectedOperationMapId))
            {
                error = "Expected operation-map id is invalid.";
                return false;
            }
            if (!DenseCitySemanticHierarchyBuilder.TryValidate(
                    operationMapScene,
                    entityPresentationScene,
                    expectedGenerationId,
                    out error))
            {
                return false;
            }

            DenseCityGeneratedRootAuthoring mapGeneratedRoot = FindGeneratedRoots(operationMapScene)
                .Single(root => root.Role == DenseCityGeneratedRootRole.MapBakeSource);
            DenseCityGeneratedRootAuthoring entityGeneratedRoot = FindGeneratedRoots(entityPresentationScene)
                .Single(root => root.Role == DenseCityGeneratedRootRole.EntityPresentationSource);
            if (IsAcceptedOperationMapScene(operationMapScene) &&
                !TryValidateApprovedProtectedContent(
                    operationMapScene,
                    entityGeneratedRoot,
                    out error))
            {
                return false;
            }
            if (!TryValidateGeneratedRendererOwnership(
                    mapGeneratedRoot,
                    entityGeneratedRoot,
                    out error))
            {
                return false;
            }
            if (!TryValidateProxyOwnership(mapGeneratedRoot, out error))
                return false;
            if (!DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    mapGeneratedRoot.gameObject,
                    out error) ||
                !DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(
                    entityGeneratedRoot.gameObject,
                    out error))
            {
                return false;
            }

            DenseCityAuthoredOverrideAuthoring[] mapOverrides = FindOverrides(operationMapScene);
            if (FindOverrides(entityPresentationScene).Length != 0)
            {
                error = "Dense-city authored overrides must remain in the operation-map authoring scene.";
                return false;
            }
            var overrideIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (DenseCityAuthoredOverrideAuthoring authoredOverride in mapOverrides)
            {
                if (!authoredOverride.TryValidate(out error))
                    return false;
                if (authoredOverride.transform == mapGeneratedRoot.transform ||
                    authoredOverride.transform.IsChildOf(mapGeneratedRoot.transform))
                {
                    error = $"Authored override '{authoredOverride.StableId}' is inside the disposable generated root.";
                    return false;
                }
                if (!overrideIds.Add(authoredOverride.StableId))
                {
                    error = $"Duplicate dense-city authored override id: '{authoredOverride.StableId}'.";
                    return false;
                }
            }

            if (FindBuildings(operationMapScene).Length != 0)
            {
                error = "Operation-map building authoring must live in the entity-presentation scene.";
                return false;
            }
            OperationMapBuildingAuthoring[] buildings = FindBuildings(entityPresentationScene);
            var stableIds = new HashSet<string>(StringComparer.Ordinal);
            var placementIds = new HashSet<string>(StringComparer.Ordinal);
            var generatedBuildings = new List<OperationMapBuildingAuthoring>();
            foreach (OperationMapBuildingAuthoring building in buildings)
            {
                if (!building.TryValidate(out error))
                    return false;
                if (!string.Equals(building.OperationMapId, expectedOperationMapId, StringComparison.Ordinal))
                {
                    error = $"Building {building.PlacementIndex} belongs to a different operation map.";
                    return false;
                }

                OperationMapEntityPresentationRootAuthoring authoredOwner =
                    building.GetComponentInParent<OperationMapEntityPresentationRootAuthoring>(true);
                DenseCityGeneratedRootAuthoring generatedOwner =
                    building.GetComponentInParent<DenseCityGeneratedRootAuthoring>(true);
                bool hasAuthoredOwner = authoredOwner != null &&
                                        authoredOwner.Role == OperationMapEntityPresentationRole.GameplayBuildings;
                bool hasGeneratedOwner = generatedOwner == entityGeneratedRoot &&
                                         generatedOwner.Role == DenseCityGeneratedRootRole.EntityPresentationSource;
                if (hasAuthoredOwner == hasGeneratedOwner)
                {
                    error =
                        $"Building {building.PlacementIndex} must have exactly one approved entity-presentation owner.";
                    return false;
                }
                if (hasGeneratedOwner)
                    generatedBuildings.Add(building);
                if (!stableIds.Add(building.StableId))
                {
                    error = $"Duplicate operation-map building stable id: '{building.StableId}'.";
                    return false;
                }
                string placementKey = $"{building.OperationMapId}:{building.PlacementIndex}";
                if (!placementIds.Add(placementKey))
                {
                    error = $"Duplicate operation-map building placement: '{placementKey}'.";
                    return false;
                }
            }
            foreach (OperationMapBuildingAuthoring generatedBuilding in generatedBuildings)
            {
                if (!TryValidateGeneratedBuilding(generatedBuilding, out error))
                    return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateGeneratedBuilding(
            OperationMapBuildingAuthoring building,
            out string error)
        {
            if (!OperationMapIdentityRules.IsValidGeneratedStableId(building.StableId) ||
                building.FootprintCells.x <= 0 || building.FootprintCells.y <= 0 ||
                building.MaxHealth <= 0)
            {
                error = $"Generated building {building.PlacementIndex} has incomplete ECS gameplay data.";
                return false;
            }
            if (building.IntactVisualRoot == null || building.DestroyedVisualRoot == null)
            {
                error =
                    $"Generated building {building.PlacementIndex} requires one intact and one destroyed visual root.";
                return false;
            }
            if (building.GetComponentsInChildren<RuntimeBuildingEntityLink>(true).Length != 0)
            {
                error =
                    $"Generated building {building.PlacementIndex} contains a managed RuntimeBuildingEntityLink.";
                return false;
            }
            if (!TryValidateSharedRenderAssets(
                    building.IntactVisualRoot,
                    building.PlacementIndex,
                    "intact",
                    out error) ||
                !TryValidateSharedRenderAssets(
                    building.DestroyedVisualRoot,
                    building.PlacementIndex,
                    "destroyed",
                    out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateSharedRenderAssets(
            GameObject visualRoot,
            int placementIndex,
            string state,
            out string error)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                error = $"Generated building {placementIndex} {state} visual root has no renderer.";
                return false;
            }

            foreach (Renderer renderer in renderers)
            {
                Mesh mesh = renderer switch
                {
                    MeshRenderer => renderer.GetComponent<MeshFilter>()?.sharedMesh,
                    SkinnedMeshRenderer skinned => skinned.sharedMesh,
                    _ => null
                };
                if (!TryGetPersistentAssetIdentity(mesh, out _, out _))
                {
                    error =
                        $"Generated building {placementIndex} {state} renderer '{renderer.name}' " +
                        "does not use a persistent shared mesh asset.";
                    return false;
                }

                Material[] materials = renderer.sharedMaterials;
                if (materials.Length == 0 || materials.Any(material =>
                        !TryGetPersistentAssetIdentity(material, out _, out _)))
                {
                    error =
                        $"Generated building {placementIndex} {state} renderer '{renderer.name}' " +
                        "does not use persistent shared material assets.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryGetPersistentAssetIdentity(
            UnityEngine.Object asset,
            out string guid,
            out long localId)
        {
            guid = null;
            localId = 0;
            return asset != null &&
                   AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out guid, out localId) &&
                   !string.IsNullOrEmpty(guid) && localId > 0;
        }

        private static bool TryValidateProxyOwnership(
            DenseCityGeneratedRootAuthoring mapGeneratedRoot,
            out string error)
        {
            MeshFilter[] proxies = mapGeneratedRoot.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter proxy in proxies)
            {
                MapBakeGroupAuthoring owner = proxy.GetComponent<MapBakeGroupAuthoring>();
                if (owner == null ||
                    proxy.GetComponents<MapBakeGroupAuthoring>().Length != 1 ||
                    proxy.GetComponentInParent<MapBakeGroupAuthoring>(true) != owner)
                {
                    error =
                        $"Dense-city surface proxy requires exactly one nearest bake-group owner: " +
                        $"'{GetHierarchyPath(proxy.transform, mapGeneratedRoot.transform)}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateApprovedProtectedContent(
            Scene operationMapScene,
            DenseCityGeneratedRootAuthoring entityGeneratedRoot,
            out string error)
        {
            if (!TryValidateProtectedRootContracts(
                    operationMapScene,
                    ApprovedProtectedRoots,
                    out error))
            {
                return false;
            }

            StaticMapPresentationManifest manifest =
                AssetDatabase.LoadAssetAtPath<StaticMapPresentationManifest>(
                    OperationMapAddressablesLayoutBuilder.ManifestPath);
            if (manifest == null ||
                !string.Equals(
                    manifest.CanonicalScenePath,
                    OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    manifest.OperationMapId,
                    OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                    StringComparison.Ordinal))
            {
                error = "Accepted protected-content source manifest is missing or has the wrong identity.";
                return false;
            }

            var protectedBounds = new ProtectedBoundsIndex(32f);
            int protectedSourceCount = 0;
            foreach (StaticMapPresentationSourceEntry source in manifest.Sources)
            {
                if (!TryFindProtectedContract(source.SourceHierarchyPath, out ProtectedRootContract contract))
                    continue;
                protectedSourceCount++;
                if (!TryValidateProtectedManifestSource(
                        operationMapScene,
                        source,
                        contract,
                        out Bounds sourceBounds,
                        out error))
                {
                    return false;
                }
                if (contract.OverlapMargin > 0f)
                    protectedBounds.Add(sourceBounds, contract.OverlapMargin, source.SourceHierarchyPath);
            }
            if (protectedSourceCount == 0)
            {
                error = "Accepted source manifest contains no protected authored presentation sources.";
                return false;
            }

            if (entityGeneratedRoot != null)
            {
                Renderer[] generatedRenderers =
                    entityGeneratedRoot.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer generated in generatedRenderers)
                {
                    if (generated == null || !generated.enabled || !generated.gameObject.activeInHierarchy ||
                        IsSubgradeCanalUnderpassRenderer(generated))
                    {
                        continue;
                    }
                    if (protectedBounds.TryFindOverlap(generated.bounds, out string protectedPath))
                    {
                        error =
                            $"Dense-city generated renderer overlaps protected authored content: " +
                            $"generated='{GetHierarchyPath(generated.transform, entityGeneratedRoot.transform)}' " +
                            $"protected='{protectedPath}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

        internal static bool TryValidateProtectedRootContracts(
            Scene operationMapScene,
            IReadOnlyList<ProtectedRootContract> contracts,
            out string error)
        {
            if (!operationMapScene.IsValid() || !operationMapScene.isLoaded ||
                contracts == null || contracts.Count == 0)
            {
                error = "Protected authored-root validation requires a loaded scene and contracts.";
                return false;
            }

            var resolved = new HashSet<GameObject>();
            for (int index = 0; index < contracts.Count; index++)
            {
                ProtectedRootContract contract = contracts[index];
                if (!GlobalObjectId.TryParse(contract.GlobalObjectId, out GlobalObjectId globalId) ||
                    GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) is not GameObject owner ||
                    owner.scene != operationMapScene)
                {
                    error = $"Protected authored root is missing: '{contract.HierarchyPath}'.";
                    return false;
                }
                if (!resolved.Add(owner) ||
                    !string.Equals(owner.name, contract.Name, StringComparison.Ordinal) ||
                    !string.Equals(
                        BuildIndexedHierarchyPath(owner.transform),
                        contract.HierarchyPath,
                        StringComparison.Ordinal))
                {
                    error = $"Protected authored root was renamed, reparented, or reordered: '{contract.HierarchyPath}'.";
                    return false;
                }
                if (owner.activeSelf != contract.ActiveSelf)
                {
                    error = $"Protected authored root active state changed: '{contract.HierarchyPath}'.";
                    return false;
                }
                if (!HasIdentityLocalTransform(owner.transform))
                {
                    error = $"Protected authored root transform moved: '{contract.HierarchyPath}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool TryValidateProtectedManifestSource(
            Scene operationMapScene,
            StaticMapPresentationSourceEntry source,
            ProtectedRootContract contract,
            out Bounds sourceBounds,
            out string error)
        {
            sourceBounds = default;
            if (!GlobalObjectId.TryParse(source.SourceGlobalObjectId, out GlobalObjectId globalId) ||
                GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId) is not MeshRenderer renderer ||
                renderer.gameObject.scene != operationMapScene)
            {
                error = $"Protected authored renderer was deleted: '{source.SourceHierarchyPath}'.";
                return false;
            }
            if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
            {
                error = $"Protected authored renderer was disabled: '{source.SourceHierarchyPath}'.";
                return false;
            }
            if (!string.Equals(
                    BuildIndexedHierarchyPath(renderer.transform),
                    source.SourceHierarchyPath,
                    StringComparison.Ordinal))
            {
                error = $"Protected authored renderer was renamed or moved: '{source.SourceHierarchyPath}'.";
                return false;
            }
            if (BoundsResidual(renderer.bounds, source.WorldBounds) > 0.001f)
            {
                error = $"Protected authored renderer bounds moved: '{source.SourceHierarchyPath}'.";
                return false;
            }

            Mesh mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if (mesh == null ||
                !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string meshGuid, out long meshLocalId) ||
                !string.Equals(meshGuid, source.MeshAssetGuid, StringComparison.Ordinal) ||
                meshLocalId != source.MeshLocalId)
            {
                error = $"Protected authored renderer mesh changed: '{source.SourceHierarchyPath}'.";
                return false;
            }
            Material[] materials = renderer.sharedMaterials;
            if (materials.Length != source.Materials.Count)
            {
                error = $"Protected authored renderer materials changed: '{source.SourceHierarchyPath}'.";
                return false;
            }
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                StaticMapPresentationMaterialEntry expected = source.Materials[materialIndex];
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                        materials[materialIndex],
                        out string materialGuid,
                        out long materialLocalId) ||
                    !string.Equals(materialGuid, expected.AssetGuid, StringComparison.Ordinal) ||
                    materialLocalId != expected.LocalId)
                {
                    error = $"Protected authored renderer materials changed: '{source.SourceHierarchyPath}'.";
                    return false;
                }
            }

            sourceBounds = renderer.bounds;
            error = null;
            return true;
        }

        private static bool TryFindProtectedContract(
            string hierarchyPath,
            out ProtectedRootContract contract)
        {
            int bestLength = -1;
            contract = default;
            for (int index = 0; index < ApprovedProtectedRoots.Length; index++)
            {
                ProtectedRootContract candidate = ApprovedProtectedRoots[index];
                bool matches =
                    string.Equals(hierarchyPath, candidate.HierarchyPath, StringComparison.Ordinal) ||
                    hierarchyPath.StartsWith(candidate.HierarchyPath + "/", StringComparison.Ordinal);
                if (!matches || candidate.HierarchyPath.Length <= bestLength)
                    continue;

                contract = candidate;
                bestLength = candidate.HierarchyPath.Length;
            }
            return bestLength >= 0;
        }

        private static bool TryValidateGeneratedRendererOwnership(
            DenseCityGeneratedRootAuthoring mapGeneratedRoot,
            DenseCityGeneratedRootAuthoring entityGeneratedRoot,
            out string error)
        {
            Renderer[] proxyRenderers = mapGeneratedRoot.GetComponentsInChildren<Renderer>(true);
            if (proxyRenderers.Length != 0)
            {
                error =
                    $"Dense-city proxy hierarchy contains detailed renderer " +
                    $"'{GetHierarchyPath(proxyRenderers[0].transform, mapGeneratedRoot.transform)}'.";
                return false;
            }

            Transform entityRoot = entityGeneratedRoot.transform;
            Transform[] renderOnlyCategories =
            {
                entityRoot.Find("RenderOnly/Infrastructure"),
                entityRoot.Find("RenderOnly/Vegetation"),
                entityRoot.Find("RenderOnly/Props"),
                entityRoot.Find("RenderOnly/Horizon")
            };
            Renderer[] presentationRenderers =
                entityGeneratedRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in presentationRenderers)
            {
                OperationMapBuildingAuthoring buildingOwner =
                    renderer.GetComponentInParent<OperationMapBuildingAuthoring>(true);
                bool hasBuildingOwner = buildingOwner != null &&
                                        buildingOwner.transform.IsChildOf(entityRoot);
                bool hasRenderOnlyOwner = renderOnlyCategories.Any(category =>
                    category != null &&
                    (renderer.transform == category || renderer.transform.IsChildOf(category)));
                if (hasBuildingOwner || hasRenderOnlyOwner)
                    continue;

                error =
                    $"Dense-city generated renderer is unclassified: " +
                    $"'{GetHierarchyPath(renderer.transform, entityRoot)}'.";
                return false;
            }

            error = null;
            return true;
        }

        private static void ValidateCurrentCandidateCore()
        {
            string mapPath = OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath;
            string entityPath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                 throw new InvalidOperationException("Project root is unavailable.");
            if (!File.Exists(Path.Combine(projectRoot, mapPath)) ||
                !File.Exists(Path.Combine(projectRoot, entityPath)))
            {
                throw new FileNotFoundException("Dense-city readiness source scene pair is incomplete.");
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene mapScene = EditorSceneManager.OpenScene(mapPath, OpenSceneMode.Single);
                Scene entityScene = EditorSceneManager.OpenScene(entityPath, OpenSceneMode.Additive);
                if (!TryResolveGenerationState(
                        mapScene,
                        entityScene,
                        out bool generated,
                        out string generationId,
                        out string stateError))
                {
                    throw new InvalidOperationException(stateError);
                }
                if (!generated)
                {
                    if (!TryValidateApprovedProtectedContent(mapScene, null, out string protectedError))
                        throw new InvalidOperationException(protectedError);
                    Debug.Log("[DenseCityBakeReadiness] result=NotGenerated");
                    return;
                }
                if (!TryValidateAuthoringOwnership(
                        mapScene,
                        entityScene,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        generationId,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }

                Debug.Log($"[DenseCityBakeReadiness] result=Passed generationId={generationId}");
            }
            finally
            {
                if (previousSetup.Any(entry => entry.isLoaded && entry.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static DenseCityGeneratedRootAuthoring[] FindGeneratedRoots(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                .ToArray();

        private static DenseCityAuthoredOverrideAuthoring[] FindOverrides(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityAuthoredOverrideAuthoring>(true))
                .ToArray();

        private static OperationMapBuildingAuthoring[] FindBuildings(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<OperationMapBuildingAuthoring>(true))
                .ToArray();

        private static bool IsAcceptedOperationMapScene(Scene scene) =>
            scene.IsValid() &&
            string.Equals(
                scene.path.Replace('\\', '/'),
                OperationMapEntityPresentationCandidateSceneBuilder.AcceptedOperationMapScenePath,
                StringComparison.Ordinal);

        private static string BuildIndexedHierarchyPath(Transform owner)
        {
            var parts = new Stack<string>();
            for (Transform current = owner; current != null; current = current.parent)
                parts.Push($"{current.name}[{current.GetSiblingIndex()}]");
            return string.Join("/", parts);
        }

        private static bool HasIdentityLocalTransform(Transform owner) =>
            Vector3.SqrMagnitude(owner.localPosition) <= 0.000001f &&
            Quaternion.Angle(owner.localRotation, Quaternion.identity) <= 0.001f &&
            Vector3.SqrMagnitude(owner.localScale - Vector3.one) <= 0.000001f;

        private static float BoundsResidual(Bounds actual, Bounds expected)
        {
            Vector3 center = actual.center - expected.center;
            Vector3 size = actual.size - expected.size;
            return Mathf.Max(
                Mathf.Abs(center.x),
                Mathf.Abs(center.y),
                Mathf.Abs(center.z),
                Mathf.Abs(size.x),
                Mathf.Abs(size.y),
                Mathf.Abs(size.z));
        }

        private static bool IsSubgradeCanalUnderpassRenderer(Renderer renderer)
        {
            string parentName = renderer.transform.parent?.name;
            return renderer.name.EndsWith("_Underpass", StringComparison.Ordinal) &&
                   (string.Equals(parentName, "CanalWaterSurfaces", StringComparison.Ordinal) ||
                    string.Equals(parentName, "CanalBeds", StringComparison.Ordinal)) &&
                   renderer.bounds.max.y < -0.5f;
        }

        internal sealed class ProtectedBoundsIndex
        {
            private readonly float cellSize;
            private readonly Dictionary<long, List<ProtectedBoundsEntry>> cells = new();

            internal ProtectedBoundsIndex(float cellSize)
            {
                if (cellSize <= 0f)
                    throw new ArgumentOutOfRangeException(nameof(cellSize));
                this.cellSize = cellSize;
            }

            internal void Add(Bounds bounds, float margin, string sourcePath)
            {
                Rect footprint = CreateFootprint(bounds, Mathf.Max(0f, margin));
                GetCellRange(footprint, out int minX, out int maxX, out int minZ, out int maxZ);
                var entry = new ProtectedBoundsEntry(footprint, sourcePath);
                for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    long key = PackCell(x, z);
                    if (!cells.TryGetValue(key, out List<ProtectedBoundsEntry> entries))
                    {
                        entries = new List<ProtectedBoundsEntry>();
                        cells.Add(key, entries);
                    }
                    entries.Add(entry);
                }
            }

            internal bool TryFindOverlap(Bounds bounds, out string sourcePath)
            {
                Rect footprint = CreateFootprint(bounds, 0f);
                GetCellRange(footprint, out int minX, out int maxX, out int minZ, out int maxZ);
                var visited = new HashSet<string>(StringComparer.Ordinal);
                for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!cells.TryGetValue(PackCell(x, z), out List<ProtectedBoundsEntry> entries))
                        continue;
                    foreach (ProtectedBoundsEntry entry in entries)
                    {
                        if (!visited.Add(entry.SourcePath) || !entry.Footprint.Overlaps(footprint))
                            continue;
                        sourcePath = entry.SourcePath;
                        return true;
                    }
                }
                sourcePath = null;
                return false;
            }

            private static Rect CreateFootprint(Bounds bounds, float margin) =>
                Rect.MinMaxRect(
                    bounds.min.x - margin,
                    bounds.min.z - margin,
                    bounds.max.x + margin,
                    bounds.max.z + margin);

            private void GetCellRange(
                Rect footprint,
                out int minX,
                out int maxX,
                out int minZ,
                out int maxZ)
            {
                minX = Mathf.FloorToInt(footprint.xMin / cellSize);
                maxX = Mathf.FloorToInt(footprint.xMax / cellSize);
                minZ = Mathf.FloorToInt(footprint.yMin / cellSize);
                maxZ = Mathf.FloorToInt(footprint.yMax / cellSize);
            }

            private static long PackCell(int x, int z) => ((long)x << 32) | (uint)z;

            private readonly struct ProtectedBoundsEntry
            {
                internal ProtectedBoundsEntry(Rect footprint, string sourcePath)
                {
                    Footprint = footprint;
                    SourcePath = sourcePath;
                }

                internal Rect Footprint { get; }
                internal string SourcePath { get; }
            }
        }

        private static string GetHierarchyPath(Transform owner, Transform root)
        {
            string path = owner.name;
            Transform current = owner.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return root.name + "/" + path;
        }
    }
}
