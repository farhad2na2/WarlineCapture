using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
using Game.Composition;
using Game.Configs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class OperationMapEntityPresentationReadinessValidator
    {
        [MenuItem("Game/Operation Maps/EntityScene Migration/Validate Entity Presentation Readiness")]
        public static void ValidateCurrentCandidate() => ValidateCurrentCandidateCore();

        public static void ValidateCurrentCandidateBatch() => ValidateCurrentCandidateCore();

        internal static bool TryValidateScene(
            Scene scene,
            string expectedOperationMapId,
            int expectedBuildings,
            int expectedVehicles,
            int expectedRenderOnly,
            out string error)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                error = "Entity-presentation candidate scene must be valid and loaded.";
                return false;
            }
            if (!OperationMapIdentityRules.IsValidOperationMapId(expectedOperationMapId))
            {
                error = "Expected operation-map id is invalid.";
                return false;
            }
            if (expectedBuildings < 0 || expectedVehicles < 0 || expectedRenderOnly < 0)
            {
                error = "Expected entity-presentation counts cannot be negative.";
                return false;
            }

            OperationMapEntityPresentationRootAuthoring[] roots = FindInScene<OperationMapEntityPresentationRootAuthoring>(scene);
            if (roots.Length != 3)
            {
                error = $"Entity-presentation candidate requires exactly three role roots; found {roots.Length}.";
                return false;
            }

            var roles = new HashSet<OperationMapEntityPresentationRole>();
            string migrationHash = null;
            foreach (OperationMapEntityPresentationRootAuthoring root in roots)
            {
                if (!root.TryValidate(out error))
                    return false;
                if (!string.Equals(root.OperationMapId, expectedOperationMapId, StringComparison.Ordinal))
                {
                    error = $"Entity-presentation root '{root.name}' belongs to a different operation map.";
                    return false;
                }
                if (!roles.Add(root.Role))
                {
                    error = $"Duplicate entity-presentation role root: {root.Role}.";
                    return false;
                }
                if (root.GetComponentsInParent<OperationMapEntityPresentationRootAuthoring>(true).Length != 1)
                {
                    error = $"Entity-presentation role root '{root.name}' cannot be nested beneath another role root.";
                    return false;
                }
                if (migrationHash == null)
                    migrationHash = root.MigrationRecordSetHash;
                else if (!string.Equals(migrationHash, root.MigrationRecordSetHash, StringComparison.Ordinal))
                {
                    error = "Entity-presentation role roots do not share one migration record-set hash.";
                    return false;
                }
            }

            if (!roles.SetEquals(new[]
                {
                    OperationMapEntityPresentationRole.GameplayBuildings,
                    OperationMapEntityPresentationRole.GameplayVehicles,
                    OperationMapEntityPresentationRole.RenderOnly
                }))
            {
                error = "Entity-presentation candidate role set is incomplete.";
                return false;
            }

            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                if (!DenseCityPhysicsComponentStripper.TryValidateNoProhibitedComponents(sceneRoot, out error))
                    return false;
            }

            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            var placementKeys = new HashSet<string>(StringComparer.Ordinal);
            int buildings = 0;
            int vehicles = 0;
            int renderOnly = 0;
            OperationMapEntityPresentationIdentityAuthoring[] identities =
                FindInScene<OperationMapEntityPresentationIdentityAuthoring>(scene);
            foreach (OperationMapEntityPresentationIdentityAuthoring identity in identities)
            {
                if (!identity.TryValidate(out error))
                    return false;
                if (!string.Equals(identity.OperationMapId, expectedOperationMapId, StringComparison.Ordinal))
                {
                    error = $"Entity-presentation identity '{identity.name}' belongs to a different operation map.";
                    return false;
                }
                if (!sourceIds.Add(identity.SourceGlobalObjectId))
                {
                    error = $"Duplicate entity-presentation source identity: '{identity.SourceGlobalObjectId}'.";
                    return false;
                }

                OperationMapEntityPresentationRootAuthoring owner =
                    identity.GetComponentInParent<OperationMapEntityPresentationRootAuthoring>(true);
                if (owner == null || owner.Role != identity.Role)
                {
                    error = $"Entity-presentation identity '{identity.name}' does not match its nearest role owner.";
                    return false;
                }

                switch (identity.Role)
                {
                    case OperationMapEntityPresentationRole.GameplayBuildings:
                        buildings++;
                        if (!placementKeys.Add($"building:{identity.PlacementIndex}"))
                        {
                            error = $"Duplicate gameplay-building placement index: {identity.PlacementIndex}.";
                            return false;
                        }
                        break;
                    case OperationMapEntityPresentationRole.GameplayVehicles:
                        vehicles++;
                        if (!placementKeys.Add($"vehicle:{identity.PlacementIndex}"))
                        {
                            error = $"Duplicate gameplay-vehicle placement index: {identity.PlacementIndex}.";
                            return false;
                        }
                        break;
                    case OperationMapEntityPresentationRole.RenderOnly:
                        renderOnly++;
                        break;
                }
            }

            if (buildings != expectedBuildings || vehicles != expectedVehicles || renderOnly != expectedRenderOnly)
            {
                error =
                    $"Entity-presentation identity counts differ from the accepted migration: " +
                    $"buildings={buildings}/{expectedBuildings} vehicles={vehicles}/{expectedVehicles} " +
                    $"renderOnly={renderOnly}/{expectedRenderOnly}.";
                return false;
            }

            error = null;
            return true;
        }

        internal static bool TryValidateLegacyPlacementParity(
            Scene candidateScene,
            MapBuildingPlacementConfig buildingConfig,
            IReadOnlyList<OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildingPlacementReport> buildingRows,
            MapVehiclePlacementConfig vehicleConfig,
            IReadOnlyList<OperationMapVehicleEcsConversionInventoryProbe.PlacementConversionReport> vehicleRows,
            out string error)
        {
            if (buildingConfig == null || buildingRows == null ||
                vehicleConfig == null || vehicleRows == null ||
                buildingConfig.Placements.Count != buildingRows.Count ||
                vehicleConfig.Placements.Count != vehicleRows.Count)
            {
                error = "Legacy placement evidence is missing or count-mismatched.";
                return false;
            }

            OperationMapEntityPresentationIdentityAuthoring[] identities =
                FindInScene<OperationMapEntityPresentationIdentityAuthoring>(candidateScene);
            OperationMapEntityPresentationIdentityAuthoring[] buildingIdentities = identities
                .Where(identity => identity.Role == OperationMapEntityPresentationRole.GameplayBuildings)
                .ToArray();
            OperationMapEntityPresentationIdentityAuthoring[] vehicleIdentities = identities
                .Where(identity => identity.Role == OperationMapEntityPresentationRole.GameplayVehicles)
                .ToArray();
            if (buildingIdentities.Select(identity => identity.PlacementIndex).Distinct().Count() != buildingIdentities.Length ||
                vehicleIdentities.Select(identity => identity.PlacementIndex).Distinct().Count() != vehicleIdentities.Length)
            {
                error = "Candidate ECS placement identities contain duplicate indices.";
                return false;
            }
            var buildings = buildingIdentities.ToDictionary(identity => identity.PlacementIndex);
            var vehicles = vehicleIdentities.ToDictionary(identity => identity.PlacementIndex);
            if (buildings.Count != buildingRows.Count || vehicles.Count != vehicleRows.Count)
            {
                error = "Legacy placement counts do not match candidate ECS identity counts.";
                return false;
            }

            for (int index = 0; index < buildingRows.Count; index++)
            {
                MapBuildingPlacementConfigEntry placement = buildingConfig.Placements[index];
                OperationMapBuildingAttachmentOwnershipInventoryProbe.BuildingPlacementReport row =
                    buildingRows[index];
                if (placement == null || row == null || row.placementIndex != index ||
                    !string.Equals(row.authoredJoinResolveState, "Exact", StringComparison.Ordinal) ||
                    !string.Equals(placement.SourcePath, row.sourcePath, StringComparison.Ordinal) ||
                    placement.BuildingPrefab == null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(placement.BuildingPrefab),
                        row.buildingPrefabPath,
                        StringComparison.Ordinal) ||
                    !buildings.TryGetValue(index, out OperationMapEntityPresentationIdentityAuthoring identity) ||
                    !string.Equals(identity.SourceGlobalObjectId, row.ownerSourceGlobalObjectId, StringComparison.Ordinal))
                {
                    error = $"Legacy building placement {index} does not map one-to-one to its candidate identity.";
                    return false;
                }

                OperationMapBuildingAuthoring building =
                    identity.GetComponentInParent<OperationMapBuildingAuthoring>(true);
                if (building == null || building.PlacementIndex != index ||
                    !string.Equals(building.OperationMapId, identity.OperationMapId, StringComparison.Ordinal) ||
                    !string.Equals(building.SourceGlobalObjectId, identity.SourceGlobalObjectId, StringComparison.Ordinal))
                {
                    error = $"Legacy building placement {index} has no matching ECS building authoring owner.";
                    return false;
                }
            }

            for (int index = 0; index < vehicleRows.Count; index++)
            {
                MapVehiclePlacementConfigEntry placement = vehicleConfig.Placements[index];
                OperationMapVehicleEcsConversionInventoryProbe.PlacementConversionReport row = vehicleRows[index];
                if (placement == null || row == null || row.placementIndex != index ||
                    !string.Equals(row.authoredJoinResolveState, "Exact", StringComparison.Ordinal) ||
                    !string.Equals(row.conversionDisposition, "AlreadyProducesEcsGameplayAndRender", StringComparison.Ordinal) ||
                    !string.Equals(placement.SourcePath, row.sourcePath, StringComparison.Ordinal) ||
                    placement.VehiclePrefab == null ||
                    !string.Equals(
                        AssetDatabase.GetAssetPath(placement.VehiclePrefab),
                        row.vehiclePrefabPath,
                        StringComparison.Ordinal) ||
                    !vehicles.TryGetValue(index, out OperationMapEntityPresentationIdentityAuthoring identity) ||
                    !string.Equals(identity.SourceGlobalObjectId, row.authoredSourceGlobalObjectId, StringComparison.Ordinal) ||
                    identity.GetComponentsInParent<UnitGridAuthoring>(true).Length != 1)
                {
                    string components = vehicles.TryGetValue(index, out OperationMapEntityPresentationIdentityAuthoring owner)
                        ? string.Join(",", owner.GetComponentsInChildren<MonoBehaviour>(true)
                            .Where(component => component != null)
                            .Select(component => component.GetType().Name)
                            .Distinct())
                        : "<missing-identity>";
                    error =
                        $"Legacy vehicle placement {index} does not map one-to-one to an ECS unit Baker owner; " +
                        $"candidateComponents='{components}'.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal static bool TryValidateCandidatePlacementRetirement(
            OperationMapDefinition definition,
            OperationMapSceneView runtimeView,
            OperationMapEntitySceneCandidateAddressablesLayoutPlan layout,
            out string error)
        {
            if (definition == null || definition.PresentationKind != OperationMapPresentationKind.EntityScene ||
                HasAssetGuid(definition.BuildingPlacementsReference) ||
                HasAssetGuid(definition.VehiclePlacementsReference))
            {
                error = "Candidate EntityScene definition still references legacy placement content.";
                return false;
            }
            if (runtimeView == null || runtimeView.BuildingPlacements != null || runtimeView.VehiclePlacements != null)
            {
                error = "Candidate runtime binding still exposes legacy placement spawning inputs.";
                return false;
            }
            if (layout.Entries == null || layout.Entries.Any(entry =>
                    string.Equals(entry.Role, "building-placements", StringComparison.Ordinal) ||
                    string.Equals(entry.Role, "vehicle-placements", StringComparison.Ordinal) ||
                    string.Equals(entry.AssetPath, OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath, StringComparison.Ordinal) ||
                    string.Equals(entry.AssetPath, OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath, StringComparison.Ordinal)))
            {
                error = "Candidate Addressables layout still owns legacy placement content.";
                return false;
            }

            error = null;
            return true;
        }

        private static void ValidateCurrentCandidateCore()
        {
            string candidatePath = OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string physicalPath = Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? throw new InvalidOperationException("Project root is unavailable."),
                candidatePath);
            if (!File.Exists(physicalPath))
                throw new FileNotFoundException("Protected candidate SubScene has not been created.", physicalPath);

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                Scene candidate = EditorSceneManager.OpenScene(candidatePath, OpenSceneMode.Single);
                if (!TryValidateScene(
                        candidate,
                        OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                        OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayBuildings,
                        OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayVehicles,
                        OperationMapEntityPresentationCandidateBakeValidator.ExpectedRenderOnlyOwners,
                        out string error))
                {
                    throw new InvalidOperationException(error);
                }

                string projectRoot = Path.GetDirectoryName(Application.dataPath) ??
                                     throw new InvalidOperationException("Project root is unavailable.");
                OperationMapBuildingAttachmentOwnershipInventoryProbe.AttachmentOwnershipInventoryReport
                    buildingInventory = OperationMapBuildingCandidateMigrationEditor.LoadInventory(projectRoot);
                OperationMapVehicleEcsConversionInventoryProbe.ConversionReport vehicleInventory =
                    OperationMapVehicleCandidateMigrationEditor.LoadInventory(projectRoot);
                MapBuildingPlacementConfig buildingConfig =
                    AssetDatabase.LoadAssetAtPath<MapBuildingPlacementConfig>(
                        buildingInventory.buildingPlacementConfigPath);
                MapVehiclePlacementConfig vehicleConfig =
                    AssetDatabase.LoadAssetAtPath<MapVehiclePlacementConfig>(
                        vehicleInventory.vehiclePlacementConfigPath);
                if (!TryValidateLegacyPlacementParity(
                        candidate,
                        buildingConfig,
                        buildingInventory.placements,
                        vehicleConfig,
                        vehicleInventory.placements,
                        out error))
                {
                    throw new InvalidOperationException(error);
                }

                OperationMapDefinition definition = AssetDatabase.LoadAssetAtPath<OperationMapDefinition>(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateDefinitionPath);
                if (!OperationMapEntitySceneCandidateAddressablesLayoutPlanner.TryCreatePlan(
                        out OperationMapEntitySceneCandidateAddressablesLayoutPlan layout,
                        out string layoutError))
                {
                    throw new InvalidOperationException($"Candidate layout rejected: {layoutError}");
                }
                Scene runtimeScene = EditorSceneManager.OpenScene(
                    OperationMapEntitySceneCandidateAddressablesLayoutPlanner.CandidateRuntimeBindingPath,
                    OpenSceneMode.Additive);
                OperationMapSceneView[] runtimeViews = FindInScene<OperationMapSceneView>(runtimeScene);
                if (runtimeViews.Length != 1 ||
                    !TryValidateCandidatePlacementRetirement(
                        definition,
                        runtimeViews.SingleOrDefault(),
                        layout,
                        out error))
                {
                    throw new InvalidOperationException(
                        runtimeViews.Length != 1
                            ? $"Candidate runtime binding requires one operation-map view; found {runtimeViews.Length}."
                            : error);
                }

                Debug.Log(
                    "[OperationMapEntityPresentationReadiness] result=Passed " +
                    $"buildings={OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayBuildings} " +
                    $"vehicles={OperationMapEntityPresentationCandidateBakeValidator.ExpectedGameplayVehicles} " +
                    $"renderOnly={OperationMapEntityPresentationCandidateBakeValidator.ExpectedRenderOnlyOwners}");
            }
            finally
            {
                if (previousSetup.Any(entry => entry.isLoaded && entry.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static T[] FindInScene<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();

        private static bool HasAssetGuid(UnityEngine.AddressableAssets.AssetReference reference) =>
            reference != null && !string.IsNullOrEmpty(reference.AssetGUID);
    }
}
