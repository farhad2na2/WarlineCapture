using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.Authoring;
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
    }
}
