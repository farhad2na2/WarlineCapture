using System;
using System.Collections.Generic;
using System.Linq;
using Game.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal static class DenseCitySemanticHierarchyBuilder
    {
        internal const string MapBakeRootName = "Generated_GiantDenseMiddleEasternCity_MapBakeSource";
        internal const string EntityPresentationRootName =
            "Generated_GiantDenseMiddleEasternCity_EntityPresentation";

        private static readonly (string Name, MapBakeGroupRole Role)[] ProxyGroups =
        {
            ("Terrain", MapBakeGroupRole.Terrain),
            ("Roads", MapBakeGroupRole.Road),
            ("Bridges", MapBakeGroupRole.Bridge),
            ("Ramps", MapBakeGroupRole.Ramp),
            ("Blockers", MapBakeGroupRole.Blocker)
        };

        internal static (
            DenseCityGeneratedRootAuthoring MapBakeSource,
            DenseCityGeneratedRootAuthoring EntityPresentationSource) Create(
            Scene operationMapScene,
            Scene entityPresentationScene,
            string generationId,
            string generatorSchema,
            int generatorSchemaVersion,
            int deterministicSeed,
            string deterministicGenerationHash)
        {
            RequireWritableScene(operationMapScene, nameof(operationMapScene));
            RequireWritableScene(entityPresentationScene, nameof(entityPresentationScene));
            if (operationMapScene == entityPresentationScene)
                throw new InvalidOperationException("Dense-city ownership roots must span two distinct scenes.");
            if (FindGeneratedRoots(operationMapScene).Count != 0 ||
                FindGeneratedRoots(entityPresentationScene).Count != 0)
            {
                throw new InvalidOperationException(
                    "A marked dense-city generation root already exists; replacement must be transactional.");
            }

            DenseCityGeneratedRootAuthoring mapRoot = CreateMarkedRoot(
                operationMapScene,
                MapBakeRootName,
                DenseCityGeneratedRootRole.MapBakeSource,
                generationId,
                generatorSchema,
                generatorSchemaVersion,
                deterministicSeed,
                deterministicGenerationHash);
            Transform bakeSources = CreateIdentityChild(mapRoot.transform, "BakeSources");
            foreach ((string name, MapBakeGroupRole role) in ProxyGroups)
            {
                Transform group = CreateIdentityChild(bakeSources, name);
                ConfigureMapBakeGroup(group.gameObject.AddComponent<MapBakeGroupAuthoring>(), role);
            }

            DenseCityGeneratedRootAuthoring entityRoot = CreateMarkedRoot(
                entityPresentationScene,
                EntityPresentationRootName,
                DenseCityGeneratedRootRole.EntityPresentationSource,
                generationId,
                generatorSchema,
                generatorSchemaVersion,
                deterministicSeed,
                deterministicGenerationHash);
            Transform gameplayBuildings = CreateIdentityChild(entityRoot.transform, "GameplayBuildings");
            CreateIdentityChild(gameplayBuildings, "Buildings");
            CreateIdentityChild(gameplayBuildings, "CivicAndMarket");
            Transform renderOnly = CreateIdentityChild(entityRoot.transform, "RenderOnly");
            CreateIdentityChild(renderOnly, "Infrastructure");
            CreateIdentityChild(renderOnly, "Vegetation");
            CreateIdentityChild(renderOnly, "Props");
            CreateIdentityChild(renderOnly, "Horizon");

            if (!TryValidate(operationMapScene, entityPresentationScene, generationId, out string error))
                throw new InvalidOperationException(error);

            return (mapRoot, entityRoot);
        }

        internal static bool TryValidate(
            Scene operationMapScene,
            Scene entityPresentationScene,
            string expectedGenerationId,
            out string error)
        {
            if (!operationMapScene.IsValid() || !operationMapScene.isLoaded ||
                !entityPresentationScene.IsValid() || !entityPresentationScene.isLoaded ||
                operationMapScene == entityPresentationScene)
            {
                error = "Dense-city semantic hierarchy requires two distinct loaded scenes.";
                return false;
            }

            List<DenseCityGeneratedRootAuthoring> mapRoots = FindGeneratedRoots(operationMapScene);
            List<DenseCityGeneratedRootAuthoring> entityRoots = FindGeneratedRoots(entityPresentationScene);
            if (mapRoots.Count != 1 || entityRoots.Count != 1 ||
                mapRoots[0].Role != DenseCityGeneratedRootRole.MapBakeSource ||
                entityRoots[0].Role != DenseCityGeneratedRootRole.EntityPresentationSource)
            {
                error = "Each scene must own exactly one marked dense-city root with its expected role.";
                return false;
            }

            DenseCityGeneratedRootAuthoring mapRoot = mapRoots[0];
            DenseCityGeneratedRootAuthoring entityRoot = entityRoots[0];
            if (!mapRoot.TryValidate(out error) || !entityRoot.TryValidate(out error))
                return false;
            if (!string.Equals(mapRoot.GenerationId, expectedGenerationId, StringComparison.Ordinal) ||
                !string.Equals(entityRoot.GenerationId, expectedGenerationId, StringComparison.Ordinal) ||
                !string.Equals(mapRoot.GeneratorSchema, entityRoot.GeneratorSchema, StringComparison.Ordinal) ||
                mapRoot.GeneratorSchemaVersion != entityRoot.GeneratorSchemaVersion ||
                mapRoot.DeterministicSeed != entityRoot.DeterministicSeed ||
                !string.Equals(
                    mapRoot.DeterministicGenerationHash,
                    entityRoot.DeterministicGenerationHash,
                    StringComparison.Ordinal))
            {
                error = "Dense-city generated roots do not describe the same deterministic generation set.";
                return false;
            }
            if (!RequireRoot(mapRoot.transform, MapBakeRootName, out error) ||
                !RequireRoot(entityRoot.transform, EntityPresentationRootName, out error))
            {
                return false;
            }

            Transform bakeSources = RequireIdentityPath(mapRoot.transform, "BakeSources", out error);
            if (bakeSources == null)
                return false;
            var approvedGroups = new HashSet<MapBakeGroupAuthoring>();
            foreach ((string name, MapBakeGroupRole role) in ProxyGroups)
            {
                Transform groupTransform = RequireIdentityPath(bakeSources, name, out error);
                if (groupTransform == null)
                    return false;
                MapBakeGroupAuthoring group = groupTransform.GetComponent<MapBakeGroupAuthoring>();
                if (group == null || group.Role != role ||
                    groupTransform.GetComponents<MapBakeGroupAuthoring>().Length != 1 ||
                    groupTransform.GetComponentInParent<MapBakeGroupAuthoring>(true) != group)
                {
                    error = $"Proxy group '{name}' does not have exactly one nearest {role} owner.";
                    return false;
                }
                approvedGroups.Add(group);
                for (int childIndex = 0; childIndex < groupTransform.childCount; childIndex++)
                {
                    Transform partition = groupTransform.GetChild(childIndex);
                    MapBakeGroupAuthoring partitionGroup = partition.GetComponent<MapBakeGroupAuthoring>();
                    if (partitionGroup == null)
                        continue;
                    if (partition.GetComponents<MapBakeGroupAuthoring>().Length != 1 ||
                        partitionGroup.Role != role)
                    {
                        error = $"Proxy partition '{partition.name}' must have one matching {role} owner.";
                        return false;
                    }
                    approvedGroups.Add(partitionGroup);
                }
            }
            MapBakeGroupAuthoring[] groups =
                mapRoot.GetComponentsInChildren<MapBakeGroupAuthoring>(true);
            if (groups.Length != approvedGroups.Count || groups.Any(group => !approvedGroups.Contains(group)))
            {
                error = $"Map-bake root must own exactly {ProxyGroups.Length} proxy role groups " +
                        "plus direct matching proxy partitions.";
                return false;
            }

            string[] entityPaths =
            {
                "GameplayBuildings",
                "GameplayBuildings/Buildings",
                "GameplayBuildings/CivicAndMarket",
                "RenderOnly",
                "RenderOnly/Infrastructure",
                "RenderOnly/Vegetation",
                "RenderOnly/Props",
                "RenderOnly/Horizon"
            };
            foreach (string path in entityPaths)
            {
                if (RequireIdentityPath(entityRoot.transform, path, out error) == null)
                    return false;
            }
            if (entityRoot.GetComponentsInChildren<MapBakeGroupAuthoring>(true).Length != 0)
            {
                error = "Entity-presentation hierarchy cannot contain map-bake proxy role owners.";
                return false;
            }

            error = null;
            return true;
        }

        private static DenseCityGeneratedRootAuthoring CreateMarkedRoot(
            Scene scene,
            string name,
            DenseCityGeneratedRootRole role,
            string generationId,
            string generatorSchema,
            int generatorSchemaVersion,
            int deterministicSeed,
            string deterministicGenerationHash)
        {
            var owner = new GameObject(name);
            SceneManager.MoveGameObjectToScene(owner, scene);
            DenseCityGeneratedRootAuthoring marker = owner.AddComponent<DenseCityGeneratedRootAuthoring>();
            var serialized = new SerializedObject(marker);
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.FindProperty("generationId").stringValue = generationId;
            serialized.FindProperty("generatorSchema").stringValue = generatorSchema;
            serialized.FindProperty("generatorSchemaVersion").intValue = generatorSchemaVersion;
            serialized.FindProperty("deterministicSeed").intValue = deterministicSeed;
            serialized.FindProperty("deterministicGenerationHash").stringValue = deterministicGenerationHash;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return marker;
        }

        private static Transform CreateIdentityChild(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void ConfigureMapBakeGroup(
            MapBakeGroupAuthoring group,
            MapBakeGroupRole role)
        {
            var serialized = new SerializedObject(group);
            serialized.FindProperty("role").enumValueIndex = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<DenseCityGeneratedRootAuthoring> FindGeneratedRoots(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<DenseCityGeneratedRootAuthoring>(true))
                .ToList();

        private static bool RequireRoot(Transform root, string expectedName, out string error)
        {
            if (root.parent != null || !string.Equals(root.name, expectedName, StringComparison.Ordinal) ||
                !HasIdentityTransform(root))
            {
                error = $"Generated root '{expectedName}' must be a scene root with identity transform.";
                return false;
            }

            error = null;
            return true;
        }

        private static Transform RequireIdentityPath(Transform root, string path, out string error)
        {
            Transform current = root;
            string[] segments = path.Split('/');
            for (int segmentIndex = 0; segmentIndex < segments.Length; segmentIndex++)
            {
                string segment = segments[segmentIndex];
                Transform match = null;
                int matchCount = 0;
                for (int childIndex = 0; childIndex < current.childCount; childIndex++)
                {
                    Transform candidate = current.GetChild(childIndex);
                    if (!string.Equals(candidate.name, segment, StringComparison.Ordinal))
                        continue;
                    match = candidate;
                    matchCount++;
                }

                if (matchCount != 1 || !HasIdentityTransform(match))
                {
                    error = $"Dense-city semantic path '{path}' must contain exactly one identity-transformed '{segment}' segment.";
                    return null;
                }

                current = match;
            }

            if (current == null)
            {
                error = $"Dense-city semantic path '{path}' is missing or not identity transformed.";
                return null;
            }

            error = null;
            return current;
        }

        private static bool HasIdentityTransform(Transform transform) =>
            transform.localPosition == Vector3.zero &&
            transform.localRotation == Quaternion.identity &&
            transform.localScale == Vector3.one;

        private static void RequireWritableScene(Scene scene, string argumentName)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                throw new ArgumentException("Scene must be valid and loaded.", argumentName);
        }
    }
}
