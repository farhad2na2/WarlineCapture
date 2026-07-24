#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using Game.Configs;
    using UnityEditor;

    /// <summary>
    /// Fail-closed planner for a candidate-only EntityScene Addressables ownership layout.
    /// Does not mutate production Addressables groups, labels, or the production definition.
    /// </summary>
    internal static class OperationMapEntitySceneCandidateAddressablesLayoutPlanner
    {
        internal const string CandidatePackLabel =
            "operation-map-candidate-pack-skirmish-desert-base-01-entity-scene";
        internal const string CandidateAddressPrefix =
            "operation-map-candidate/opmap.skirmish.desert_base_01/";
        internal const string DenseCandidatePackLabel =
            "operation-map-candidate-pack-skirmish-desert-base-01-dense-city-entity-scene";
        internal const string DenseCandidateAddressPrefix =
            "operation-map-candidate/opmap.skirmish.desert_base_01/dense-city/";
        internal const string EntitySceneRoleLabel = "operation-map-role-entity-scene";

        internal const string CandidateDefinitionPath =
            "Assets/Game/Configs/OperationMaps/Candidates/" +
            "OperationMap_Compatibility_DesertBase01_EntityScene_Candidate.asset";
        internal const string CandidateRuntimeBindingPath =
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_scene_runtime.unity";
        internal const string DenseCandidateDefinitionPath =
            "Assets/Game/Configs/OperationMaps/Candidates/" +
            "OperationMap_Compatibility_DesertBase01_DenseCity_EntityScene_Candidate.asset";
        internal const string DenseCandidateRuntimeBindingPath =
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/" +
            "opmap_skirmish_desert_base_01_dense_city_entity_scene_runtime.unity";

        internal static bool TryCreatePlan(
            out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            out string rejectionReason)
        {
            plan = default;
            rejectionReason = null;

            string candidateSubScenePath =
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath;
            string candidateGuid = AssetDatabase.AssetPathToGUID(candidateSubScenePath);
            if (string.IsNullOrEmpty(candidateGuid))
            {
                rejectionReason = "candidate-entity-scene-missing";
                return false;
            }

            string acceptedSubSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            if (string.Equals(candidateGuid, acceptedSubSceneGuid, StringComparison.Ordinal))
            {
                rejectionReason = "candidate-entity-scene-guid-collides-with-accepted-subscene";
                return false;
            }

            string productionDefinitionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            string candidateDefinitionGuid = AssetDatabase.AssetPathToGUID(CandidateDefinitionPath);
            if (!string.IsNullOrEmpty(candidateDefinitionGuid) &&
                string.Equals(candidateDefinitionGuid, productionDefinitionGuid, StringComparison.Ordinal))
            {
                rejectionReason = "candidate-definition-guid-collides-with-production";
                return false;
            }

            var entries = new List<OperationMapEntitySceneCandidateAddressablesLayoutEntry>(16)
            {
                new(
                    "definition",
                    CandidateDefinitionPath,
                    CandidateAddressPrefix + "definition",
                    OperationMapAddressablesLayoutBuilder.DefinitionRoleLabel),
                new(
                    "source-scene",
                    CandidateRuntimeBindingPath,
                    CandidateAddressPrefix + "source-scene",
                    OperationMapAddressablesLayoutBuilder.SourceSceneRoleLabel),
                new(
                    "entity-scene",
                    candidateSubScenePath,
                    CandidateAddressPrefix + "entity-scene",
                    EntitySceneRoleLabel),
                new(
                    "map-surface",
                    OperationMapAddressablesLayoutBuilder.MapSurfacePath,
                    CandidateAddressPrefix + "map-surface",
                    OperationMapAddressablesLayoutBuilder.MetadataRoleLabel),
                new(
                    "minimap-raster",
                    OperationMapAddressablesLayoutBuilder.MinimapRasterPath,
                    CandidateAddressPrefix + "minimap-raster",
                    OperationMapAddressablesLayoutBuilder.MinimapRasterRoleLabel)
            };

            string[] forbiddenRuntimePaths =
            {
                OperationMapAddressablesLayoutBuilder.ManifestPath,
                OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath,
                OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath,
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OperationMapAddressablesLayoutBuilder.DefinitionPath,
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath
            };

            for (int i = 0; i < entries.Count; i++)
            {
                OperationMapEntitySceneCandidateAddressablesLayoutEntry entry = entries[i];
                for (int f = 0; f < forbiddenRuntimePaths.Length; f++)
                {
                    if (string.Equals(entry.AssetPath, forbiddenRuntimePaths[f], StringComparison.Ordinal))
                    {
                        rejectionReason = $"forbidden-runtime-entry:{entry.Role}:{entry.AssetPath}";
                        return false;
                    }
                }

                if (string.Equals(entry.Role, "entity-scene", StringComparison.Ordinal) ||
                    string.Equals(entry.Role, "minimap-raster", StringComparison.Ordinal) ||
                    string.Equals(entry.Role, "map-surface", StringComparison.Ordinal))
                {
                    continue;
                }

                if (entry.AssetPath.IndexOf(
                        "GeneratedStaticMapPresentation",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rejectionReason = $"static-presentation-path-in-candidate-layout:{entry.AssetPath}";
                    return false;
                }
            }

            if (CountRole(entries, "definition") != 1 ||
                CountRole(entries, "source-scene") != 1 ||
                CountRole(entries, "entity-scene") != 1 ||
                CountRole(entries, "map-surface") != 1 ||
                CountRole(entries, "minimap-raster") != 1)
            {
                rejectionReason = "required-core-roles-incomplete";
                return false;
            }

            if (CountRole(entries, "shared-dependency") != 0)
            {
                rejectionReason = "explicit-shared-dependency-ownership-present";
                return false;
            }

            if (CountRole(entries, "static-manifest") != 0 ||
                CountRole(entries, "presentation") != 0 ||
                CountRole(entries, "building-placements") != 0 ||
                CountRole(entries, "vehicle-placements") != 0)
            {
                rejectionReason = "legacy-static-or-placement-roles-present";
                return false;
            }

            plan = new OperationMapEntitySceneCandidateAddressablesLayoutPlan(
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                CandidatePackLabel,
                CandidateAddressPrefix,
                candidateGuid,
                entries);
            return true;
        }

        internal static bool TryCreateDenseCityPlan(
            out OperationMapEntitySceneCandidateAddressablesLayoutPlan plan,
            out string rejectionReason)
        {
            plan = default;
            rejectionReason = null;

            string denseEntityScenePath = DenseCityCandidateAuthoringTransaction.CandidateEntityScenePath;
            string denseEntitySceneGuid = AssetDatabase.AssetPathToGUID(denseEntityScenePath);
            if (string.IsNullOrEmpty(denseEntitySceneGuid))
            {
                rejectionReason = "dense-city-candidate-entity-scene-missing";
                return false;
            }

            string acceptedSourceSubSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath);
            if (string.Equals(
                    denseEntitySceneGuid,
                    acceptedSourceSubSceneGuid,
                    StringComparison.Ordinal))
            {
                rejectionReason =
                    "dense-city-candidate-entity-scene-guid-collides-with-accepted-subscene";
                return false;
            }

            string acceptedCandidateSubSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath);
            if (string.Equals(
                    denseEntitySceneGuid,
                    acceptedCandidateSubSceneGuid,
                    StringComparison.Ordinal))
            {
                rejectionReason =
                    "dense-city-candidate-entity-scene-guid-collides-with-accepted-candidate-subscene";
                return false;
            }

            string productionDefinitionGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.DefinitionPath);
            string productionRuntimeBindingGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.SourceScenePath);
            string productionAuthoringSceneGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath);
            string mapSurfaceGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MapSurfacePath);
            string minimapGuid = AssetDatabase.AssetPathToGUID(
                OperationMapAddressablesLayoutBuilder.MinimapRasterPath);
            if (string.IsNullOrEmpty(mapSurfaceGuid))
            {
                rejectionReason = "dense-city-map-surface-missing";
                return false;
            }

            if (string.IsNullOrEmpty(minimapGuid))
            {
                rejectionReason = "dense-city-minimap-raster-missing";
                return false;
            }

            string acceptedCandidateDefinitionGuid =
                AssetDatabase.AssetPathToGUID(CandidateDefinitionPath);
            string acceptedCandidateRuntimeBindingGuid =
                AssetDatabase.AssetPathToGUID(CandidateRuntimeBindingPath);
            string denseDefinitionGuid =
                AssetDatabase.AssetPathToGUID(DenseCandidateDefinitionPath);
            string denseRuntimeBindingGuid =
                AssetDatabase.AssetPathToGUID(DenseCandidateRuntimeBindingPath);
            if (string.IsNullOrEmpty(denseDefinitionGuid))
            {
                rejectionReason = "dense-city-candidate-definition-missing";
                return false;
            }

            if (string.IsNullOrEmpty(denseRuntimeBindingGuid))
            {
                rejectionReason = "dense-city-candidate-runtime-binding-missing";
                return false;
            }

            if (GuidCollides(
                    denseEntitySceneGuid,
                    acceptedSourceSubSceneGuid,
                    acceptedCandidateSubSceneGuid,
                    productionDefinitionGuid,
                    productionRuntimeBindingGuid,
                    productionAuthoringSceneGuid,
                    mapSurfaceGuid,
                    minimapGuid,
                    acceptedCandidateDefinitionGuid,
                    acceptedCandidateRuntimeBindingGuid))
            {
                rejectionReason =
                    "dense-city-candidate-entity-scene-guid-collides-with-protected-candidate-asset";
                return false;
            }

            if (GuidCollides(
                    denseDefinitionGuid,
                    acceptedSourceSubSceneGuid,
                    acceptedCandidateSubSceneGuid,
                    denseEntitySceneGuid,
                    productionDefinitionGuid,
                    productionRuntimeBindingGuid,
                    productionAuthoringSceneGuid,
                    mapSurfaceGuid,
                    minimapGuid,
                    acceptedCandidateDefinitionGuid,
                    acceptedCandidateRuntimeBindingGuid))
            {
                rejectionReason = "dense-city-candidate-definition-guid-collision";
                return false;
            }

            if (GuidCollides(
                    denseRuntimeBindingGuid,
                    acceptedSourceSubSceneGuid,
                    acceptedCandidateSubSceneGuid,
                    denseEntitySceneGuid,
                    denseDefinitionGuid,
                    productionDefinitionGuid,
                    productionRuntimeBindingGuid,
                    productionAuthoringSceneGuid,
                    mapSurfaceGuid,
                    minimapGuid,
                    acceptedCandidateDefinitionGuid,
                    acceptedCandidateRuntimeBindingGuid))
            {
                rejectionReason = "dense-city-candidate-runtime-binding-guid-collision";
                return false;
            }

            var entries = new List<OperationMapEntitySceneCandidateAddressablesLayoutEntry>(5)
            {
                new(
                    "definition",
                    DenseCandidateDefinitionPath,
                    DenseCandidateAddressPrefix + "definition",
                    OperationMapAddressablesLayoutBuilder.DefinitionRoleLabel),
                new(
                    "source-scene",
                    DenseCandidateRuntimeBindingPath,
                    DenseCandidateAddressPrefix + "source-scene",
                    OperationMapAddressablesLayoutBuilder.SourceSceneRoleLabel),
                new(
                    "entity-scene",
                    denseEntityScenePath,
                    DenseCandidateAddressPrefix + "entity-scene",
                    EntitySceneRoleLabel),
                new(
                    "map-surface",
                    OperationMapAddressablesLayoutBuilder.MapSurfacePath,
                    DenseCandidateAddressPrefix + "map-surface",
                    OperationMapAddressablesLayoutBuilder.MetadataRoleLabel),
                new(
                    "minimap-raster",
                    OperationMapAddressablesLayoutBuilder.MinimapRasterPath,
                    DenseCandidateAddressPrefix + "minimap-raster",
                    OperationMapAddressablesLayoutBuilder.MinimapRasterRoleLabel)
            };

            string[] forbiddenRuntimePaths =
            {
                OperationMapAddressablesLayoutBuilder.ManifestPath,
                OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath,
                OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath,
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OperationMapAddressablesLayoutBuilder.DefinitionPath,
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                OperationMapEntityPresentationMigrationEditor.CandidateSubScenePath,
                CandidateDefinitionPath,
                CandidateRuntimeBindingPath
            };

            for (int i = 0; i < entries.Count; i++)
            {
                OperationMapEntitySceneCandidateAddressablesLayoutEntry entry = entries[i];
                for (int f = 0; f < forbiddenRuntimePaths.Length; f++)
                {
                    if (string.Equals(entry.AssetPath, forbiddenRuntimePaths[f], StringComparison.Ordinal))
                    {
                        rejectionReason =
                            $"forbidden-dense-city-runtime-entry:{entry.Role}:{entry.AssetPath}";
                        return false;
                    }
                }
            }

            if (CountRole(entries, "definition") != 1 ||
                CountRole(entries, "source-scene") != 1 ||
                CountRole(entries, "entity-scene") != 1 ||
                CountRole(entries, "map-surface") != 1 ||
                CountRole(entries, "minimap-raster") != 1)
            {
                rejectionReason = "dense-city-required-core-roles-incomplete";
                return false;
            }

            if (CountRole(entries, "shared-dependency") != 0)
            {
                rejectionReason = "dense-city-explicit-shared-dependency-ownership-present";
                return false;
            }

            if (CountRole(entries, "static-manifest") != 0 ||
                CountRole(entries, "presentation") != 0 ||
                CountRole(entries, "building-placements") != 0 ||
                CountRole(entries, "vehicle-placements") != 0)
            {
                rejectionReason = "dense-city-legacy-static-or-placement-roles-present";
                return false;
            }

            plan = new OperationMapEntitySceneCandidateAddressablesLayoutPlan(
                OperationMapEntityPresentationCandidateSceneBuilder.OperationMapId,
                DenseCandidatePackLabel,
                DenseCandidateAddressPrefix,
                denseEntitySceneGuid,
                entries);
            return true;
        }

        private static bool GuidCollides(string candidateGuid, params string[] protectedGuids)
        {
            if (string.IsNullOrEmpty(candidateGuid))
                return false;

            for (int i = 0; i < protectedGuids.Length; i++)
            {
                if (!string.IsNullOrEmpty(protectedGuids[i]) &&
                    string.Equals(candidateGuid, protectedGuids[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountRole(
            IReadOnlyList<OperationMapEntitySceneCandidateAddressablesLayoutEntry> entries,
            string role)
        {
            int count = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Role, role, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }
    }

    internal readonly struct OperationMapEntitySceneCandidateAddressablesLayoutEntry
    {
        internal OperationMapEntitySceneCandidateAddressablesLayoutEntry(
            string role,
            string assetPath,
            string address,
            string roleLabel)
        {
            Role = role;
            AssetPath = assetPath;
            Address = address;
            RoleLabel = roleLabel;
        }

        internal string Role { get; }
        internal string AssetPath { get; }
        internal string Address { get; }
        internal string RoleLabel { get; }
    }

    internal readonly struct OperationMapEntitySceneCandidateAddressablesLayoutPlan
    {
        internal OperationMapEntitySceneCandidateAddressablesLayoutPlan(
            string operationMapId,
            string packLabel,
            string addressPrefix,
            string entitySceneGuid,
            IReadOnlyList<OperationMapEntitySceneCandidateAddressablesLayoutEntry> entries)
        {
            OperationMapId = operationMapId;
            PackLabel = packLabel;
            AddressPrefix = addressPrefix;
            EntitySceneGuid = entitySceneGuid;
            Entries = entries;
        }

        internal string OperationMapId { get; }
        internal string PackLabel { get; }
        internal string AddressPrefix { get; }
        internal string EntitySceneGuid { get; }
        internal IReadOnlyList<OperationMapEntitySceneCandidateAddressablesLayoutEntry> Entries { get; }

        internal int SharedDependencyCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Entries.Count; i++)
                {
                    if (string.Equals(Entries[i].Role, "shared-dependency", StringComparison.Ordinal))
                        count++;
                }

                return count;
            }
        }
    }
}

#endif
