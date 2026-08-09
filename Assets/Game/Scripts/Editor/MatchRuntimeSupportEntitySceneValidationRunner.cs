using System;
using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class MatchRuntimeSupportEntitySceneValidationRunner
    {
        private const string ScenePath = "Assets/Game/Scenes/Match/MatchRuntimeSubScene.unity";

        public static void Run()
        {
            try
            {
                ValidatePackedRegistry();
                Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[MatchRuntimeSupportEntitySceneValidation] result=Failed");
                Exit(1);
            }
        }

        private static void ValidatePackedRegistry()
        {
            string guidText = AssetDatabase.AssetPathToGUID(ScenePath);
            Require(!string.IsNullOrWhiteSpace(guidText), $"Match runtime support scene is missing: {ScenePath}");

            Unity.Entities.Hash128 sceneGuid = new(guidText);
            Require(sceneGuid.IsValid, $"Match runtime support scene GUID is invalid: {guidText}");

            using World world = CreateStreamingWorld();
            Entity sceneEntity = SceneSystem.LoadSceneAsync(
                world.Unmanaged,
                sceneGuid,
                new SceneSystem.LoadParameters
                {
                    Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
                });
            world.Update();

            Require(
                SceneSystem.IsSceneLoaded(world.Unmanaged, sceneEntity),
                "Match runtime support EntityScene did not finish streaming.");

            EntityManager entityManager = world.EntityManager;
            using EntityQuery registryQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitPrefabRegistryTag>(),
                ComponentType.ReadOnly<UnitPrefabRegistryEntry>());
            Require(
                registryQuery.CalculateEntityCount() == 1,
                $"Expected exactly one packed unit-prefab registry; found {registryQuery.CalculateEntityCount()}.");

            Entity registryEntity = registryQuery.GetSingletonEntity();
            DynamicBuffer<UnitPrefabRegistryEntry> entries =
                entityManager.GetBuffer<UnitPrefabRegistryEntry>(registryEntity);
            Require(entries.Length > 0, "Packed unit-prefab registry is empty.");

            for (int index = 0; index < entries.Length; index++)
            {
                Entity prefab = entries[index].Prefab;
                Require(
                    prefab != Entity.Null && entityManager.Exists(prefab),
                    $"Packed unit-prefab registry entry {index} has no valid prefab entity.");
            }

            Debug.Log(
                "[MatchRuntimeSupportEntitySceneValidation] result=Passed " +
                $"registryEntities=1 entries={entries.Length} sceneGuid={guidText}");
            Debug.Log("Application will terminate with return code 0");
        }

        private static World CreateStreamingWorld()
        {
            var systems = DefaultWorldInitialization.GetAllSystems(
                WorldSystemFilterFlags.Default,
                true);
            var world = new World(
                "MatchRuntimeSupportEntitySceneValidation",
                WorldFlags.Game);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systems);
            return world;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void Exit(int code)
        {
            if (Application.isBatchMode)
                EditorApplication.Exit(code);
        }
    }
}
