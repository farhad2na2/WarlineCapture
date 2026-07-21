#if UNITY_EDITOR

namespace Game.Editor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Game.Configs;
    using UnityEditor;
    using UnityEngine;

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
        internal const string EntitySceneRoleLabel = "operation-map-role-entity-scene";

        internal const string CandidateDefinitionPath =
            "Assets/Game/Configs/OperationMaps/Candidates/" +
            "OperationMap_Compatibility_DesertBase01_EntityScene_Candidate.asset";
        internal const string CandidateRuntimeBindingPath =
            "Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/Candidates/" +
            "opmap_skirmish_desert_base_01_entity_scene_runtime.unity";

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

            string[] sharedDependencies = CollectCandidateSharedDependencyPaths(
                candidateSubScenePath,
                entries);
            for (int i = 0; i < sharedDependencies.Length; i++)
            {
                string path = sharedDependencies[i];
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (string.IsNullOrEmpty(guid))
                {
                    rejectionReason = $"shared-dependency-missing:{path}";
                    return false;
                }

                entries.Add(
                    new OperationMapEntitySceneCandidateAddressablesLayoutEntry(
                        "shared-dependency",
                        path,
                        "operation-map-candidate/shared/" + guid,
                        OperationMapAddressablesLayoutBuilder.SharedDependencyRoleLabel));
            }

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

            if (CountRole(entries, "shared-dependency") == 0)
            {
                rejectionReason = "shared-art-dependencies-empty";
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

        internal static string[] CollectCandidateSharedDependencyPaths(
            string entityScenePath,
            IReadOnlyList<OperationMapEntitySceneCandidateAddressablesLayoutEntry> coreEntries = null)
        {
            var excluded = new HashSet<string>(StringComparer.Ordinal)
            {
                entityScenePath,
                OperationMapAddressablesLayoutBuilder.ManifestPath,
                OperationMapAddressablesLayoutBuilder.BuildingPlacementsPath,
                OperationMapAddressablesLayoutBuilder.VehiclePlacementsPath,
                OperationMapAddressablesLayoutBuilder.SourceScenePath,
                OperationMapAddressablesLayoutBuilder.DefinitionPath,
                OperationMapAddressablesLayoutBuilder.AuthoringScenePath,
                OperationMapEntityPresentationMigrationEditor.AcceptedSubScenePath,
                CandidateDefinitionPath,
                CandidateRuntimeBindingPath
            };
            if (coreEntries != null)
            {
                for (int i = 0; i < coreEntries.Count; i++)
                    excluded.Add(coreEntries[i].AssetPath);
            }

            var usage = new Dictionary<string, int>(StringComparer.Ordinal);
            string[] dependencies = AssetDatabase.GetDependencies(entityScenePath, true);
            for (int i = 0; i < dependencies.Length; i++)
            {
                string path = dependencies[i];
                if (excluded.Contains(path))
                    continue;
                if (!OperationMapAddressablesLayoutBuilder.IsShareableDependencyPath(path))
                    continue;
                // Chunk scenes and other static presentation scenes are never candidate shared art.
                if (path.IndexOf(
                        "/GeneratedStaticMapPresentation/",
                        StringComparison.OrdinalIgnoreCase) >= 0 &&
                    path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                usage.TryGetValue(path, out int count);
                usage[path] = count + 1;
            }

            return usage.Keys
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
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
